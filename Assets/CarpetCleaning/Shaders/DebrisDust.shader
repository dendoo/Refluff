Shader "CarpetCleaning/DebrisDust"
{
    // Dust puffs: camera-facing billboards from the debris pool (kind 1). Docs/FLOOR_SYSTEM.md section 3,
    // Docs/DEBRIS_SYSTEM.md "Dust puff look (2026-09-25)".
    // A puff spreads its dust over a growing disc, so its column optical depth falls as 1/area (1/grow^2), faster still as the
    // dust settles back (exp(-_Settle age)), and fades out over the last quarter of its life. Alpha = 1 - exp(-tau), so
    // overlapping puffs add optical depth: a lane of old puffs stays a veil, never an opaque disc. The quad is lifted half a
    // radius above the pile top and fades out analytically over the lowest _SoftMm above it (no camera depth texture here),
    // so on locally flat pile or floor the ground never cuts a hard edge into the cloud. The fade follows the ground under
    // the puff's centre only (K_DebSim low-passes it over ~0.12 s, so crossing the carpet rim slides instead of popping):
    // where a relief step taller than _SoftMm crosses the footprint (the carpet rim, 19-23 mm) the step still cuts an edge.
    Properties
    {
        _DustColor ("Dust Color", Color) = (0.68, 0.64, 0.58, 1)
        _Density ("Newborn optical depth (puff centre)", Float) = 0.7
        _Settle ("Airborne dust lost per second", Float) = 0.6
        _SoftMm ("Soft fade above the pile (mm)", Float) = 12
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Compute/DebrisCommon.hlsl"

            // globals (CarpetSimulation) + per-draw data set by DebrisSystem, OUTSIDE the material cbuffer
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_linear_repeat);
            StructuredBuffer<DebrisParticle> _Particles;
            float4x4 _CarpetToWorld;
            int _KindFilter;

            CBUFFER_START(UnityPerMaterial)
                float4 _DustColor;
                float _Density;
                float _Settle;
                float _SoftMm;
            CBUFFER_END

            #define DUST_TWO_PI 6.28318530718

            struct Attributes
            {
                float3 positionOS : POSITION;   // quad corners +-0.5 in XY
                float2 uv         : TEXCOORD0;
                uint   iid        : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 info       : TEXCOORD1;   // centre optical depth, age s, age 0..1, grow
                float2 misc       : TEXCOORD2;   // height above the pile top (mm, linear over the quad), seed
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                DebrisParticle p = _Particles[IN.iid];
                uint kind = DebKind(p.kindState);
                uint state = DebState(p.kindState);
                bool visible = (state != DEB_ST_DEAD) && (kind == (uint)_KindFilter);
                float lifeMax = max(p.pad, 1e-3);
                float life01 = saturate(p.life / lifeMax);
                float age01 = 1.0 - life01;
                float ageS = max(lifeMax - p.life, 0.0);
                float grow = 1.0 + 3.0 * age01;
                float sizeMm = visible ? max(p.size, 0.0) * grow : 0.0;

                // the same dust over grow^2 the area: 1/grow^2, minus what has already settled back (exp(-_Settle age)); the last
                // quarter of the life settles out completely; a 50 ms birth ramp avoids a pop
                float tauC = _Density / (grow * grow) * exp(-max(_Settle, 0.0) * ageS) * smoothstep(0.0, 0.25, life01) * saturate(ageS / 0.05);

                // carpet up in world (column 1 of the carpet matrix) and the carpet-local length of a world offset along it
                float3 upCol = _CarpetToWorld._m01_m11_m21;
                float upLen2 = max(dot(upCol, upCol), 1e-8);
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                // K_DebSim keeps the ground (pile top + water, or the floor; low-passed) under a puff in p.dirt. Lift the puff until its centre
                // stands half a radius (along the view's up) above it: a spreading cloud billows up instead of sinking half of itself
                // into the pile, and its base still hugs the ground inside the soft fade.
                float groundMm = p.dirt;
                float upness = abs(dot(up, upCol)) * rsqrt(upLen2);
                float liftMm = max(0.0, 0.5 * 0.5 * sizeMm * upness - (p.pos.y - groundMm));
                float3 local = float3((p.pos.x - CARPET_CENTRE_MM.x) * 0.001, (p.pos.y + liftMm) * 0.001, (p.pos.z - CARPET_CENTRE_MM.y) * 0.001);
                float3 centreWS = mul(_CarpetToWorld, float4(local, 1.0)).xyz;
                // camera-facing billboard, rotated per puff and slowly spinning
                float ang = p.seed * DUST_TWO_PI + age01 * 1.5 * (p.seed - 0.5);
                float c = cos(ang), s = sin(ang);
                float2 o = IN.positionOS.xy;
                o = float2(c * o.x - s * o.y, s * o.x + c * o.y);
                float3 offWS = (right * o.x + up * o.y) * (sizeMm * 0.001);
                float3 posWS = centreWS + offWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.uv = IN.uv;
                OUT.info = float4(tauC, ageS, age01, grow);
                OUT.misc = float2((p.pos.y + liftMm - groundMm) + dot(offWS, upCol) / upLen2 * 1000.0, p.seed);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float tauC = IN.info.x, ageS = IN.info.y, age01 = IN.info.z, grow = IN.info.w;
                float hAbove = IN.misc.x, seed = IN.misc.y;
                float2 q = IN.uv - 0.5;
                float r2 = 4.0 * dot(q, q);                                              // 0 centre .. 1 at the quad's inscribed circle
                float prof = exp(-3.0 * r2) * saturate(1.0 - r2);                      // soft Gaussian column, no flat core
                // density breakup: coarse lobes carried by the spreading puff (puff UV) + fine eddies at a fixed world scale
                // (newborn-size units), log-normal so the mean stays ~1 (mass kept) while the contrast grows with age: old puffs
                // tear into wisps and gaps instead of magnifying one blob pattern
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_linear_repeat, q * 0.85 + seed * 23.0 + float2(0.0, ageS * 0.10)).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_linear_repeat, q * grow * 0.75 + seed * 41.0 - float2(ageS * 0.18, 0.0)).a;
                float nz = (0.55 * n1 + 0.45 * n2 - 0.46) / 0.085;                       // ~N(0,1)
                float sig = lerp(0.55, 1.15, age01);
                float wisp = min(exp(sig * nz - 0.5 * sig * sig), 4.0);
                float soft = smoothstep(0.0, max(_SoftMm, 0.1), hAbove);
                float tau = tauC * prof * wisp * soft;
                float a = 1.0 - exp(-tau);

                Light L = GetMainLight();
                float3 col = _DustColor.rgb * (L.color * 0.6 + SampleSH(float3(0.0, 1.0, 0.0)) * 0.8) * lerp(1.0, 0.8, saturate(tau));
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
