// HighlightCommon.hlsl - the inspection lamp (Docs/HIGHLIGHTS.md), shared by every surface that shows soil or water:
// CarpetFibers, CarpetWater, CarpetFoam (both domains), FloorSurface, FloorWater, DebrisChunk, DebrisDrop (splash drops, 2026-09-25).
//
// While a lamp is on the surface is dimmed and desaturated and what still costs the pay slip glows on top (emissive, so it
// reads in shadow too): soil deep amber -> bright amber, water teal -> blue, brighter with more. The carpet measures are the pay
// slip's own (CarpetSimulation.DirtScore weights, Wetness = w + h / W_CAP); the response is logarithmic so a trace that still
// keeps the bar under 100 % shows as clearly as a mud patch reads as a mud patch. With both lamps on, cells that are dirty AND
// wet alternate amber and blue in 28 mm diagonal stripes instead of adding up to a muddy white.
//
// Globals (OUTSIDE UnityPerMaterial, like the sim globals):
//   _Highlight     = (dirt amount 0..1, moisture amount 0..1, dirt sweep 0..1, moisture sweep 0..1)   SurfaceHighlight
//   _HighlightTune = (dirtRef, wetRef, dim 0..1, glow)                                              SurfaceHighlight
//   _HighlightCut  = (carpet dirt level, carpet wet level, 0, 0)                                    CarpetSimulation
// The level makes the carpet agree with the HUD bars: a carpet cell glows with what it holds ABOVE the level, which is chosen
// so that the carpet cut down to it holds exactly what the bar tolerates at 100 % (CarpetSimulation.WaterLevel). Near 0 on a
// soiled carpet (everything glows), rising as the bar climbs, out of reach (1e30) while the bar shows 100 %: nothing lights
// up on a carpet the HUD calls clean / dry (Docs/HIGHLIGHTS.md 4). The floor has no bar and no level: its callers pass 0.
// Every caller branches on HL_Active() first: with both lamps off the lamp costs one uniform test.
#ifndef CARPET_HIGHLIGHT_COMMON_INCLUDED
#define CARPET_HIGHLIGHT_COMMON_INCLUDED

#include "../Compute/SimulationDomain.hlsl"

float4 _Highlight;
float4 _HighlightTune;
float4 _HighlightCut;

#define HL_CENTRE_MM  CARPET_CENTRE_MM   // the scan ring starts at the carpet centre (carpet mm, the floor shares the frame)
#define HL_SWEEP_MM   (FLOOR_SIZE * 0.70710678 + 150.0) // beyond the floor domain's farthest corner
#define HL_RING_MM    70.0                   // half-width of the scan ring
#define HL_HATCH_MM   28.0                   // stripe pitch where both lamps show on one cell (~20 px at the play camera: 12 mm blurred to grey)
#define HL_FULL_REFS  100.0                  // the response reaches 1 at this many refs (ref 0.02: 2.0 = a caked cell / a pool)

bool HL_Active() { return _Highlight.x + _Highlight.y > 1e-4; }

// ---------------------------------------------------------------- measures
// carpet cell soil, the DirtScore weights: dirt = (crust, mud, stain, grit), fl.w = slurry, soap.z = foam dirt
float HL_CarpetDirt(float4 dirt, float4 fl, float4 soap)
{
    return 1.0 * saturate(dirt.x) + 0.8 * saturate(dirt.y) + 0.6 * saturate(dirt.z) + 0.9 * saturate(dirt.w)
         + 0.7 * max(fl.w, 0.0) + 0.5 * max(soap.z, 0.0);
}

// carpet cell water in units of W_CAP (the Wetness measure): pile moisture + free water
float HL_CarpetWet(float4 fb, float4 fl, float wcap)
{
    return saturate(fb.w) + max(fl.x, 0.0) / max(wcap, 1e-3);
}

// floor cell soil: dust + dried residue + slurry + foam dirt (FloorSim.compute's "dirty cell" threshold is 0.02 on each)
float HL_FloorDirt(float4 dp, float4 fl, float4 soap)
{
    return max(dp.x, 0.0) + max(dp.y, 0.0) + 0.7 * max(fl.w, 0.0) + 0.5 * max(soap.z, 0.0);
}

