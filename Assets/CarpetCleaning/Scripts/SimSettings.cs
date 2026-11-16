using System.Reflection;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Every simulation constant of section 4.1. HLSL uniform name = "_" + fieldName.</summary>
    [CreateAssetMenu(fileName = "SimSettings", menuName = "CarpetCleaning/SimSettings")]
    public sealed class SimSettings : ScriptableObject
    {
        [Header("Grid / water")]
        public float DX = CarpetSimulation.CellSizeMm;
        public float G = 9810f;
        public float H_MAX = 10f;
        public float U_MAX = 350f;
        public float W_CAP = 6f;
        public float K_ABS = 0.25f;
        public float H_FILM = 1f;
        public float E_SURF = 0.002f;   // free-water evaporation mm/s (pools mostly soak in instead)
        public float E_PILE = 0.006f;   // relative: w loses E_PILE/W_CAP = 0.1 % of itself per second (~1 % per 10 s)
        public float DRYER_BOOST = 1500f; // dryer: ~150 %/s relative pile evaporation under the nozzle (3x the original 500)
        public float D_WICK = 3f;

        [Header("Dirt chemistry")]
        public float K_HYD = 0.01f;
        public float K_HYD_AGIT = 0.5f;
        public float K_G2M = 0.5f;
        public float K_DIS = 3.0f;
        public float K_STAIN = 1.5f;
        public float C_SAT = 2.0f;
        public float SOAP_C_REF = 0.3f;
        public float PILE_H = 20f;
        public float FOAM_SLIP = 0.8f;
        public float SPEED_REF = 50f;

        [Header("Foam")]
        public float K_GEN = 60f;
        public float F_MAX = 12f;
        public float GAMMA_SOAP = 0.01f;
        public float K_DRAIN = 0.02f;
        public float K_DIL = 1.2f;
        public float K_DRYPOP = 0.15f;
        public float K_FD = 0.05f;
        public float K_FDM = 0.15f;
        public float REDEP = 0.4f;

        [Header("Fibers")]
        public float K_SPRING_DRY = 40f;
        public float K_SPRING_WET = 12f;
        public float C_DAMP_DRY = 6f;
        public float C_DAMP_WET = 14f;
        public float TAU_FLAT_DRY = 1.5f;
        public float TAU_FLAT_WET = 25f;
        // Fiber set (Docs/FIBER_SET.md): a tool pressing WET pile leaves it bent/flattened while damp; drying stands it back up.
        public float SET_W_LO = 0.10f;       // wetness below which pressing leaves no set
        public float SET_W_HI = 0.40f;       // wetness at which the set is captured at the full rate
        public float SET_RATE = 10f;         // 1/s per unit pressure*wetness: capture rate of the tool-imposed bend/flattening into the set
        public float SET_CONTACT_S = 0.25f;  // s of contact ONE FULL PASS of a moving tool counts as (swept footprint: no banding, speed/frame-rate independent)
        public float SET_FLUFF_VAC = 10f;    // 1/s per unit pressure: dry vacuum erases the set
        public float SET_FLUFF_BRUSH = 4f;   // 1/s per unit pressure*mech: brushing fully DRY pile erases the set (wet brushing sets it)
        public float SET_DRY_RELAX = 0.6f;   // 1/s: as the pile dries (wetness SET_W_LO -> 0.2 SET_W_LO) the set relaxes; dry pile rests upright (a lay on dry pile fades over ~3-5 s)
        public float WETVAC_W_MIN = 0.25f;   // pile moisture the wet vacuum cannot pull out (damp + dark); only the dryer dries fully. Keep < 0.35 (HUD hint)
        public float WETVAC_FLUSH = 4f;      // 1/s per unit pressure: dissolved dirt/soap the wet vac still flushes out of that damp pile
        // Flowing water lays the pile the way it runs (Docs/FIBER_SET.md 5): into the set, so it holds until dried or pressed another way.
        public float FLOW_SET_B = 0.6f;      // lay (bend) a full-strength flow presses into the pile
        public float FLOW_SET_F = 0.35f;     // flattening a full-strength flow presses in (never lifts pile pressed flatter)
        public float FLOW_SET_RATE = 4f;     // 1/s at full strength
        public float FLOW_V_LO = 10f;        // mm/s: slower water (a settling puddle) leaves the lay alone
        public float FLOW_V_HI = 100f;       // mm/s: full strength (a hose sheet runs at 150-300)
        public float FLOW_H_LO = 0.05f;      // mm of free water below which a flow drags nothing
        public float FLOW_H_HI = 0.5f;       // mm of free water for full strength
        // Dry vacuum suction (Docs/FIBER_SET.md 6): the pile around the round nozzle leans into it.
        public float VAC_PULL_B = 0.5f;      // lean toward the nozzle centre at full pull
        public float VAC_PULL_RATE = 25f;    // 1/s at full pull
        public float VAC_PULL_REACH = 1.8f;  // nozzle radii the pull reaches (it peaks at the skirt, ~0.85-0.9 radii)
        // Squeegee on dry pile (Docs/FIBER_SET.md 7): capture gain of its press where the pile is too dry to set (wet pile: 1).
        public float SQ_SET_DRY = 0.8f;

        [Header("Flux")]
        public float PILE_DRAG_LO = 2f;
        public float PILE_DRAG_HI = 14f;
        public float BED_PILE = 0.3f;
        public float EDGE_DROP = 5f;
        public float EDGE_LIP = 0.3f;   // mm: the bound edge holds a film this deep; only deeper water (and tool pushes) run off (Docs/CARPET_EDGE.md)
        public float D_FOAM = 20f;
        public float AGIT_SPEED = 300f;

        // Squeegee plow (Docs/SQUEEGEE.md): the blade moves the surface water + foam geometrically into a roll ahead of it.
        [Header("Squeegee plow")]
        public float SQ_W_DAMP = 0.30f;        // pile moisture the blade leaves: wrings wetter pile down to it, presses a film into drier pile up to it. Keep < 0.35 (HUD hint)
        public float SQ_W_ROLL = 0.9f;         // pushed water soaks into the pile ahead up to this before any stands on top (a free roll runs back when the blade lifts)
        public float SQ_H_PACK = 1.5f;         // mm of free water a packed roll cell carries on top of its soaked pile (more = a taller roll that runs back)
        public float SQ_F_PACK = 7f;           // foam in one packed roll cell (<= 0.6 F_MAX, so roll + leftovers stay under the F_MAX clamp)
        public float SQ_FOAM_CRUSH = 0.004f;   // 1/mm of blade travel: pushed foam collapses into the water (its dirt joins the slurry)
        public float SQ_LOOKAHEAD = 300f;      // mm ahead of the blade the roll is re-packed every sweep (longer than the steady roll)
        public float SQ_SUB_MM = 60f;          // longest single sweep; a longer frame of travel is split (ToolController)
        public float SQ_SPILL_H_RATE = 400f;   // mm/s of carpet-cell water one rim cell may pour onto the floor (the floor clamps h in its drip band)
        public float SQ_SPILL_F_RATE = 600f;   // foam per second one rim cell may pour onto the floor

        // Rotary brush (Docs/ROTARY_BRUSH.md): the brush is a floor machine's pad; FloorSimulation uses these values too.
        [Header("Rotary brush")]
        public float BRUSH_RPM = 350f;         // pad speed, clockwise seen from above (the head's ToolModelSet spinRpm is the visual twin: keep equal)
        public float BRUSH_SWIRL = 0.4f;       // share of the pad's speed the foam (and the water it moves) is dragged round with
        public float BRUSH_FLING = 0.1f;       // outward throw, as a share of the pad's speed at that radius

        // Dirt of a new carpet (Docs/DIRT_LEVEL.md): the level and the mix (spread, dust, grime, crust, spills) are rolled per
        // carpet from the seed (CarpetSimulation.RollDirt); these shape the fields K_Init spends them on.
        [Header("Dirt of a new carpet")]
        public float DIRT_MASK_MM = 500f;      // mm per soil-noise cell: 2.4 x 3 cells over the carpet, i.e. a few big soiled / clean regions
        public float DIRT_MASK_WARP = 1.5f;    // domain warp in noise cells: higher = swirled, ragged regions instead of round blobs
        public float DIRT_GRAIN = 0.3f;        // mottling of the soil, relative (+-): 0 = an even film, 0.5 = blotchy
        public float DIRT_GRAIN_MM = 50f;      // size of the mottling blotches, mm
        public float DIRT_CRUST_MM = 220f;     // mm per noise cell of the dried-mud cake patches
        public float DIRT_DUST = 0.35f;        // grit of the dustiest ground (the fibre shader's _DustVeil turns it into the grey film)

        static FieldInfo[] s_fields;

        static FieldInfo[] Fields
        {
            get
            {
                if (s_fields == null)
                {
                    var list = new System.Collections.Generic.List<FieldInfo>();
                    foreach (var f in typeof(SimSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (f.FieldType == typeof(float)) list.Add(f);
                    s_fields = list.ToArray();
                }
                return s_fields;
            }
        }

        /// <summary>Uploads every public float field as cs.SetFloat("_" + name, value).</summary>
        public void Apply(ComputeShader cs)
        {
            if (cs == null) return;
            var fields = Fields;
            for (int i = 0; i < fields.Length; i++)
                cs.SetFloat("_" + fields[i].Name, (float)fields[i].GetValue(this));
        }
    }
}
