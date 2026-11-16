using System.Reflection;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Floor simulation (F_*) and debris (D_*) constants. HLSL uniform name = "_" + fieldName. See Docs/FLOOR_SYSTEM.md 4.5.</summary>
    [CreateAssetMenu(fileName = "FloorSettings", menuName = "CarpetCleaning/FloorSettings")]
    public sealed class FloorSettings : ScriptableObject
    {
        [Header("Floor water")]
        public float F_H_MAX = 12f;
        public float F_U_MAX = 600f;
        public float F_H_REST = 1.2f;      // mm of water that adheres to the tile and never flows on its own (puddle thickness ~1-2 mm)
        public float F_DRAG_LO = 1.5f;
        public float F_DRAG_HI = 25f;
        public float F_G = 9810f;
        public float F_E_SURF = 0.008f;    // mm/s evaporation on the tile (the ~0.01 mm a mop leaves must be gone in ~1 s)
        public float F_DRYER_BOOST = 1200f;   // 3x the original 400 (dryer power, carpet DRYER_BOOST matches)
        public float F_TAU_WET = 16f;      // s, dark wet film fade. Short on purpose: a mopped tile has to READ dry almost at once
        public float F_REDEP = 1.0f;       // drying dirty water -> residue rings
        public float F_EDGE_DROP = 20f;
        public float F_DRIP_W = 10f;       // mm band outside the carpet edge that receives the runoff

        [Header("Floor dirt")]
        public float F_K_DUST_DIS = 6f;
        public float F_K_RES_DIS = 0.05f;
        public float F_K_RES_AGIT = 2f;
        public float F_C_SAT = 2f;

        [Header("Floor foam")]
        public float F_K_GEN = 40f;
        public float F_F_MAX = 12f;
        public float F_GAMMA_SOAP = 0.01f;
        public float F_K_DRAIN = 0.03f;
        public float F_K_DIL = 1.2f;
        public float F_K_DRYPOP = 0.5f;
        public float F_K_FD = 0.05f;
        public float F_D_FOAM = 20f;
        public float F_FOAM_SLIP = 0.9f;
        public float F_SOAP_C_REF = 0.3f;

        [Header("Floor tiles / tools")]
        public float F_GROUT_DEPTH = 1.0f;
        public float F_GROUT_W = 6f;
        public float F_TILE = 300f;
        public float F_SPEED_REF = 50f;
        public float F_AGIT_SPEED = 300f;

        [Header("Debris emission")]
        public float D_GRAV = 9810f;
        public float D_CHIP_FLY = 0.45f;        // fraction of chipped crust that becomes flying chunks (<= 0.8)
        public float D_DUST_FRAC = 0.5f;
        public float D_CHIPS_PER_UNIT = 0.12f;
        public float D_DUST_PER_UNIT = 0.06f;
        public float D_DROPS_PER_MM = 0.004f;
        public float D_CHIP_UP = 1.5f;
        public float D_CHIP_SIZE_MIN = 4.0f;
        public float D_CHIP_SIZE_MAX = 12f;

        [Header("Debris motion")]
        public float D_REST_CARPET = 0.2f;
        public float D_REST_FLOOR = 0.35f;
        public float D_FRICTION = 4f;
        public float D_DISSOLVE = 0.15f;        // mm/s of chunk size lost while sitting in water
        public float D_DROP_SPEED = 1500f;
        public float D_DROP_WATER = 3.5f;       // mm^3 per droplet
        public float D_DUST_LIFE = 1.6f;
        public float D_DUST_AMOUNT = 0.02f;
        public float D_SUCK_SPEED = 12f;
        public float D_MOP_PICK_MM = 13f;       // chunks smaller than this are absorbed by the mop; bigger ones are swept along.
                                                // Above D_CHIP_SIZE_MAX (12), so a mop pass really does leave the tile bare.

        [Header("Floor detergent (Docs/FLOOR_TOOLS.md)")]
        public float F_DET_MAX = 1f;            // film saturation cap
        public float F_DET_REF = 0.16f;         // film level that counts as fully treated (mop efficiency + shader); low, so one pass arms the mop
        public float F_DET_RATE = 7.0f;         // film units/s laid down at full pressure: one brisk pass must already saturate
        public float F_DET_WATER = 0.35f;       // mm/s of liquid sprayed with it (stays under F_H_REST, never flows; less to dry afterwards)
        public float F_DET_SOAP = 0.15f;        // surfactant/s; deliberately small, the spray must not raise a foam mound
        public float F_DET_DRY = 0.015f;        // 1/s film loss on a dry tile (~65 s)
        public float F_DET_WASH = 0.5f;         // 1/s film loss where it is diluted by standing water
        public float F_DET_TO_RES = 0.10f;      // fraction of a dried film that hazes into residue
        public float F_DET_K_RES = 0.60f;       // 1/s residue -> slurry loosening under the film: the spray visibly eats dried rings on its own

        [Header("Mop")]
        public float F_MOP_K = 260f;            // 1/s dust / residue / slurry removal on a treated cell: one pass over a sprayed tile is spotless
        public float F_MOP_K_DRY = 8f;          // 1/s removal by a dry mop, modulated by a static streak pattern
        public float F_MOP_EAT = 45f;           // 1/s film consumed by the head
        public float F_MOP_SOAK = 320f;         // mm/s water absorbed (wet vacuum is 200): a wrung mop leaves the tile dry, not damp
        public float F_MOP_FILM = 0.008f;       // mm of water the head always leaves behind (evaporates in ~1 s at F_E_SURF)
        public float F_MOP_FOAM = 120f;         // F/s wiped away
        public float F_MOP_WET = 0.22f;         // wetFilm CAP the head leaves behind (it pulls a soaked tile DOWN to this), fades over F_TAU_WET

        static FieldInfo[] s_fields;
        static int[] s_ids;   // Shader.PropertyToID("_" + name), built once (Apply runs three times per frame)

        static FieldInfo[] Fields
        {
            get
            {
                if (s_fields == null)
                {
                    var list = new System.Collections.Generic.List<FieldInfo>();
                    foreach (var f in typeof(FloorSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (f.FieldType == typeof(float)) list.Add(f);
                    s_fields = list.ToArray();
                }
                return s_fields;
            }
        }

        /// <summary>Uploads every public float field as cs.SetFloat("_" + name, value). Unknown names are ignored by Unity.</summary>
        public void Apply(ComputeShader cs)
        {
            if (cs == null) return;
            var fields = Fields;
            if (s_ids == null || s_ids.Length != fields.Length)
            {
                s_ids = new int[fields.Length];
                for (int i = 0; i < fields.Length; i++) s_ids[i] = Shader.PropertyToID("_" + fields[i].Name);
            }
            for (int i = 0; i < fields.Length; i++)
                cs.SetFloat(s_ids[i], (float)fields[i].GetValue(this));
        }
    }
}