// floor cell water in the carpet's unit (6 mm = 1): standing water plus the damp film (dp.z, 1 right after water, fades ~16 s)
float HL_FloorWet(float4 fl, float4 dp)
{
    return max(fl.x, 0.0) / 6.0 + 0.06 * saturate(dp.z);
}

// ---------------------------------------------------------------- response and colour
// 0 at x = 0, 0.15 at x = ref, 0.39 at 5 ref, 1 at HL_FULL_REFS refs (log: every halving of the soil is one step down, so a
// trace at a quarter of ref still shows while a caked cell is not just "more white"); a tiny cut-off, because a bounded decay
// never quite reaches 0
float HL_Response(float x, float ref)
{
    ref = max(ref, 1e-5);
    x = max(x - 0.04 * ref, 0.0);
    return saturate(log2(1.0 + x / ref) / log2(1.0 + HL_FULL_REFS));
}

// how bright a response glows: a quick onset (so a faint trace is not lost in the dimmed rug) then linear
float HL_Intensity(float r) { return smoothstep(0.0, 0.03, r) * (0.28 + 0.72 * r); }

// saturated all the way up: the brightest channel stays near 1 after the glow scale, so the neutral tonemapper keeps the hue
// (a yellow-white peak read as flat cream on a fully soiled carpet)
float3 HL_DirtRamp(float r)
{
    float3 a = float3(1.00, 0.24, 0.01);   // trace: deep amber
    float3 b = float3(1.00, 0.38, 0.03);   // soiled: orange
    float3 c = float3(1.00, 0.54, 0.10);   // caked: bright amber (the tonemapper already pulls bright orange toward yellow)
    return r < 0.5 ? lerp(a, b, r * 2.0) : lerp(b, c, r * 2.0 - 1.0);
}

float3 HL_WetRamp(float r)
{
    float3 a = float3(0.02, 0.62, 0.72);   // damp: teal
    float3 b = float3(0.08, 0.45, 1.00);   // wet: blue
    float3 c = float3(0.28, 0.52, 1.00);   // soaked / standing water: bright blue
    return r < 0.5 ? lerp(a, b, r * 2.0) : lerp(b, c, r * 2.0 - 1.0);
}

// ---------------------------------------------------------------- scan ring
// 1 behind the ring's front, 0 ahead of it (sweep 1 = everywhere); ring = the bright front itself, gone as the sweep ends
float HL_Reveal(float2 cmm, float sweep, out float ring)
{
    ring = 0.0;
    if (sweep >= 1.0) return 1.0;
    float R = sweep * HL_SWEEP_MM;
    float d = distance(cmm, HL_CENTRE_MM);
    float x = (d - R) / HL_RING_MM;
    ring = exp(-x * x) * (1.0 - smoothstep(0.7, 1.0, sweep));
    return saturate(0.5 - (d - R) / (2.0 * HL_RING_MM));
}

// ---------------------------------------------------------------- the lamp
struct HLTerms
{
    float  dimAmt;   // how much of the surface colour to dim (0..1)
    float3 glow;     // emissive light to add (already scaled by the lamp amounts)
};

