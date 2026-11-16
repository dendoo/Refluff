Shader "CarpetCleaning/CarpetFibers"
{
    Properties
    {
        _PictureTex ("Picture", 2D) = "black" {}
        _CarpetColor ("Carpet Color", Color) = (0.20, 0.36, 0.62, 1)
        _MudColor ("Mud Color", Color) = (0.24, 0.15, 0.08, 1)
        _CrustColor ("Crust Color", Color) = (0.50, 0.45, 0.38, 1)
        _GritColor ("Grit Color", Color) = (0.55, 0.48, 0.40, 1)
        _DustColor ("Dust Color", Color) = (0.60, 0.56, 0.50, 1)
        _StainA ("Stain A (tea)", Color) = (0.55, 0.38, 0.20, 1)
        _StainB ("Stain B (wine)", Color) = (0.50, 0.18, 0.25, 1)
        _StainC ("Stain C (oil)", Color) = (0.35, 0.35, 0.33, 1)
        _StainD ("Stain D (grass)", Color) = (0.40, 0.50, 0.22, 1)
        _PileHeight ("Pile Height (m)", Float) = 0.02
        _ShellCount ("Shell Count", Float) = 48
        _StrandGrid ("Strand Grid", Vector) = (240, 300, 0, 0)
        _ClumpGrid ("Clump Grid", Vector) = (100, 125, 0, 0)
        _BendUV ("Bend UV", Vector) = (0.0200, 0.0160, 0, 0)
        _PicOffset ("Picture Offset", Vector) = (0, 0, 0, 0)
        _PicScale ("Picture Scale", Vector) = (1, 1, 0, 0)
        _StrandRadius ("Strand Radius", Float) = 0.40
        _WetDarken ("Wet Darken", Float) = 0.5
        _DustVeil ("Dust Veil (max film over the tips)", Float) = 0.45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        // ------------------------------------------------------------------
        // Shared HLSL (globals, material cbuffer, helpers, vertex, fragment core)
        // ------------------------------------------------------------------
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "HighlightCommon.hlsl"

        // Simulation globals (Shader.SetGlobalTexture / SetGlobalVector) - OUTSIDE the material cbuffer
        TEXTURE2D(_FluidTex);
        TEXTURE2D(_SoapTex);
        TEXTURE2D(_DirtTex);
        TEXTURE2D(_CrustTex);
        TEXTURE2D(_FiberTex);
        TEXTURE2D(_BedTex);
        TEXTURE2D(_NoiseTex);
        TEXTURE2D(_PictureTex);
        SAMPLER(sampler_linear_clamp);
        SAMPLER(sampler_linear_repeat);
        SAMPLER(sampler_PictureTex);

        float4 _SimTexel;     // (inverse width, inverse height, width, height)
        float4 _CarpetMm;     // (width mm, height mm, inverse width, inverse height)
        float4 _SimConsts;    // (W_CAP, PILE_H, F_MAX, H_MAX)
        float4 _RimDir;       // world dir TO the rim light
        float4 _RimColor;
        float4 _ToolCursor;   // (u, v, radiusMm, active)
        float4 _ToolCursor2;  // (dir.x, dir.y, lengthMm, toolId)

        CBUFFER_START(UnityPerMaterial)
            float4 _PictureTex_ST;
            float4 _CarpetColor;
            float4 _MudColor;
            float4 _CrustColor;
            float4 _GritColor;
            float4 _DustColor;
            float4 _StainA;
            float4 _StainB;
            float4 _StainC;
            float4 _StainD;
            float4 _StrandGrid;
            float4 _ClumpGrid;
            float4 _BendUV;
            float4 _PicOffset;
            float4 _PicScale;
            float _PileHeight;
            float _ShellCount;
            float _StrandRadius;
            float _WetDarken;
            float _DustVeil;
        CBUFFER_END

        // ---------------- helpers (inlined; no shared HLSL between lanes) ----------------
        float hash1(float2 p)
        {
            p = frac(p * float2(443.897, 441.423));
            p += dot(p, p.yx + 19.19);
            return frac((p.x + p.y) * p.x);
        }

        float2 hash2(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(443.897, 441.423, 437.195));
            p3 += dot(p3, p3.yzx + 19.19);
            return frac((p3.xx + p3.yz) * p3.zy);
        }

        float4 hash4(float2 p)
        {
            float4 p4 = frac(float4(p.xyxy) * float4(443.897, 441.423, 437.195, 444.129));
            p4 += dot(p4, p4.wzxy + 19.19);
            return frac((p4.xxyz + p4.yzzw) * p4.zywx);
        }

        // stain hue quartile -> stain colour
        float3 palette(float h)
        {
            float3 c = (h < 0.25) ? _StainA.rgb : ((h < 0.5) ? _StainB.rgb : ((h < 0.75) ? _StainC.rgb : _StainD.rgb));
            return c;
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;   // carpet uv
            float2 uv1        : TEXCOORD1;   // x = shell t (0 backing .. 1 tip)
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float  t          : TEXCOORD1;
            float3 posWS      : TEXCOORD2;
        };

        // ---------------- vertex: sink the shell by pileTopMm(f, w) of 3.1 ----------------
        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            float t = IN.uv1.x;
            float4 fb = SAMPLE_TEXTURE2D_LOD(_FiberTex, sampler_linear_clamp, IN.uv, 0);   // bx, by, f, w
            float pileMm = _SimConsts.y * (1.0 - 0.55 * fb.z) * (1.0 - 0.25 * fb.w);
            float3 pos = IN.positionOS.xyz;
            pos.y = t * pileMm * 0.001;
            OUT.positionCS = TransformObjectToHClip(pos);
            OUT.posWS = TransformObjectToWorld(pos);
            OUT.uv = IN.uv;
            OUT.t = t;
            return OUT;
        }

        // ---------------- strand geometry shared by both passes ----------------
        struct StrandData
        {
            float2 uvRoot;
            float2 off;
            float4 hs;
            float  taper;
            float  alpha;
            float  flat;
            float  w;
            float  crustCover;
            float  crack;
            float  caked;      // 0..crustCover: this shell texel is inside the dried-mud slab
            float2 bend;
        };

        // Computes the strand mask; returns alpha (caller clips). Shell 0 never discards.
        StrandData EvalStrand(float2 uv, float t, float4 fb, float4 dirt, float4 cr)
        {
            StrandData s;
            float cw = 0.26 + 0.5 * cr.r;                                   // crack widens with scraper damage
            s.crack = 1.0 - smoothstep(cw, cw + 0.08, cr.a);
            s.bend = fb.xy;
            s.w = fb.w;
            s.crustCover = 1.0 - exp(-3.5 * dirt.x);
            s.flat = max(fb.z, 0.6 * s.crustCover);

            // 1) cantilever bend: where is this shell texel's root?
            float2 uvRoot = uv - s.bend * (t * t) * _BendUV.xy;

            // 2) wet clumping toward jittered clump centres (pull grows toward the tip)
            float2 cell = floor(uvRoot * _ClumpGrid.xy);
            float2 centre = (cell + 0.5 + 0.4 * (hash2(cell) - 0.5)) / _ClumpGrid.xy;
            uvRoot += (centre - uvRoot) * saturate(s.w * 1.3) * 0.7 * t;
            s.uvRoot = uvRoot;

            // 3) strand mask from a hashed 2.5 mm grid
            float2 g = uvRoot * _StrandGrid.xy;
            float2 sc = floor(g);
            s.off = frac(g) - 0.5;
            s.hs = hash4(sc);

            float len = (0.55 + 0.45 * s.hs.x) * (1.0 - 0.5 * s.flat) * (1.0 - 0.3 * s.w);
            len *= 1.0 - 0.85 * s.crustCover * s.crustCover;                // caked pile: strands stay below the plate
            float tOverLen = t / max(len, 1e-4);
            s.taper = sqrt(saturate(1.0 - tOverLen));
            float radius = _StrandRadius * s.taper * (1.0 + 0.5 * s.w);

            // minified far shells: fade toward a smooth surface
            float2 fwg = fwidth(g);
            float minify = saturate(max(fwg.x, fwg.y) - 1.0);
            radius = lerp(radius, 0.5, minify);

            // dry crust cakes the lower pile into a solid cracked plate (top at t = crustTop); every slab has its own
            // thickness (plateShade cr.g, 0.72-1.28 x), so neighbouring slabs stand at different heights and show their sides
            float crustTop = 0.5 * s.crustCover * (0.72 + 0.56 * cr.g);
            float caked = (t <= crustTop) ? s.crustCover : 0.0;
            s.caked = caked;
            radius = lerp(radius, 1.5, caked);                              // solid plate, no corner holes

            float d = length(s.off + 0.45 * (s.hs.yz - 0.5));
            float edge = saturate((radius - d) / max(fwidth(d), 1e-4) + 0.5);
            bool tooLong = (t > 0.0) && (t > max(len, crustTop));
            edge *= 1.0 - s.crack * smoothstep(0.55, 0.85, caked);            // cracks cut through the plate down to shell 0
            s.alpha = (t <= 0.0) ? 1.0 : (tooLong ? 0.0 : edge);
            return s;
        }
        ENDHLSL

        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = IN.t;
                float4 fb   = SAMPLE_TEXTURE2D(_FiberTex, sampler_linear_clamp, uv);
                float4 dirt = SAMPLE_TEXTURE2D(_DirtTex,  sampler_linear_clamp, uv);
                float4 cr   = SAMPLE_TEXTURE2D(_CrustTex, sampler_linear_clamp, uv);
                float4 fl   = SAMPLE_TEXTURE2D(_FluidTex, sampler_linear_clamp, uv);

                StrandData s = EvalStrand(uv, t, fb, dirt, cr);
                clip(s.alpha - 0.02);                                   // A2C soft edge under MSAA; shell 0 never clips

                float w = s.w;
                float flat = s.flat;
                float crustCover = s.crustCover;
                float2 bend = s.bend;
                float2 uvRoot = s.uvRoot;
                float4 hs = s.hs;
                float C = fl.w / (fl.x + w * _SimConsts.x + 1e-3);

                // 4) albedo: print over base, dirt composited by height
                // the rug photo covers the whole carpet (CarpetHandover.PictureFit), its own border is the carpet's edge;
                // the clamp keeps strands bent past the rim on the edge texels (Docs/HANDOVER.md 5)
                float2 picUV = (uvRoot - _PicOffset.xy) * _PicScale.xy;
                float4 pic = SAMPLE_TEXTURE2D(_PictureTex, sampler_PictureTex, saturate(picUV));
                float3 base = lerp(_CarpetColor.rgb, pic.rgb, pic.a) * (0.92 + 0.16 * hs.w);
                // Shell 0 repeats the same local picture colour as the fibres. At t=0 uvRoot equals uv,
                // so the backing stays on the carpet while bent strands retain their root colour.
                // Dirt, wetness and lighting still shade the backing below, including the occlusion between tufts.

                float3 stainTint = palette(cr.b);
                base = lerp(base, base * stainTint, 1.0 - exp(-4.0 * dirt.z));

                float mudHere = 1.0 - exp(-dirt.y * lerp(8.0, 3.0, t));
                base = lerp(base, _MudColor.rgb * (0.8 + 0.4 * hs.x), mudHere);

                float crack = s.crack;
                float noiseR = SAMPLE_TEXTURE2D(_NoiseTex, sampler_linear_repeat, uvRoot * 40.0).r;
                // slab tone: per slab (cr.g) and a slow blotch, a little yellower or greyer per slab, lighter where the dry edge
                // curls up just inside a crack (dried mud whitens at its rim), dark in the crack itself (below)
                float slabH = frac(cr.g * 7.31 + 0.13);
                float3 plateCol = _CrustColor.rgb * (0.76 + 0.48 * cr.g) * (0.88 + 0.24 * noiseR);
                plateCol *= lerp(float3(1.04, 1.0, 0.92), float3(0.95, 0.98, 1.03), slabH);
                plateCol *= 0.92 + 0.16 * SAMPLE_TEXTURE2D(_NoiseTex, sampler_linear_repeat, uvRoot * 170.0 + cr.g).r;   // sandy grain on the dry top
                float cwT = 0.26 + 0.5 * cr.r;
                plateCol *= 1.0 + 0.10 * (1.0 - smoothstep(cwT + 0.06, cwT + 0.20, cr.a)) * (1.0 - crack);
                plateCol = lerp(plateCol, _MudColor.rgb * 0.45, crack);
                float crustHere = crustCover * ((t <= 0.0) ? 0.85 : smoothstep(0.03, 0.25, t));
                base = lerp(base, plateCol, crustHere);

                // Loose soil (grit, Docs/DIRT_LEVEL.md): tips and backing share the same live local dust/crumb amount.
                // Give shell 0 the full loose-soil mask so gaps cannot reveal a clean print under dirty fibres.
                // Actual fibre shells retain their height profile; cleaning dirt.w clears both surfaces together.
                float looseSoilHeight = (t <= 0.0) ? 1.0 : t;
                float gritHere = saturate((dirt.w - 0.2) * 2.5) * step(0.55, hs.y) * smoothstep(0.4, 0.8, looseSoilHeight);
                base = lerp(base, _GritColor.rgb * (0.8 + 0.4 * hs.z), gritHere);
                float dustV = _DustVeil * (1.0 - exp(-3.5 * dirt.w)) * smoothstep(0.1, 0.8, looseSoilHeight) * (0.55 + 0.9 * hs.y);
                base = lerp(base, _DustColor.rgb * (0.9 + 0.2 * hs.z), saturate(dustV));

                // shading wetness: pile moisture (w, fraction of W_CAP; soft curve so drying shows over the whole range) or a free-water film both read as wet
                float wetV = saturate(1.0 - exp(-3.0 * w) + saturate(fl.x / 0.3));
                base = lerp(base, _MudColor.rgb * 0.7, saturate(C / 0.5) * wetV * 0.5);   // dirty water tints
                // wet darkening: water in the pile traps light -> darker AND more saturated (x^1.25), then _WetDarken
                base = lerp(base, pow(max(base, 1e-4), 1.25), wetV) * lerp(1.0, _WetDarken, wetV);

                // pressed / laid pile (Docs/FIBER_SET.md): the eye sees dense strand sides instead of lit upright tips and light is
                // trapped between them -> darker and a touch more saturated, like a matted patch. Never brighter than upright pile.
                float laid = saturate(length(bend) * 1.6) * (1.0 - crustCover);
                base = lerp(base, pow(max(base, 1e-4), 1.15), 0.6 * laid);

                // 5) lighting
                float3 nS = normalize(float3(s.off.x, 0.6 + 0.8 * s.taper, s.off.y));
                // the pile SURFACE stays roughly up when the strands lie down: only a mild tilt (a strong one made the grazing
                // rim / wet spec blow out into milky white stripes)
                float3 N = normalize(nS + float3(bend.x, 0.0, bend.y) * 0.45);
                // dried-mud slab: a flat top (no per-strand domes: they drew a 2.5 mm grid over every plate), tilted a little
                // per slab and bevelled down into the cracks along the crack field's gradient
                if (s.caked > 0.0)
                {
                    float eX = SAMPLE_TEXTURE2D(_CrustTex, sampler_linear_clamp, uv + float2(_SimTexel.x, 0.0)).a
                             - SAMPLE_TEXTURE2D(_CrustTex, sampler_linear_clamp, uv - float2(_SimTexel.x, 0.0)).a;
                    float eZ = SAMPLE_TEXTURE2D(_CrustTex, sampler_linear_clamp, uv + float2(0.0, _SimTexel.y)).a
                             - SAMPLE_TEXTURE2D(_CrustTex, sampler_linear_clamp, uv - float2(0.0, _SimTexel.y)).a;
                    float nearCrack = 1.0 - smoothstep(0.30, 0.55, cr.a);
                    float2 tilt = (float2(frac(cr.g * 13.7), frac(cr.g * 29.3)) - 0.5) * 0.30;
                    float3 nPlate = normalize(float3(tilt.x + eX * 2.2 * nearCrack, 1.0, tilt.y + eZ * 2.2 * nearCrack));
                    N = normalize(lerp(N, nPlate, saturate(s.caked * 1.5)));
                }
                Light L = GetMainLight();
                float3 V = normalize(GetCameraPositionWS() - IN.posWS);
                float3 H = normalize(L.direction + V);
                float ndl = saturate(dot(N, L.direction) * 0.6 + 0.4);
                float ao = lerp(0.45, 1.0, t) * (1.0 - 0.25 * flat) * (1.0 - 0.15 * crustCover);
                float spec = pow(saturate(dot(N, H)), lerp(12.0, 60.0, wetV)) * lerp(0.02, 0.25, wetV) * (1.0 - crustCover) * (1.0 - 0.6 * laid);
                float3 Tdir = normalize(float3(bend.x, 0.35, bend.y) + 1e-3);
                float aniso = pow(1.0 - abs(dot(Tdir, H)), 8.0) * 0.12 * (1.0 - wetV) * (1.0 - crustCover) * (1.0 - 0.7 * laid);
                float3 rim = pow(1.0 - saturate(dot(N, V)), 3.0) * saturate(dot(N, _RimDir.xyz)) * _RimColor.rgb * wetV * (1.0 - crustCover) * (1.0 - 0.8 * laid);   // wet-fibre sheen only; the caked plate must not pick up the bluish back light

                // nap direction cue (lawn stripes): laid pile is darker; pile leaning away from the viewer (you look along the
                // strand sides) a little less dark than pile leaning toward the viewer (you look into the shadowed roots)
                float2 vXZ = normalize(V.xz + 1e-5);
                float lean = dot(bend, vXZ) / max(length(bend), 1e-3);    // +1 toward the viewer, -1 away
                float velvet = 1.0 - laid * (0.20 + 0.08 * lean);
                float3 col = base * (L.color * ndl + SampleSH(N)) * ao * velvet + (spec + aniso) * L.color + rim;

                // 6) inspection lamp (Docs/HIGHLIGHTS.md): the pile dims, what still keeps a HUD bar under 100 % glows (the carpet
                // cut; nothing at 100 %). The glow keeps a little of the strand shading (deeper shells dimmer) so the pile reads.
                if (HL_Active())
                {
                    float4 sp = SAMPLE_TEXTURE2D(_SoapTex, sampler_linear_clamp, uv);
                    col = HL_ApplyCarpet(col, HL_CarpetDirt(dirt, fl, sp), HL_CarpetWet(fb, fl, _SimConsts.x), uv * _CarpetMm.xy, lerp(0.6, 1.0, t));
                }

                return half4(col, s.alpha);
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
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma target 4.5

            half4 fragDepth(Varyings IN) : SV_Target
            {
                float4 fb   = SAMPLE_TEXTURE2D(_FiberTex, sampler_linear_clamp, IN.uv);
                float4 dirt = SAMPLE_TEXTURE2D(_DirtTex,  sampler_linear_clamp, IN.uv);
                float4 cr   = SAMPLE_TEXTURE2D(_CrustTex, sampler_linear_clamp, IN.uv);
                StrandData s = EvalStrand(IN.uv, IN.t, fb, dirt, cr);
                clip(s.alpha - 0.02);
                return half4(0, 0, 0, s.alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
