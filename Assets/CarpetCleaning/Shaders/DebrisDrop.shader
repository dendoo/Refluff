Shader "CarpetCleaning/DebrisDrop"
{
    // Splash droplets (debris kind 2) as screen-space motion streaks. Docs/DEBRIS_SYSTEM.md "Splash drop look (2026-09-25)",
    // "Drop look v2" and "Drop look v3" (2026-09-26). A real 1.5-3 mm drop is only 1-2.4 px at the 1080p game camera, so the
    // old physically sized sphere was never seen. The physics (size, velocity, water carried) is untouched; only the drawing:
    // - one quad per drop, laid out in screen pixels from where the drop is now (the head) back along its motion. The
    //   streak is a SHORT exposure (_Shutter, jittered per drop), but never shorter than _MinStreakPx while the drop really
    //   moves that far in one 60 Hz frame (a fast drop still reads as motion, a slow one never claims more than it moves),
    //   with a soft knee toward _MaxStreakPx. The half-width is the true radius x _SizeBoost but never under _MinRadiusPx
    //   (both scale with the target height / 1080, so a 4K capture shows the same drop), jittered per drop;
    // - a teardrop: the width eases to _TailWidth and the alpha to a faint _TailAlpha at the tail, so the motion reads; the
    //   taper grows in with the streak (full from 2 radii), so a drop flying along the view ray is a round bead, not a pop;
    // - coverage is analytic (distance to the swept segment, 1 px ramp), so a 2.6 px streak stays smooth without a texture;
    // - the shading is a water bead smeared along its motion, a normal from the offset to the swept centre (round caps):
    //   a lens that passes on _LensTransmit of the scene behind it, sampled upside down from the opaque texture (a ball
    //   lens), at _BodyAlpha for a few-pixel drop (its ring and lens seen box-filtered: a darker bead, which is what lets it
    //   read over the lit tile) easing to _BodyAlphaNear for a near, resolved drop (a clear lens, not a grey egg); a band
    //   toward the edge (_BandInner.._BandOuter, thinner on a resolved drop) that bends the rays out to the dim room
    //   (_RimAlpha, colour _RimShade x ambient: a dark outline over the tile); a Fresnel rim that mirrors the sky colour
    //   the puddles reflect; a small caustic where the drop focuses the sun (the side away from the glint, _Caustic);
    // - the sun's glint is a small sparkle (_GlintHDR, lobe _GlintSize x r, never under _GlintMinPx at 1080p, integrated
    //   over the pixel so it does not shimmer), placed where the wobbling drop flashed during the exposure (a per-drop point
    //   along the streak, not always the head) with a thin smear around it (_GlintLine); its strength is skewed per drop;
    // - per drop (_Vary): exposure length, width and brightness (down to _BrightMin), so a crown is not a ring of pins;
    // - a long fast streak gets fainter, not solid (softened by _StreakFade); the head disc, the drop now, never fades;
    // - premultiplied blending, so the glint and caustic add light over the tile and the carpet alike; every alpha <= 1;
    // - the inspection lamp (Docs/HIGHLIGHTS.md): the moisture lamp lights drops blue, the dirt lamp amber by the slurry they
    //   carry, and either lamp dims the body and switches the white glint and the caustic off.
    Properties
    {
        _SkyColor ("Water tint (the sky the puddles reflect)", Color) = (0.62, 0.68, 0.76, 1)
        _MudColor ("Mud Color", Color) = (0.24, 0.15, 0.08, 1)
        _Shutter ("Streak exposure (s), jittered per drop", Float) = 0.011
        _MinStreakPx ("Min streak length (px at 1080p) if the drop moves that far in 1/60 s", Float) = 11
        _MaxStreakPx ("Max streak length (px at 1080p, soft knee)", Float) = 20
        _MinRadiusPx ("Min half-width (px at 1080p)", Float) = 1.7
        _SizeBoost ("Half-width over the physical radius", Float) = 1.3
        _BodyAlpha ("Centre alpha of a few-pixel drop (its ring and lens seen box-filtered)", Range(0, 1)) = 0.5
        _BodyAlphaNear ("Centre alpha of a resolved near drop (half-width 9+ px at 1080p): the clear lens", Range(0, 1)) = 0.15
        _RimAlpha ("Edge alpha (refraction band, Fresnel rim)", Range(0, 1)) = 0.9
        _RimShade ("Refraction band colour (x ambient: the dim room the edge rays see)", Float) = 0.28
        _BandInner ("Refraction band start (x half-width)", Range(0, 1)) = 0.12
        _BandOuter ("Refraction band full (x half-width)", Range(0, 1)) = 0.82
        _TailAlpha ("Alpha at the tail end (head = 1)", Range(0, 1)) = 0.15
        _TailWidth ("Half-width at the tail end (head = 1)", Range(0, 1)) = 0.3
        _LensTransmit ("Lens: share of the scene behind it the drop passes on (tinted by the water)", Range(0, 1)) = 0.35
        _LensInvert ("Lens: inverted image scale (the scene sampled mirrored through the centre)", Float) = 1.5
        _Caustic ("Focused sun inside the drop (x light colour)", Float) = 0.12
        _GlintHDR ("Sun glint sparkle (HDR, x light colour)", Float) = 3.2
        _GlintSize ("Glint lobe (x half-width)", Float) = 0.15
        _GlintMinPx ("Glint lobe floor (px at 1080p)", Float) = 0.75
        _GlintLine ("Glint smeared along the streak (x sparkle)", Float) = 0.35
        _StreakFade ("Long-streak fade exponent (1 = physical share of the shutter, 0 = none)", Float) = 0.2
        _Vary ("Per-drop variation (length, width, brightness), 0..1", Range(0, 1)) = 1.0
        _BrightMin ("Dimmest drop (x alpha) of the per-drop brightness jitter", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+5" "RenderPipeline"="UniversalPipeline" }

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
            #include "../Compute/DebrisCommon.hlsl"
            #include "HighlightCommon.hlsl"

            // per-draw data set by DebrisSystem (OUTSIDE the material cbuffer)
            StructuredBuffer<DebrisParticle> _Particles;
            float4x4 _CarpetToWorld;
            int _KindFilter;

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyColor;
                float4 _MudColor;
                float _Shutter;
                float _MinStreakPx;
                float _MaxStreakPx;
                float _MinRadiusPx;
                float _SizeBoost;
                float _BodyAlpha;
                float _BodyAlphaNear;
                float _RimAlpha;
                float _RimShade;
                float _BandInner;
                float _BandOuter;
                float _TailAlpha;
                float _TailWidth;
                float _LensTransmit;
                float _LensInvert;
                float _Caustic;
                float _GlintHDR;
                float _GlintSize;
                float _GlintMinPx;
                float _GlintLine;
                float _StreakFade;
                float _Vary;
                float _BrightMin;
            CBUFFER_END

            #define DROP_FRAME_S (1.0 / 60.0)

            struct Attributes
            {
                float3 positionOS : POSITION;   // DebrisMeshBuilder.BuildQuad: corners +-0.5, uv 0..1
                float2 uv         : TEXCOORD0;  // x: 0 tail end .. 1 head end, y: across
                uint   iid        : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 q          : TEXCOORD0;  // px from the head centre: x along the motion (+ = ahead), y across
                float4 info       : TEXCOORD1;  // streak length px, half-width px, sub-pixel fade, slurry concentration
                float3 posWS      : TEXCOORD2;  // head centre
                float3 alongWS    : TEXCOORD3;  // world direction of screen +x of q
                float3 acrossWS   : TEXCOORD4;  // world direction of screen +y of q
                float2 cmm        : TEXCOORD5;  // carpet mm (lamp reveal ring / stripes)
                float3 rnd        : TEXCOORD6;  // per drop: glint strength, where it flashed (0 tail .. 1 head), brightness
            };

            // three decorrelated 0..1 values from a drop's seed (the seed also sets its size and speed, so it is hashed)
            float3 DropHash3(float s, float salt)
            {
                float3 p3 = frac(float3(s * 443.897 + salt, s * 441.423 + 0.37 + salt, s * 437.195 + 0.71 + salt));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.xxy + p3.yzz) * p3.zyx);
            }

            // erf, Winitzki's approximation (abs error < 2e-4)
            float2 DropErf(float2 x)
            {
                float2 x2 = x * x;
                const float ka = 0.147;
                return sign(x) * sqrt(1.0 - exp(-x2 * (1.2732395 + ka * x2) / (1.0 + ka * x2)));
            }

            // the streak's half-width profile: full near the head, easing to _TailWidth at the tail (a teardrop); taper 0 = a
            // round bead (a drop that hardly moves on screen), 1 = the full teardrop
            float DropWidthAt(float t, float taper)
            {
                return lerp(1.0, lerp(saturate(_TailWidth), 1.0, t * (2.0 - t)), taper);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                DebrisParticle p = _Particles[IN.iid];
                uint kind = DebKind(p.kindState);
                uint state = DebState(p.kindState);
                float3 local = float3((p.pos.x - CARPET_CENTRE_MM.x) * 0.001, p.pos.y * 0.001, (p.pos.z - CARPET_CENTRE_MM.y) * 0.001);
                float3 cWS = mul(_CarpetToWorld, float4(local, 1.0)).xyz;
                float3 vWS = mul((float3x3)_CarpetToWorld, p.vel * 0.001);            // m/s
                float4 hc = TransformWorldToHClip(cWS);
                bool visible = (state != DEB_ST_DEAD) && (kind == (uint)_KindFilter) && (hc.w > 1e-3);
                if (!visible)
                {
                    OUT.positionCS = float4(2.0, 2.0, 2.0, 1.0);                        // all four corners outside the clip volume: culled
                    return OUT;
                }
                float vary = saturate(_Vary);
                float3 h0 = DropHash3(p.seed, 0.0);                                    // length, width, brightness
                float3 h1 = DropHash3(p.seed, 5.31);                                   // glint strength, flash position

                // a short exposure (jittered per drop) ...
                float shut = max(_Shutter, 0.0) * lerp(1.0, lerp(0.55, 1.45, h0.x), vary);
                float4 tc = TransformWorldToHClip(cWS - vWS * shut);                   // where the drop was one exposure ago
                float4 tf = TransformWorldToHClip(cWS - vWS * DROP_FRAME_S);           // ... and one 60 Hz frame ago

                float2 halfRes = 0.5 * _ScaledScreenParams.xy;
                float res = _ScaledScreenParams.y / 1080.0;
                float2 hs = hc.xy / hc.w * halfRes;
                float2 ts = (tc.w > 1e-3) ? tc.xy / tc.w * halfRes : hs;              // tail behind the camera: a dot
                float2 fs = (tf.w > 1e-3) ? tf.xy / tf.w * halfRes : hs;
                float Lraw = length(hs - ts);
                float Lframe = length(hs - fs);
                float2 dv = (Lframe > 1e-3) ? hs - fs : hs - ts;
                float Ldv = length(dv);
                float2 axis = (Ldv > 1e-3) ? dv / Ldv : float2(1.0, 0.0);
                // ... never shorter than _MinStreakPx while the drop really moves that far in a frame (a fast drop still reads
                // as motion; a slow one keeps its true one-frame travel), with a soft knee toward _MaxStreakPx
                float Lexp = max(Lraw, min(Lframe, max(_MinStreakPx, 0.0) * res));
                float Lmax = max(_MaxStreakPx, 1.0) * res;
                float L = Lexp * rsqrt(1.0 + (Lexp / Lmax) * (Lexp / Lmax));

                // true radius in px (abs: the projection is y-flipped when rendering into a texture), boosted, floored, jittered
                float focalPx = abs(UNITY_MATRIX_P[1][1]) * halfRes.y;
                float rWant = 0.5 * max(p.size, 0.0) * 0.001 * focalPx / hc.w * max(_SizeBoost, 0.0);
                float rFloor = max(rWant, max(_MinRadiusPx, 0.1) * res);
                float cf = sqrt(saturate(rWant / rFloor));                             // a far, sub-pixel drop fades instead of popping
                float rPx = rFloor * lerp(1.0, lerp(0.8, 1.3, h0.y), vary);

                float2 perp = float2(-axis.y, axis.x);
                float m = rPx + 1.0;                                                    // 1 px margin for the coverage ramp
                float along = lerp(-(L + m), m, IN.uv.x);
                float across = m * (IN.uv.y * 2.0 - 1.0);
                float2 offPx = axis * along + perp * across;
                OUT.positionCS = float4(hc.xy + offPx / halfRes * hc.w, hc.zw);

                // the screen axes of q as world directions (for the fake sphere normal); screen y is view up, unless flipped
                float flipY = (UNITY_MATRIX_P[1][1] < 0.0) ? -1.0 : 1.0;
                float3 camR = UNITY_MATRIX_V[0].xyz;
                float3 camU = UNITY_MATRIX_V[1].xyz;
                OUT.alongWS = camR * axis.x + camU * (axis.y * flipY);
                OUT.acrossWS = camR * perp.x + camU * (perp.y * flipY);
                OUT.q = float2(along, across);
                OUT.info = float4(L, rPx, cf, saturate(p.dirt));
                OUT.posWS = cWS;
                OUT.cmm = p.pos.xz;
                // glint strength skewed (drops wobble: most catch the sun softly, a few flash), where along the streak the
                // flash happened (the tail half is too faint to carry it), and the drop's overall brightness
                float gS = lerp(0.2, 1.0, h1.x * h1.x * h1.x);
                OUT.rnd = float3(lerp(0.5, gS, vary), lerp(1.0, lerp(0.4, 1.0, h1.y), vary), lerp(1.0, lerp(saturate(_BrightMin), 1.0, h0.z), vary));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float L = IN.info.x, r = max(IN.info.y, 1e-3), cf = IN.info.z, dirt = IN.info.w;
                float sparkle = IN.rnd.x, tg = IN.rnd.y, bright = IN.rnd.z;
                float t = saturate(1.0 + IN.q.x / max(L, 1e-3));                       // 0 at the tail, 1 at the head
                // the teardrop grows in with the streak (full from 2 radii): a drop that hardly moves on screen (flying along the
                // view ray) is a round bead, and its back half cannot pop between opaque and faded as its streak crosses a pixel
                float taper = saturate(L / (2.0 * r));
                float rr = r * DropWidthAt(t, taper);
                // offset from the swept centre (the segment from the tail (-L, 0) to the head (0, 0))
                float sx = clamp(IN.q.x, -L, 0.0);
                float2 o = float2(IN.q.x - sx, IN.q.y);
                // q (px along / across the motion) -> screen px: the inverse of its screen derivatives (q is affine over the
                // screen-aligned quad; taken before any early out, so the derivatives are defined)
                float2 dqx = ddx(IN.q), dqy = ddy(IN.q);
                float det = dqx.x * dqy.y - dqy.x * dqx.y;
                float cov = saturate(rr + 0.5 - length(o));
                if (cov <= 0.0) return half4(0.0, 0.0, 0.0, 0.0);
                // the lens shows the scene behind it upside down (a ball lens): sample the opaque scene mirrored through the
                // swept centre (q offset -(1 + k) o), pass on _LensTransmit of it with a water tint. Over the lit tile that is
                // a darker bead, over the dark pile a dark one (not a milky grey), and a near drop shows a real refraction
                float2 dqL = -(1.0 + max(_LensInvert, 0.0)) * o;
                float2 dpx = (abs(det) > 1e-6) ? float2(dqy.y * dqL.x - dqy.x * dqL.y, dqx.x * dqL.y - dqx.y * dqL.x) / det : 0.0;
                float3 behind = SampleSceneColor(GetNormalizedScreenSpaceUV(float4(IN.positionCS.xy + dpx, IN.positionCS.zw)));
                // a pixel of a long streak is covered for a shorter share of the exposure (2r / (L + 2r)); softened by
                // _StreakFade so a fast drop reads fainter, not invisible
                float streakFade = pow(saturate(2.0 * r / (L + 2.0 * r)), max(_StreakFade, 0.0));
                streakFade = lerp(streakFade, 1.0, saturate((IN.q.x + r) / r));      // the head disc is the drop now: never faded
                float aT = lerp(1.0, lerp(saturate(_TailAlpha), 1.0, smoothstep(0.0, 0.75, t)), taper);  // the tail fades: the motion reads

                // sphere normal at this offset: across in the body, round at the caps
                float2 on = o / max(rr, 1e-3);
                float d = length(on);
                float nz = sqrt(saturate(1.0 - d * d));
                float3 V = normalize(GetCameraPositionWS() - IN.posWS);
                float3 N = normalize(IN.acrossWS * on.y + IN.alongWS * on.x + V * nz);
                float fres = 0.02 + 0.98 * pow(1.0 - saturate(dot(N, V)), 5.0);

                Light Ld = GetMainLight();
                float3 amb = SampleSH(N);
                // a clear bead: the centre mostly shows what is behind it (low alpha, a little light gathered inside); a thin
                // band at the edge bends the rays out to the dimmer room (a dark outline over the lit tile, a light one over the
                // pile); the very edge mirrors the sky colour the puddles reflect (Fresnel): water-tinted, not grey
                // a drop drawn only a few pixels wide is seen box-filtered: its dark ring and clear centre average to a darker
                // bead, which is what lets it read over the lit tile (_BodyAlpha, a wide band). A near drop (half-width 3 -> 9 px
                // at 1080p) resolves: a clear lens (_BodyAlphaNear) inside a thinner ring, not a grey egg
                float resolved = saturate((r * 1080.0 / _ScaledScreenParams.y - 3.0) / 6.0);
                float bodyA = lerp(saturate(_BodyAlpha), saturate(_BodyAlphaNear), resolved);
                float bIn = lerp(saturate(_BandInner), max(saturate(_BandInner), 0.5), resolved);
                float band = smoothstep(bIn, max(saturate(_BandOuter), bIn + 0.01), d);
                float3 lens = behind * (saturate(_LensTransmit) * 1.4 * _SkyColor.rgb) + _SkyColor.rgb * Ld.color * 0.02;
                float3 col = lerp(lens, amb * max(_RimShade, 0.0), band);
                col = lerp(col, _SkyColor.rgb * 1.15, fres * 0.6);
                col = lerp(col, _MudColor.rgb * (amb * 0.6 + Ld.color * 0.15), dirt * 0.4);   // dirty water tint
                float a = cov * cf * aT * bright * lerp(bodyA, saturate(_RimAlpha), max(band, fres)) * streakFade;

                // where the sun's reflection sits on the bead (N = H), and the focused sun (caustic) on the opposite side
                float3 H = normalize(Ld.direction + V);
                float hA = clamp(dot(H, IN.alongWS), -0.9, 0.9), hC = clamp(dot(H, IN.acrossWS), -0.9, 0.9);
                float2 dc = on + float2(hA, hC) * 0.45;
                float caus = exp(-dot(dc, dc) / 0.04) * saturate(1.0 - d);             // small: a point, not a milky glow
                float3 caustic = Ld.color * (max(_Caustic, 0.0) * caus * cov * cf * aT * bright * streakFade);

                // sun glint: the wobbling drop flashed at one moment of the exposure, so the sparkle is a point at tg along the
                // streak (not always the head), with a weaker smear of the same highlight around it along the streak
                float rg = r * DropWidthAt(tg, taper);
                float2 pg = float2(-(1.0 - tg) * L + hA * rg, hC * rg);
                float2 dq = IN.q - pg;
                // the lobe is small (_GlintSize x r), never under 0.55 px at 1080p (it scales with the target, so 4K shows the
                // same sparkle), and it is integrated over the pixel (erf box) instead of sampled at the pixel centre: a sub-pixel
                // lobe keeps its energy wherever it lands, so it does not shimmer with the drop's sub-pixel position
                float wd = max(max(_GlintSize, 0.0) * r, max(_GlintMinPx, 0.1) * _ScaledScreenParams.y / 1080.0);
                float2 e2 = DropErf((dq + 0.5) / wd) - DropErf((dq - 0.5) / wd);
                float gDot = 0.7853982 * wd * wd * e2.x * e2.y;                    // (pi wd^2 / 4) x box integral: = exp(-dq^2/wd^2) for a wide lobe
                float gx = clamp(IN.q.x - hA * rr, -L, 0.0) + hA * rr;
                float2 og = IN.q - float2(gx, hC * rr);
                float wl = 1.2 * wd;                                                   // the smear is as thin as the sparkle
                float dtg = (t - tg) / 0.3;
                float gLine = exp(-dot(og, og) / (wl * wl)) * exp(-dtg * dtg) * aT * streakFade;
                float3 glint = Ld.color * (max(_GlintHDR, 0.0) * sparkle * (gDot + max(_GlintLine, 0.0) * gLine) * cov * cf * (1.0 - 0.7 * dirt));

                float3 glow = 0.0;
                if (HL_Active())
                {
                    // pure water (2.0 = a pool in the moisture measure); the dirt lamp reads the slurry it carries like the
                    // floor puddle does (0.7 x slurry)
                    HLTerms ht = HL_Terms(0.7 * dirt, 2.0, IN.cmm, 1.0, float2(0.0, 0.0));
                    col = HL_Dim(col, ht.dimAmt);
                    glint *= 1.0 - ht.dimAmt;
                    caustic *= 1.0 - ht.dimAmt;
                    glow = ht.glow * (cov * cf * lerp(0.4, 1.0, t) * streakFade);
                }
                return half4(col * a + glint + caustic + glow, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
