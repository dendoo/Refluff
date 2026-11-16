// RoomReflection.hlsl — the room that standing water mirrors (Docs/FLOOR_SYSTEM.md 5 "Water gloss", TRAILER.md 8.3).
// The game camera looks ~55 deg down, so the sun's mirror point lies behind it and a flat puddle never shows the sun: with
// only a fresnel sky term a brown puddle on the white tile read as a cast shadow. Real water on a glazed floor is seen by
// what it reflects: here a flat white ceiling ROOM_CEIL_Y above the floor top with a grid of long LED fixtures. The reflected
// ray is intersected with the ceiling plane, so the panels are parallax-correct (they stay put in the world while ripples
// and flow bend the water surface), and the ripples break them into moving glints the way a real puddle does.
// Radiance is in units of the main light colour. Used by FloorWater (puddles) and FloorSurface (the damp film).
#ifndef ROOM_REFLECTION_INCLUDED
#define ROOM_REFLECTION_INCLUDED

#define ROOM_CEIL_Y      2.7     // m above the floor top (world y 0 = carpet backing, floor top -3 mm: close enough)
#define ROOM_PITCH       float2(4.0, 3.0)   // m between fixture centres (x, z): few enough that a flooded rug shows at most one, not a grid
#define ROOM_PANEL       float2(0.5, 1.8)   // m, long linear fixtures running along z (away from the camera)
// grid phase: one fixture's mirror image lies on the tiles just right of the rug's right rim (u 1.0-1.17) from the near
// corner to mid-rug (v 0.02-0.42), where the squeegee pours (trailer #01 at v ~0.1, #11 at v 0.36); the next ones fall on
// the tiles left of the rug (u ~-0.11) and past its far half (a white strip lying on a flooded rug read as a sheet of paper)
#define ROOM_OFFSET      float2(0.0100, 0.2067)
#define ROOM_PANEL_RAD   8.0     // fixture radiance / main light colour (at 12 its image on murky water blew out to a white shape)
#define ROOM_CEIL_RAD    0.30    // the white ceiling between the panels (lit by the room)

// Schlick for water (F0 0.02)
float RoomFresnel(float ndv) { float m = 1.0 - saturate(ndv); float m2 = m * m; return 0.02 + 0.98 * m2 * m2 * m; }

// radiance seen along the reflected ray R (unit, world) from the surface point posWS. R must come from the SMOOTH water
// surface (the sim's bed + h slope and slow waves): a 4 mm ripple normal bounces R in and out of a panel on every pixel and
// reads as camouflage blotches. Fine ripples act as roughness instead: roughRad (radians of normal spread) widens the panel
// edge by the reflected cone's footprint on the ceiling.
float3 RoomRadiance(float3 posWS, float3 R, float3 lightColor, float3 skyColor, float roughRad)
{
    float3 ceil = skyColor * ROOM_CEIL_RAD * max(lightColor.g, 0.0);
    if (R.y < 0.05) return ceil;                            // grazing rays hit the walls: plain room colour
    float t = (ROOM_CEIL_Y - posWS.y) / R.y;
    float2 X = posWS.xz + R.xz * t;                         // metres on the ceiling
    float2 f = abs(frac(X / ROOM_PITCH + ROOM_OFFSET) - 0.5) * ROOM_PITCH;   // distance from the nearest fixture centre
    float2 q = f - ROOM_PANEL * 0.5;
    float d = max(q.x, q.y);                                // < 0 inside a fixture (box distance, metres)
    float aa = max(fwidth(d), 2.0 * roughRad * t) + 1e-4;   // pixel footprint, or the ripple cone (2 x normal spread x path)
    float panel = 1.0 - smoothstep(-aa, aa, d);
    // a footprint wider than a few decimetres sees the mean ceiling (fixture area share 0.1), not sparkle noise
    panel = lerp(panel, (ROOM_PANEL.x * ROOM_PANEL.y) / (ROOM_PITCH.x * ROOM_PITCH.y), saturate(aa * 2.5 - 0.4));
    return lerp(ceil, lightColor * ROOM_PANEL_RAD, panel);
}

#endif
