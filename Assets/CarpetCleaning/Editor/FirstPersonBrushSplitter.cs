using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>
    /// Splits a regenerated brush even when Tripo has welded its top and skirt into a single connected mesh.
    /// Blue albedo proposes the skirt's upper seam; the reviewed height splits triangles along a horizontal
    /// plane. Interpolated vertex attributes retain the texture and normals, with the complete lower skirt
    /// under Spin and all upper housing static. No source GLB or material is modified.
    /// </summary>
    public static class FirstPersonBrushSplitter
    {
        const string MeshFolder = "Assets/CarpetCleaning/Prefabs/FirstPerson/Meshes";

        struct Vertex
        {
            public Vector3 p, n;
            public Vector4 tangent;
            public Vector2 uv, uv1;
            public Color color;
            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                Vector4 tangent = Vector4.Lerp(a.tangent, b.tangent, t);
                Vector3 tangentAxis = new Vector3(tangent.x, tangent.y, tangent.z).normalized;
                return new Vertex { p = Vector3.Lerp(a.p, b.p, t), n = Vector3.Lerp(a.n, b.n, t).normalized,
                    tangent = new Vector4(tangentAxis.x, tangentAxis.y, tangentAxis.z, a.tangent.w),
                    uv = Vector2.Lerp(a.uv, b.uv, t), uv1 = Vector2.Lerp(a.uv1, b.uv1, t), color = Color.Lerp(a.color, b.color, t) };
            }
        }

        sealed class Source
        {
            public Vertex[] vertices;
            public int[][] triangles;
            public Material[] materials;
        }

        sealed class Part
        {
            public readonly List<Vertex> vertices = new List<Vertex>();
            public readonly List<int>[] triangles;
            public Part(int submeshes)
            {
                triangles = new List<int>[submeshes];
                for (int i = 0; i < submeshes; i++) triangles[i] = new List<int>();
            }
            public void Add(List<Vertex> polygon, int submesh)
            {
                for (int i = 1; i + 1 < polygon.Count; i++)
                {
                    var a = polygon[0]; var b = polygon[i]; var c = polygon[i + 1];
                    if (Vector3.Cross(b.p - a.p, c.p - a.p).sqrMagnitude < 1e-20f) continue;
                    int offset = vertices.Count;
                    vertices.Add(a); vertices.Add(b); vertices.Add(c);
                    triangles[submesh].Add(offset); triangles[submesh].Add(offset + 1); triangles[submesh].Add(offset + 2);
                }
            }
            public int TriangleCount => vertices.Count / 3;
            public Mesh Build(string name, Vector3 origin)
            {
                var positions = new List<Vector3>(vertices.Count); var normals = new List<Vector3>(vertices.Count);
                var tangents = new List<Vector4>(vertices.Count); var uvs = new List<Vector2>(vertices.Count);
                var uv1 = new List<Vector2>(vertices.Count); var colors = new List<Color>(vertices.Count);
                foreach (var vertex in vertices)
                {
                    positions.Add(vertex.p - origin); normals.Add(vertex.n); tangents.Add(vertex.tangent);
                    uvs.Add(vertex.uv); uv1.Add(vertex.uv1); colors.Add(vertex.color);
                }
                var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetTangents(tangents);
                mesh.SetUVs(0, uvs); mesh.SetUVs(1, uv1); mesh.SetColors(colors);
                mesh.subMeshCount = triangles.Length;
                for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i, false);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        public static string Split(Transform root, GameObject model, float reviewedBristleTopM, out float usedBristleTopM)
        {
            var sources = Gather(root, model);
            Bounds bounds = BoundsOf(sources);
            float proposed = reviewedBristleTopM >= 0f ? reviewedBristleTopM : EstimateSeam(sources, bounds);
            if (proposed <= bounds.min.y + 0.001f || proposed >= bounds.max.y - 0.001f)
                throw new InvalidOperationException("Bristle seam must leave both a spinning skirt and a static housing; inspect the normalized brush.");
            usedBristleTopM = proposed;
            var still = new List<Part>(); var spinning = new List<Part>();
            bool anySpin = false; Bounds spinBounds = default;
            int bodyCount = 0, spinCount = 0;
            foreach (var source in sources)
            {
                var body = new Part(source.triangles.Length); var skirt = new Part(source.triangles.Length);
                for (int sub = 0; sub < source.triangles.Length; sub++)
                {
                    var indices = source.triangles[sub];
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        var tri = new List<Vertex>(3) { source.vertices[indices[t]], source.vertices[indices[t + 1]], source.vertices[indices[t + 2]] };
                        if (Mathf.Abs(tri[0].p.y - proposed) < 1e-7f && Mathf.Abs(tri[1].p.y - proposed) < 1e-7f && Mathf.Abs(tri[2].p.y - proposed) < 1e-7f)
                        {
                            // A face lying exactly on the joint belongs to the housing only, avoiding coincident caps.
                            body.Add(tri, sub);
                            continue;
                        }
                        body.Add(Clip(tri, proposed, false), sub);
                        skirt.Add(Clip(tri, proposed, true), sub);
                    }
                }
                foreach (var vertex in skirt.vertices)
                {
                    if (!anySpin) { spinBounds = new Bounds(vertex.p, Vector3.zero); anySpin = true; } else spinBounds.Encapsulate(vertex.p);
                }
                bodyCount += body.TriangleCount; spinCount += skirt.TriangleCount;
                still.Add(body); spinning.Add(skirt);
            }
            if (bodyCount == 0 || spinCount == 0) throw new InvalidOperationException("Brush split produced an empty body or skirt.");
            EnsureFolder(MeshFolder);
            Vector3 spinOrigin = new Vector3(spinBounds.center.x, 0f, spinBounds.center.z);
            var pivot = new GameObject(ToolModelView.SpinNode).transform;
            pivot.SetParent(root, false); pivot.localPosition = spinOrigin;
            var container = new GameObject("BrushSkirt").transform;
            container.SetParent(pivot, false);
            for (int i = 0; i < sources.Count; i++)
            {
                if (still[i].TriangleCount > 0)
                    AddPart(root, "BrushHousing_" + i, still[i], sources[i].materials, Vector3.zero);
                if (spinning[i].TriangleCount > 0)
                    AddPart(container, "BrushSkirt_" + i, spinning[i], sources[i].materials, spinOrigin);
            }
            UnityEngine.Object.DestroyImmediate(model);
            return string.Format(CultureInfo.InvariantCulture,
                "; brush split: static {0} tri, rotating {1} tri, seam {2:F5}m, axis ({3:F5},{4:F5})m — inspect at idle and 30 degrees",
                bodyCount, spinCount, proposed, spinOrigin.x, spinOrigin.z);
        }

        /// <summary>Review data only; callers may use the result as ImportContact's explicit normalized seam height.</summary>
        public static float EstimateSeamForNormalizedModel(Transform root, GameObject model)
        {
            var sources = Gather(root, model);
            return EstimateSeam(sources, BoundsOf(sources));
        }

        static List<Vertex> Clip(List<Vertex> input, float height, bool below)
        {
            var output = new List<Vertex>(4);
            for (int i = 0; i < input.Count; i++)
            {
                var a = input[i]; var b = input[(i + 1) % input.Count];
                bool aInside = below ? a.p.y <= height : a.p.y >= height;
                bool bInside = below ? b.p.y <= height : b.p.y >= height;
                if (aInside) output.Add(a);
                if (aInside != bInside)
                {
                    float t = (height - a.p.y) / (b.p.y - a.p.y);
                    var intersection = Vertex.Lerp(a, b, Mathf.Clamp01(t));
                    intersection.p.y = height;
                    output.Add(intersection);
                }
            }
            return output;
        }

        static List<Source> Gather(Transform root, GameObject model)
        {
            var result = new List<Source>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh; var renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null) continue;
                var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var positions = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents;
                var uv = mesh.uv; var uv1 = mesh.uv2; var colors = mesh.colors;
                var source = new Source { vertices = new Vertex[positions.Length], triangles = new int[mesh.subMeshCount][], materials = renderer.sharedMaterials };
                float sign = matrix.determinant < 0f ? -1f : 1f;
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector4 tangent = i < tangents.Length ? tangents[i] : new Vector4(1, 0, 0, 1);
                    Vector3 axis = matrix.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;
                    source.vertices[i] = new Vertex {
                        p = matrix.MultiplyPoint3x4(positions[i]),
                        n = normalMatrix.MultiplyVector(i < normals.Length ? normals[i] : Vector3.up).normalized,
                        tangent = new Vector4(axis.x, axis.y, axis.z, tangent.w * sign),
                        uv = i < uv.Length ? uv[i] : Vector2.zero, uv1 = i < uv1.Length ? uv1[i] : Vector2.zero,
                        color = i < colors.Length ? colors[i] : Color.white
                    };
                }
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    if (mesh.GetTopology(sub) != MeshTopology.Triangles) throw new InvalidOperationException("Brush contains non-triangle primitives.");
                    var triangles = mesh.GetTriangles(sub);
                    if (sign < 0f)
                        for (int i = 0; i < triangles.Length; i += 3) { int swap = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = swap; }
                    source.triangles[sub] = triangles;
                }
                result.Add(source);
            }
            if (result.Count == 0) throw new InvalidOperationException("Brush has no readable static meshes.");
            return result;
        }

        static Bounds BoundsOf(List<Source> sources)
        {
            bool first = true; Bounds bounds = default;
            foreach (var source in sources) foreach (var vertex in source.vertices)
            {
                if (first) { bounds = new Bounds(vertex.p, Vector3.zero); first = false; } else bounds.Encapsulate(vertex.p);
            }
            return bounds;
        }

        static float EstimateSeam(List<Source> sources, Bounds bounds)
        {
            var copies = new Dictionary<Texture, Texture2D>();
            var heights = new List<float>();
            try
            {
                foreach (var source in sources)
                    for (int sub = 0; sub < source.triangles.Length; sub++)
                    {
                        Material material = sub < source.materials.Length ? source.materials[sub] : null;
                        Texture texture = material != null && material.HasProperty("baseColorTexture") ? material.GetTexture("baseColorTexture")
                            : material != null ? material.mainTexture : null;
                        Color tint = material != null && material.HasProperty("baseColorFactor") ? material.GetColor("baseColorFactor") : Color.white;
                        Texture2D copy = null;
                        if (texture != null && !copies.TryGetValue(texture, out copy)) copies[texture] = copy = ReadTexture(texture);
                        var indices = source.triangles[sub];
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            var a = source.vertices[indices[i]]; var b = source.vertices[indices[i + 1]]; var c = source.vertices[indices[i + 2]];
                            Vector2 uv = (a.uv + b.uv + c.uv) / 3f;
                            Color color = (copy != null ? copy.GetPixelBilinear(uv.x, uv.y) : Color.white) * tint;
                            bool blue = color.b > 0.10f && color.b > color.r * 1.5f && color.b >= color.g * 0.95f && color.b - color.r > 0.07f;
                            float high = Mathf.Max(a.p.y, Mathf.Max(b.p.y, c.p.y));
                            if (blue && high < bounds.min.y + bounds.size.y * 0.80f) heights.Add(high);
                        }
                    }
                if (heights.Count < 24) throw new InvalidOperationException("Could not identify a blue lower skirt. Inspect and pass an explicit bristle seam height.");
                heights.Sort();
                // Ignore isolated blue specular pixels above the seam; dense blue skirt triangles determine the upper envelope.
                return heights[Mathf.Clamp(Mathf.FloorToInt(heights.Count * 0.985f), 0, heights.Count - 1)];
            }
            finally { foreach (var copy in copies.Values) if (copy != null) UnityEngine.Object.DestroyImmediate(copy); }
        }

        static Texture2D ReadTexture(Texture source)
        {
            int size = Mathf.Clamp(Mathf.Max(source.width, source.height), 32, 1024);
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target); RenderTexture.active = target;
                var copy = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
                copy.ReadPixels(new Rect(0, 0, size, size), 0, 0); copy.Apply();
                return copy;
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }

        static void AddPart(Transform parent, string name, Part part, Material[] materials, Vector3 origin)
        {
            var mesh = SaveMesh(part.Build(name, origin), MeshFolder + "/" + name + ".asset");
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        static Mesh SaveMesh(Mesh source, string path)
        {
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(source, path); return source; }
            // Preserve the GUID and update Unity's GPU copy through Mesh API, never CopySerialized.
            saved.Clear(); saved.indexFormat = source.indexFormat;
            saved.vertices = source.vertices; saved.normals = source.normals; saved.tangents = source.tangents;
            saved.uv = source.uv; saved.uv2 = source.uv2; saved.colors = source.colors;
            saved.subMeshCount = source.subMeshCount;
            for (int sub = 0; sub < source.subMeshCount; sub++) saved.SetTriangles(source.GetTriangles(sub), sub, false);
            saved.RecalculateBounds(); saved.UploadMeshData(false); EditorUtility.SetDirty(saved);
            UnityEngine.Object.DestroyImmediate(source);
            return saved;
        }

        static void EnsureFolder(string path)
        {
            var segments = path.Split('/'); string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
