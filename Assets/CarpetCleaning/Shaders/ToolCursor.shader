Shader "CarpetCleaning/ToolCursor"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 0.6)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // globals (set by CarpetRenderer.SetCursor)
            float4 _ToolCursor;   // (u, v, radiusMm, active)
            float4 _ToolCursor2;  // (dir.x, dir.y, lengthMm, toolId)

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // quad UV -> centred [-0.5, 0.5]^2; the quad is scaled to the footprint by CarpetRenderer
                float2 p = IN.uv - 0.5;
                float sd;
                if (_ToolCursor2.z > 0.0)
                {
                    // blade: rounded-rect outline. Corner radius = 12% of the shorter side.
                    float2 b = float2(0.5, 0.5);
                    float r = 0.06;
                    float2 q = abs(p) - (b - r);
                    sd = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
                }
                else
                {
                    // disc: ring at the quad's inscribed circle
                    sd = length(p) - 0.5;
                }
                // 2 px line, anti-aliased with screen-space derivatives of the distance field
                float px = max(fwidth(sd), 1e-5);
                float halfW = 1.0 * px;                              // 2 px total width
                float ring = 1.0 - smoothstep(halfW, halfW + px, abs(sd + halfW));
                float a = ring * _Color.a * _ToolCursor.w;
                if (a < 0.003) discard;
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
