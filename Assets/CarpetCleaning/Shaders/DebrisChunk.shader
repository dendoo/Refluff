Shader "CarpetCleaning/DebrisChunk"
{
    // Crust shards drawn with Graphics.RenderMeshIndirect from the debris pool (Docs/FLOOR_SYSTEM.md section 3).
    // Instance data: StructuredBuffer<DebrisParticle> _Particles indexed by SV_InstanceID. Dead / other-kind instances get scale 0.
    Properties
    {
        _CrustColor ("Crust Color", Color) = (0.50, 0.45, 0.38, 1)
        _MudColor ("Mud Color", Color) = (0.24, 0.15, 0.08, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "../Compute/DebrisCommon.hlsl"
        #include "HighlightCommon.hlsl"

        // per-draw data set by DebrisSystem (material properties; OUTSIDE the material cbuffer, like the sim globals)
        StructuredBuffer<DebrisParticle> _Particles;
        float4x4 _CarpetToWorld;
        int _KindFilter;

        CBUFFER_START(UnityPerMaterial)
            float4 _CrustColor;
            float4 _MudColor;
        CBUFFER_END

        float3 QRotate(float4 q, float3 v) { return v + 2.0 * cross(q.xyz, cross(q.xyz, v) + q.w * v); }

        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            uint   iid        : SV_InstanceID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 posWS      : TEXCOORD0;
            float3 nWS        : TEXCOORD1;
            float2 ds         : TEXCOORD2;   // dirt, seed
            float2 cmm        : TEXCOORD3;   // carpet mm of the chip (the inspection lamp's scan ring)
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            DebrisParticle p = _Particles[IN.iid];
            uint kind = DebKind(p.kindState);
            uint state = DebState(p.kindState);
            bool visible = (state != DEB_ST_DEAD) && (kind == (uint)_KindFilter);
            float scale = visible ? max(p.size, 0.0) * 0.001 : 0.0;          // mesh is unit diameter, size is mm
            float4 q = p.rot;
            float l2 = dot(q, q);
            q = (l2 > 1e-8) ? q * rsqrt(l2) : float4(0.0, 0.0, 0.0, 1.0);
            float3 local = float3((p.pos.x - CARPET_CENTRE_MM.x) * 0.001, p.pos.y * 0.001, (p.pos.z - CARPET_CENTRE_MM.y) * 0.001);
            float3 v = QRotate(q, IN.positionOS * scale);
            float3 n = QRotate(q, IN.normalOS);
            float3 posWS = mul(_CarpetToWorld, float4(local + v, 1.0)).xyz;
            OUT.positionCS = TransformWorldToHClip(posWS);
            OUT.posWS = posWS;
            OUT.nWS = mul((float3x3)_CarpetToWorld, n);
            OUT.ds = float2(saturate(p.dirt), saturate(p.seed));
            OUT.cmm = p.pos.xz;
            return OUT;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = IN.nWS;
                float l2 = dot(N, N);
                N = (l2 > 1e-8) ? N * rsqrt(l2) : float3(0.0, 1.0, 0.0);
                float dirt = IN.ds.x, seed = IN.ds.y;

                // palette: crust plate colour darkening toward mud with dirt and per-chip variation
                float3 albedo = lerp(_CrustColor.rgb, _MudColor.rgb, saturate(0.35 * dirt + 0.25 * (1.0 - seed)));
                albedo *= 0.85 + 0.3 * frac(seed * 7.31);
                float upness = saturate(dot(N, float3(0.0, 1.0, 0.0)) * 0.5 + 0.5);
                albedo *= lerp(0.55, 1.0, upness);                                  // underside darker

                Light L = GetMainLight();
                float3 V = normalize(GetCameraPositionWS() - IN.posWS);
                float3 H = normalize(L.direction + V);
                float ndl = saturate(dot(N, L.direction) * 0.6 + 0.4);            // Lambert wrap
                float spec = pow(saturate(dot(N, H)), 24.0) * 0.06;
                float fres = pow(1.0 - saturate(dot(N, V)), 4.0) * 0.05;          // fresnel rim
                float3 col = albedo * (L.color * ndl + SampleSH(N)) + (spec + fres) * L.color;
                // inspection lamp (Docs/HIGHLIGHTS.md): a crust chip is soil through and through (it counts as a caked cell, 1.0);
                // lit faces glow more than the underside so the chip keeps its shape. A chip lying on the carpet follows the
                // carpet's cut (dark once CLEAN shows 100 %), one on the tile always glows.
                if (HL_Active())
                {
                    HLTerms ht = HL_Terms(1.0, 0.0, IN.cmm, lerp(0.55, 1.0, upness), CarpetCoversCMM(IN.cmm) ? HL_CarpetCut() : float2(0.0, 0.0));
                    col = HL_Dim(col, ht.dimAmt) + ht.glow;
                }
                return half4(col, 1.0);
            }
            ENDHLSL
        }

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