// dirtX / wetX: this point's measures (pass 0 where there is nothing to show); glowMul scales only the glow;
// level: (dirt, wet) the point glows with what it holds above, 0 = the whole of it (carpet callers pass HL_CarpetCut())
HLTerms HL_Terms(float dirtX, float wetX, float2 cmm, float glowMul, float2 level)
{
    HLTerms o;
    float ringD, ringW;
    float aD = _Highlight.x * HL_Reveal(cmm, _Highlight.z, ringD);
    float aW = _Highlight.y * HL_Reveal(cmm, _Highlight.w, ringW);
    o.dimAmt = max(aD, aW);

    // what is left to do here: a cell cleaned / dried down to the level stops glowing exactly then (the response's onset
    // fades it in and out, no hard edge)
    float rD = HL_Response(max(dirtX - level.x, 0.0), _HighlightTune.x);
    float rW = HL_Response(max(wetX - level.y, 0.0), _HighlightTune.y);
    // both lamps on and both present: alternate stripes (soft sine edge, no derivatives: callers may have discarded already)
    float both = min(aD, aW) * smoothstep(0.0, 0.06, rD) * smoothstep(0.0, 0.06, rW);
    float stripe = smoothstep(-0.35, 0.35, sin((cmm.x + cmm.y) * (6.2831853 / HL_HATCH_MM)));
    float mD = lerp(1.0, stripe, both);
    float mW = lerp(1.0, 1.0 - stripe, both);

    float g = max(_HighlightTune.w, 0.0) * glowMul * 1.1;    // the peak channel lands just under the 1.2 bloom threshold
    o.glow = HL_DirtRamp(rD) * (HL_Intensity(rD) * aD * mD * g)
           + HL_WetRamp(rW) * (HL_Intensity(rW) * aW * mW * g)
           + float3(1.00, 0.62, 0.18) * (ringD * _Highlight.x * 0.55)
           + float3(0.25, 0.60, 1.00) * (ringW * _Highlight.y * 0.55);
    return o;
}

// dimmed, desaturated surface colour (lit colour in, lit colour out). The luminance is compressed, not just scaled: after the
// neutral tonemapper a plain x0.38 left the white tile a light grey beside a dark rug; this brings both down to one dark
// "room with the lights off" (dim 0.62: tile 1.0 -> 0.15, rug 0.25 -> 0.07, shade 0.1 -> 0.03) and keeps their order
float3 HL_Dim(float3 col, float amt)
{
    float d = saturate(_HighlightTune.z);
    float lum = dot(col, float3(0.2126, 0.7152, 0.0722));
    float lumOut = (1.0 - d) * lum / (1.0 + 2.5 * d * lum);
    float3 grey = lerp(col, lum.xxx, 0.85) * (lumOut / max(lum, 1e-4));
    return lerp(col, grey, amt);
}

// the carpet's level: what glows on the carpet follows the HUD bars (0 % .. 99 %: what is left to do, 100 %: nothing)
float2 HL_CarpetCut() { return _HighlightCut.xy; }

// the whole lamp on an opaque lit colour; the floor version has no level, the carpet version follows the bars
float3 HL_Apply(float3 col, float dirtX, float wetX, float2 cmm, float glowMul)
{
    HLTerms t = HL_Terms(dirtX, wetX, cmm, glowMul, float2(0.0, 0.0));
    return HL_Dim(col, t.dimAmt) + t.glow;
}

float3 HL_ApplyCarpet(float3 col, float dirtX, float wetX, float2 cmm, float glowMul)
{
    HLTerms t = HL_Terms(dirtX, wetX, cmm, glowMul, HL_CarpetCut());
    return HL_Dim(col, t.dimAmt) + t.glow;
}

// water / film over a surface the lamp has already lit: `seen` is the part of col that is that surface seen through the
// film (refraction), which must not be dimmed twice; only the film's own light (silt, sky, glints) dims. Pass glowMul = the
// share of the surface the film hides (1 - transmittance): murky water then glows as brightly as the pile it covers, and
// clear water adds nothing on top of the lit tile it shows (no double glow, no dark murk)
float3 HL_ApplyOver(float3 col, float3 seen, float dirtX, float wetX, float2 cmm, float glowMul)
{
    HLTerms t = HL_Terms(dirtX, wetX, cmm, glowMul, float2(0.0, 0.0));
    return seen + HL_Dim(max(col - seen, 0.0), t.dimAmt) + t.glow;
}

float3 HL_ApplyOverCarpet(float3 col, float3 seen, float dirtX, float wetX, float2 cmm, float glowMul)
{
    HLTerms t = HL_Terms(dirtX, wetX, cmm, glowMul, HL_CarpetCut());
    return seen + HL_Dim(max(col - seen, 0.0), t.dimAmt) + t.glow;
}

#endif
