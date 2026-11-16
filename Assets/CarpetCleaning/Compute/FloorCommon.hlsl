// FloorCommon.hlsl — declarations for FloorSim.compute (its only user). Contract: Docs/FLOOR_SYSTEM.md section 4.
// Frames / constants (FLOOR_MIN, FLOOR_SIZE, FLOOR_W, FLOOR_DX, FLOOR_AREA, FLOOR_Y, INJECT_SCALE) and the _D_* uniforms
// come from DebrisCommon.hlsl. SimCommon.hlsl is NOT included (it declares the carpet resources), so the hash/noise
// helpers, ToolStamp and FootprintWeight are duplicated here byte-for-byte.
#ifndef CARPET_FLOOR_COMMON_INCLUDED
#define CARPET_FLOOR_COMMON_INCLUDED

#include "DebrisCommon.hlsl"

// ---------------------------------------------------------------- ToolStamp (48 bytes, identical to SimCommon.hlsl / ToolStamp.cs)
struct ToolStamp
{
    float2 uv;        // stamp centre in carpet UV (may lie outside 0..1 when the pointer is on the floor)
    float2 dir;       // unit stroke direction in mm space
    float  radiusMm;  // disc radius or blade half-thickness along the stroke
    float  lengthMm;  // blade full length across the stroke (0 for discs)
    float  pressure;  // 0..1
    float  speed;     // stroke speed [mm/s]
    float  dtS;       // seconds this stamp represents
    int    tool;      // ToolId
    float2 vStroke;   // stroke velocity [mm/s] in mm space
};

// ---------------------------------------------------------------- Resources (4.1)
// UAV rule: a kernel may bind at most 8 RW resources, so every texture that a kernel only READS is bound to the
// matching *In SRV alias below (same RenderTexture, different declaration). Per-kernel UAV counts are listed in FloorSim.compute.
Texture2D<float4>   _FFluidIn;     RWTexture2D<float4> _FFluidOut;    // (h mm, u, v mm/s, s slurry)
Texture2D<float4>   _FSoapIn;      RWTexture2D<float4> _FSoapOut;     // (sigma, F, Fd, Fage)
Texture2D<float4>   _FDepositIn;   RWTexture2D<float4> _FDeposit;     // (dust, residue, wetFilm, detergent film)
Texture2D<float4>   _FFluxIn;      RWTexture2D<float4> _FFlux;        // outflow per face L,R,B,T [mm^3/s]
Texture2D<float>    _FBedIn;       RWTexture2D<float>  _FBed;         // tile relief mm (+40 inside the carpet rect)
Texture2D<float4>   _FToolFlowIn;  RWTexture2D<float4> _FToolFlow;    // (vTool.xy, wt, wf)
Texture2D<float4>   _FAgitIn;      RWTexture2D<float4> _FAgit;        // (agit, inflow, dryer, 0)
RWStructuredBuffer<uint> _FInject;                                    // FW*FH*4 fixed point (x INJECT_SCALE), shared with DebrisSystem
RWStructuredBuffer<uint> _FStatsBuf;                                  // 16 uints, K_FReduce sums
Texture2D<float4>   _EdgeOut;      Texture2D<float4> _EdgeOut2;       // carpet runoff this frame (512x640, CarpetSimulation.EdgeOut/EdgeOut2)
StructuredBuffer<ToolStamp> _Stamps;                                  // carpet's stamp buffer (CarpetSimulation.StampBuffer)

// ---------------------------------------------------------------- Uniforms (4.1)
int   _FW, _FH, _StampCount;
float _FDX, _DtSim, _DtFrame, _Time;
float2 _FloorMin;
float _FloorSize;
// FloorSettings floats (uploaded by FloorSettings.Apply as "_" + fieldName)
float _F_H_MAX, _F_U_MAX, _F_H_REST, _F_DRAG_LO, _F_DRAG_HI, _F_G, _F_E_SURF, _F_DRYER_BOOST, _F_TAU_WET;
float _F_REDEP, _F_EDGE_DROP, _F_DRIP_W;
float _F_K_DUST_DIS, _F_K_RES_DIS, _F_K_RES_AGIT, _F_C_SAT;
float _F_K_GEN, _F_F_MAX, _F_GAMMA_SOAP, _F_K_DRAIN, _F_K_DIL, _F_K_DRYPOP, _F_K_FD, _F_D_FOAM, _F_FOAM_SLIP, _F_SOAP_C_REF;
float _F_GROUT_DEPTH, _F_GROUT_W, _F_TILE, _F_SPEED_REF, _F_AGIT_SPEED;
// Detergent film + mop (Docs/FLOOR_TOOLS.md). A missing declaration reads 0 and the tool silently does nothing.
float _F_DET_MAX, _F_DET_REF, _F_DET_RATE, _F_DET_WATER, _F_DET_SOAP, _F_DET_DRY, _F_DET_WASH, _F_DET_TO_RES, _F_DET_K_RES;
float _F_MOP_K, _F_MOP_K_DRY, _F_MOP_EAT, _F_MOP_SOAK, _F_MOP_FILM, _F_MOP_FOAM, _F_MOP_WET;

#define EPS 1e-6

// ---------------------------------------------------------------- Small helpers
float DecayAmount(float x, float k, float dt) { return x * (1.0 - exp(-max(k, 0.0) * dt)); }
bool  InsideFloorGrid(int2 c) { return c.x >= 0 && c.y >= 0 && c.x < _FW && c.y < _FH; }
float2 FloorCellCentreCMM(int2 c) { return _FloorMin + (float2(c) + 0.5) * _FDX; }

// Footprint weight of a stamp at mm offset p (identical to SimCommon.hlsl): disc or blade.
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
        float cellMm = max(_FDX, 1e-3);
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

// ---------------------------------------------------------------- Hash / noise (identical to SimCommon.hlsl)
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

#endif // CARPET_FLOOR_COMMON_INCLUDED
