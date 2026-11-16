Shader "CarpetCleaning/CarpetWater"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (0.62, 0.68, 0.76, 1)
        _SiltColor ("Silt Color (suspended dirt albedo)", Color) = (0.26, 0.18, 0.10, 1)
        _AbsorbClean ("Absorb Clean (per mm)", Color) = (0.02, 0.03, 0.05, 1)
        _AbsorbDirty ("Absorb Dirty (extra per mm at full murk)", Color) = (0.15, 0.30, 0.60, 1)
        _MurkDensity ("Murk Density (silt extinction per mm)", Float) = 0.8
        _MurkCRef ("Murk Slurry Ref (dirt per mm of water at full murk)", Float) = 0.4
        _MurkCarpet ("Murk from Carpet Dirt (weight of the pile dirt)", Float) = 1.0
        _RefrStrength ("Refraction Strength", Float) = 0.02
        _PileHeight ("Pile Height (m)", Float) = 0.02
        _FilmLo ("Film Lo mm: below = water held in the pile, drawn by CarpetFibers", Float) = 0.3
        _FilmHi ("Film Hi mm: full free surface", Float) = 0.9
        _Drape ("Drape mm: tuft relief under shallow water (pools between tufts)", Float) = 0.5
        _DrapeMm ("Drape feature size mm", Float) = 6
        _DrapeTilt ("Drape tilt (meniscus curvature over the relief)", Float) = 1.5
        _ShallowSky ("Shallow sky: share of sky + rim sheen a sheet below 2.5 mm keeps (the tufts block the reflected rays)", Float) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "HighlightCommon.hlsl"
            #include "RoomReflection.hlsl"

            // ---- globals (set by CarpetSimulation / CarpetRenderer; OUTSIDE the material cbuffer) ----
            TEXTURE2D(_FluidTex);
            TEXTURE2D(_SoapTex);
            TEXTURE2D(_DirtTex);
            TEXTURE2D(_CrustTex);
            TEXTURE2D(_FiberTex);
            TEXTURE2D(_BedTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_linear_clamp);
            SAMPLER(sampler_linear_repeat);

            float4 _SimTexel;     // (inverse width, inverse height, width, height)
            float4 _CarpetMm;     // (width mm, height mm, inverse width, inverse height)
            float4 _SimConsts;    // (W_CAP, PILE_H, F_MAX, H_MAX)
            float4 _SimConsts2;   // (BED_PILE, 0, 0, 0): K_Flux's head = bed + BED_PILE (pileTop - PILE_H) + h
            float4 _RimDir;       // world dir TO the rim light
            float4 _RimColor;
            float4 _ToolCursor;   // (u, v, radiusMm, active)
            float4 _ToolCursor2;  // (dir.x, dir.y, lengthMm, toolId)

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
                half4 _SiltColor;
                half4 _AbsorbClean;
                half4 _AbsorbDirty;
                float _MurkDensity;
                float _MurkCRef;
                float _MurkCarpet;
                float _RefrStrength;
                float _PileHeight;
                float _FilmLo;
                float _FilmHi;
                float _Drape;
                float _DrapeMm;
                float _DrapeTilt;
                float _ShallowSky;
            CBUFFER_END

            // ---- inlined helpers ----
            float hash1(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // value noise, one octave, -1..1
            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash1(i);
                float b = hash1(i + float2(1, 0));
                float c = hash1(i + float2(0, 1));
                float d = hash1(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 2.0 - 1.0;
            }

            // 2-octave noise, -1..1
            float snoise2(float2 p)
            {
                return vnoise(p) * 0.667 + vnoise(p * 2.03 + 17.1) * 0.333;
            }

            // the tuft relief (Docs/WET_LOOK.md 5): 2-octave value noise whose finer octave is weighted by oct2 (the pixel-footprint
            // band limit, 0 once its cell is under ~2 px)
            float TuftNoise(float2 p, float oct2)
            {
                return vnoise(p) * 0.667 + vnoise(p * 2.03 + 17.1) * 0.333 * oct2;
            }

            // pile top height in mm from a fiber sample (f, w) -- identical to 3.1
            float PileTopMm(float4 fb)
            {
                return _SimConsts.y * (1.0 - 0.55 * fb.z) * (1.0 - 0.25 * fb.w);
            }

            // hydraulic head in mm at a UV, exactly K_Flux's BedHeadAt + h (CarpetSim.compute). The pile top (pileTop + h, used
            // for the vertex height) steps ~8 mm at a squeegee / wet-vac lane edge and read as a 50-degree mirror facet; the head
            // steps only BED_PILE x that. It is still not level there: a pressed lane keeps drawing seep water in because the heads
            // differ, so at a lane's far wall the head still steps ~2-3 mm over one texel. The fragment therefore limits the
            // slope while the sheet is shallower than the tufts (Docs/WET_LOOK.md 5).
            float HeadMm(float2 uv)
            {
                float4 fl = SAMPLE_TEXTURE2D(_FluidTex, sampler_linear_clamp, uv);
                float4 fb = SAMPLE_TEXTURE2D(_FiberTex, sampler_linear_clamp, uv);
                float bed = SAMPLE_TEXTURE2D(_BedTex, sampler_linear_clamp, uv).x;
                return bed + _SimConsts2.x * (PileTopMm(fb) - _SimConsts.y) + fl.x;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                float  surfMm     : TEXCOORD2;
            };

            // ---- vertex: lift the grid to pile top + water (the foam mound is its own surface, CarpetFoam.shader) ----
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 fl = SAMPLE_TEXTURE2D_LOD(_FluidTex, sampler_linear_clamp, IN.uv, 0);
                float4 fb = SAMPLE_TEXTURE2D_LOD(_FiberTex, sampler_linear_clamp, IN.uv, 0);
                float pileMm = PileTopMm(fb);
                float h = fl.x;
                float3 pos = IN.positionOS.xyz;
                pos.y = (pileMm + h) * 0.001;
                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.posWS = TransformObjectToWorld(pos);
                OUT.uv = IN.uv;
                OUT.surfMm = pileMm + h;
                return OUT;
            }

            // ---- fragment ----
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float4 fl = SAMPLE_TEXTURE2D(_FluidTex, sampler_linear_clamp, uv);
                float4 sp = SAMPLE_TEXTURE2D(_SoapTex,  sampler_linear_clamp, uv);
                float4 fb = SAMPLE_TEXTURE2D(_FiberTex, sampler_linear_clamp, uv);
                float4 di = SAMPLE_TEXTURE2D(_DirtTex,  sampler_linear_clamp, uv);   // crust, mud, stain, grit (0..1)
                float h    = fl.x;
                float2 flow = fl.yz;
                float w    = fb.w;                                                // (foam F from _SoapTex is no longer used here: the foam is its own surface)
                float C    = fl.w / (h + w * _SimConsts.x + 1e-3);

                // --- murk: how dirty is this water column? ---
                // Potential slurry concentration (dirt units per mm of water held in the cell): the dissolved slurry C plus
                // the dirt of the pile the water stands on (mud 1, crust 0.5 - it hydrates to mud first -, stain 0.5, grit 0.5)
                // over the same water volume. Mud dissolving into slurry is murk-neutral, inflow dilutes, wet-vac extraction
                // keeps the ratio; water over a dirty carpet reads dirty at once, water over a clean carpet stays clear.
                float waterTot = h + w * _SimConsts.x + 1e-3;
                float pileDirt = (di.y * 1.0 + di.x * 0.5 + di.z * 0.5 + di.w * 0.5) * _MurkCarpet;
                float murk = saturate((C + pileDirt / waterTot) / max(_MurkCRef, 1e-3));

                // --- Beer-Lambert: the water absorbs per channel (blue most when dirty -> brown transmission),
                //     suspended silt extinguishes (scalar) and in-scatters its lit brown albedo ---
                float pathMm = 2.0 * h + 0.4 * murk;                                // in + out; + silt settled on the fibres (only drawn from _FilmLo, below)
                float3 sigA = _AbsorbClean.rgb + _AbsorbDirty.rgb * murk;
                float3 Ta = exp(-sigA * pathMm);
                float  Ts = exp(-_MurkDensity * murk * pathMm);

                // --- film vs free surface (Docs/WET_LOOK.md 5). Water below _FilmLo (0.3 mm = K_Reduce's "pooled" threshold) is
                //     held in the pile: CarpetFibers draws it as wet, dark fibres (wetV has saturate(h/0.3)) tinted by its slurry
                //     (C), so this layer skips it. Drawn here as well it laid a flat grey mirror with bright edges over every damp
                //     pressed lane (and its murk, a brown one: it is already in the fibres' C tint). Fades in over _FilmLo.._FilmHi
                //     (the drape jitter below lets a 0.15-0.3 mm film show in sparse pools; nothing under 0.15 mm is drawn).
                //     Shallow water drapes over the tuft tips: below ~2.5 mm the tallest tufts break through and the surface
                //     follows their relief, so a 0.5-1 mm sheet reads as water pooled between wet tufts, not a level pane.
                //     The relief is band-limited by the pixel footprint (fw = drape cells per pixel): an octave whose cell is
                //     under ~2 px fades out instead of aliasing into grain that depends on the resolution and crawls when the
                //     carpet moves. The jitter only acts from 0.5 _FilmLo up, so held films stay the fibres' alone.
                float drapeS = max(_DrapeMm, 0.5);
                float2 pT    = uv * _CarpetMm.xy / drapeS;
                float fw     = max(length(ddx(pT)), length(ddy(pT)));
                float bl1    = saturate(2.0 - 4.0 * fw);                        // octave 1 (cell _DrapeMm)
                float bl2    = saturate(2.0 - 4.0 * 2.03 * fw);                 // octave 2 (half the cell)
                float drape  = _Drape * (1.0 - smoothstep(_FilmHi, 2.5, h));    // mm of relief; deep water is level
                float tuft   = TuftNoise(pT, bl2);                              // -1..1
                float freeW  = smoothstep(_FilmLo, _FilmHi, h + drape * tuft) * smoothstep(0.5 * _FilmLo, _FilmLo, h);
                float alphaW = (1.0 - exp(-h / 0.15)) * freeW;
                if (alphaW < 0.01) discard;
                // contact band where the coverage fades in (not the whole sheet: that brightened every lane's seep water), plus
                // the glow of a sheet that has just closed over the tuft tips (1.1-1.9 mm, peak ~0.5 at 1.45 mm)
                float meniscus = 4.0 * freeW * (1.0 - freeW)
                               + smoothstep(_FilmHi + 0.2, _FilmHi + 0.6, h) * (1.0 - smoothstep(_FilmHi, _FilmHi + 1.0, h));

                // --- surface normal: central differences of the head (bed + pile term + h) over one sim texel ---
                float2 tx = float2(_SimTexel.x, 0.0);
                float2 tz = float2(0.0, _SimTexel.y);
                float dsx = (HeadMm(uv + tx) - HeadMm(uv - tx)) / (2.0 * _CarpetMm.x * _SimTexel.x);
                float dsz = (HeadMm(uv + tz) - HeadMm(uv - tz)) / (2.0 * _CarpetMm.y * _SimTexel.y);
                // a sheet shallower than the tufts cannot hold a steep free surface: the head step at a pressed lane's wall lies
                // inside the pile (0.3-2.5 mm of water there would otherwise read as a 20-degree facet that catches the sun in
                // the near half). Limit the slope to ~5 degrees up to _FilmHi, free from 2.5 mm (a real puddle's rim).
                float2 g = float2(dsx, dsz);
                float gMax = lerp(0.08, 4.0, smoothstep(_FilmHi, 2.5, h));
                g *= min(1.0, gMax / max(length(g), 1e-4));

                // --- flow-driven noise ripples (only where there is water) ---
                float rip = (snoise2(uv * 180.0 + flow * _Time.y * 0.02) + snoise2(uv * 310.0 - _Time.y * 0.7))
                            * 0.02 * saturate(length(flow) / 60.0 + 0.3) * saturate(h / 0.5);

                // --- wet-vacuum cursor feedback: suction look inside the blade footprint ---
                float3 suck = float3(0, 0, 0);
                if (_ToolCursor.w > 0.5 && abs(_ToolCursor2.w - 6.0) < 0.5)
                {
                    float2 p = (uv - _ToolCursor.xy) * _CarpetMm.xy;              // mm
                    float2 d = _ToolCursor2.xy;
                    float dl = length(d);
                    d = (dl > 1e-4) ? d / dl : float2(1, 0);
                    float2 perp = float2(-d.y, d.x);
                    float inside = step(abs(dot(p, d)), _ToolCursor.z) * step(abs(dot(p, perp)), _ToolCursor2.z * 0.5);
                    rip *= lerp(1.0, 3.0, inside);
                    float pl = length(p);
                    float3 toC = (pl > 1e-3) ? float3(-p.x, 0.0, -p.y) / pl : float3(0, 0, 0);
                    suck = toC * 0.15 * inside;
                }

                float3 N = normalize(float3(-g.x + rip, 1.0, -g.y + rip) + suck);
                N = lerp(float3(0, 1, 0), N, saturate(h / 0.5));                 // films are flat
                float3 Nrefr = N;                                                 // the refraction ignores the drape (below)
                // drape relief (height drape * tuft mm): slope from one-sided differences of the tuft noise, band-limited (bl1)
                float2 tg = float2(TuftNoise(pT + float2(0.25, 0.0), bl2), TuftNoise(pT + float2(0.0, 0.25), bl2)) - tuft;
                tg *= drape * _DrapeTilt / (0.25 * drapeS) * bl1;
                N = normalize(N + float3(-tg.x, 0.0, -tg.y));

                // --- refraction through the opaque texture (fibers): from the level / flow normal only, so the drape tilt breaks
                //     up the sheen but never displaces the pile under the water (that scrambled the medallions into grain) ---
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float2 refr = clamp(Nrefr.xz * _RefrStrength * saturate(h / 3.0), -0.012, 0.012);
                float3 scene = SampleSceneColor(screenUV + refr);

                Light L = GetMainLight();
                float3 siltLit = _SiltColor.rgb * (L.color * 0.75 + SampleSH(float3(0.0, 1.0, 0.0)));
                float3 body = scene * Ta * Ts + siltLit * (1.0 - Ts);

                // --- lighting ---
                float3 V = normalize(GetCameraPositionWS() - IN.posWS);
                float3 Ld = normalize(L.direction);
                float3 H = normalize(Ld + V);
                float ndv = saturate(dot(N, V));
                // specular occlusion (Docs/WET_LOOK.md 5): a sheet shallower than the tuft relief sits between the tufts, whose
                // tips catch most of its reflected rays, so it keeps only _ShallowSky of the sky and rim sheen; a level
                // puddle (> 2.5 mm) reflects the whole sky. The key-light glints stay: they are what makes wet pile look wet.
                // Seep water just over _FilmHi (0.6-1.3 mm along a lane wall) sits lower still between the tufts: half of that
                // share there, and a partly covered pool (freeW < 1) keeps freeW^2 of it, so a thin sheet never lays a grey-blue
                // veil over the dark wet pile.
                float shallowSky = _ShallowSky * lerp(0.15, 1.0, smoothstep(_FilmHi, _FilmHi + 0.6, h));
                float envOcc = lerp(shallowSky, 1.0, smoothstep(_FilmHi, 2.5, h)) * freeW * freeW;
                float fres = (0.02 + 0.98 * pow(1.0 - ndv, 5.0)) * envOcc;
                float ndh = saturate(dot(N, H));
                float spec = pow(ndh, 300.0) * 2.0 + pow(ndh, 40.0) * 0.15;
                float3 rim = pow(1.0 - ndv, 3.0) * saturate(dot(N, _RimDir.xyz)) * _RimColor.rgb * envOcc;
                float3 water = body * (1.0 - fres) + fres * _SkyColor.rgb + L.color * spec + rim + meniscus * 0.2 * body;   // contact-line brightening keeps the water's own hue (a flat +0.12 grey haloed every film)
                // the room it mirrors (RoomReflection.hlsl, same ceiling panels as the floor water): from the level / flow slope and
                // a slow swell only (the tuft drape and fine ripples act as roughness), times the tuft occlusion envOcc, so only
                // a free sheet (the squeegee wave, a pool) shows the panels and a film between the tufts keeps its dark wet look
                float swell = snoise2(uv * 24.0 + float2(_Time.y * 0.11, -_Time.y * 0.07)) * 0.004;
                float3 Nm = normalize(float3(-g.x + swell, 1.0, -g.y - swell) + suck);
                Nm = normalize(lerp(float3(0, 1, 0), Nm, saturate(h / 0.5)));
                float roughRad = 0.006 + 0.02 * saturate(length(flow) / 60.0 + 0.3) * saturate(h / 0.5) * 0.25 + drape * 0.002;
                water += RoomFresnel(saturate(dot(Nm, V))) * envOcc * RoomRadiance(IN.posWS, reflect(-V, Nm), L.color, _SkyColor.rgb, roughRad);

                // --- inspection lamp (Docs/HIGHLIGHTS.md): the refracted pile is already lit by the lamp (CarpetFibers), so only the
                //     water's own light dims, and the cell's glow is added only for the share of the pile the murk hides ---
                if (HL_Active())
                {
                    float trans = dot(Ta, 1.0 / 3.0) * Ts * (1.0 - fres);
                    float3 seen = scene * Ta * Ts * (1.0 - fres) * (1.0 + 0.2 * meniscus);
                    water = HL_ApplyOverCarpet(water, seen, HL_CarpetDirt(di, fl, sp), HL_CarpetWet(fb, fl, _SimConsts.x), uv * _CarpetMm.xy, 1.0 - trans);
                }

                // --- composite (premultiplied): water over scene. The foam mound (CarpetFoam.shader, Transparent+1, ZWrite On)
                //     is drawn after this pass and simply overdraws the water under it (no discard here, so hiding the foam
                //     for A/B captures never leaves holes). ---
                float a = alphaW;
                return half4(water * a, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
