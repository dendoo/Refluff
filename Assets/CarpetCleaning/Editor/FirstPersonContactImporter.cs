using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    public static partial class FirstPersonModelImporter
    {
        const string ContactPrefabFolder = "Assets/CarpetCleaning/Prefabs/Tools";
        const string ContactRecipePath = "Tools/first_person/contact_imports.json";
        public const string SocketNode = ToolModelView.SocketNode;

        [Serializable]
        public sealed class ContactRecipe
        {
            public int tool;
            public string source;
            public Vector3 sourceEuler;
            public Vector3 socketLocalM;
            public float brushBristleTopM = -1f;
            public float contactBandFraction = 0.15f;
        }

        [Serializable]
        sealed class ContactRecipes
        {
            public int version = 1;
            public List<ContactRecipe> items = new List<ContactRecipe>();
        }

        /// <summary>
        /// Imports an inspected replacement head. The lower working edge, not an off-centre handle bracket,
        /// defines the pivot and width. The explicit source rotation must make +Y up and +Z across the stroke.
        /// BristleTop is in normalized metres; a negative value requests a blue-albedo seam estimate for review.
        /// </summary>
        public static string ImportContact(ToolId id, string sourcePath, Vector3 sourceEuler,
            Vector3? socketLocalM = null, float brushBristleTopM = -1f, float contactBandFraction = 0.15f)
        {
            if (id != ToolId.Scraper && id != ToolId.Brush && id != ToolId.Squeegee && id != ToolId.WetVacuum && id != ToolId.Mop)
                throw new ArgumentException("Use Import for a complete handheld sprayer/body.", nameof(id));
            if (!sourcePath.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(sourcePath))
                throw new ArgumentException("Expected an accepted simplified model inside Assets.", nameof(sourcePath));
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new InvalidOperationException("No model asset at " + sourcePath);
            var root = new GameObject("Tool_" + id);
            try
            {
                var model = UnityEngine.Object.Instantiate(source, root.transform);
                model.name = "Generated";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(sourceEuler);
                model.transform.localScale = Vector3.one;
                var raw = VertexBounds(root.transform);
                var working = HeightBandBounds(root.transform, raw.min.y,
                    raw.min.y + raw.size.y * Mathf.Clamp(contactBandFraction, 0.02f, 0.5f));
                // Authoring uses the factory dimension; runtime AREA upgrades scale this normalized prefab exactly once.
                float width = ToolModelPostprocess.TargetSpanM(id);
                if (working.size.z <= 1e-5f) throw new InvalidOperationException("Working band has no across-stroke width; inspect sourceEuler.");
                float scale = width / working.size.z;
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = new Vector3(-working.center.x, -raw.min.y, -working.center.z) * scale;
                var normalized = VertexBounds(root.transform);
                var top = HeightBandBounds(root.transform, normalized.max.y - normalized.size.y * 0.08f, normalized.max.y + 0.00001f);
                Vector3 socket = socketLocalM ?? top.center;
                var marker = new GameObject(SocketNode).transform;
                marker.SetParent(root.transform, false);
                marker.localPosition = socket;

                string split = "";
                if (id == ToolId.Brush)
                    split = FirstPersonBrushSplitter.Split(root.transform, model, brushBristleTopM, out brushBristleTopM);

                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
                string prefabPath = ContactPrefabFolder + "/Tool_" + id + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                RegisterContact(id, prefab);
                SaveContactRecipe(new ContactRecipe { tool = (int)id, source = sourcePath, sourceEuler = sourceEuler,
                    socketLocalM = socket, brushBristleTopM = brushBristleTopM, contactBandFraction = contactBandFraction });
                AssetDatabase.SaveAssets();
                return string.Format(CultureInfo.InvariantCulture,
                    "{0}: contact span {1:F4}m; full size {2:F4}/{3:F4}/{4:F4}m; HandleSocket {5}; {6}{7}",
                    id, width, normalized.size.x, normalized.size.y, normalized.size.z,
                    socket.ToString("F4", CultureInfo.InvariantCulture), prefabPath, split);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>Normalize Tool Models honours accepted replacements instead of silently restoring the older heads.</summary>
        public static bool TryReimportContact(ToolId id, out string report)
        {
            report = null;
            if (!File.Exists(ContactRecipePath)) return false;
            var recipes = JsonUtility.FromJson<ContactRecipes>(File.ReadAllText(ContactRecipePath));
            if (recipes == null || recipes.items == null) throw new InvalidOperationException("Invalid contact import recipes.");
            var recipe = recipes.items.Find(e => e.tool == (int)id);
            if (recipe == null) return false;
            report = ImportContact(id, recipe.source, recipe.sourceEuler, recipe.socketLocalM,
                recipe.brushBristleTopM, recipe.contactBandFraction);
            return true;
        }

        public static string FitNozzle(ToolId id, float frontSliceM = 0.004f, float scale = 1f, Vector3? cameraPosition = null)
        {
            var set = EnsureSet();
            if (!set.TryGet(id, out var entry)) throw new InvalidOperationException("No held prefab for " + id);
            var bounds = VertexBounds(entry.prefab.transform);
            var points = MeshPoints(entry.prefab.transform);
            bool first = true; Bounds rim = default;
            foreach (var point in points)
            {
                if (point.z < bounds.max.z - Mathf.Max(0.0001f, frontSliceM)) continue;
                if (first) { rim = new Bounds(point, Vector3.zero); first = false; } else rim.Encapsulate(point);
            }
            if (first) throw new InvalidOperationException("No nozzle rim in the requested slice.");
            entry.nozzlePosition = new Vector3(rim.center.x, rim.center.y, bounds.max.z + 0.002f);
            entry.scale = Mathf.Max(0.01f, scale);
            if (cameraPosition.HasValue) entry.cameraPosition = cameraPosition.Value;
            for (int i = 0; i < set.entries.Length; i++) if (set.entries[i].id == id) set.entries[i] = entry;
            EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
            return id + " nozzle " + entry.nozzlePosition.ToString("F4", CultureInfo.InvariantCulture);
        }

        /// <summary>Optional inspected inlet for a complete held hose/spray gun; marker travels with its exact pose.</summary>
        public static string SetHoseSocket(ToolId id, Vector3 localPositionM)
        {
            var set = EnsureSet();
            if (!set.TryGet(id, out var entry)) throw new InvalidOperationException("No held prefab for " + id);
            string path = AssetDatabase.GetAssetPath(entry.prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var marker = root.transform.Find("HoseSocket");
                if (marker == null) { marker = new GameObject("HoseSocket").transform; marker.SetParent(root.transform, false); }
                marker.localPosition = localPositionM;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return id + " hose inlet " + localPositionM.ToString("F4", CultureInfo.InvariantCulture);
        }

        static void RegisterContact(ToolId id, GameObject prefab)
        {
            const string path = "Assets/CarpetCleaning/Settings/ToolModelSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<ToolModelSet>(path);
            if (set == null) throw new InvalidOperationException("Build Scene once before importing contact heads.");
            var entries = new List<ToolModelSet.Entry>(set.entries);
            int index = entries.FindIndex(e => e.id == id);
            var entry = index >= 0 ? entries[index] : new ToolModelSet.Entry { id = id };
            entry.prefab = prefab;
            entry.posOffsetM = Vector3.zero; entry.eulerOffset = Vector3.zero; entry.extraScale = 1f;
            if (id == ToolId.Brush) { entry.spinRpm = 350f; entry.pileSinkM = 0.008f; }
            if (index >= 0) entries[index] = entry; else entries.Add(entry);
            set.entries = entries.ToArray(); EditorUtility.SetDirty(set);
        }

        static void SaveContactRecipe(ContactRecipe recipe)
        {
            var all = File.Exists(ContactRecipePath)
                ? JsonUtility.FromJson<ContactRecipes>(File.ReadAllText(ContactRecipePath)) : new ContactRecipes();
            if (all == null || all.items == null) throw new InvalidOperationException("Invalid contact import recipes; refusing to overwrite.");
            int index = all.items.FindIndex(e => e.tool == recipe.tool);
            if (index >= 0) all.items[index] = recipe; else all.items.Add(recipe);
            Directory.CreateDirectory(Path.GetDirectoryName(ContactRecipePath));
            File.WriteAllText(ContactRecipePath, JsonUtility.ToJson(all, true));
        }

        static List<Vector3> MeshPoints(Transform root)
        {
            var points = new List<Vector3>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices) points.Add(matrix.MultiplyPoint3x4(vertex));
            }
            if (points.Count == 0) throw new InvalidOperationException("No readable mesh vertices.");
            return points;
        }

        static Bounds HeightBandBounds(Transform root, float low, float high)
        {
            bool first = true; Bounds result = default;
            foreach (var point in MeshPoints(root))
            {
                if (point.y < low - 0.000001f || point.y > high + 0.000001f) continue;
                if (first) { result = new Bounds(point, Vector3.zero); first = false; } else result.Encapsulate(point);
            }
            if (first) throw new InvalidOperationException("Requested mesh height band is empty.");
            return result;
        }
    }
}
