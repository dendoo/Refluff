Shader "CarpetCleaning/WindowLightVolume"
{
    Properties
    {
        [HDR] _Tint ("Daylight scattering colour", Color) = (1,0.96,0.86,1)
        _WindowOrigin ("Portal centre (world metres)", Vector) = (0,2.65,-5.8,1)
        _WindowRight ("Portal right / half width", Vector) = (1,0,0,0.7)
        _WindowUp ("Portal up / half height", Vector) = (0,1,0,0.835)
        _BeamDirection ("Travel direction / length", Vector) = (-0.321,-0.766,0.557,4.8)
        _BeamParams ("Extinction / edge metres / depth fade / half mullion", Vector) = (0.046,0.055,0.16,0.0275)
        _NoiseParams ("Density noise scale / variation / drift / scattering", Vector) = (1.15,0.18,0.035,3.5)
        _Anisotropy ("Forward scattering", Range(0,0.65)) = 0.28
        _DensityNoise ("Tiling atmospheric density", 3D) = "white" {}
        _AtmosphereClock ("Preview clock / enabled", Vector) = (0,0,0,0)
        _ShadowWeight ("Volumetric shadow strength", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+25" }
        Pass
        {
            Name "WindowSingleScattering"
            Tags { "LightMode"="UniversalForward" }
            // Rasterise the exit face, even when the camera is inside the volume. Opaque depth is
            // evaluated analytically below; hardware depth-testing the far face would erase foreground rays.
            Cull Front
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE3D(_DensityNoise);
            SAMPLER(sampler_DensityNoise);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float4 _WindowOrigin;
                float4 _WindowRight;
                float4 _WindowUp;
                float4 _BeamDirection;
                float4 _BeamParams;
                float4 _NoiseParams;
                float _Anisotropy;
                float4 _AtmosphereClock;
                float _ShadowWeight;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                return output;
            }

            float DensityMotion(float3 positionWS)
            {
                float time=_AtmosphereClock.y>.5?_AtmosphereClock.x:_Time.y;
                float3 uvw=positionWS*_NoiseParams.x;
                float3 wind=time*_NoiseParams.z*float3(.37,.15,-.23);
                float broad=SAMPLE_TEXTURE3D_LOD(_DensityNoise,sampler_DensityNoise,uvw+wind,0).r;
                float detail=SAMPLE_TEXTURE3D_LOD(_DensityNoise,sampler_DensityNoise,
                    uvw*2.3-wind*.63+float3(.19,.61,.37),0).r;
                // Two counter-drifting fields form and dissolve pockets of illuminated dust.
                // Multiplication retains contrast after integration; x4 preserves the average.
                return lerp(1.0,4.0*broad*detail,saturate(_NoiseParams.y));
            }

            float SunVisibility(float3 positionWS)
            {
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord=TransformWorldToShadowCoord(positionWS);
                    if(BEYOND_SHADOW_FAR(shadowCoord)) return 1.0;
                    // One filtered comparison per march sample. Surface PCF kernels are unnecessary
                    // here: the volume's spatial integral already softens a shadow through the air.
                    float shadow=SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,
                        sampler_LinearClampCompare,shadowCoord.xyz);
                    return lerp(1.0,shadow,saturate(GetMainLightShadowParams().x*_ShadowWeight));
                #else
                    return 1.0;
                #endif
            }

            float2 BoundedRayInterval(float3 rayOrigin,float3 rayDirection,float3 boundMin,float3 boundMax)
            {
                float3 reciprocal=(step(0.0,rayDirection)*2.0-1.0)/max(abs(rayDirection),1e-6);
                float3 a=(boundMin-rayOrigin)*reciprocal;
                float3 b=(boundMax-rayOrigin)*reciprocal;
                float3 lo=min(a,b), hi=max(a,b);
                return float2(max(max(lo.x,lo.y),lo.z),min(min(hi.x,hi.y),hi.z));
            }

            float PortalDensity(float3 positionWS,float3 normal,float normalTravel)
            {
                float3 relative=positionWS-_WindowOrigin.xyz;
                float travel=dot(relative,normal)/normalTravel;
                float3 aperture=relative-_BeamDirection.xyz*travel;
                float x=abs(dot(aperture,_WindowRight.xyz));
                float y=abs(dot(aperture,_WindowUp.xyz));
                float edge=max(_BeamParams.y,.001);
                float mask=smoothstep(0.0,edge,_WindowRight.w-x)*smoothstep(0.0,edge,_WindowUp.w-y);
                mask*=smoothstep(_BeamParams.w,_BeamParams.w+edge*.45,y); // horizontal window mullion
                mask*=smoothstep(0.0,.055,travel)*(1.0-smoothstep(_BeamDirection.w-.45,_BeamDirection.w,travel));
                // The raster bounds extend beyond the room; air never exists under the solid floor.
                mask*=smoothstep(-.003,.035,positionWS.y)*(1.0-smoothstep(3.76,3.8,positionWS.y));
                return mask*max(_BeamParams.x,0.0);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 cameraWS=GetCameraPositionWS();
                float3 rayWS=normalize(input.positionWS-cameraWS);

                float2 uv=GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    rawDepth=lerp(UNITY_NEAR_CLIP_VALUE,1.0,rawDepth);
                #endif
                float3 sceneWS=ComputeWorldSpacePosition(uv,rawDepth,UNITY_MATRIX_I_VP);
                float sceneDistance=max(0.0,dot(sceneWS-cameraWS,rayWS));
                // Physical room clipping also protects captures/reflection cameras whose copied depth
                // may not contain the procedural floor. Distances here remain world metres.
                if(rayWS.y<-.00001) sceneDistance=min(sceneDistance,max(0.0,(-.003-cameraWS.y)/rayWS.y));
                if(rayWS.y>.00001) sceneDistance=min(sceneDistance,max(0.0,(3.8-cameraWS.y)/rayWS.y));

                float3 normal=normalize(cross(_WindowRight.xyz,_WindowUp.xyz));
                float normalTravel=max(dot(normal,_BeamDirection.xyz),.001);
                // Work in sheared portal coordinates (aperture x/y, distance along the sun ray).
                // Clip each HALF pane analytically before sampling. Integrating the loose AABB
                // missed thin mullions at regular intervals and produced visible marching bands.
                float3 relative=cameraWS-_WindowOrigin.xyz;
                float originTravel=dot(relative,normal)/normalTravel;
                float directionTravel=dot(rayWS,normal)/normalTravel;
                float3 apertureOrigin=relative-_BeamDirection.xyz*originTravel;
                float3 apertureRay=rayWS-_BeamDirection.xyz*directionTravel;
                float3 portalOrigin=float3(dot(apertureOrigin,_WindowRight.xyz),dot(apertureOrigin,_WindowUp.xyz),originTravel);
                float3 portalRay=float3(dot(apertureRay,_WindowRight.xyz),dot(apertureRay,_WindowUp.xyz),directionTravel);
                float2 lower=BoundedRayInterval(portalOrigin,portalRay,
                    float3(-_WindowRight.w,-_WindowUp.w,0.0),float3(_WindowRight.w,-_BeamParams.w,_BeamDirection.w));
                float2 upper=BoundedRayInterval(portalOrigin,portalRay,
                    float3(-_WindowRight.w,_BeamParams.w,0.0),float3(_WindowRight.w,_WindowUp.w,_BeamDirection.w));
                lower=float2(max(lower.x,0.0),min(lower.y,sceneDistance));
                upper=float2(max(upper.x,0.0),min(upper.y,sceneDistance));
                float2 lengths=max(float2(lower.y-lower.x,upper.y-upper.x),0.0);
                clip(lengths.x+lengths.y-.0001);
                // Traverse front-to-back, including cameras located inside either half-pane.
                if(lower.x>upper.x)
                {
                    float2 swap=lower; lower=upper; upper=swap;
                    lengths=lengths.yx;
                }

                float cosine=dot(_BeamDirection.xyz,-rayWS);
                float g=clamp(_Anisotropy,0.0,.65);
                float phase=(1.0-g*g)/pow(max(1.0+g*g-2.0*g*cosine,.05),1.5);
                phase=.65+.35*phase;

                const int SamplesPerPane=12;
                const int Steps=SamplesPerPane*2;
                float transmittance=1.0;
                float scattering=0.0;
                float jitter=frac(52.9829189*frac(dot(input.positionCS.xy,float2(.06711056,.00583715))));
                [loop] for(int i=0;i<Steps;i++)
                {
                    bool second=i>=SamplesPerPane;
                    float stride=(second?lengths.y:lengths.x)/SamplesPerPane;
                    if(stride<=.000001) continue;
                    float distance=(second?upper.x:lower.x)+((second?i-SamplesPerPane:i)+jitter)*stride;
                    float3 sampleWS=cameraWS+rayWS*distance;
                    float density=PortalDensity(sampleWS,normal,normalTravel);
                    density*=DensityMotion(sampleWS);
                    // The finite volume terminates on the actual opaque scene; this softens the last centimetres.
                    density*=saturate((sceneDistance-distance)/max(_BeamParams.z,.001));
                    float stepTransmission=exp(-density*stride);
                    scattering+=transmittance*(1.0-stepTransmission)*SunVisibility(sampleWS);
                    transmittance*=stepTransmission;
                }
                float3 radiance=_Tint.rgb*(scattering*phase*max(_NoiseParams.w,0.0));
                return half4(radiance,saturate(1.0-transmittance));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
