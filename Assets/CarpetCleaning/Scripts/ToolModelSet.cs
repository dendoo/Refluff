using System;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Per-tool 3D head prefab plus the art-side fine tuning (Docs/TOOL_MODELS.md). The prefabs are
    /// Tripo-generated heads normalised by CarpetCleaning/Normalize Tool Models: pivot centred on XZ,
    /// bottom at y = 0, long axis along +Z, sized to the tool footprint. Everything that needs nudging
    /// after that lives here so ToolModelView stays pure maths.
    /// </summary>
    [CreateAssetMenu(menuName = "CarpetCleaning/Tool Model Set", fileName = "ToolModelSet")]
    public sealed class ToolModelSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ToolId id;
            public GameObject prefab;
            public Vector3 posOffsetM;     // local offset applied after the cursor placement
            public Vector3 eulerOffset;    // local rotation applied before the stroke yaw
            public float extraScale;       // 1 = keep the normalised size; 0 is treated as 1
            public float spinRpm;          // speed of the prefab's "Spin" part while the tool works; 0 = still, + = clockwise seen from above
            public float pileSinkM;        // how far the head presses into the pile on the carpet (m); none on the tile
        }

        public Entry[] entries = new Entry[0];

        /// <summary>
        /// Added to the cursor hover height before the head is placed. ToolController builds both hover
        /// heights as "surface + 0.012 m" (0.03 over the carpet backing, 0.009 over the tile at -3 mm),
        /// so -0.012 drops the head exactly onto the surface in either domain: 2 mm into the 20 mm pile
        /// on the carpet, flush with the tile on the floor.
        /// </summary>
        public float hoverMarginM = -0.012f;

        [Header("Spinning part (Docs/TOOL_MODELS.md 7)")]
        [Tooltip("Spin-up rate (1/s) once the tool starts working: 4 reaches 90% of spinRpm in ~0.55 s.")]
        public float spinUpHz = 4f;
        [Tooltip("Coast-down rate (1/s) after the tool stops: 1.2 leaves 10% of the speed after ~1.9 s, like a pad running out.")]
        public float spinDownHz = 1.2f;
        [Tooltip("Seconds without a stamp before the tool counts as stopped. Same grace as ToolAudio.strokeReleaseGrace, so a frame off the carpet does not brake the pad.")]
        public float spinReleaseGraceS = 0.12f;
        [Tooltip("Widest gap (deg) allowed between copies of the spinning part drawn along one frame's turn. The brush has ~40 tufts 9 deg apart; one frame at 175 rpm / 60 fps turns 17.5 deg, which without copies strobes into a still or backwards brush.")]
        public float smearMaxStepDeg = 4.5f;
        [Tooltip("Most extra copies drawn per frame to fill that gap.")]
        public int smearMaxCopies = 4;

        public bool TryGet(ToolId id, out Entry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].id != id) continue;
                entry = entries[i];
                return entry.prefab != null;
            }
            entry = default;
            return false;
        }
    }
}
