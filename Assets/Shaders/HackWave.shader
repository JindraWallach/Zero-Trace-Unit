Shader "Custom/HackWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaveProgress ("Wave Progress", Range(0, 1)) = 0
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.1
        _WaveWidth ("Wave Width", Range(0.01, 0.5)) = 0.1
        _BlurAmount ("Blur Amount", Range(0, 0.02)) = 0.005
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "HackWavePass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _WaveProgress;
                float _DistortionStrength;
                float _WaveWidth;
                float _BlurAmount;
            CBUFFER_END
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                
                // Vypočítej vzdálenost od aktuální pozice vlny
                float wavePos = _WaveProgress;
                float distanceFromWave = abs(uv.y - wavePos);
                
                // Vytvoř falloff - síla efektu klesá se vzdáleností od vlny
                float waveMask = 1.0 - saturate(distanceFromWave / _WaveWidth);
                waveMask = pow(waveMask, 2.0); // Exponenciální falloff
                
                // Horizontální distortion
                float distortion = sin(uv.y * 50.0 + _Time.y * 10.0) * _DistortionStrength * waveMask;
                float2 distortedUV = float2(uv.x + distortion, uv.y);
                
                // Základní vzorek
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                
                // Blur efekt - více vzorků kolem aktuální pozice
                if (waveMask > 0.01)
                {
                    float blur = _BlurAmount * waveMask;
                    half4 blurCol = col;
                    
                    // 4-směrný blur
                    blurCol += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(blur, 0));
                    blurCol += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(-blur, 0));
                    blurCol += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(0, blur));
                    blurCol += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(0, -blur));
                    
                    col = blurCol / 5.0;
                }
                
                // RGB shift pro hack efekt
                float shift = 0.003 * waveMask;
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(shift, 0)).r;
                float g = col.g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV - float2(shift, 0)).b;
                
                col.rgb = float3(r, g, b);
                
                // Přidej scan line efekt
                float scanline = sin(uv.y * 500.0 + _Time.y * 20.0) * 0.05 * waveMask;
                col.rgb += scanline;
                
                return col;
            }
            ENDHLSL
        }
    }
}