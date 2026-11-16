using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>Procedural unit meshes for the GPU debris (Docs/FLOOR_SYSTEM.md section 3). All meshes have diameter 1.0; the vertex
    /// shaders scale / expand them per particle. Meshes are created at runtime and owned by DebrisSystem.</summary>
    public static class DebrisMeshBuilder
    {
        /// <summary>Crust shard: icosphere (1 subdivision, 80 tris) with radial jitter r *= 0.7 + 0.6 hash, flattened y x 0.45,
        /// unwelded for flat facets. Bounding diameter 1.0 in XZ.</summary>
        public static Mesh BuildChunk()
        {
            Icosphere(1, out var verts, out var tris);
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i].normalized;
                float hsh = Hash(v * 17.31f + new Vector3(3.1f, 7.7f, 1.3f));
                float r = 0.5f * (0.7f + 0.6f * hsh);
                v *= r;
                v.y *= 0.45f;
                verts[i] = v;
            }
            // unweld -> flat shading (recomputed face normals)
            var pos = new Vector3[tris.Count];
            var nrm = new Vector3[tris.Count];
            var idx = new int[tris.Count];
            for (int t = 0; t < tris.Count; t += 3)
            {
                Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                n = n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up;
                pos[t] = a; pos[t + 1] = b; pos[t + 2] = c;
                nrm[t] = n; nrm[t + 1] = n; nrm[t + 2] = n;
                idx[t] = t; idx[t + 1] = t + 1; idx[t + 2] = t + 2;
            }
            var mesh = new Mesh { name = "DebrisChunk", indexFormat = IndexFormat.UInt16 };
            mesh.SetVertices(pos);
            mesh.SetNormals(nrm);
            mesh.SetIndices(idx, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>Unit quad in local XY, corners +-0.5, UV 0..1, facing -Z: the dust billboard and the splash-drop motion streak
        /// (both expanded in their vertex shaders; Docs/DEBRIS_SYSTEM.md).</summary>
        public static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "DebrisQuad", indexFormat = IndexFormat.UInt16 };
            mesh.SetVertices(new List<Vector3> {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            mesh.SetNormals(new List<Vector3> { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward });
            mesh.SetIndices(new[] { 0, 2, 1, 1, 2, 3 }, MeshTopology.Triangles, 0, false);   // one winding; the dust and drop passes are Cull Off (they face the camera by construction)
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            mesh.UploadMeshData(false);
            return mesh;
        }

        // ------------------------------------------------------------------ icosphere (shared vertices)
        static void Icosphere(int subdivisions, out List<Vector3> verts, out List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            verts = new List<Vector3> {
                new Vector3(-1,  t, 0), new Vector3( 1,  t, 0), new Vector3(-1, -t, 0), new Vector3( 1, -t, 0),
                new Vector3( 0, -1,  t), new Vector3( 0,  1,  t), new Vector3( 0, -1, -t), new Vector3( 0,  1, -t),
                new Vector3( t, 0, -1), new Vector3( t, 0,  1), new Vector3(-t, 0, -1), new Vector3(-t, 0,  1) };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            // Unity front faces: Cross(b - a, c - a) points outward (same convention as ShellMeshBuilder's grids)
            tris = new List<int> {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int s = 0; s < subdivisions; s++)
            {
                var cache = new Dictionary<long, int>();
                var next = new List<int>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(verts, cache, a, b), bc = Mid(verts, cache, b, c), ca = Mid(verts, cache, c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }
        }

        static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            idx = verts.Count;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache.Add(key, idx);
            return idx;
        }

        static float Hash(Vector3 p)
        {
            float h = Mathf.Sin(Vector3.Dot(p, new Vector3(12.9898f, 78.233f, 37.719f))) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}
