// SimCommon.hlsl — LANE-SIM shared declarations for CarpetSim.compute (its only user).
// Uniform names, ToolStamp layout and resource names are the binding contract of spec section 3.
#ifndef CARPET_SIM_COMMON_INCLUDED
#define CARPET_SIM_COMMON_INCLUDED

// Shared debris structs / floor frame constants / _D_* uniforms (Docs/FLOOR_SYSTEM.md section 2)
#include "DebrisCommon.hlsl"

// ---------------------------------------------------------------- ToolStamp (48 bytes, spec 3.4)
struct ToolStamp
{
    float2 uv;        // stamp centre in carpet UV
    float2 dir;       // unit stroke direction in mm space
    float  radiusMm;  // disc radius or blade half-thickness along the stroke
    float  lengthMm;  // blade full length across the stroke (0 for discs)
    float  pressure;  // 0..1
    float  speed;     // stroke speed [mm/s]
    float  dtS;       // seconds this stamp represents
    int    tool;      // ToolId
    float2 vStroke;   // stroke velocity [mm/s] in mm space
};

// ---------------------------------------------------------------- Resources (spec 3.5)
Texture2D<float4>   _FluidIn;   RWTexture2D<float4> _FluidOut;
RWTexture2D<float4> _Flux;
Texture2D<float4>   _SoapIn;    RWTexture2D<float4> _SoapOut;
RWTexture2D<float4> _Dirt;      RWTexture2D<float4> _Crust;
Texture2D<float4>   _FiberIn;   RWTexture2D<float4> _FiberOut;
RWTexture2D<float4> _FiberVel;  RWTexture2D<float4> _Agit;  RWTexture2D<float4> _ToolFlow;  RWTexture2D<float> _Bed;
RWTexture2D<float4> _FiberSet;  // (bSet.xy, fSet, 0): bend/flattening a tool pressed into WET pile (Docs/FIBER_SET.md); K_Fiber only
StructuredBuffer<ToolStamp> _Stamps;   RWStructuredBuffer<uint> _StatsBuf;
Texture2D<float4> _NoiseTex;  SamplerState sampler_linear_repeat;  SamplerState sampler_linear_clamp;
// Floor coupling (FLOOR_SYSTEM.md 1.1 / 1.3): per-frame runoff over the carpet edge, and the debris emission list.
RWTexture2D<float4> _EdgeOut;    // (waterVol mm^3, sOut, FOut F*mm^2, FdOut F*mm^2)   boundary cells only
RWTexture2D<float4> _EdgeOut2;   // (sigmaOut, FAOut F*age*mm^2, 0, 0)
AppendStructuredBuffer<DebrisEmit> _EmitBuf;   // 8192 entries, counter reset by the CPU every frame

// ---------------------------------------------------------------- Uniforms (cbuffer-free; spec 3.5 / 4.1)
int _SimW, _SimH, _StampCount, _Seed;
float _DtSim, _DtFrame, _Time;
float _DX, _G, _H_MAX, _U_MAX, _W_CAP, _K_ABS, _H_FILM, _E_SURF, _E_PILE, _DRYER_BOOST, _D_WICK;
float _K_HYD, _K_HYD_AGIT, _K_G2M, _K_DIS, _K_STAIN, _C_SAT, _SOAP_C_REF, _PILE_H, _FOAM_SLIP, _SPEED_REF;
float _K_GEN, _F_MAX, _GAMMA_SOAP, _K_DRAIN, _K_DIL, _K_DRYPOP, _K_FD, _K_FDM, _REDEP;
float _K_SPRING_DRY, _K_SPRING_WET, _C_DAMP_DRY, _C_DAMP_WET, _TAU_FLAT_DRY, _TAU_FLAT_WET;
float4 _ToolSpacing[4];   // ToolDefinition.spacingMm by ToolId, 4 per element
float _SET_W_LO, _SET_W_HI, _SET_RATE, _SET_CONTACT_S, _SET_FLUFF_VAC, _SET_FLUFF_BRUSH, _SET_DRY_RELAX, _WETVAC_W_MIN, _WETVAC_FLUSH;
// Pile laid by flowing water, the dry vacuum's suction and the squeegee on dry pile (Docs/FIBER_SET.md 5-7)
float _FLOW_SET_B, _FLOW_SET_F, _FLOW_SET_RATE, _FLOW_V_LO, _FLOW_V_HI, _FLOW_H_LO, _FLOW_H_HI;
float _VAC_PULL_B, _VAC_PULL_RATE, _VAC_PULL_REACH, _SQ_SET_DRY;
float _PILE_DRAG_LO, _PILE_DRAG_HI, _BED_PILE, _EDGE_DROP, _EDGE_LIP, _D_FOAM, _AGIT_SPEED;
// Dirt of a new carpet (Docs/DIRT_LEVEL.md), K_Init only, rolled per carpet by CarpetSimulation.RollDirt:
// _DirtMask = (level 0..1, noise offset.xy, spread), _DirtMix = (dust, mud, crust share, spill share). The floats are
// SimSettings tunables.
float4 _DirtMask, _DirtMix;
float _DIRT_MASK_MM, _DIRT_MASK_WARP, _DIRT_CRUST_MM, _DIRT_DUST, _DIRT_GRAIN, _DIRT_GRAIN_MM;

