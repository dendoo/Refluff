// FloorSurface.shader — LANE-A. Opaque procedural tile floor with wet / dust / residue overlays driven by the floor
// simulation textures (Docs/FLOOR_SYSTEM.md section 5). Object space of the FloorSurface child = FloorRoot space
// (child at local y = -0.003); CMM = (posOS.x*1000 + 600, posOS.z*1000 + 750). Lighting: URP UniversalFragmentPBR.
Shader "CarpetCleaning/FloorSurface"
{
    Properties
    {
        _TileColor ("Tile Color", Color) = (0.82, 0.83, 0.85, 1)
        _GroutColor ("Grout Color", Color) = (0.60, 0.60, 0.62, 1)
        _DustColor ("Dust Color", Color) = (0.40, 0.33, 0.23, 1)
        _ResidueColor ("Residue Color", Color) = (0.19, 0.13, 0.07, 1)
        _SkyColor ("Sky Color (wet fresnel)", Color) = (0.62, 0.68, 0.76, 1)
        _DetTint ("Detergent Tint", Color) = (0.86, 0.94, 0.98, 1)
        _DetSparkle ("Detergent Sparkle", Range(0, 1)) = 0.35
        _CleanGloss ("Clean Tile Gloss", Range(0, 1)) = 0.74
        _CleanSheen ("Clean Tile Sheen", Range(0, 2)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "HighlightCommon.hlsl"
        #include "RoomReflection.hlsl"

        // ---- globals (set by FloorSimulation; OUTSIDE the material cbuffer) ----
        TEXTURE2D(_FFluidTex);      // (h, u, v, s)
        TEXTURE2D(_FSoapTex);       // (sigma, F, Fd, Fage)
        TEXTURE2D(_FDepositTex);    // (dust, residue, wetFilm, spare)
        TEXTURE2D(_FBedTex);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_linear_clamp);
        SAMPLER(sampler_linear_repeat);
        float4 _FloorRect;          // (FLOOR_MIN.x, FLOOR_MIN.y, 1/FLOOR_SIZE, FLOOR_Y)
        float4 _FloorTexel;         // (inverse width, inverse height, width, height)
        float4 _FloorConsts;        // (F_H_MAX, F_F_MAX, FDX, 0)
        float _F_TILE;              // tile pitch mm (300)
        float _F_GROUT_W;           // grout width mm (6)
        float _F_GROUT_DEPTH;       // grout depth mm (1)
        float _F_DET_REF;           // detergent film level that reads as fully covered (FloorSettings.F_DET_REF)
        float _CarpetAway;          // 1 while the carpet has been handed over: the rect is plain tile (Docs/HANDOVER.md 6)

        CBUFFER_START(UnityPerMaterial)
            half4 _TileColor;
            half4 _GroutColor;
            half4 _DustColor;
            half4 _ResidueColor;
            half4 _SkyColor;
            half4 _DetTint;
            half _DetSparkle;
            half _CleanGloss;
            half _CleanSheen;
        CBUFFER_END

        // ---- inlined helpers ----
        float hash1(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.x + p3.y) * p3.z);
        }
        float2 hash2(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.xx + p3.yz) * p3.zy);
        }
        float vnoise(float2 p)      // 0..1
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            float a = hash1(i), b = hash1(i + float2(1, 0)), c = hash1(i + float2(0, 1)), d = hash1(i + float2(1, 1));
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }
        float fbm3(float2 p)        // 0..1
        {
            return vnoise(p) * 0.5714 + vnoise(p * 2.03 + 17.1) * 0.2857 + vnoise(p * 4.11 + 9.7) * 0.1429;
        }

        // Tile relief profile in mm, same shape as FloorSim.compute TileReliefMm (grout channel + 1.5 mm chamfer).
        float GroutMask(float2 cmm)
        {
            float tile = max(_F_TILE, 1.0);
            float2 local = cmm - floor(cmm / tile) * tile;
            float2 dGrout = (local < _F_GROUT_W) ? 0.0 : min(local - _F_GROUT_W, tile - local);
            float2 gAxis = 1.0 - smoothstep(0.0, 1.5, dGrout);
            return max(gAxis.x, gAxis.y);
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 cmm        : TEXCOORD0;
            float3 posWS      : TEXCOORD1;
            float3 nrmWS      : TEXCOORD2;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            float3 pos = IN.positionOS.xyz;
            OUT.positionCS = TransformObjectToHClip(pos);
            OUT.posWS = TransformObjectToWorld(pos);
            OUT.nrmWS = TransformObjectToWorldNormal(IN.normalOS);
            OUT.cmm = pos.xz * 1000.0 + CARPET_CENTRE_MM;
            return OUT;
        }
        ENDHLSL

        // ------------------------------------------------------------------
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            half4 frag(Varyings IN) : SV_Target
            {
                float2 cmm = IN.cmm;
                float2 fuv = (cmm - _FloorRect.xy) * _FloorRect.z;
                float inDomain = (fuv.x > 0.0 && fuv.y > 0.0 && fuv.x < 1.0 && fuv.y < 1.0) ? 1.0 : 0.0;
                float inCarpet = (_CarpetAway < 0.5 && cmm.x >= 0.0 && cmm.x <= CARPET_W_MM && cmm.y >= 0.0 && cmm.y <= CARPET_H_MM) ? 1.0 : 0.0;
                float simMask = inDomain * (1.0 - inCarpet);       // under a carpet that is there the floor is dry and clean;
                                                                   // once it is handed over the rect simulates like any tile

                float4 fl = SAMPLE_TEXTURE2D(_FFluidTex,   sampler_linear_clamp, fuv) * simMask;
                float4 dp = SAMPLE_TEXTURE2D(_FDepositTex, sampler_linear_clamp, fuv) * simMask;
                float h = max(fl.x, 0.0);
                float dust = max(dp.x, 0.0), residue = max(dp.y, 0.0), wetFilm = saturate(dp.z);
                float det = max(dp.w, 0.0);                 // detergent film (Docs/FLOOR_TOOLS.md)
                float detc = 0.0, spark = 0.0;

                // --- procedural tiles ---
                float tile = max(_F_TILE, 1.0);
                float2 tid = floor(cmm / tile);
                float tileHash = hash1(tid + 0.5);
                float grout = GroutMask(cmm);
                float mottle = fbm3(cmm * 0.011 + tileHash * 37.0);                 // ~90 mm blotches
                float speck = hash1(floor(cmm * 0.7));                              // ~1.4 mm speckle
                float bright = 1.0 + 0.06 * (tileHash * 2.0 - 1.0) + 0.10 * (mottle - 0.5) + 0.03 * (speck - 0.5);
                float3 tileAlb = _TileColor.rgb * bright;
                float groutN = vnoise(cmm * 0.35);
                float3 groutAlb = _GroutColor.rgb * (0.9 + 0.2 * groutN);
                float3 albedo = lerp(tileAlb, groutAlb, grout);
                // polished porcelain when clean; the grout line stays matte so the tile grid still reads
                float smooth = lerp(_CleanGloss, _CleanGloss * 0.30, grout);

                // bevel normal from the relief profile (central differences, 0.5 mm)
                float dGx = (GroutMask(cmm + float2(0.5, 0.0)) - GroutMask(cmm - float2(0.5, 0.0))) * -_F_GROUT_DEPTH;
                float dGz = (GroutMask(cmm + float2(0.0, 0.5)) - GroutMask(cmm - float2(0.0, 0.5))) * -_F_GROUT_DEPTH;
                float3 Nrel = normalize(float3(-dGx, 1.0, -dGz));

                // --- detergent coverage: a ragged fbm edge, so a sprayed patch never looks like a hard decal ---
                if (det > 0.002)
                {
                    float detv = saturate(det / max(_F_DET_REF, 1e-3));
                    detc = saturate(detv * (0.55 + 0.9 * fbm3(cmm * 0.05 + 23.0)));
                }

                // --- wetness (standing water, the damp film, or a detergent film) ---
                float wetv = saturate(max(max(h / 0.15, wetFilm), detc));
                // glazed porcelain barely darkens when wet (the glaze does not soak water); the porous grout does. The old flat
                // 0.55 on the whole tile made a puddle read as a cast shadow (TRAILER.md 8.3)
                albedo *= lerp(1.0, lerp(0.86, 0.55, grout), wetv);
                smooth = lerp(smooth, 0.92, wetv);

                // --- dust overlay (matte) ---
                float dustN = 0.6 + 0.8 * fbm3(cmm * 0.09 + 3.1);
                float aD = saturate((1.0 - exp(-3.0 * dust)) * dustN);
                albedo = lerp(albedo, _DustColor.rgb * (0.9 + 0.2 * speck), aD);
                smooth = lerp(smooth, 0.08, aD);

                // --- dried residue rings (high-frequency emphasis) ---
                float ringN = 0.55 + 0.9 * fbm3(cmm * 0.45 + 11.0);
                float aR = saturate((1.0 - exp(-4.0 * residue)) * ringN);
                albedo = lerp(albedo, _ResidueColor.rgb, aR);
                smooth = lerp(smooth, 0.28, aR * (1.0 - wetv));

                // how soiled this pixel reads. Only a clean tile gets the polish terms below, so the player sees the
                // shine appear exactly where the mop has been: that IS the dirty/clean contrast.
                float soil = saturate(max(aD, aR) * 1.15 + 0.35 * saturate(dust * 6.0));
                float polish = 1.0 - soil;

                // --- detergent film sits ON TOP of dust and residue: soaked dirt reads wet, it is the mop that removes it ---
                if (detc > 0.004)
                {
                    albedo = lerp(albedo, albedo * 0.90 * _DetTint.rgb, detc);
                    smooth = lerp(smooth, 0.96, detc);                        // slicker than plain wet tile (0.92)
                    float sparkH = hash1(floor(cmm * 0.22) + 7.0);            // 4.5 mm cells, coarse enough not to alias
                    spark = saturate((sparkH - 0.90) * 10.0) * detc * _DetSparkle;
                }

                // --- URP PBR lighting ---
                float3 N = normalize(IN.nrmWS);
                float3 T = abs(N.y) > 0.99 ? float3(1, 0, 0) : normalize(cross(float3(0, 1, 0), N));
                float3 B = cross(N, T);
                float3 nWS = normalize(T * Nrel.x + N * Nrel.y + B * Nrel.z);
                float3 V = normalize(GetCameraPositionWS() - IN.posWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.posWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = nWS;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.posWS);
                inputData.fogCoord = 0.0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(nWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = smooth;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = lerp(1.0, 0.75, grout);
                surfaceData.alpha = 1.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // wet fresnel sky term + the dry polish sheen of a clean tile
                float ndv = saturate(dot(nWS, V));
                float fres = pow(1.0 - ndv, 5.0);
                color.rgb += fres * (wetv * 0.6 + polish * _CleanSheen) * _SkyColor.rgb;

                // tight sun glint: the actual "blesk". Grout is excluded (1 - grout) so the highlight rides the tile faces.
                Light mainLight = GetMainLight(inputData.shadowCoord);
                float3 Hv = normalize(V + mainLight.direction);
                float glint = pow(saturate(dot(nWS, Hv)), 260.0) * polish * (1.0 - grout) * (1.0 - 0.35 * wetv);
                color.rgb += glint * _CleanSheen * 3.0 * mainLight.color * mainLight.shadowAttenuation;

                // coarse micro-sparkle (~6 mm cells) so a clean dry floor twinkles instead of reading as flat paint
                float twH = hash1(floor(cmm * 0.16) + 41.0);
                // A sparkle smaller than a pixel must converge to its mean, not flicker as the player walks.
                float footprint=max(length(ddx(cmm)),length(ddy(cmm)));
                float resolvedSparkle=1.0-smoothstep(2.0,6.0,footprint);
                float twinkle = saturate((twH - 0.975) * 34.0) * polish * (1.0 - wetv) * resolvedSparkle * 0.25;
                color.rgb += twinkle * _CleanSheen * 1.2 * _SkyColor.rgb;

                color.rgb += spark * 0.35 * _SkyColor.rgb;

                // a damp or detergent film on the glaze mirrors the room's ceiling panels, blurred (RoomReflection.hlsl); standing
                // water is drawn by FloorWater on top, so the film term fades out where that sheet turns opaque
                float filmW = saturate(max(wetFilm, detc)) * exp(-h / 0.12) * (1.0 - grout) * simMask;
                if (filmW > 0.002)
                    color.rgb += filmW * RoomFresnel(ndv) * RoomRadiance(IN.posWS, reflect(-V, nWS), mainLight.color, _SkyColor.rgb, 0.03);

                // inspection lamp (Docs/HIGHLIGHTS.md): the whole floor dims (no bright frame round the sim domain), only the
                // simulated tile glows (fl / dp are already 0 outside the domain and under a carpet that is there)
                if (HL_Active())
                {
                    float4 fsp = SAMPLE_TEXTURE2D(_FSoapTex, sampler_linear_clamp, fuv) * simMask;
                    color.rgb = HL_Apply(color.rgb, HL_FloorDirt(dp, fl, fsp), HL_FloorWet(fl, dp), cmm, 1.0);
                }
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma target 4.5

            half4 fragDepth(Varyings IN) : SV_Target
            {
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
