Shader "DisasterResponse/FLIRThermalVision"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _ThermalIntensity ("Thermal Intensity", Range(0, 2)) = 1.0
        _NoiseStrength ("Thermal Sensor Noise", Range(0, 0.2)) = 0.04
        _ScanlineDensity ("Scanline Density", Float) = 450.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "FLIR_Ironbow"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _ThermalIntensity;
                float _NoiseStrength;
                float _ScanlineDensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Authentic Ironbow false-color thermal palette generator
            half3 Ironbow(float t)
            {
                t = saturate(t);
                half3 c;
                if (t < 0.2)
                {
                    // Dark navy to deep purple
                    c = lerp(half3(0.04, 0.04, 0.20), half3(0.35, 0.02, 0.45), t / 0.2);
                }
                else if (t < 0.45)
                {
                    // Purple to crimson red
                    c = lerp(half3(0.35, 0.02, 0.45), half3(0.85, 0.08, 0.12), (t - 0.2) / 0.25);
                }
                else if (t < 0.75)
                {
                    // Crimson to vivid amber/orange
                    c = lerp(half3(0.85, 0.08, 0.12), half3(1.0, 0.65, 0.02), (t - 0.45) / 0.3);
                }
                else if (t < 0.92)
                {
                    // Amber to bright lemon yellow (human body heat / hot spots)
                    c = lerp(half3(1.0, 0.65, 0.02), half3(1.0, 0.98, 0.35), (t - 0.75) / 0.17);
                }
                else
                {
                    // Pure white-hot (active flame / combustion core)
                    c = lerp(half3(1.0, 0.98, 0.35), half3(1.0, 1.0, 1.0), (t - 0.92) / 0.08);
                }
                return c;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Calculate perceived luminance / heat signature
                float heat = dot(sceneColor.rgb, float3(0.299, 0.587, 0.114)) * _ThermalIntensity;

                // Subtle infrared sensor micro-noise
                float2 noiseCoord = input.uv * 80.0 + float2(_Time.y * 3.0, _Time.y * 5.0);
                float sensorNoise = (frac(sin(dot(noiseCoord, float2(12.9898, 78.233))) * 43758.5453) - 0.5) * _NoiseStrength;
                heat = saturate(heat + sensorNoise);

                half3 thermalColor = Ironbow(heat);

                // Subtle tactical scanlines
                float scanline = sin(input.uv.y * _ScanlineDensity) * 0.04;
                thermalColor -= scanline;

                return half4(thermalColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
