using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>Atmosphere assets and scene wiring. Invoked by the idempotent workshop builder.</summary>
    public static class WorkshopAtmosphereBuilder
    {
        const string Directory = "Assets/CarpetCleaning/Materials/Workshop";

        public static void Configure(Transform room)
        {
            Texture3D noise = DensityTexture();
            Material dustMaterial = DustMaterial();
            var shafts = new List<Renderer>();
            var motes = new List<ParticleSystem>();
            var foliage = new List<Transform>();
            var restRotations = new List<Quaternion>();
            foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
            {
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader == null
                    || material.shader.name != "CarpetCleaning/WindowLightVolume") continue;
                material.SetTexture("_DensityNoise", noise);
                material.SetColor("_Tint", new Color(1f, .86f, .62f, 1f));
                material.SetVector("_NoiseParams", new Vector4(.16f, .65f, .035f, 2.55f));
                material.SetFloat("_Anisotropy", .32f);
                material.SetFloat("_ShadowWeight", 1f);
                material.SetVector("_AtmosphereClock", Vector4.zero);
                var parameters = material.GetVector("_BeamParams");
                parameters.x = .042f;
                parameters.y = .065f;
                parameters.z = .18f;
                material.SetVector("_BeamParams", parameters);
                EditorUtility.SetDirty(material);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                shafts.Add(renderer);
                if (renderer.name.EndsWith("Beam1"))
                    Foliage(room, renderer, foliage, restRotations);

                var existing = room.Find(renderer.name + "Dust");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var go = new GameObject(renderer.name + "Dust");
                go.transform.SetParent(room, false);
                go.layer = 2;
                var system = go.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.playOnAwake = false;
                main.loop = false;
                main.maxParticles = 32;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.simulationSpeed = 0f;
                main.startSpeed = 0f;
                main.startLifetime = 100f;
                var emission = system.emission; emission.enabled = false;
                var shape = system.shape; shape.enabled = false;
                var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
                particleRenderer.sharedMaterial = dustMaterial;
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
                particleRenderer.receiveShadows = false;
                particleRenderer.lightProbeUsage = LightProbeUsage.Off;
                particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                particleRenderer.minParticleSize = 0f;
                particleRenderer.maxParticleSize = .018f;
                motes.Add(system);
            }
            var atmosphere = room.GetComponent<WorkshopAtmosphere>();
            if (atmosphere == null) atmosphere = room.gameObject.AddComponent<WorkshopAtmosphere>();
            atmosphere.shafts = shafts.ToArray();
            atmosphere.motes = motes.ToArray();
            atmosphere.foliage = foliage.ToArray();
            atmosphere.foliageRestRotations = restRotations.ToArray();
            EditorUtility.SetDirty(atmosphere);
        }

        static void Foliage(Transform room, Renderer shaft, List<Transform> foliage, List<Quaternion> rotations)
        {
            const string materialPath = Directory + "/AtmosphereFoliage.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(.09f, .15f, .07f));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Vector3 origin = shaft.sharedMaterial.GetVector("_WindowOrigin");
            Vector3 direction = shaft.sharedMaterial.GetVector("_BeamDirection");
            Vector3 right = shaft.sharedMaterial.GetVector("_WindowRight");
            Vector3 up = shaft.sharedMaterial.GetVector("_WindowUp");
            for (int layer = 0; layer < 2; layer++)
            {
                string name = shaft.name + "ExteriorFoliage" + layer;
                var old = room.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var go = new GameObject(name);
                go.transform.SetParent(room, false);
                go.transform.position = origin - direction * (1.1f + layer * .37f)
                    + up * (.16f + layer * .18f) + right * (layer == 0 ? -.3f : .45f);
                go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(right, up), up)
                    * Quaternion.Euler(0f, 0f, layer == 0 ? -12f : 158f);
                go.AddComponent<MeshFilter>().sharedMesh = FoliageMesh(layer);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                foliage.Add(go.transform);
                rotations.Add(go.transform.localRotation);
            }
        }

        static Mesh FoliageMesh(int variant)
        {
            string path = Directory + "/AtmosphereFoliage" + variant + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            // A sparse branch silhouette: one combined, double-sided mesh per moving layer.
            for (int branch = 0; branch < 4; branch++)
            {
                Vector3 start = new Vector3(-1.7f + branch * .7f, -.13f + branch * .04f, 0f);
                Vector3 end = start + new Vector3(.8f, .4f + .11f * (branch % 2), .02f);
                Vector3 tangent = (end - start).normalized;
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);
                Quad(vertices, triangles, start - side * .012f, end - side * .004f,
                    end + side * .004f, start + side * .012f);
                for (int leaf = 0; leaf < 7; leaf++)
                {
                    float t = .15f + leaf * .12f;
                    float sign = ((leaf + variant) & 1) == 0 ? 1f : -1f;
                    Vector3 stem = Vector3.Lerp(start, end, t);
                    Vector3 axis = (tangent * .5f + side * sign).normalized;
                    Vector3 leafSide = new Vector3(-axis.y, axis.x, 0f);
                    float length = .23f + .04f * ((branch + leaf + variant) % 3);
                    float width = .055f + .009f * (leaf % 3);
                    int first = vertices.Count;
                    vertices.Add(stem + axis * length * .47f);
                    const int segments = 8;
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = i * Mathf.PI * 2f / segments;
                        float along = (Mathf.Cos(angle) + 1f) * .5f;
                        Vector3 p = stem + axis * (along * length)
                            + leafSide * (Mathf.Sin(angle) * width);
                        p.z += Mathf.Sin(angle) * .018f;
                        vertices.Add(p);
                    }
                    for (int i = 0; i < segments; i++)
                        DoubleTriangle(triangles, first, first + 1 + i, first + 1 + (i + 1) % segments);
                }
            }
            var mesh = new Mesh { name = "AtmosphereFoliage" + variant };
            // Back faces need separate vertices so normal bias remains valid in the shadow pass.
            var flatVertices = new Vector3[triangles.Count];
            var normals = new Vector3[triangles.Count];
            var indices = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                flatVertices[i] = a; flatVertices[i + 1] = b; flatVertices[i + 2] = c;
                for (int j = 0; j < 3; j++) { normals[i + j] = normal; indices[i + j] = i + j; }
            }
            mesh.vertices = flatVertices;
            mesh.normals = normals;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int first = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            DoubleTriangle(triangles, first, first + 1, first + 2);
            DoubleTriangle(triangles, first, first + 2, first + 3);
        }

        static void DoubleTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(c); triangles.Add(b); triangles.Add(a);
        }

        static Material DustMaterial()
        {
            const string texturePath = Directory + "/AtmosphereMote.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                const int size = 32;
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                texture.name = "AtmosphereMote";
                texture.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float u = ((x + .5f) / size - .5f) * 2f;
                    float v = ((y + .5f) / size - .5f) * 2f;
                    float radius = u * u + v * v;
                    float alpha = Mathf.Exp(-radius * 5f) * Mathf.Clamp01((1f - radius) * 3f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            const string path = Directory + "/AtmosphereDust.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(1.7f, 1.5f, 1.15f, 1f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_SoftParticlesEnabled", 1f);
            material.SetVector("_SoftParticleFadeParams", new Vector4(0f, 6.67f, 0f, 0f));
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SOFTPARTICLES_ON");
            material.renderQueue = 3030;
            material.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture3D DensityTexture()
        {
            const string path = Directory + "/AtmosphereDensity.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
            if (existing != null) return existing;
            const int size = 48;
            var values = new byte[size * size * size];
            for (int z = 0; z < size; z++) for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector3 p = new Vector3(x, y, z) / size;
                float n = Noise(p * 4f, 4) * .75f + Noise(p * 8f, 8) * .25f;
                // Stretch softly around the mean: shafts retain organic pockets after ray integration.
                n = Mathf.Clamp01((n - .5f) * 1.55f + .5f);
                values[x + size * (y + size * z)] = (byte)Mathf.RoundToInt(n * 255f);
            }
            var texture = new Texture3D(size, size, size, TextureFormat.R8, false)
            {
                name = "AtmosphereDensity", wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixelData(values, 0);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        static float Noise(Vector3 p, int period)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
            Vector3 f = new Vector3(p.x - x, p.y - y, p.z - z);
            f = new Vector3(Smooth(f.x), Smooth(f.y), Smooth(f.z));
            float value = 0f;
            for (int i = 0; i < 8; i++)
            {
                int dx = i & 1, dy = (i >> 1) & 1, dz = (i >> 2) & 1;
                uint h = unchecked((uint)((x + dx) % period) * 1597334677u
                    ^ (uint)((y + dy) % period) * 3812015801u ^ (uint)((z + dz) % period) * 2798796415u);
                h = unchecked((h ^ (h >> 16)) * 2246822519u);
                h ^= h >> 13;
                float n = (h & 0x00ffffffu) / 16777216f;
                value += n * (dx == 0 ? 1f - f.x : f.x)
                    * (dy == 0 ? 1f - f.y : f.y) * (dz == 0 ? 1f - f.z : f.z);
            }
            return value;
        }

        static float Smooth(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
