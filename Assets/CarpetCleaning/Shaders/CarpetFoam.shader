// CarpetFoam.shader — pink-violet 3D foam (Docs/FOAM_LOOK.md).
// One displaced height-field surface per domain (CarpetRoot/FoamSurface, FloorRoot/FloorFoam with _FLOOR_DOMAIN):
// the vertex lifts the grid to base + foamMm(F) (tens of mm, lumpy, steep foot) + a cap lift, the fragment ray-traces
// packed spherical bubble caps (exact ray-sphere per pixel, analytic 1-px AA, no derivative noise), shades with a
// hue-preserving wrap model (violet shadows, pink crest, light-pink rim, blooming sparkles), marches the height field
// toward the sun for self/cast shadows (the scene has no shadow maps), and writes depth so the mound occludes water,
// fibres and droplets behind it. Transparent+1: drawn after the water, never inside the opaque texture.
// Both variants read ONE cross-domain field (FieldAt): the union of the carpet mound (spilling over the edge onto the tile)
// and the tile heap (overhanging the rim where it is taller than the carpet), so gradients, shadows and bubbles are continuous
// at the edge and the rim never shows as a bare strip or a ridge (Docs/CARPET_EDGE.md).
Shader "CarpetCleaning/CarpetFoam"
{
    Properties
    {
        _FoamColor ("Foam Color (crest, hot pink)", Color) = (0.98, 0.40, 0.88, 1)
        _FoamShadowColor ("Foam Shadow Color (violet)", Color) = (0.45, 0.16, 0.72, 1)
        _FoamDirtColor ("Foam Dirt Color (mauve)", Color) = (0.55, 0.30, 0.45, 1)
        _FoamRimColor ("Foam Rim Color (light pink)", Color) = (1.0, 0.75, 0.95, 1)
        _FoamHeight ("Foam Height Max (mm)", Float) = 45
        _FoamKnee ("Foam Height Knee (F units)", Float) = 3
        _LumpMm ("Lump Amplitude (mm)", Float) = 8
        _BubbleMm ("Bubble Cells (mm): large, small", Vector) = (16, 7, 0, 0)
        _CapLiftMm ("Cap Lift (mm, mesh above the film)", Float) = 5
        _SunScale ("Sun Scale", Float) = 0.5
        _AmbientScale ("Ambient Scale", Float) = 1.0
        _WrapK ("Wrap Lighting", Float) = 0.35
        _FormK ("Form Shading", Float) = 0.5
        _ShadowStrength ("Cast Shadow Strength", Float) = 0.55
        _AlphaKnee ("Alpha Knee (F units)", Float) = 0.25
        _SparklePx ("Sparkle Min Bubble Radius (px)", Float) = 6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma shader_feature_local _FLOOR_DOMAIN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HighlightCommon.hlsl"

            // ---- globals (CarpetSimulation / FloorSimulation / CarpetRenderer; OUTSIDE the material cbuffer) ----
            TEXTURE2D(_FluidTex);       // carpet (h, u, v, s)
            TEXTURE2D(_SoapTex);        // carpet (sigma, F, Fd, Fage)
            TEXTURE2D(_FiberTex);       // carpet (bend.xy, f, w)
            TEXTURE2D(_FFluidTex);      // floor (h, u, v, s)
            TEXTURE2D(_FSoapTex);       // floor (sigma, F, Fd, Fage)
            TEXTURE2D(_DirtTex);        // carpet (crust, mud, stain, grit): the inspection lamp only
            TEXTURE2D(_FDepositTex);    // floor (dust, residue, wetFilm, detergent): the inspection lamp only
            TEXTURE2D(_NoiseTex);       // 256x256 tileable: R fbm, G worley F1, B cell hash, A fbm2 (mipmapped)
            SAMPLER(sampler_linear_clamp);
            SAMPLER(sampler_trilinear_repeat);

            float4 _SimTexel;     // (inverse width, inverse height, width, height)
            float4 _CarpetMm;     // (width mm, height mm, inverse width, inverse height)
            float4 _SimConsts;    // (W_CAP, PILE_H, F_MAX, H_MAX)
            float4 _FloorRect;    // (FLOOR_MIN.x, FLOOR_MIN.y, 1/FLOOR_SIZE, FLOOR_Y)
            float4 _FloorTexel;   // (inverse width, inverse height, width, height)
            float4 _FloorConsts;  // (F_H_MAX, F_F_MAX, FDX mm, 0)
            float  _FloorFoamPitchMm;   // floor foam grid pitch (FloorRenderer): 12000 / 640 = 18.75 mm
            float4 _RimDir;       // world dir TO the rim light
            float4 _RimColor;
            float4 _ToolCursor;   // (u, v, radiusMm, active)
            float4 _ToolCursor2;  // (dir.x, dir.y, lengthMm, toolId)
            float  _CarpetAway;   // 1 while the carpet has been handed over: the rect is plain tile (Docs/HANDOVER.md 6)

            CBUFFER_START(UnityPerMaterial)
                half4 _FoamColor;
                half4 _FoamShadowColor;
                half4 _FoamDirtColor;
                half4 _FoamRimColor;
                float _FoamHeight;
                float _FoamKnee;
                float _LumpMm;
                float4 _BubbleMm;
                float _CapLiftMm;
                float _SunScale;
                float _AmbientScale;
                float _WrapK;
                float _FormK;
                float _ShadowStrength;
                float _AlphaKnee;
                float _SparklePx;
            CBUFFER_END

            // Everything below works in carpet mm (x along u, y along v; the floor's cmm is the same frame) and in the
            // CARPET height frame (carpet backing = 0, floor top = -3 mm). The floor object sits at FLOOR_Y, hence FRAME_MM.
            #if defined(_FLOOR_DOMAIN)
                #define FRAME_MM    3.0
                #define FOAM_CELLMM (max(_FloorConsts.z, 1e-3))
            #else
                #define FRAME_MM    0.0
                #define FOAM_CELLMM (_CarpetMm.x * _SimTexel.x)
            #endif

            bool InsideCarpetMm(float2 mm, float margin)
            {
                return mm.x >= -margin && mm.x <= _CarpetMm.x + margin && mm.y >= -margin && mm.y <= _CarpetMm.y + margin;
            }

            float PileTopMm(float4 fb) { return _SimConsts.y * (1.0 - 0.55 * fb.z) * (1.0 - 0.25 * fb.w); }
            float2 FloorUv(float2 mm)  { return (mm - _FloorRect.xy) * _FloorRect.z; }

            // ---- foam height field (see FOAM_LOOK.md section 2) ----
            float FoamMask(float F) { return smoothstep(0.05, 0.6, F); }

            // mip level for the lump noise: 0 in the vertex shader, set from the pixel footprint in the fragment (band-limits the taps)
            static float s_lumpLod = 0.0;

            float LumpMm(float2 mm, float F)
            {
                float2 nuv = mm / 1200.0;                                   // fixed 1200 mm physical noise period, continuous across the edge
                float nR = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_trilinear_repeat, nuv * 6.0, max(s_lumpLod - 1.74, 0.0)).r;
                float nA = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_trilinear_repeat, nuv * 20.0, s_lumpLod).a;
                return _LumpMm * ((2.0 * nR - 1.0) + 0.5 * (2.0 * nA - 1.0)) * saturate(F / 1.5);
            }

            // wet-vacuum feedback: the mound dips inside the blade footprint (same footprint test as CarpetWater)
            float VacFactor(float2 mm)
            {
                float f = 1.0;
                if (_ToolCursor.w > 0.5 && abs(_ToolCursor2.w - 6.0) < 0.5)
                {
                    float2 p = mm - _ToolCursor.xy * _CarpetMm.xy;
                    float2 d = _ToolCursor2.xy;
                    float dl = length(d);
                    d = (dl > 1e-4) ? d / dl : float2(1.0, 0.0);
                    float2 perp = float2(-d.y, d.x);
                    float inside = step(abs(dot(p, d)), _ToolCursor.z) * step(abs(dot(p, perp)), _ToolCursor2.z * 0.5);
                    f = lerp(1.0, 0.6, inside);
                }
                return f;
            }

            // foam thickness in mm above the base: steep foot (mask), saturating height, +-lumps, 2.5 mm plinth (keeps fibre tips under the foot)
            float FoamMmAt(float F, float2 mm)
            {
                if (F <= 0.05) return 0.0;                                   // exact (mask and plinth are 0 there), saves the noise taps where a heap is absent
                float mask = FoamMask(F);
                float hm = mask * (_FoamHeight * (1.0 - exp(-F / max(_FoamKnee, 1e-3))) + LumpMm(mm, F)) + 2.5 * smoothstep(0.0, 0.05, mask);
                return max(hm, 0.0) * VacFactor(mm);
            }


            #define EDGE_OUT_MM 45.0   // the carpet mound runs down onto the tile over this distance (the dollop stays gentler than ~1.5 slope)
            #define EDGE_IN_MM  24.0   // the tile's rim heap reaches back over the carpet's edge cells over this distance

            // Rim geometry of a carpet-mm point (Docs/CARPET_EDGE.md): the clamped carpet point, the floor sample point (never
            // nearer than half a floor cell to the rim: the floor cells under the carpet are zero and a bilinear tap at the rim
            // would read half the rim cell), the outside / inside falloffs.
            struct RimInfo
            {
                float2 mmC;      // nearest point inside the carpet rect
                float2 mmF;      // floor sample point: outside, at least half a floor cell from every rim line (per axis); inside, across the nearest rim
                float2 mmF2;     // inside a corner square: across the other rim
                float  dOut;
                float  fall;     // 1 inside, 0 beyond EDGE_OUT_MM outside: the carpet mound running down onto the tile
                float  fallIn;   // 1 outside, 0 beyond EDGE_IN_MM inside: the tile's rim heap reaching back over the rim
                float  fallIn2;  // the same for the other rim of a corner square (0 elsewhere)
                bool   outside;
                bool   nearRim;  // the floor field matters here (every outside point, inside points within EDGE_IN_MM)
            };

            RimInfo Rim(float2 mm)
            {
                RimInfo r;
                r.mmC = clamp(mm, float2(0.0, 0.0), _CarpetMm.xy);
                float2 dv = mm - r.mmC;
                r.dOut = length(dv);
                r.outside = r.dOut > 1e-3;                                   // rounding residue of a vertex on the rim line counts as ON the rim (both meshes must agree there)
                float halfCell = 0.5 * max(_FloorConsts.z, 1e-3);
                float4 dE = float4(r.mmC.x, _CarpetMm.x - r.mmC.x, r.mmC.y, _CarpetMm.y - r.mmC.y);   // distance to each rim (L, R, B, T)
                float dInX = min(dE.x, dE.y), dInY = min(dE.z, dE.w);
                float dIn = r.outside ? 0.0 : min(dInX, dInY);
                r.nearRim = r.outside || (dIn < EDGE_IN_MM);
                r.fall = 1.0 - smoothstep(0.0, EDGE_OUT_MM, r.dOut);
                if (r.outside)
                {
                    // the floor cells under the carpet are zero: keep the tap half a cell clear of every rim line, per axis
                    // (a push along the diagonal alone left one axis inside the zeroed band next to a corner)
                    float2 push = float2(dv.x != 0.0 ? sign(dv.x) * max(abs(dv.x), halfCell) : 0.0,
                                         dv.y != 0.0 ? sign(dv.y) * max(abs(dv.y), halfCell) : 0.0);
                    r.mmF = r.mmC + push;
                    r.mmF2 = r.mmF;
                    r.fallIn = 1.0;
                    r.fallIn2 = 0.0;
                }
                else
                {
                    float2 nX = (dE.x <= dE.y) ? float2(-1.0, 0.0) : float2(1.0, 0.0);
                    float2 nY = (dE.z <= dE.w) ? float2(0.0, -1.0) : float2(0.0, 1.0);
                    bool xFirst = dInX <= dInY;
                    r.mmF  = mm + (xFirst ? nX * (dInX + halfCell) : nY * (dInY + halfCell));
                    r.mmF2 = mm + (xFirst ? nY * (dInY + halfCell) : nX * (dInX + halfCell));
                    r.fallIn  = 1.0 - smoothstep(0.0, EDGE_IN_MM, xFirst ? dInX : dInY);
                    r.fallIn2 = 1.0 - smoothstep(0.0, EDGE_IN_MM, xFirst ? dInY : dInX);   // > 0 only within EDGE_IN_MM of both rims (corner squares)
                }
                return r;
            }

            // Cross-domain foam field at any carpet-mm point: (sigma, F, Fd, Fage) for the shading and the SURFACE height (mm,
            // carpet frame). The surface is the union (max) of two heaps that both cross the rim: the carpet mound (its own
            // textures inside; outside, the rim value running down onto the tile over EDGE_OUT_MM, base lifted to the carpet
            // top where its film shows) and the tile heap (its own textures outside; inside, the rim cell's heap fading over
            // EDGE_IN_MM, seated on the floor, so it shows over the rim only where it is taller than the carpet top + carpet
            // foam). A mound cut by the edge runs down onto the tile, foam heaped against the carpet's side overhangs the rim,
            // and there is never a valley (the sim's bare rim cells) or an artificial ridge along the edge.
            float4 FieldAt(float2 mm, out float surfMm)
            {
                surfMm = 0.0;
            #if defined(_FLOOR_DOMAIN)
                // carpet handed over: there is no mound and no rim to blend against, the whole domain is the floor's own
                // field. Without this the rect would carry the phantom pile top of an invisible carpet (Docs/HANDOVER.md 6).
                if (_CarpetAway > 0.5)
                {
                    float2 fuvA = FloorUv(mm);
                    float4 spA = max(SAMPLE_TEXTURE2D_LOD(_FSoapTex, sampler_linear_clamp, fuvA, 0), 0.0);
                    float hA = max(SAMPLE_TEXTURE2D_LOD(_FFluidTex, sampler_linear_clamp, fuvA, 0).x, 0.0);
                    surfMm = min(hA, 6.0) + 0.3 - 3.0 + FoamMmAt(spA.y, mm);
                    return spA;
                }
            #endif
                RimInfo r = Rim(mm);
                float2 uvC = r.mmC * _CarpetMm.zw;
                float4 spC = max(SAMPLE_TEXTURE2D_LOD(_SoapTex, sampler_linear_clamp, uvC, 0), 0.0);
                float hC = max(SAMPLE_TEXTURE2D_LOD(_FluidTex, sampler_linear_clamp, uvC, 0).x, 0.0);
                float4 fbC = SAMPLE_TEXTURE2D_LOD(_FiberTex, sampler_linear_clamp, uvC, 0);
                float carpetBase = PileTopMm(fbC) + hC;
                float4 spCm = spC * r.fall;                                    // the carpet mound, running down onto the tile
                if (!r.nearRim)
                {
                    surfMm = carpetBase + FoamMmAt(spCm.y, mm);
                    return spCm;
                }
                float2 fuv = FloorUv(r.mmF);
                float4 spF = max(SAMPLE_TEXTURE2D_LOD(_FSoapTex, sampler_linear_clamp, fuv, 0), 0.0) * r.fallIn;   // the tile heap, reaching back over the rim
                if (r.fallIn2 > 0.0)                                         // corner square: the heap across the other rim too
                    spF = max(spF, max(SAMPLE_TEXTURE2D_LOD(_FSoapTex, sampler_linear_clamp, FloorUv(r.mmF2), 0), 0.0) * r.fallIn2);
                float hF = max(SAMPLE_TEXTURE2D_LOD(_FFluidTex, sampler_linear_clamp, fuv, 0).x, 0.0);
                float floorBase = min(hF, 6.0) + 0.3 - 3.0;
                // outside, the carpet mound's base follows the carpet top where its film shows and drops to the tile where it does not;
                // blended in over the first 6 mm so the surface stays continuous at the rim for thin films too
                float lift = r.outside ? lerp(1.0, smoothstep(0.03, 0.12, spC.y), smoothstep(0.0, 6.0, r.dOut)) * r.fall : 1.0;
                float sC = lerp(floorBase, carpetBase, lift) + FoamMmAt(spCm.y, mm);
                float sF = floorBase + FoamMmAt(spF.y, mm);
                float hs = max(6.0 - abs(sC - sF), 0.0) / 6.0;               // smooth max, 6 mm: foam fills the crease where the spill meets the heap
                surfMm = max(sC, sF) + hs * hs * 1.5;
                float show = r.outside ? 1.0 : smoothstep(-3.0, 1.0, sF - sC);   // inside, the tile heap's film / caps show only where it stands above the carpet surface
                return max(spCm, spF * show);
            }

            // light version: foam F only (vertex early-outs); must agree with FieldAt(...).y
            float FAt(float2 mm)
            {
            #if defined(_FLOOR_DOMAIN)
                if (_CarpetAway > 0.5) return max(SAMPLE_TEXTURE2D_LOD(_FSoapTex, sampler_linear_clamp, FloorUv(mm), 0).y, 0.0);
            #endif
                RimInfo r = Rim(mm);
                float F = max(SAMPLE_TEXTURE2D_LOD(_SoapTex, sampler_linear_clamp, r.mmC * _CarpetMm.zw, 0).y, 0.0) * r.fall;
                if (r.nearRim) F = max(F, max(SAMPLE_TEXTURE2D_LOD(_FSoapTex, sampler_linear_clamp, FloorUv(r.mmF), 0).y, 0.0) * r.fallIn);
                return F;
            }

            // surface height (mm, carpet frame) at a carpet-mm point
            float SurfAt(float2 mm)
            {
                float s = 0.0;
                FieldAt(mm, s);
                return s;
            }

            // mesh height (mm, carpet frame, without FRAME_MM) a foam vertex gets at a carpet-mm point
            float RimVertexHeight(float2 p)
            {
                float s = 0.0;
                float4 sp = FieldAt(p, s);
                return s + 0.25 + _CapLiftMm * FoamMask(sp.y);
            }

            // one tap of the silhouette exit test: 1 where the eye ray (at height rayH there) is clear of the foam, 0 where the
            // foam surface is above it; a tap without a film (F under the film's own alpha threshold) counts as clear, the pile
            // or tile behind the mound is not part of it
            float ExitTap(float2 p, float rayH)
            {
                float s = 0.0;
                float4 sp = FieldAt(p, s);
                float noFilm = 1.0 - smoothstep(0.03, 0.12, sp.y);
                return max(noFilm, saturate((rayH - s - 1.0) / 2.0));
            }

            // shadow march distances (mm) toward the sun; the transition widens with distance so occluders never band
            static const float kShadowD[8] = { 4.0, 8.0, 14.0, 22.0, 32.0, 44.0, 58.0, 74.0 };

            float4 FoamHash4(float2 p)
            {
                float4 p4 = frac(float4(p.xyxy) * float4(443.897, 441.423, 437.195, 444.129));
                p4 += dot(p4, p4.wzxy + 19.19);
                return frac((p4.xxyz + p4.yzzw) * p4.zywx);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 mm         : TEXCOORD0;   // carpet mm
                float3 posWS      : TEXCOORD1;
                float2 near       : TEXCOORD2;   // (foamNear, shadowNear): max F around / toward the sun, fragment early-outs
            };

            // ---- vertex: lift the grid to base + foam + 0.25 mm + cap lift ----
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
            #if defined(_FLOOR_DOMAIN)
                float2 mm = pos.xz * 1000.0 + CARPET_CENTRE_MM;
            #else
                float2 mm = IN.uv * _CarpetMm.xy;
            #endif
                float surfMm = 0.0;
                float4 sp = FieldAt(mm, surfMm);
                float F = sp.y;
                float mask = FoamMask(F);
                float capLift = _CapLiftMm * mask;
                float liftMm = surfMm + 0.25 + capLift;
            #if !defined(_FLOOR_DOMAIN)
                // rim vertices take the height the coarser floor foam grid rasterizes along the rim (linear between ITS vertices,
                // which lie on the rim lines): the two meshes then share one rim edge, so the ray-traced caps and the film do not
                // step at the seam (Docs/CARPET_EDGE.md). Corner vertices coincide with floor vertices, either branch is exact.
                bool onRimX = IN.uv.x <= 0.0 || IN.uv.x >= 1.0;
                bool onRimY = IN.uv.y <= 0.0 || IN.uv.y >= 1.0;
                if (onRimX || onRimY)
                {
                    float pitch = max(_FloorFoamPitchMm, 1e-3);
                    float2 g = (mm - _FloorRect.xy) / pitch;
                    float2 g0 = floor(g + 1e-3);
                    float2 fr = saturate(g - g0);
                    float2 pa = onRimY ? float2(_FloorRect.x + g0.x * pitch, mm.y) : float2(mm.x, _FloorRect.y + g0.y * pitch);
                    float2 pb = onRimY ? pa + float2(pitch, 0.0) : pa + float2(0.0, pitch);
                    liftMm = lerp(RimVertexHeight(pa), RimVertexHeight(pb), onRimY ? fr.x : fr.y);
                }
            #endif
                pos.y = (liftMm + FRAME_MM) * 0.001;

                float fn = F;
                fn = max(fn, FAt(mm + float2(8.0, 0.0)));
                fn = max(fn, FAt(mm - float2(8.0, 0.0)));
                fn = max(fn, FAt(mm + float2(0.0, 8.0)));
                fn = max(fn, FAt(mm - float2(0.0, 8.0)));

                float3 Ld = normalize(GetMainLight().direction);
                float2 ldir = normalize(Ld.xz + float2(1e-5, 0.0));          // toward the sun, in mm (x -> u, z -> v)
                float sn = 0.0;
                sn = max(sn, FAt(mm + ldir * 12.0));
                sn = max(sn, FAt(mm + ldir * 26.0));
                sn = max(sn, FAt(mm + ldir * 44.0));
                sn = max(sn, FAt(mm + ldir * 70.0));

                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.posWS = TransformObjectToWorld(pos);
                OUT.mm = mm;
                OUT.near = float2(fn, sn);
                return OUT;
            }

            // ---- fragment ----
            // Cull Off and no back-face branch: on a grazing ramp (the spill over the carpet edge) the two triangles of a
            // quad can face opposite ways, and the shading only depends on the height field, never on the triangle.
            half4 frag(Varyings IN) : SV_Target
            {
                // pixel footprint on the ground (mm per pixel) from the view distance, NOT from screen derivatives: derivatives are
                // constant per triangle, and on steep coarse quads (the spill ramp, ridge fronts) the two triangles of a quad differ,
                // which toggled the small-bubble layer and the edge softness along every quad diagonal (a comb pattern)
                float2 mm = IN.mm;
                float3 camWS = GetCameraPositionWS();
                float3 toCam = camWS - IN.posWS;
                float distMm = length(toCam) * 1000.0;
                float3 V = toCam / max(length(toCam), 1e-4);
                float pxAngle = 2.0 / (abs(UNITY_MATRIX_P._m11) * _ScreenParams.y);      // radians per pixel (vertical)
                float mmPerPx = max(distMm * pxAngle / max(V.y, 0.25), 1e-3);
                s_lumpLod = log2(max(mmPerPx / 0.234, 1.0));                 // 20x lump tile: 0.234 mm per texel
                float microLod = log2(max(mmPerPx / 0.094, 1.0));            // 50x froth tile: 0.094 mm per texel

                if (IN.near.x < 0.005 && IN.near.y < 0.05) discard;
            #if defined(_FLOOR_DOMAIN)
                if (_CarpetAway < 0.5 && InsideCarpetMm(mm, 0.0)) discard;   // the carpet surface draws the rect itself (unless it is away)
                float2 fuv = FloorUv(mm);
                if (fuv.x <= 0.0 || fuv.y <= 0.0 || fuv.x >= 1.0 || fuv.y >= 1.0) discard;
            #endif

                float surfMm = 0.0;
                float4 sp = FieldAt(mm, surfMm);
                float F = sp.y, Fd = sp.z;
                float mask = FoamMask(F);
                float foamMm = FoamMmAt(F, mm);                              // foam thickness from F (colour, AO); the surface itself is the field union
                float capLift = _CapLiftMm * mask;

                Light L = GetMainLight();
                float3 Ld = normalize(L.direction);
                float3 Lc = L.color;

                // ---- macro normal from the height field in mm (never from noise derivatives) ----
                float cellMm = FOAM_CELLMM;
                float hR = SurfAt(mm + float2(cellMm, 0.0)), hL = SurfAt(mm - float2(cellMm, 0.0));
                float hT = SurfAt(mm + float2(0.0, cellMm)), hB = SurfAt(mm - float2(0.0, cellMm));
                float2 grad = float2(hR - hL, hT - hB) / (2.0 * cellMm);    // d(height)/d(mm), UNCLAMPED: bubbles are placed by extrapolating
                                                                             // this slope, so it must match the real surface even on steep ramps
                                                                             // (a clamp smeared every sphere into a pill along the spill ramp)
                float3 Ng = normalize(float3(-grad.x, 1.0, -grad.y));

                // ---- self / cast shadow: 8 taps toward the sun; occluded when the surface there rises faster than the sun ray ----
                float2 ldir = normalize(Ld.xz + float2(1e-5, 0.0));
                float tanElev = 0.65 * Ld.y / max(length(Ld.xz), 1e-3);     // a lower effective sun for the foam: the scene has no shadow maps, the crescent must read
                float occ = 0.0;
                if (IN.near.y > 0.03)
                {
                    [loop]                                                   // not unrolled: 8 inlined FieldAt made the compiled shader huge (a 30 s compile stall preceded a GPU hang)
                    for (int t = 0; t < 8; t++)
                    {
                        // horizon test: the slope from here to the tap vs the sun slope, with a wide soft transition
                        // (a ramp rising at about the sun slope must give a smooth partial shadow, never per-tap flicker)
                        float d = kShadowD[t];
                        float sT = 0.0;
                        float4 spT = FieldAt(mm + ldir * d, sT);
                        float film = smoothstep(0.03, 0.12, spT.y);         // only foam occludes: the carpet block beside bare tile is not part of the mound (the scene has no shadow maps at all)
                        float slopeT = (sT - surfMm) / d;
                        occ = max(occ, film * saturate((slopeT - tanElev) / 0.35));
                    }
                }
                float shadow = 1.0 - 0.7 * occ;                              // self-shadow on the foam is partial (it is translucent)

                // ---- film alpha: thin foam is a translucent film, domes stay solid; below F ~0.05 nothing shows (no wide smudge) ----
                float alphaM = (1.0 - exp(-F / max(_AlphaKnee, 1e-3))) * smoothstep(0.03, 0.12, F);
                float shadowA = _ShadowStrength * occ;
                if (alphaM < 0.01)
                {
                    // skirt: only the cast shadow of a nearby mound darkens the carpet / water / tile
                    if (shadowA < 0.01) discard;
                    return half4(0.0, 0.0, 0.0, shadowA);
                }

                // ---- bubble caps: packed spheres in two cell sizes, exact ray-sphere against the eye ray ----
                // local frame: x = mm.x, y = up (mm), z = mm.y; the film plane is y = 0 at this pixel, tilted by grad
                // the ray starts at the RASTERIZED point, not at the analytic surface: between vertices the flat triangle
                // deviates from the curved height field (several mm on the spill ramp), and a per-triangle offset of the
                // ray origin smeared every sphere into diagonal streaks along the quad diagonals
                float p0y = IN.posWS.y * 1000.0 - surfMm;                    // height of this fragment above the analytic film (carpet frame)
                float3 P0 = float3(mm.x, p0y, mm.y);
                float vn = max(V.y - dot(grad, V.xz), 0.25);                 // ray climb rate against the TILTED film plane (normal (-grad.x, 1, -grad.y));
                float sFilm = -p0y / vn;                                     // floored: at grazing angles the tangent plane runs away from the curved surface
                float sBest = -1e9, sBestEff = -1e9, s2 = -1e9;
                float3 Cbest = float3(0.0, 0.0, 0.0);
                float rBest = 0.0, discBest = 0.0, wBest = 0.0;
                float ageGrow = 1.0 + 0.25 * saturate(sp.w / 25.0);       // bubbles coarsen with age (grid never rescales)
                [unroll]
                for (int k = 0; k < 2; k++)
                {
                    float cell = (k == 0) ? _BubbleMm.x : _BubbleMm.y;
                    float wK = smoothstep(2.5, 5.0, cell / mmPerPx);        // small bubbles only when they cover >= 5 px
                    if (wK > 0.001)
                    {
                        float2 id0 = floor(mm / cell);
                        [loop]                                               // 3x3 search kept dynamic: 18 inlined FieldAt (one per seat) blew the shader up (see the shadow loop)
                        for (int j = -1; j <= 1; j++)
                        {
                            [loop]
                            for (int i = -1; i <= 1; i++)
                            {
                                float2 id = id0 + float2(i, j);
                                float4 hs = FoamHash4(id + 57.0 * k + 0.37);
                                float2 c = (id + 0.5 + (hs.xy - 0.5) * 0.6) * cell;
                                float th = 0.15 + 0.35 * hs.w;
                                // sphere height: every pixel must place a given sphere at the SAME height, otherwise it smears
                                // along curved ramps (the spill over the carpet edge, ridge fronts), so each sphere is seated on
                                // the real field height under its centre, not on a local tangent plane
                                float surfC = 0.0;
                                float4 spc = FieldAt(c, surfC);
                                float Fc = spc.y;
                                float tilt = surfC - surfMm;
                                float grow = smoothstep(th, th + 0.25, Fc);          // foam at the bubble centre: grow / pop per bubble
                                float r = cell * min(0.45 + 0.2 * hs.z, 0.6) * ageGrow * grow;
                                if (r > 0.5)
                                {
                                    float cy = -0.35 * r + tilt;
                                    float3 C = float3(c.x, cy, c.y);
                                    float3 oc = P0 - C;
                                    float b = dot(V, oc);
                                    float disc = b * b - (dot(oc, oc) - r * r);
                                    if (disc > 0.0)
                                    {
                                        float s = -b + sqrt(disc);                   // larger root = the sphere point nearest the eye
                                        float sEff = s - (1.0 - wK) * 1000.0;        // a fading-in small bubble never wins over a full dome
                                        if (sEff > sBestEff) { s2 = sBest; sBestEff = sEff; sBest = s; Cbest = C; rBest = r; discBest = disc; wBest = wK; }
                                        else if (s > s2 && wK > 0.5) { s2 = s; }
                                    }
                                }
                            }
                        }
                    }
                }
                bool hitDome = (rBest > 0.0) && (sBest > sFilm);
                float cov = 0.0, hk = 1.0, seam = 1.0, rPx = 0.0;
                float3 Nd = Ng;
                if (hitDome)
                {
                    cov = wBest * saturate(discBest / (2.0 * rBest * mmPerPx));   // ~pixels inside the silhouette -> 1-px AA
                    float3 hit = P0 + V * sBest;
                    Nd = normalize(hit - Cbest);                             // a sphere keeps its own normal on any slope (leaning it with Ng sheared every highlight on ramps)
                    hk = saturate((hit.y - Cbest.y - 0.35 * rBest) / (0.65 * rBest));          // 0 at the film level of THIS sphere, 1 at its cap top (consistent with the clamped tilt)
                    seam = (s2 > -1e8) ? smoothstep(0.0, 0.3 * rBest, sBest - s2) : 1.0;  // second cap close behind -> violet seam
                    rPx = rBest / mmPerPx;
                }

                // micro froth: mipmapped Worley (noise.g), two 1-texel taps at an explicit mip (no implicit derivatives after the discards)
                float2 nuv = mm / 1200.0 * 50.0;
                float g0 = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_trilinear_repeat, nuv, microLod).g;
                float gx = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_trilinear_repeat, nuv + float2(1.0 / 256.0, 0.0), microLod).g;
                float gz = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_trilinear_repeat, nuv + float2(0.0, 1.0 / 256.0), microLod).g;
                float3 micro = float3(-(gx - g0), 0.0, -(gz - g0)) * 1.5 * saturate(3.0 / max(mmPerPx, 1e-3) - 0.5);

                // silhouette exit test: behind this pixel (away from the eye) the eye ray leaves the mound where the surface
                // falls away faster than the ray descends; there only bubble caps may draw, so the outline is made of caps.
                // The ray must be clear of the foam at EVERY tap out to 40 mm (min): a local back-slope that the film catches
                // again a little further on (a lump, the ridge along the carpet rim) is not a silhouette - the old 2-tap max
                // test punched pinholes into every such slope. Where a tap has no film, the ray is out by definition.
                float tanCam = V.y / max(length(V.xz), 0.05);
                float2 vxz = normalize(V.xz + float2(1e-5, 0.0));
                float exitW = 1.0;
                float rayAbs = IN.posWS.y * 1000.0;
                exitW = min(exitW, ExitTap(mm - vxz * 6.0,  rayAbs - 6.0 * tanCam));
                exitW = min(exitW, ExitTap(mm - vxz * 12.0, rayAbs - 12.0 * tanCam));
                exitW = min(exitW, ExitTap(mm - vxz * 24.0, rayAbs - 24.0 * tanCam));
                exitW = min(exitW, ExitTap(mm - vxz * 40.0, rayAbs - 40.0 * tanCam));

                float3 N = normalize(lerp(normalize(Ng + micro), Nd, cov));
                float crease = lerp(1.0, lerp(0.45, 1.0, hk) * (0.6 + 0.4 * seam), cov);
                float gap = lerp(0.75, 1.0, cov);

                // ---- colour: hue-preserving, shadows rotate to violet. Pink from mid height up and on thin films; violet only at the foot of a thick mound ----
                float hRel = saturate(foamMm / max(_FoamHeight, 1e-3));
                float pinkness = max(smoothstep(0.05, 0.6, hRel), 0.7 * (1.0 - mask));
                float3 alb = lerp(_FoamShadowColor.rgb, _FoamColor.rgb, pinkness);
                float dirtF = saturate(Fd / (F * 0.04 + 1e-3)) * mask;
                alb = lerp(alb, _FoamDirtColor.rgb, 0.85 * dirtF);
                float wrap = saturate((dot(N, Ld) + _WrapK) / (1.0 + _WrapK)) * shadow;
                float form = lerp(1.0 - _FormK, 1.0, saturate(dot(Ng, Ld)));
                float3 albSh = lerp(_FoamShadowColor.rgb, alb, wrap);
                float baseAO = lerp(0.55, 1.0, saturate(foamMm / 12.0));
                float3 col = (alb * Lc * _SunScale * wrap * form + albSh * SampleSH(N) * _AmbientScale) * crease * gap * baseAO;

                float3 H = normalize(Ld + V);
                float ndh = saturate(dot(N, H));
                float ndv = saturate(dot(N, V));
                float3 specTint = lerp(alb, float3(1.0, 1.0, 1.0), 0.5);                      // pinkish-white sheen, not a white speckle
                float3 spec = pow(ndh, 16.0) * 0.35 * Lc * specTint * cov * saturate(rPx / 3.0) * (1.0 - dirtF);
                spec += pow(ndh, 120.0) * 1.0 * Lc * cov * step(_SparklePx, rPx) * (1.0 - dirtF);   // sparkles (bloom) only on bubbles >= _SparklePx
                float f3 = pow(1.0 - ndv, 3.0);
                float3 rim = f3 * _FoamRimColor.rgb * 0.35 * (1.0 - 0.6 * dirtF)
                           + f3 * saturate(dot(N, _RimDir.xyz)) * _RimColor.rgb;
                col += spec + rim;

                float alpha = saturate(alphaM * lerp(alphaM, 1.6, cov)) * lerp(1.0, cov, exitW);

                // ---- inspection lamp (Docs/HIGHLIGHTS.md): the mound dims and glows with the cell under it (its own foam dirt is part
                //      of that cell's measure), and turns translucent so the lit pile / tile under it shows. Explicit-LOD taps only:
                //      this runs after discards. ----
                if (HL_Active())
                {
                    float hlD, hlW;
                    float2 hlCut = float2(0.0, 0.0);                             // the tile has no bar and no cut
                #if defined(_FLOOR_DOMAIN)
                    float2 fuvH = FloorUv(mm);
                    float4 flH = SAMPLE_TEXTURE2D_LOD(_FFluidTex, sampler_linear_clamp, fuvH, 0);
                    float4 dpH = SAMPLE_TEXTURE2D_LOD(_FDepositTex, sampler_linear_clamp, fuvH, 0);
                    float4 spH = SAMPLE_TEXTURE2D_LOD(_FSoapTex, sampler_linear_clamp, fuvH, 0);
                    hlD = HL_FloorDirt(dpH, flH, spH);
                    hlW = HL_FloorWet(flH, dpH);
                #else
                    float2 uvH = saturate(mm * _CarpetMm.zw);                   // the spill over the rim takes the rim cell
                    float4 flH = SAMPLE_TEXTURE2D_LOD(_FluidTex, sampler_linear_clamp, uvH, 0);
                    float4 fbH = SAMPLE_TEXTURE2D_LOD(_FiberTex, sampler_linear_clamp, uvH, 0);
                    float4 diH = SAMPLE_TEXTURE2D_LOD(_DirtTex, sampler_linear_clamp, uvH, 0);
                    float4 spH = SAMPLE_TEXTURE2D_LOD(_SoapTex, sampler_linear_clamp, uvH, 0);
                    hlD = HL_CarpetDirt(diH, flH, spH);
                    hlW = HL_CarpetWet(fbH, flH, _SimConsts.x);
                    hlCut = HL_CarpetCut();                                      // the carpet follows the HUD bars
                #endif
                    HLTerms ht = HL_Terms(hlD, hlW, mm, 0.8, hlCut);
                    col = HL_Dim(col, ht.dimAmt) + ht.glow;
                    alpha *= lerp(1.0, 0.55, ht.dimAmt);
                }

                if (alpha < 0.01 && shadowA < 0.01) discard;
                float aOut = alpha + shadowA * (1.0 - alpha);                   // the cast shadow darkens what shows through a thin film
                return half4(col * alpha, aOut);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

