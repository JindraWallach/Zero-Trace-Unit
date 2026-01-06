Shader "Hidden/ScreenGlitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0, 0.15)) = 0.02
        _TimeScale ("Time Scale", Range(0, 20)) = 1.0
        _ColorShift ("RGB Shift", Range(0, 0.1)) = 0.01
        _BlockSize ("Block Size", Range(1, 200)) = 20
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _InversionIntensity ("Inversion Intensity", Range(0, 1)) = 0.5
        _VerticalShift ("Vertical Shift", Range(0, 0.2)) = 0.0
        _NoiseFrequency ("Noise Frequency", Range(0, 10)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ScreenGlitchPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            // Optimization: Disable unnecessary variants
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            float _Intensity;
            float _TimeScale;
            float _ColorShift;
            float _BlockSize;
            float _ScanlineIntensity;
            float _InversionIntensity;
            float _VerticalShift;
            float _NoiseFrequency;
            
            // Optimized hash function (fewer operations)
            float hash(float2 p)
            {
                // Single frac operation instead of multiple
                p = frac(p * 0.3183099 + 0.71);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }
            
            // Optimized noise (fewer texture lookups)
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // Smoothstep is faster than custom interpolation
                f = f * f * (3.0 - 2.0 * f);
                
                // Use single hash call with offsets
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Block noise (optimized)
            float blockNoise(float2 uv, float time)
            {
                float2 block = floor(uv * _BlockSize);
                return hash(block + time);
            }
            
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                
                // Early exit if intensity is zero (CRITICAL for performance)
                if (_Intensity < 0.001)
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }
                
                float time = _Time.y * _TimeScale;
                
                // === HORIZONTAL GLITCH ===
                float blockN = blockNoise(uv, floor(time * 10.0));
                float horizontalOffset = (blockN - 0.5) * _Intensity;
                
                // === VERTICAL GLITCH (optional) ===
                float verticalOffset = 0;
                if (_VerticalShift > 0.001)
                {
                    float verticalNoise = noise(float2(uv.x * 50.0, time * 3.0)) - 0.5;
                    verticalOffset = verticalNoise * _VerticalShift;
                }
                
                // === SCANLINE GLITCH ===
                if (_ScanlineIntensity > 0.001)
                {
                    float scanline = noise(float2(uv.y * 100.0 * _NoiseFrequency, time * 5.0));
                    horizontalOffset += (scanline - 0.5) * _Intensity * _ScanlineIntensity;
                }
                
                // === RGB CHROMATIC ABERRATION ===
                float2 uvR = uv + float2(horizontalOffset + _ColorShift, verticalOffset);
                float2 uvG = uv + float2(horizontalOffset, verticalOffset);
                float2 uvB = uv + float2(horizontalOffset - _ColorShift, verticalOffset);
                
                // Clamp UVs to prevent sampling outside texture (FIXES WHITE BLOCKS!)
                uvR = saturate(uvR);
                uvG = saturate(uvG);
                uvB = saturate(uvB);
                
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvR).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvG).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvB).b;
                
                half4 col = half4(r, g, b, 1.0);
                
                // === STRONG GLITCH (color inversion) ===
                if (_InversionIntensity > 0.001)
                {
                    float inversionThreshold = 1.0 - _InversionIntensity * 0.02; // 0.98 to 1.0
                    float strongGlitch = step(inversionThreshold, blockN) * step(0.5, noise(float2(time, 0)));
                    col.rgb = lerp(col.rgb, 1.0 - col.rgb, strongGlitch * _InversionIntensity);
                }
                
                return col;
            }
            ENDHLSL
        }
    }
}