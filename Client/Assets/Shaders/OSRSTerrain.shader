Shader "Custom/OSRSTerrain"
{
    Properties
    {
        _BlendBands ("Blend Bands", Range(2, 16)) = 10
        _BlendPower ("Blend Power", Range(0.1, 5)) = 1.0
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.7
        _LightStrength ("Light Strength", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OSRSTerrain"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;

                float4 corner00 : TEXCOORD0;
                float4 corner10 : TEXCOORD1;
                float4 corner01 : TEXCOORD2;
                float4 corner11 : TEXCOORD3;

                float2 tileUV : TEXCOORD4;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;

                float4 corner00 : TEXCOORD2;
                float4 corner10 : TEXCOORD3;
                float4 corner01 : TEXCOORD4;
                float4 corner11 : TEXCOORD5;

                float2 tileUV : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)

            float _BlendBands;
            float _BlendPower;
            float _AmbientStrength;
            float _LightStrength;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);

                output.corner00 = input.corner00;
                output.corner10 = input.corner10;
                output.corner01 = input.corner01;
                output.corner11 = input.corner11;

                output.tileUV = input.tileUV;

                return output;
            }

            float QuantizeBlend(float blend)
            {
                float bandCount = max(2.0, _BlendBands);

                blend = saturate(blend);

                return round(blend * (bandCount - 1.0)) / (bandCount - 1.0);
            }

            float3 BlendEdge(float3 colorA, float alphaA, float3 colorB, float alphaB, float position)
            {
                float blendAmount = min(alphaA, alphaB);
                float blend = saturate(position);

                blend = pow(blend, _BlendPower);
                blend = QuantizeBlend(blend);
                blend *= blendAmount;

                return lerp(colorA, colorB, blend);
            }

            float3 GetTerrainColor(float2 uv, float4 c00, float4 c10, float4 c01, float4 c11)
            {
                float3 bottom = BlendEdge(c00.rgb, c00.a, c10.rgb, c10.a, uv.x);
                float3 top = BlendEdge(c01.rgb, c01.a, c11.rgb, c11.a, uv.x);

                float bottomBlend = min(c00.a, c10.a);
                float topBlend = min(c01.a, c11.a);
                float verticalBlend = min(bottomBlend, topBlend);

                float blend = saturate(uv.y);

                blend = pow(blend, _BlendPower);
                blend = QuantizeBlend(blend);
                blend *= verticalBlend;

                return lerp(bottom, top, blend);
            }

            half3 GetURPLighting(float3 positionWS, float3 normalWS)
            {
                normalWS = normalize(normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);

                Light mainLight = GetMainLight(shadowCoord);

                float NdotL = saturate(dot(normalWS, mainLight.direction));

                float3 directLighting =
                    mainLight.color *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation *
                    NdotL;

                float3 ambientLighting =
                    SampleSH(normalWS);

                float3 lighting =
                    ambientLighting * _AmbientStrength +
                    directLighting * _LightStrength;

                return lighting;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 terrainColor = GetTerrainColor(
                    input.tileUV,
                    input.corner00,
                    input.corner10,
                    input.corner01,
                    input.corner11
                );

                float3 normalWS = normalize(input.normalWS);

                float3 lighting = GetURPLighting(
                    input.positionWS,
                    normalWS
                );

                terrainColor *= lighting;

                return half4(terrainColor, 1.0);
            }

            ENDHLSL
        }
    }
}