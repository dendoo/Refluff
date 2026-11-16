Shader "CarpetCleaning/FirstPersonSpray"
{
    Properties { _Foam ("Foam", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Foam;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; o.color = i.color; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float edge = saturate(1.0 - abs(i.uv.y * 2.0 - 1.0));
                float along = i.uv.x * 46.0 - _Time.y * 18.0;
                float grain = frac(sin(floor(along) * 41.17 + floor(i.uv.y * 15.0) * 127.1) * 43758.54);
                float droplets = lerp(0.36, 1.0, smoothstep(0.22, 0.67, grain));
                float alpha = i.color.a * edge * edge * lerp(0.6 + 0.4 * droplets, droplets, _Foam);
                return half4(i.color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
