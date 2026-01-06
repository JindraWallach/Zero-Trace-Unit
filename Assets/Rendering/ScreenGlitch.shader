Shader "Hidden/ScreenGlitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0, 0.1)) = 0.02
        _TimeScale ("Time Scale", Range(0, 10)) = 1.0
        _ColorShift ("RGB Shift", Range(0, 0.05)) = 0.01
        _BlockSize ("Block Size", Range(1, 100)) = 20
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            float _Intensity;
            float _TimeScale;
            float _ColorShift;
            float _BlockSize;
            
            // Vylepšená noise funkce
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Blockový glitch
            float blockNoise(float2 uv, float time)
            {
                float2 block = floor(uv * _BlockSize);
                return hash(block + time);
            }
            
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                float time = _Time.y * _TimeScale;
                
                // Horizontální offset s blocky
                float blockN = blockNoise(uv, floor(time * 10.0));
                float horizontalOffset = (blockN - 0.5) * _Intensity;
                
                // Vertical scanline glitch
                float scanline = noise(float2(uv.y * 100.0, time * 5.0));
                horizontalOffset += (scanline - 0.5) * _Intensity * 0.5;
                
                // RGB chromatic aberration
                float2 uvR = uv + float2(horizontalOffset + _ColorShift, 0);
                float2 uvG = uv + float2(horizontalOffset, 0);
                float2 uvB = uv + float2(horizontalOffset - _ColorShift, 0);
                
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvR).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvG).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvB).b;
                
                half4 col = half4(r, g, b, 1.0);
                
                // Občasné silné glitche
                float strongGlitch = step(0.98, blockN) * step(0.5, noise(float2(time, 0)));
                col.rgb = lerp(col.rgb, 1.0 - col.rgb, strongGlitch * 0.5);
                
                return col;
            }
            ENDHLSL
        }
    }
}