// Squeegee plow (Docs/SQUEEGEE.md). _PlowAcc: fixed-point accumulators over the grid plus a 1-cell ghost ring,
// (W+2)(H+2) x PLOW_CH uint; the ring is the spill reservoir for everything pushed off the carpet (K_PlowDrain empties it).
RWStructuredBuffer<uint> _PlowAcc;
float4 _PlowA;   // (P0.xy, P1.xy): blade centre before / after this sweep, carpet mm
float4 _PlowB;   // (a0.xy, a1.xy): blade axis (the drawn head direction, unsigned) before / after
float4 _PlowC;   // (travel sign, blade half-thickness r, blade half-length, 0)
float _SQ_W_DAMP, _SQ_W_ROLL, _SQ_H_PACK, _SQ_F_PACK, _SQ_FOAM_CRUSH, _SQ_LOOKAHEAD, _SQ_SPILL_H_RATE, _SQ_SPILL_F_RATE;

#define EPS 1e-6

// ---------------------------------------------------------------- Small helpers
// Bounded decay: x -= x*(1-exp(-k*dt)); returns the removed amount (never more than x).
float DecayAmount(float x, float k, float dt) { return x * (1.0 - exp(-max(k, 0.0) * dt)); }

// pileTopMm(f, w) = PILE_H * (1 - 0.55 f) * (1 - 0.25 w)  (spec 3.1)
float PileTopMm(float f, float w) { return _PILE_H * (1.0 - 0.55 * f) * (1.0 - 0.25 * w); }

bool InsideGrid(int2 c) { return c.x >= 0 && c.y >= 0 && c.x < _SimW && c.y < _SimH; }
int2 ClampCell(int2 c) { return clamp(c, int2(0, 0), int2(_SimW - 1, _SimH - 1)); }

// Clamped neighbour fetches (edge cells read themselves).
float4 FluidInC(int2 c)  { return _FluidIn[ClampCell(c)]; }
float4 FluxC(int2 c)     { return _Flux[ClampCell(c)]; }
float4 SoapInC(int2 c)   { return _SoapIn[ClampCell(c)]; }
float4 FiberInC(int2 c)  { return _FiberIn[ClampCell(c)]; }
float4 FiberOutC(int2 c) { return _FiberOut[ClampCell(c)]; }
float4 ToolFlowC(int2 c) { return _ToolFlow[ClampCell(c)]; }
float  BedC(int2 c)      { return _Bed[ClampCell(c)]; }

// Footprint weight of a stamp at mm offset p (spec 4.4): disc or blade.
// Nine subcell samples resolve narrow blades without increasing their physical dimensions.
// Broad tools retain their point sample; particle/tool collision in Debris.compute stays a point query.
float BladePointWeight(float2 q, float2 extent)
{
    float2 edge = abs(q) - extent;
    return smoothstep(4.0, 0.0, edge.x) * smoothstep(4.0, 0.0, edge.y);
}

