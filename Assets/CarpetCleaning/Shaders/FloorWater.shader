// FloorWater.shader — LANE-A. CarpetWater adapted to the floor domain (Docs/FLOOR_SYSTEM.md section 5):
// transparent 3.6 m grid lifted by h above the floor top (the FloorWater child already sits at FLOOR_Y),
// normal from bed + h, refraction of the opaque texture, Beer-Lambert murk, fresnel sky. Foam is a separate
// surface (FloorRoot/FloorFoam, CarpetFoam.shader with _FLOOR_DOMAIN, Docs/FOAM_LOOK.md) drawn after and over the water.
// Discarded inside the carpet rect (2 mm margin) and where alpha < 0.01.
Shader "CarpetCleaning/FloorWater"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (0.62, 0.68, 0.76, 1)
        _SiltColor ("Silt Color (suspended dirt albedo)", Color) = (0.26, 0.18, 0.10, 1)
        _AbsorbClean ("Absorb Clean (per mm)", Color) = (0.02, 0.03, 0.05, 1)
        _AbsorbDirty ("Absorb Dirty (extra per mm at full murk)", Color) = (0.15, 0.30, 0.60, 1)
        _MurkDensity ("Murk Density (silt extinction per mm)", Float) = 0.8
        _MurkCRef ("Murk Slurry Ref (dirt per mm of water at full murk)", Float) = 0.4
        _RefrStrength ("Refraction Strength", Float) = 0.02
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

            // ---- globals (set by FloorSimulation / CarpetSimulation / CarpetRenderer; OUTSIDE the material cbuffer) ----
            TEXTURE2D(_FFluidTex);      // (h, u, v, s)
            TEXTURE2D(_FSoapTex);       // (sigma, F, Fd, Fage)
            TEXTURE2D(_FBedTex);        // relief mm (+40 under the carpet)
            TEXTURE2D(_FDepositTex);    // (dust, residue, wetFilm, detergent): the inspection lamp's floor measures
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_linear_clamp);
            SAMPLER(sampler_linear_repeat);

            float4 _FloorRect;          // (FLOOR_MIN.x, FLOOR_MIN.y, 1/FLOOR_SIZE, FLOOR_Y)
            float4 _FloorTexel;         // (inverse width, inverse height, width, height)
            float4 _FloorConsts;        // (F_H_MAX, F_F_MAX, FDX, 0)
            float4 _RimDir;             // world dir TO the rim light
            float4 _RimColor;
            float4 _ToolCursor;         // (u, v, radiusMm, active)  carpet UV, may lie outside 0..1 on the floor
            float4 _ToolCursor2;        // (dir.x, dir.y, lengthMm, toolId)
            float  _CarpetAway;         // 1 while the carpet has been handed over: the rect is plain tile (Docs/HANDOVER.md 6)

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
                half4 _SiltColor;
                half4 _AbsorbClean;
                half4 _AbsorbDirty;
                float _MurkDensity;
                float _MurkCRef;
                float _RefrStrength;
            CBUFFER_END

            // ---- inlined helpers (identical to CarpetWater) ----
            float hash1(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            float vnoise(float2 p)   // -1..1
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash1(i), b = hash1(i + float2(1, 0)), c = hash1(i + float2(0, 1)), d = hash1(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 2.0 - 1.0;
            }
            float snoise2(float2 p) { return vnoise(p) * 0.667 + vnoise(p * 2.03 + 17.1) * 0.333; }

            // "a carpet is lying here": false everywhere once it has been handed over, so the water sheet covers the
            // bare rect like any other tile instead of leaving a hole in the middle of the floor (Docs/HANDOVER.md 6)
            bool CarpetCoversMm(float2 cmm, float margin)
            {
                return _CarpetAway < 0.5 && cmm.x >= -margin && cmm.x <= CARPET_W_MM + margin && cmm.y >= -margin && cmm.y <= CARPET_H_MM + margin;
            }

            // surface height (bed + h) in mm at a floor UV; samples under the carpet return 'fallback' so the wall never tilts normals
            float SurfMm(float2 fuv, float fallback)
            {
                float2 cmm = _FloorRect.xy + fuv / _FloorRect.z;
                if (CarpetCoversMm(cmm, 0.0)) return fallback;
                float bed = SAMPLE_TEXTURE2D(_FBedTex, sampler_linear_clamp, fuv).x;
                float h = SAMPLE_TEXTURE2D(_FFluidTex, sampler_linear_clamp, fuv).x;
                return min(bed, 5.0) + max(h, 0.0);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 fuv        : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                float2 cmm        : TEXCOORD2;
            };

            // ---- vertex: lift the grid to h above the floor top (object origin is already at FLOOR_Y; foam is FloorFoam) ----
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                float2 cmm = pos.xz * 1000.0 + CARPET_CENTRE_MM;
                float2 fuv = (cmm - _FloorRect.xy) * _FloorRect.z;
                float4 fl = SAMPLE_TEXTURE2D_LOD(_FFluidTex, sampler_linear_clamp, fuv, 0);
                float h = max(fl.x, 0.0);
                if (CarpetCoversMm(cmm, 0.0)) { h = 0.0; }
                pos.y = min(h, 6.0) * 0.001 + 0.0003;   // relief capped at 6 mm: transient inflow crests spread out over the next frames
                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.posWS = TransformObjectToWorld(pos);
                OUT.fuv = fuv;
                OUT.cmm = cmm;
                return OUT;
            }

            // ---- fragment ----
            half4 frag(Varyings IN) : SV_Target
            {
                float2 fuv = IN.fuv;
                float2 cmm = IN.cmm;
                if (CarpetCoversMm(cmm, 2.0)) discard;
                if (fuv.x <= 0.0 || fuv.y <= 0.0 || fuv.x >= 1.0 || fuv.y >= 1.0) discard;

                float4 fl = SAMPLE_TEXTURE2D(_FFluidTex, sampler_linear_clamp, fuv);
                float4 sp = SAMPLE_TEXTURE2D(_FSoapTex,  sampler_linear_clamp, fuv);
                float h     = max(fl.x, 0.0);
                float2 flow = fl.yz;
                float s     = max(fl.w, 0.0);
                float C     = s / (h + 1e-3);                       // (foam F from _FSoapTex is no longer used here: the foam is its own surface)

                // --- surface normal: central differences of (bed + h) over one floor texel (FDX mm) ---
                float fdx = max(_FloorConsts.z, 1e-3);
                float2 tx = float2(_FloorTexel.x, 0.0);
                float2 tz = float2(0.0, _FloorTexel.y);
                float sc = SurfMm(fuv, 0.0);
                float dsx = (SurfMm(fuv + tx, sc) - SurfMm(fuv - tx, sc)) / (2.0 * fdx);
                float dsz = (SurfMm(fuv + tz, sc) - SurfMm(fuv - tz, sc)) / (2.0 * fdx);

                // --- flow-driven ripples (only where there is water) ---
                // standing puddles keep a faint shimmer (0.5 floor) so the sun glints across them; flow adds real ripples
                float rip = (snoise2(cmm * 0.15 + flow * _Time.y * 0.02) + snoise2(cmm * 0.26 - _Time.y * 0.7))
                            * 0.045 * saturate(length(flow) / 60.0 + 0.5) * saturate(h / 0.5);

                // --- wet-vacuum cursor feedback (cursor uv may lie outside the carpet) ---
                float3 suck = float3(0, 0, 0);
                if (_ToolCursor.w > 0.5 && abs(_ToolCursor2.w - 6.0) < 0.5)
                {
                    float2 p = cmm - _ToolCursor.xy * CARPET_MM;
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

                float3 N = normalize(float3(-dsx + rip, 1.0, -dsz + rip) + suck);
                N = lerp(float3(0, 1, 0), N, saturate(h / 0.5));
                N = normalize(N);

                float alphaW   = 1.0 - exp(-h / 0.12);
                float meniscus = smoothstep(0.15, 0.5, h) * (1.0 - smoothstep(0.5, 1.5, h));

                // --- refraction through the opaque texture (tiles) ---
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float2 refr = clamp(N.xz * _RefrStrength * saturate(h / 3.0), -0.012, 0.012);
                float3 scene = SampleSceneColor(screenUV + refr);

                // --- murk (same model as CarpetWater, minus the carpet-dirt term): dissolved slurry only ---
                float murk = saturate(C / max(_MurkCRef, 1e-3));

                // --- Beer-Lambert: per-channel water absorption (blue most when dirty), scalar silt extinction + lit brown in-scatter ---
                Light L = GetMainLight();
                float pathMm = 2.0 * h + 0.4 * murk;
                float3 sigA = _AbsorbClean.rgb + _AbsorbDirty.rgb * murk;
                float3 Ta = exp(-sigA * pathMm);
                float  Ts = exp(-_MurkDensity * murk * pathMm);
                float3 siltLit = _SiltColor.rgb * (L.color * 0.75 + SampleSH(float3(0.0, 1.0, 0.0)));
                float3 body = scene * Ta * Ts + siltLit * (1.0 - Ts);

                // --- lighting ---
                float3 V = normalize(GetCameraPositionWS() - IN.posWS);
                float3 Ld = normalize(L.direction);
                float3 H = normalize(Ld + V);
                float ndv = saturate(dot(N, V));
                float fres = 0.04 + 0.96 * pow(1.0 - ndv, 4.0);          // slightly boosted so puddles read as water on the matte tile
                float ndh = saturate(dot(N, H));
                float spec = pow(ndh, 300.0) * 2.0 + pow(ndh, 40.0) * 0.15;
                float3 rim = pow(1.0 - ndv, 3.0) * saturate(dot(N, _RimDir.xyz)) * _RimColor.rgb;
                float3 water = body * (1.0 - fres) + fres * _SkyColor.rgb + L.color * spec + rim + meniscus * 0.2 * body;   // hue-preserving contact-line brightening (was a flat +0.12 grey)
                // the room it mirrors (RoomReflection.hlsl): ceiling panels, broken up by the ripples. This is what makes a
                // puddle read as water and not as a shadow on the white tile at store size.
                // Mirror normal: the sim's surface slope + a slow swell (the 4 mm ripples only blur the image, as roughness)
                float swell = snoise2(cmm * 0.02 + float2(_Time.y * 0.11, -_Time.y * 0.07)) * 0.004
                            + snoise2(cmm * 0.045 + flow * _Time.y * 0.004) * 0.006 * saturate(length(flow) / 60.0);
                float3 Nm = normalize(float3(-dsx + swell, 1.0, -dsz - swell) + suck);
                Nm = normalize(lerp(float3(0, 1, 0), Nm, saturate(h / 0.5)));
                float3 R = reflect(-V, Nm);
                float ripAmp = 0.045 * saturate(length(flow) / 60.0 + 0.5) * saturate(h / 0.5);   // envelope of 'rip' above
                float roughRad = 0.004 + ripAmp * 0.25;                  // ripple spread: still puddle ~0.01, flowing ~0.015
                water += RoomFresnel(saturate(dot(Nm, V))) * RoomRadiance(IN.posWS, R, L.color, _SkyColor.rgb, roughRad);

                // --- inspection lamp (Docs/HIGHLIGHTS.md): same split as CarpetWater (the refracted tile is lit by FloorSurface) ---
                if (HL_Active())
                {
                    float4 dp = SAMPLE_TEXTURE2D(_FDepositTex, sampler_linear_clamp, fuv);
                    float trans = dot(Ta, 1.0 / 3.0) * Ts * (1.0 - fres);
                    float3 seen = scene * Ta * Ts * (1.0 - fres) * (1.0 + 0.2 * meniscus);
                    water = HL_ApplyOver(water, seen, HL_FloorDirt(dp, fl, sp), HL_FloorWet(fl, dp), cmm, 1.0 - trans);
                }

                // --- composite (premultiplied): water over scene. FloorFoam (CarpetFoam.shader, Transparent+1, ZWrite On) is drawn
                //     after this pass and overdraws the water under the mound (no discard, so hiding the foam never leaves holes) ---
                float a = alphaW;
                if (a < 0.01) discard;
                return half4(water * a, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
