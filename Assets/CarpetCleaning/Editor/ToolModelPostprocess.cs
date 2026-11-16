using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Turns the raw Tripo .glb heads in Models/Tools into scene-ready prefabs (Docs/TOOL_MODELS.md).
    /// Tripo returns arbitrary pivot, orientation and scale, so every head is re-based here:
    ///   - a head Tripo built upside down is turned over first,
    ///   - the longest horizontal axis is rotated onto +Z (across the stroke, matching the cursor convention),
    ///   - the pivot is centred on XZ with the bottom of the mesh at y = 0,
    ///   - the model is uniformly scaled so that +Z extent equals the tool footprint from ToolTable,
    ///   - a head with a spinning part is cut into a static body and that part under a "Spin" pivot.
    /// Idempotent: re-running rebuilds the prefabs from the .glb sources.
    /// </summary>
    public static class ToolModelPostprocess
    {
        const string SrcFolder = "Assets/CarpetCleaning/Models/Tools";
        const string PrefabFolder = "Assets/CarpetCleaning/Prefabs/Tools";
        const string MeshFolder = PrefabFolder + "/Meshes";

        /// <summary>
        /// Source files Tripo generated working side up (by .glb basename: orientation belongs to the take, not the tool).
        /// The first brush came back with its bristles in the air; its v2 stands on them.
        /// </summary>
        static readonly HashSet<string> UpsideDown = new HashSet<string> { "Tool_Brush" };

        /// <summary>
        /// Heads whose bristles spin while the tool works, under a still top (Docs/TOOL_MODELS.md 7). The bristles are
        /// the coloured pieces (IsBristle), the body and its hardware neutral grey, so colour alone finds them.
        /// </summary>
        static readonly HashSet<ToolId> SpinSplit = new HashSet<ToolId> { ToolId.Brush };

        /// <summary>
        /// Split heads that keep nothing but their spinning part. None at the moment: the ring-only brush of the first
        /// take was replaced by the v2 model, whose top stands still over the spinning bristles (user, 2026-09-24).
        /// </summary>
        static readonly HashSet<ToolId> SpinOnly = new HashSet<ToolId>();

        /// <summary>Source .glb basename per tool; heads that were regenerated point at their newer take.</summary>
        public static readonly Dictionary<ToolId, string> Source = new Dictionary<ToolId, string>
        {
            { ToolId.Scraper,      "Tool_Scraper_v2" },
            { ToolId.DryVacuum,    "Tool_DryVacuum"  },
            { ToolId.Hose,         "Tool_Hose"       },
            { ToolId.FoamSprayer,  "Tool_FoamSprayer"},
            { ToolId.Brush,        "Tool_Brush_v2"   },
            { ToolId.Squeegee,     "Tool_Squeegee"   },
            { ToolId.WetVacuum,    "Tool_WetVacuum"  },
            // Dryer: no head on purpose (user request 2026-09-21), it keeps the outline cursor like the foam sprayer
            { ToolId.FloorCleaner, "Tool_FloorCleaner"},
            { ToolId.Mop,          "Tool_Mop"        },
        };

        /// <summary>Footprint across the stroke, in metres: blade length, or disc diameter for a round head.</summary>
        public static float TargetSpanM(ToolId id)
        {
            var d = ToolTable.Get(id);
            float mm = d.lengthMm > 0f ? d.lengthMm : d.radiusMm * 2f;
            return mm * 0.001f;
        }

        [MenuItem("CarpetCleaning/Normalize Tool Models")]
        public static void NormalizeAll()
        {
            EnsureFolder(PrefabFolder);
            int done = 0, missing = 0;
            var log = new System.Text.StringBuilder();
            foreach (var kv in Source)
            {
                if (FirstPersonModelImporter.TryReimportContact(kv.Key, out string replacement))
                {
                    log.AppendLine(replacement); done++; continue;
                }
                string src = SrcFolder + "/" + kv.Value + ".glb";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(src);
                if (asset == null) { log.AppendLine("MISSING " + src); missing++; continue; }
                string info = BuildPrefab(kv.Key, asset);
                log.AppendLine(info);
                done++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ToolModelPostprocess] " + done + " prefab(s), " + missing + " missing\n" + log);
        }

        /// <summary>Rebuilds one head and leaves the other prefabs alone.</summary>
        public static string Normalize(ToolId id)
        {
            if (FirstPersonModelImporter.TryReimportContact(id, out string replacement)) return replacement;
            if (!Source.TryGetValue(id, out var name)) return "no source for " + id;
            string src = SrcFolder + "/" + name + ".glb";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(src);
            if (asset == null) return "MISSING " + src;
            EnsureFolder(PrefabFolder);
            string info = BuildPrefab(id, asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[ToolModelPostprocess] " + info);
            return info;
        }

        static string BuildPrefab(ToolId id, GameObject asset)
        {
            // Wrapper carries the normalising transform; the imported model stays untouched underneath.
            var root = new GameObject("Tool_" + id);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            // 1. Turn it the right way up, then lay the longest horizontal axis along +Z.
            var flip = UpsideDown.Contains(asset.name) ? Quaternion.Euler(180f, 0f, 0f) : Quaternion.identity;
            inst.transform.localRotation = flip;
            var b = LocalBounds(root.transform, inst);
            float yawDeg = b.size.x > b.size.z ? 90f : 0f;
            inst.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f) * flip;
            b = LocalBounds(root.transform, inst);

            // 2. Uniform scale so the across-stroke extent matches the tool footprint.
            float span = Mathf.Max(b.size.z, 1e-4f);
            float scale = TargetSpanM(id) / span;
            inst.transform.localScale = Vector3.one * scale;
            b = LocalBounds(root.transform, inst);

            // 3. Pivot: centred on XZ, bottom of the mesh at y = 0.
            inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = LocalBounds(root.transform, inst);

            // 4. Cut the spinning part out (before saving: the prefab has to reference the mesh assets).
            string split = SpinSplit.Contains(id) ? SplitSpinPart(id, root.transform, inst) : "";

            string path = PrefabFolder + "/Tool_" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return "Tool_" + id + " span=" + TargetSpanM(id).ToString("F3") + "m scale=" + scale.ToString("F4")
                   + " size=" + b.size.ToString("F4") + (yawDeg > 0f ? " yaw90" : "") + (flip != Quaternion.identity ? " flipped" : "") + split;
        }

        /// <summary>
        /// Splits the head into a static body, which stays on the imported node(s), and the bristles, which move under a
        /// "Spin" pivot placed on their own centre (ToolModelView turns it). A part-segmented model (the v2 brush) splits
        /// by whole part (SplitByParts). A single welded mesh (the first brush take) splits by connected piece: its
        /// bristle tufts were separate pieces and the only coloured ones, so a piece spins when most of its triangles
        /// sample bristle-coloured (measured: 113 beige pieces, 32 grey ones, none in between). A SpinOnly head drops
        /// the body altogether.
        /// </summary>
        static string SplitSpinPart(ToolId id, Transform root, GameObject inst)
        {
            var filters = inst.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length > 1) return SplitByParts(id, root, inst, filters);

            var mf = inst.GetComponentInChildren<MeshFilter>(true);
            var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
            if (mf == null || mr == null || mf.sharedMesh == null) return " split: no mesh";
            var albedo = ReadAlbedo(mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null, 512);
            if (albedo == null) return " split: no albedo";

            var src = mf.sharedMesh;
            var spin = BristlePieces(src, albedo, out int pieces, out int spinPieces);
            Object.DestroyImmediate(albedo);
            if (spinPieces == 0 || spinPieces == pieces) return " split: nothing to cut (" + spinPieces + "/" + pieces + " bristle pieces)";

            var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            var tris = AllTriangles(src);
            var sv = src.vertices;
            var rv = new Vector3[sv.Length];
            for (int i = 0; i < sv.Length; i++) rv[i] = toRoot.MultiplyPoint3x4(sv[i]);
            // The pivot sits on the spinning pieces' own centre, so they turn in place rather than round the body's.
            var axis = SpinAxis(rv, tris, spin);
            bool keepBody = !SpinOnly.Contains(id);
            string bodyPath = MeshFolder + "/Tool_" + id + "_Body.asset";

            // The tuft tops sit in the plate, a thin dish that dips towards its middle, so turning them slid beige
            // specks across it. Each radius gets a ceiling: the lowest the plate comes over it at any angle, because a
            // tuft passes under all of them. What of a spinning piece reaches the ceiling stays with the body (the
            // specks in the plate's holes), and the rest is trimmed to just under it. Without a body there is nothing
            // to hide under, and the tufts stay whole.
            const float Margin = 0.0005f;
            float[] ceiling = keepBody ? PlateCeiling(rv, tris, spin, axis) : null;
            int kept = 0;
            if (keepBody)
                for (int t = 0; t < spin.Length; t++)
                {
                    if (!spin[t]) continue;
                    var c = (rv[tris[3 * t]] + rv[tris[3 * t + 1]] + rv[tris[3 * t + 2]]) / 3f;
                    if (c.y >= CeilingAt(ceiling, c, axis) - Margin) { spin[t] = false; kept++; }
                }

            EnsureFolder(MeshFolder);
            Mesh body = null;
            if (keepBody) body = SaveMesh(ExtractPart(src, spin, false, "Tool_" + id + "_Body"), bodyPath);
            else if (AssetDatabase.LoadAssetAtPath<Mesh>(bodyPath) != null) AssetDatabase.DeleteAsset(bodyPath);   // a body from an earlier cut
            var cutPart = ExtractPart(src, spin, true, "Tool_" + id + "_Spin");
            int trimmed = keepBody ? ClampUnder(cutPart, toRoot, ceiling, axis, Margin) : 0;
            var part = SaveMesh(cutPart, MeshFolder + "/Tool_" + id + "_Spin.asset");

            // Alone, the part has to sit on y = 0 itself: the normalised bottom was the lowest point of the whole head.
            float seat = 0f;
            if (!keepBody)
            {
                float low = float.MaxValue;
                for (int t = 0; t < spin.Length; t++)
                    if (spin[t]) for (int j = 0; j < 3; j++) low = Mathf.Min(low, rv[tris[3 * t + j]].y);
                seat = low == float.MaxValue ? 0f : low;
            }

            var pivot = new GameObject(ToolModelView.SpinNode).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(axis.x, -seat, axis.y);
            var go = new GameObject("Tool_" + id + "_Spin");
            go.transform.SetParent(pivot, false);
            // same pose as the imported node, so the pieces sit exactly where they were cut from
            // (the wrapper and the pivot are unrotated and unscaled)
            go.transform.localPosition = root.InverseTransformPoint(mf.transform.position) - new Vector3(axis.x, 0f, axis.y);
            go.transform.localRotation = Quaternion.Inverse(root.rotation) * mf.transform.rotation;
            go.transform.localScale = mf.transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = part;
            go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;

            if (keepBody) mf.sharedMesh = body;
            else Object.DestroyImmediate(inst);   // the imported node carried nothing else

            if (!keepBody)
                return " split: spin part only, " + TriCount(part) + " tri in " + spinPieces + " pieces (" + (pieces - spinPieces)
                       + " body pieces dropped), axis (" + (axis.x * 1000f).ToString("F2") + ", " + (axis.y * 1000f).ToString("F2")
                       + ") mm, lowered " + (seat * 1000f).ToString("F2") + " mm onto y = 0";
            var probe = new System.Text.StringBuilder();
            foreach (float rMm in new[] { 30f, 34f, 38f, 42f })
            {
                float y = CeilingAt(ceiling, new Vector3(axis.x + rMm * 0.001f, 0f, axis.y), axis);
                probe.Append(" r" + rMm.ToString("F0") + "=" + (y == float.MaxValue ? "-" : (y * 1000f).ToString("F1")));
            }
            return " split: spin " + TriCount(part) + " tri in " + spinPieces + " pieces, body " + TriCount(body)
                   + " tri in " + (pieces - spinPieces) + " pieces, axis (" + (axis.x * 1000f).ToString("F2") + ", "
                   + (axis.y * 1000f).ToString("F2") + ") mm, plate ceiling mm" + probe + " (" + kept
                   + " tri left static, " + trimmed + " vertices trimmed)";
        }

        /// <summary>
        /// A part-segmented model (Tripo's parts output; the v2 brush is 71 meshes with a material each: 42 blue bristle
        /// chunks, the grey body, the clutch plate, bolts and latches) splits by whole part. A part spins when its
        /// area-weighted albedo is bristle-coloured. The spinning parts move, poses kept, into one container under the
        /// Spin pivot, so ToolModelView can smear them as one. Nothing is cut or saved: every mesh stays in the .glb.
        /// </summary>
        static string SplitByParts(ToolId id, Transform root, GameObject inst, MeshFilter[] filters)
        {
            var readable = new Dictionary<Texture, Texture2D>();
            var spinning = new List<MeshFilter>();
            int stillParts = 0, spinTris = 0, stillTris = 0;
            foreach (var mf in filters)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh == null || mr == null) continue;
                if (IsBristle(MeanAlbedo(mf.sharedMesh, mr.sharedMaterial, readable))) { spinning.Add(mf); spinTris += TriCount(mf.sharedMesh); }
                else { stillParts++; stillTris += TriCount(mf.sharedMesh); }
            }
            foreach (var t in readable.Values) Object.DestroyImmediate(t);
            if (spinning.Count == 0 || stillParts == 0) return " split: nothing to cut (" + spinning.Count + "/" + filters.Length + " bristle parts)";
            DeleteSingleMeshCut(id);

            // the pivot sits on the bristles' own centre (XZ bounds, wrapper space), so they turn in place
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue, low = float.MaxValue;
            foreach (var mf in spinning)
            {
                var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = toRoot.MultiplyPoint3x4(v);
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z); low = Mathf.Min(low, p.y);
                }
            }
            var axis = new Vector2(0.5f * (x0 + x1), 0.5f * (z0 + z1));
            bool keepBody = !SpinOnly.Contains(id);

            var pivot = new GameObject(ToolModelView.SpinNode).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(axis.x, 0f, axis.y);
            var container = new GameObject("Tool_" + id + "_Spin").transform;
            container.SetParent(pivot, false);
            foreach (var mf in spinning) mf.transform.SetParent(container, true);
            float seat = 0f;
            if (!keepBody)
            {
                // alone, the bristles have to sit on y = 0 themselves: the normalised bottom was the whole head's
                Object.DestroyImmediate(inst);
                seat = low;
                pivot.localPosition = new Vector3(axis.x, -seat, axis.y);
            }

            return " split by part: " + spinning.Count + " bristle parts spin (" + spinTris + " tri), " + stillParts + " parts "
                   + (keepBody ? "stay still" : "dropped") + " (" + stillTris + " tri), axis (" + (axis.x * 1000f).ToString("F2") + ", "
                   + (axis.y * 1000f).ToString("F2") + ") mm" + (keepBody ? "" : ", lowered " + (seat * 1000f).ToString("F2") + " mm onto y = 0");
        }

        /// <summary>Area-weighted mean albedo of one part (texture times base colour factor), read back through ReadAlbedo.</summary>
        static Color MeanAlbedo(Mesh m, Material mat, Dictionary<Texture, Texture2D> readable)
        {
            var tint = mat != null && mat.HasProperty("baseColorFactor") ? mat.GetColor("baseColorFactor") : Color.white;
            var tex = mat != null ? mat.mainTexture : null;
            var uv = m.uv;
            var v = m.vertices;
            if (tex == null || uv.Length != v.Length) return tint;
            if (!readable.TryGetValue(tex, out var copy)) readable[tex] = copy = ReadAlbedo(tex, Mathf.Clamp(tex.width, 16, 256));
            var tris = AllTriangles(m);
            Color sum = Color.clear;
            float area = 0f;
            for (int t = 0; t < tris.Length / 3; t++)
            {
                int a = tris[3 * t], b = tris[3 * t + 1], c = tris[3 * t + 2];
                float w = Vector3.Cross(v[b] - v[a], v[c] - v[a]).magnitude;
                var u = (uv[a] + uv[b] + uv[c]) / 3f;
                sum += copy.GetPixelBilinear(u.x, u.y) * w;
                area += w;
            }
            return area > 0f ? sum / area * tint : tint;
        }

        /// <summary>A part-split head needs no cut meshes; the ones a single-mesh take left behind would only mislead.</summary>
        static void DeleteSingleMeshCut(ToolId id)
        {
            foreach (var suffix in new[] { "_Body", "_Spin" })
            {
                string path = MeshFolder + "/Tool_" + id + suffix + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            }
        }

        const float CeilingBinM = 0.0005f;

        /// <summary>XZ centre (x, z) of the spinning triangles' bounds, in wrapper space.</summary>
        static Vector2 SpinAxis(Vector3[] rv, int[] tris, bool[] spin)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            for (int t = 0; t < spin.Length; t++)
            {
                if (!spin[t]) continue;
                for (int j = 0; j < 3; j++)
                {
                    var p = rv[tris[3 * t + j]];
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
                }
            }
            return new Vector2(0.5f * (x0 + x1), 0.5f * (z0 + z1));
        }

        /// <summary>
        /// Per 0.5 mm of radius round the spin axis: the lowest point of the static surfaces at that radius, at any
        /// angle (MaxValue where there is none). Heights are sampled on a 0.5 mm XZ grid inside every static triangle
        /// plus at its vertices, so a dish sloping across one big triangle is followed, not flattened to its lowest
        /// corner. Only surfaces above 80% of the spinning pieces' top count: that is the plate they hang from, and
        /// it leaves out the hub, which hangs down to the floor in the middle with nothing spinning under it.
        /// </summary>
        static float[] PlateCeiling(Vector3[] rv, int[] tris, bool[] spin, Vector2 axis)
        {
            float top = float.MinValue, rMax = 0f;
            for (int t = 0; t < spin.Length; t++)
            {
                if (!spin[t]) continue;
                for (int j = 0; j < 3; j++)
                {
                    var p = rv[tris[3 * t + j]];
                    top = Mathf.Max(top, p.y);
                    rMax = Mathf.Max(rMax, new Vector2(p.x - axis.x, p.z - axis.y).magnitude);
                }
            }
            float floorY = 0.8f * top;
            int nb = Mathf.CeilToInt(rMax / CeilingBinM) + 2;
            var ceil = new float[nb];
            for (int i = 0; i < nb; i++) ceil[i] = float.MaxValue;

            System.Action<float, float, float> sample = (x, z, y) =>
            {
                if (y < floorY) return;
                int ib = (int)(new Vector2(x - axis.x, z - axis.y).magnitude / CeilingBinM);
                if (ib < nb && y < ceil[ib]) ceil[ib] = y;
            };
            for (int t = 0; t < spin.Length; t++)
            {
                if (spin[t]) continue;
                Vector3 a = rv[tris[3 * t]], b = rv[tris[3 * t + 1]], c = rv[tris[3 * t + 2]];
                if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < floorY) continue;
                sample(a.x, a.z, a.y); sample(b.x, b.z, b.y); sample(c.x, c.z, c.y);
                float area = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
                if (Mathf.Abs(area) < 1e-12f) continue;   // a wall seen edge-on from above
                int ix0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) / CeilingBinM), ix1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) / CeilingBinM);
                int iz0 = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z)) / CeilingBinM), iz1 = Mathf.CeilToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z)) / CeilingBinM);
                for (int ix = ix0; ix <= ix1; ix++)
                    for (int iz = iz0; iz <= iz1; iz++)
                    {
                        float px = (ix + 0.5f) * CeilingBinM, pz = (iz + 0.5f) * CeilingBinM;
                        float wa = ((b.x - px) * (c.z - pz) - (b.z - pz) * (c.x - px)) / area;
                        float wb = ((c.x - px) * (a.z - pz) - (c.z - pz) * (a.x - px)) / area;
                        float wc = 1f - wa - wb;
                        if (wa < -1e-4f || wb < -1e-4f || wc < -1e-4f) continue;
                        sample(px, pz, wa * a.y + wb * b.y + wc * c.y);
                    }
            }
            // a tuft is thicker than one bin, and the grid only samples points: take each bin's neighbours too
            var wide = new float[nb];
            for (int i = 0; i < nb; i++)
                wide[i] = Mathf.Min(ceil[i], Mathf.Min(ceil[Mathf.Max(i - 1, 0)], ceil[Mathf.Min(i + 1, nb - 1)]));
            return wide;
        }

        static float CeilingAt(float[] ceiling, Vector3 p, Vector2 axis)
        {
            int ib = (int)(new Vector2(p.x - axis.x, p.z - axis.y).magnitude / CeilingBinM);
            return ib < ceiling.Length ? ceiling[ib] : float.MaxValue;
        }

        /// <summary>Pulls every vertex above its radius' ceiling minus `margin` (wrapper space) down onto it; returns how many moved.</summary>
        static int ClampUnder(Mesh m, Matrix4x4 toRoot, float[] ceiling, Vector2 axis, float margin)
        {
            var fromRoot = toRoot.inverse;
            var v = m.vertices;
            int moved = 0;
            for (int i = 0; i < v.Length; i++)
            {
                var p = toRoot.MultiplyPoint3x4(v[i]);
                float lim = CeilingAt(ceiling, p, axis);
                if (lim == float.MaxValue || p.y <= lim - margin) continue;
                p.y = lim - margin;
                v[i] = fromRoot.MultiplyPoint3x4(p);
                moved++;
            }
            m.vertices = v;
            m.RecalculateBounds();
            return moved;
        }

        /// <summary>
        /// Bristles are the coloured pieces of a brush; its body, plate and hardware are neutral grey or steel. The first
        /// take's beige tufts and v2's blue ones both pass, every grey part fails (measured chroma 0.23-0.37 against at
        /// most 0.05; this rule and the old beige-only one split the first take identically).
        /// </summary>
        static bool IsBristle(Color c)
        {
            float hi = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), lo = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return hi - lo > 0.12f && hi > 0.25f;
        }

        /// <summary>
        /// Per-triangle flag, over all submeshes in order: true when the triangle belongs to a connected piece most of
        /// whose triangles are bristle-coloured (IsBristle), not grey plastic or a silver rivet.
        /// </summary>
        static bool[] BristlePieces(Mesh m, Texture2D albedo, out int pieces, out int bristlePieces)
        {
            var v = m.vertices;
            var uv = m.uv;
            var tris = AllTriangles(m);
            int nt = tris.Length / 3;

            // Weld by position first: vertices are duplicated along UV seams, and a piece is only whole once they meet.
            var weld = new Dictionary<Vector3Int, int>();
            var wid = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 1e5f), Mathf.RoundToInt(v[i].y * 1e5f), Mathf.RoundToInt(v[i].z * 1e5f));
                if (!weld.TryGetValue(k, out int w)) { w = weld.Count; weld[k] = w; }
                wid[i] = w;
            }
            var parent = new int[weld.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            for (int t = 0; t < nt; t++)
            {
                int a = Find(parent, wid[tris[3 * t]]);
                parent[Find(parent, wid[tris[3 * t + 1]])] = a;
                parent[Find(parent, wid[tris[3 * t + 2]])] = a;
            }

            var total = new Dictionary<int, int>();
            var bristle = new Dictionary<int, int>();
            var root = new int[nt];
            for (int t = 0; t < nt; t++)
            {
                int r = Find(parent, wid[tris[3 * t]]);
                root[t] = r;
                var c = uv.Length == v.Length
                    ? albedo.GetPixelBilinear((uv[tris[3 * t]].x + uv[tris[3 * t + 1]].x + uv[tris[3 * t + 2]].x) / 3f,
                                              (uv[tris[3 * t]].y + uv[tris[3 * t + 1]].y + uv[tris[3 * t + 2]].y) / 3f)
                    : Color.black;
                total.TryGetValue(r, out int n); total[r] = n + 1;
                bristle.TryGetValue(r, out int w); bristle[r] = w + (IsBristle(c) ? 1 : 0);
            }

            pieces = total.Count;
            bristlePieces = 0;
            var spinning = new HashSet<int>();
            foreach (var kv in total)
                if (bristle[kv.Key] * 2 > kv.Value) { spinning.Add(kv.Key); bristlePieces++; }
            var flags = new bool[nt];
            for (int t = 0; t < nt; t++) flags[t] = spinning.Contains(root[t]);
            return flags;
        }

        static int Find(int[] parent, int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }

        static int[] AllTriangles(Mesh m)
        {
            var all = new List<int>();
            for (int s = 0; s < m.subMeshCount; s++) all.AddRange(m.GetTriangles(s));
            return all.ToArray();
        }

        static int TriCount(Mesh m) { return AllTriangles(m).Length / 3; }

        /// <summary>The triangles whose flag equals `keep`, with only the vertices they use; submeshes are kept apart.</summary>
        static Mesh ExtractPart(Mesh src, bool[] flags, bool keep, string name)
        {
            var map = new int[src.vertexCount];
            for (int i = 0; i < map.Length; i++) map[i] = -1;
            var order = new List<int>();
            var subs = new List<int[]>();
            int t0 = 0;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tri = src.GetTriangles(s);
                var outTri = new List<int>();
                for (int t = 0; t < tri.Length / 3; t++)
                {
                    if (flags[t0 + t] != keep) continue;
                    for (int j = 0; j < 3; j++)
                    {
                        int vi = tri[3 * t + j];
                        if (map[vi] < 0) { map[vi] = order.Count; order.Add(vi); }
                        outTri.Add(map[vi]);
                    }
                }
                subs.Add(outTri.ToArray());
                t0 += tri.Length / 3;
            }

            var m = new Mesh { name = name };
            m.indexFormat = order.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(Pick(src.vertices, order));
            if (src.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal)) m.SetNormals(Pick(src.normals, order));
            if (src.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent)) m.SetTangents(Pick(src.tangents, order));
            if (src.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)) m.SetColors(Pick(src.colors, order));
            for (int ch = 0; ch < 4; ch++)
            {
                if (!src.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0 + ch)) continue;
                var uvs = new List<Vector4>();
                src.GetUVs(ch, uvs);
                m.SetUVs(ch, Pick(uvs.ToArray(), order));
            }
            m.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) m.SetTriangles(subs[s], s, false);
            m.RecalculateBounds();
            return m;
        }

        static List<T> Pick<T>(T[] src, List<int> order)
        {
            var list = new List<T>(order.Count);
            for (int i = 0; i < order.Count; i++) list.Add(src[order[i]]);
            return list;
        }

        /// <summary>
        /// Writes the mesh over the existing asset when there is one, so its GUID (and every reference) survives a
        /// rebuild. Through the Mesh API, not EditorUtility.CopySerialized: that updates the asset but leaves the
        /// GPU copy alone, and the Editor went on drawing the first version of the mesh until it was reloaded.
        /// </summary>
        static Mesh SaveMesh(Mesh m, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(m, path); return m; }
            existing.Clear();
            existing.indexFormat = m.indexFormat;
            existing.SetVertices(m.vertices);
            if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal)) existing.SetNormals(m.normals);
            if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent)) existing.SetTangents(m.tangents);
            if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)) existing.SetColors(m.colors);
            for (int ch = 0; ch < 4; ch++)
            {
                if (!m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0 + ch)) continue;
                var uvs = new List<Vector4>();
                m.GetUVs(ch, uvs);
                existing.SetUVs(ch, uvs);
            }
            existing.subMeshCount = m.subMeshCount;
            for (int s = 0; s < m.subMeshCount; s++) existing.SetTriangles(m.GetTriangles(s), s, false);
            existing.RecalculateBounds();
            existing.name = m.name;
            Object.DestroyImmediate(m);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>
        /// A CPU copy of the albedo. glTFast imports textures non-readable, so it goes through a blit; an sRGB target
        /// keeps the values as authored, which is what the bristle/grey thresholds were measured on.
        /// </summary>
        static Texture2D ReadAlbedo(Texture tex, int size)
        {
            if (tex == null) return null;
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(size, size, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        /// <summary>Renderer bounds of `inst` expressed in `space`, so the wrapper's own transform stays out of it.</summary>
        static Bounds LocalBounds(Transform space, GameObject inst)
        {
            var rs = inst.GetComponentsInChildren<Renderer>(true);
            var acc = new Bounds();
            bool any = false;
            for (int i = 0; i < rs.Length; i++)
            {
                var mf = rs[i].GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh;
                var verts = m.vertices;
                var toSpace = space.worldToLocalMatrix * rs[i].transform.localToWorldMatrix;
                for (int v = 0; v < verts.Length; v++)
                {
                    var p = toSpace.MultiplyPoint3x4(verts[v]);
                    if (!any) { acc = new Bounds(p, Vector3.zero); any = true; }
                    else acc.Encapsulate(p);
                }
            }
            return acc;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
