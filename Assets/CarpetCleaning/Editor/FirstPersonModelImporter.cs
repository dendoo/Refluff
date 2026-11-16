using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>
    /// Receives inspected, simplified Tripo exports. High-resolution originals stay outside Assets.
    /// Explicit source rotation is supplied after inspecting the real export; no hidden axis guessing.
    /// </summary>
    public static partial class FirstPersonModelImporter
    {
        public const string SetPath = "Assets/CarpetCleaning/Settings/FirstPersonToolSet.asset";
        public const string PrefabFolder = "Assets/CarpetCleaning/Prefabs/FirstPerson";

        public static FirstPersonToolSet EnsureSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<FirstPersonToolSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<FirstPersonToolSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            // A serialized material reference also keeps this shader in player builds.
            if (set.sprayMaterial == null)
            {
                const string path = "Assets/CarpetCleaning/Materials/FirstPersonSpray.mat";
                set.sprayMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                var shader = Shader.Find("CarpetCleaning/FirstPersonSpray");
                if (set.sprayMaterial == null && shader != null)
                {
                    set.sprayMaterial = new Material(shader);
                    AssetDatabase.CreateAsset(set.sprayMaterial, path);
                }
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
            }
            return set;
        }

        /// <param name="sourceEuler">Rotation that makes the model's nozzle face +Z and grip point down.</param>
        /// <param name="longestSizeM">Physical longest dimension after simplification, before camera art tuning.</param>
        public static string Import(ToolId id, string sourcePath, Vector3 sourceEuler, float longestSizeM = 0.45f)
        {
            if (!sourcePath.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(sourcePath))
                throw new ArgumentException("Expected an existing simplified model inside Assets.", nameof(sourcePath));
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new InvalidOperationException("No model asset at " + sourcePath);
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/CarpetCleaning/Prefabs", "FirstPerson");
            var root = new GameObject("FP_" + id);
            try
            {
                var model = UnityEngine.Object.Instantiate(source, root.transform);
                model.name = "Generated";
                model.transform.localRotation = Quaternion.Euler(sourceEuler);
                Bounds bounds = VertexBounds(root.transform);
                float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (extent < 1e-5f) throw new InvalidOperationException("Imported model has no usable mesh bounds.");
                float scale = Mathf.Clamp(longestSizeM, 0.05f, 2f) / extent;
                model.transform.localScale *= scale;
                model.transform.localPosition -= bounds.center * scale;
                Bounds normalized = VertexBounds(root.transform);
                foreach (var c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                int triangles = 0;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) triangles += mf.sharedMesh.triangles.Length / 3;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                    r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
                string path = PrefabFolder + "/FP_" + id + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                var set = EnsureSet();
                var entries = new List<FirstPersonToolSet.Entry>(set.entries);
                int index = entries.FindIndex(e => e.id == id);
                var entry = index >= 0 ? entries[index] : new FirstPersonToolSet.Entry
                {
                    id = id, cameraPosition = new Vector3(0.23f, -0.28f, 0.47f), scale = 1f
                };
                entry.prefab = prefab;
                entry.nozzlePosition = new Vector3(normalized.center.x, normalized.center.y, normalized.max.z);
                if (index >= 0) entries[index] = entry; else entries.Add(entry);
                set.entries = entries.ToArray();
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
                return string.Format(CultureInfo.InvariantCulture,
                    "{0}: {1} triangles; size {2:F3} x {3:F3} x {4:F3} m; {5}",
                    id, triangles, normalized.size.x, normalized.size.y, normalized.size.z, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static string Report()
        {
            var set = AssetDatabase.LoadAssetAtPath<FirstPersonToolSet>(SetPath);
            var lines = new List<string>();
            foreach (ToolId id in new[] { ToolId.DryVacuum, ToolId.FoamSprayer, ToolId.Dryer })
            {
                if (set == null || !set.TryGet(id, out var e)) { lines.Add(id + ": MISSING"); continue; }
                int triangles = 0;
                foreach (var mf in e.prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) triangles += mf.sharedMesh.triangles.Length / 3;
                lines.Add(id + ": " + triangles.ToString(CultureInfo.InvariantCulture) + " triangles, " + AssetDatabase.GetAssetPath(e.prefab));
            }
            return string.Join("\n", lines);
        }

        static Bounds VertexBounds(Transform root)
        {
            Bounds result = default; bool started = false;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 matrix = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (Vector3 vertex in mf.sharedMesh.vertices)
                {
                    Vector3 p = matrix.MultiplyPoint3x4(vertex);
                    if (!started) { result = new Bounds(p, Vector3.zero); started = true; } else result.Encapsulate(p);
                }
            }
            if (!started) throw new InvalidOperationException("No readable mesh vertices in this model.");
            return result;
        }
    }
}