float FootprintWeight(float2 p, ToolStamp st)
{
    float weight = 0.0;
    if (st.lengthMm > 0.0)
    {
        float2 d = st.dir;
        float dl = dot(d, d);
        d = (dl > 1e-8) ? d * rsqrt(dl) : float2(1.0, 0.0);
        float2 perp = float2(-d.y, d.x);
        float2 q = float2(dot(p, d), dot(p, perp));
        float2 extent = float2(st.radiusMm, st.lengthMm * 0.5);
        float cellMm = max(_DX, 1e-3);
        if (all(abs(q) <= extent + 4.0 + cellMm * 0.707107))
        {
            if (2.0 * st.radiusMm >= 4.0 * cellMm) weight = BladePointWeight(q, extent);
            else
            {
                float2 qx = (cellMm / 3.0) * float2(d.x, perp.x);
                float2 qy = (cellMm / 3.0) * float2(d.y, perp.y);
                weight = (BladePointWeight(q, extent)
                        + BladePointWeight(q + qx, extent) + BladePointWeight(q - qx, extent)
                        + BladePointWeight(q + qy, extent) + BladePointWeight(q - qy, extent)
                        + BladePointWeight(q + qx + qy, extent) + BladePointWeight(q + qx - qy, extent)
                        + BladePointWeight(q - qx + qy, extent) + BladePointWeight(q - qx - qy, extent)) / 9.0;
            }
        }
    }
    else weight = smoothstep(1.0, 0.7, length(p) / max(st.radiusMm, 1e-3));
    return weight;
}

// Dry vacuum suction (Docs/FIBER_SET.md 6): how hard the air rushing into the round nozzle pulls the pile at mm offset p
// from its centre. 0 under the middle of the head (there the pile is pulled straight up into the slot), 1 around the
// skirt, fading out to _VAC_PULL_REACH radii. The pull points at the centre: -p / |p|.
float VacPullWeight(float2 p, float radiusMm)
{
    float d = length(p) / max(radiusMm, 1e-3);
    return smoothstep(0.35, 0.85, d) * (1.0 - smoothstep(0.9, max(_VAC_PULL_REACH, 0.95), d));
}

// ---------------------------------------------------------------- Hash / noise (inline, no external includes)
float Hash1(float2 p)
{
    p = frac(p * float2(0.1031, 0.1030)) ;
    p += dot(p, p.yx + 33.33);
    return frac((p.x + p.y) * p.x);
}
float2 Hash2(float2 p)
{
    float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// Value noise with smooth interpolation, 0..1.
float ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = Hash1(i + float2(0.0, 0.0));
    float b = Hash1(i + float2(1.0, 0.0));
    float c = Hash1(i + float2(0.0, 1.0));
    float d = Hash1(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// 4-octave fbm, normalised to 0..1.
float Fbm(float2 p)
{
    float sum = 0.0, amp = 0.5, norm = 0.0;
    [unroll]
    for (int o = 0; o < 4; o++)
    {
        sum  += amp * ValueNoise(p);
        norm += amp;
        p = p * 2.03 + float2(17.1, 9.7);
        amp *= 0.5;
    }
    return sum / max(norm, EPS);
}

// Jittered-grid Voronoi: returns F1 (x), F2 (y), and the hash of the nearest cell (z).
float3 Voronoi(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float f1 = 8.0, f2 = 8.0, cellHash = 0.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 g = float2(x, y);
            float2 o = Hash2(i + g);
            float2 r = g + o - f;
            float d = dot(r, r);
            if (d < f1) { f2 = f1; f1 = d; cellHash = Hash1(i + g + 0.5); }
            else if (d < f2) { f2 = d; }
        }
    }
    return float3(sqrt(f1), sqrt(f2), cellHash);
}

// Worley F1, roughly 0..1.
float Worley(float2 p) { return saturate(Voronoi(p).x); }

#endif // CARPET_SIM_COMMON_INCLUDED
