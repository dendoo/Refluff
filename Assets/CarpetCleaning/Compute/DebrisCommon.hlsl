// DebrisCommon.hlsl — structs, constants and uniforms shared by CarpetSim.compute (K_Emit), Debris.compute and FloorSim.compute.
// Binding contract: see Docs/FLOOR_SYSTEM.md sections 0, 2, 4.5. C# mirrors: Scripts/DebrisStructs.cs, Scripts/FloorSettings.cs.
#ifndef CARPET_DEBRIS_COMMON_INCLUDED
#define CARPET_DEBRIS_COMMON_INCLUDED

#include "SimulationDomain.hlsl"

#define INJECT_SCALE 10000.0                      // fixed point for the floor inject buffer (uint = value * INJECT_SCALE)

// ---------------------------------------------------------------- debris structs (byte-exact with DebrisStructs.cs)
struct DebrisEmit      // 48 bytes
{
    float3 pos;        // CMM x, height mm, CMM y
    float  size;       // mm
    float3 vel;        // mm/s
    uint   kind;       // 0 chunk, 1 dust, 2 droplet
    float  amount;     // chunk: mass units, dust: floor dust units, droplet: water mm^3
    float  seed;       // 0..1
    float  dirt;       // chunk: plateShade, droplet: slurry concentration C
    float  pad;
};

struct DebrisParticle  // 80 bytes
{
    float3 pos;        // CMM x, height mm, CMM y
    float  size;       // mm
    float3 vel;        // mm/s
    float  life;       // dust: seconds left; sucked: timer; otherwise unused
    float4 rot;        // quaternion (x, y, z, w)
    float3 angVel;     // rad/s; while state == 4 holds the suction target (CMM x, height, CMM y)
    uint   kindState;  // kind | (state << 8)
    float  amount;
    float  seed;
    float  dirt;       // chunk: plateShade, droplet: slurry concentration C, dust: ground mm under the puff (K_DebSim, low-passed ~0.12 s)
    float  pad;        // dust: lifeMax
};

#define DEB_KIND_CHUNK   0u
#define DEB_KIND_DUST    1u
#define DEB_KIND_DROP    2u
#define DEB_ST_DEAD      0u
#define DEB_ST_FLY       1u   // flying or sliding
#define DEB_ST_REST_FLR  2u
#define DEB_ST_REST_CPT  3u
#define DEB_ST_SUCKED    4u
#define DEB_ST_DISSOLVE  5u

uint DebKind(uint ks)  { return ks & 0xFFu; }
uint DebState(uint ks) { return (ks >> 8) & 0xFFu; }
uint DebPack(uint kind, uint state) { return (kind & 0xFFu) | ((state & 0xFFu) << 8); }

// ---------------------------------------------------------------- helpers
bool InsideCarpetCMM(float2 p) { return p.x >= 0.0 && p.x <= CARPET_W_MM && p.y >= 0.0 && p.y <= CARPET_H_MM; }
// 1 while the carpet has been handed over (CarpetSimulation.CarpetAway, Docs/HANDOVER.md 6). Declaring it here, above
// every consumer, means an unset uniform reads 0 = carpet present = the behaviour that shipped before the flag existed.
int _CarpetAway;
// "a carpet really is lying on this spot": the floor sim's wall cells, the debris ground query and the floor shaders'
// cut-out all key off THIS, never off InsideCarpetCMM, so the bare rect is ordinary tile for tools, water and foam.
bool CarpetCoversCMM(float2 p) { return _CarpetAway == 0 && InsideCarpetCMM(p); }
// Tool POWER upgrades (Docs/ECONOMY.md 2): ToolTuning uploads (P - 1) per ToolId, 4 per element like _ToolSpacing, so an
// unset uniform reads 0 = the factory power. Only the effect RATES of a tool branch are multiplied, never pile styling.
float4 _ToolPower[4];
float ToolPower(int t) { int i = clamp(t, 0, 15); return max(1.0 + _ToolPower[i >> 2][i & 3], 0.0); }
float2 FloorUV(float2 cmm) { return (cmm - FLOOR_MIN) / FLOOR_SIZE; }
int2 FloorCell(float2 cmm) { return (int2)floor((cmm - FLOOR_MIN) / FLOOR_DX); }
bool InsideFloor(int2 c) { return c.x >= 0 && c.y >= 0 && c.x < FLOOR_W && c.y < FLOOR_W; }
uint FloorInjectIndex(int2 c, uint ch) { return ((uint)c.y * (uint)FLOOR_W + (uint)c.x) * 4u + ch; }

// ---------------------------------------------------------------- debris uniforms (FloorSettings D_* fields, uploaded as _D_*)
float _D_GRAV, _D_CHIP_FLY, _D_DUST_FRAC, _D_CHIPS_PER_UNIT, _D_DUST_PER_UNIT, _D_DROPS_PER_MM, _D_CHIP_UP;
float _D_CHIP_SIZE_MIN, _D_CHIP_SIZE_MAX, _D_REST_CARPET, _D_REST_FLOOR, _D_FRICTION, _D_DISSOLVE;
float _D_DROP_SPEED, _D_DROP_WATER, _D_DUST_LIFE, _D_DUST_AMOUNT, _D_SUCK_SPEED, _D_MOP_PICK_MM;

// ---------------------------------------------------------------- rotary brush (Docs/ROTARY_BRUSH.md)
// The brush is the brush of a rotary floor machine. SimSettings BRUSH_* fields: CarpetSimulation uploads them with the
// rest of its settings, FloorSimulation copies the carpet's, so the pile and the tile feel the same machine.
float _BRUSH_RPM, _BRUSH_SWIRL, _BRUSH_FLING;

// Velocity (mm/s) of the pad at CMM offset p (mm) from its centre: clockwise seen from above, the way ToolModelView
// spins the head (CMM x = world +X, CMM y = world +Z).
float2 BrushPadVelocity(float2 p)
{
    float omega = _BRUSH_RPM * (6.2831853 / 60.0);
    return omega * float2(p.y, -p.x);
}

#endif // CARPET_DEBRIS_COMMON_INCLUDED
