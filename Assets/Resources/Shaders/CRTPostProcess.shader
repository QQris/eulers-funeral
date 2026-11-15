Shader "EulersFuneral/CRTPostProcess"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _Curvature ("Barrel Curvature", Range(0.0, 0.12)) = 0.028
        _ChromaticAberration ("Chromatic Aberration", Range(0.0, 0.01)) = 0.0018
        _ScanlineIntensity ("Scanline Intensity", Range(0.0, 0.5)) = 0.14
        _ScanlineCount ("Scanline Count", Float) = 540.0
        _VignetteIntensity ("Vignette Intensity", Range(0.0, 1.0)) = 0.30
        _NoiseIntensity ("Analog Noise", Range(0.0, 0.1)) = 0.018
        _BrightnessBoost ("Phosphor Brightness", Range(0.8, 1.4)) = 1.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Curvature;
            float _ChromaticAberration;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _VignetteIntensity;
            float _NoiseIntensity;
            float _BrightnessBoost;

            float2 DistortCRT(float2 uv, float curvature)
            {
                float2 centered = uv * 2.0 - 1.0;
                float2 offset = centered.yx;
                centered += centered * (offset * offset) * curvature;
                return centered * 0.5 + 0.5;
            }

            float HashNoise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = DistortCRT(i.uv, _Curvature);

                // Borde exterior del tubo CRT
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed4(0.0, 0.0, 0.0, 1.0);
                }

                // Aberración cromática radial hacia los bordes del monitor
                float2 centerDist = uv - 0.5;
                float2 caOffset = centerDist * _ChromaticAberration * (1.0 + dot(centerDist, centerDist) * 2.0);

                float r = tex2D(_MainTex, clamp(uv + caOffset, 0.001, 0.999)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, clamp(uv - caOffset, 0.001, 0.999)).b;
                float3 col = float3(r, g, b);

                // Líneas de escaneo horizontales (Scanlines)
                float scanWave = sin(uv.y * _ScanlineCount * 3.14159265 - _Time.y * 3.0);
                float scanFactor = 1.0 - _ScanlineIntensity * (0.5 + 0.5 * scanWave);
                col *= scanFactor;

                // Sutil rejilla vertical de subpíxeles RGB de fósforo
                float grill = 1.0 - (_ScanlineIntensity * 0.25) * (0.5 + 0.5 * sin(uv.x * _ScanlineCount * 1.77 * 3.14159265));
                col *= grill;

                // Ruido analógico tenue
                float grain = (HashNoise(uv * 512.0 + frac(_Time.y * 7.13)) - 0.5) * _NoiseIntensity;
                col += grain;

                // Viñeteado suave en las esquinas del tubo
                float2 vigUv = uv * (1.0 - uv.yx);
                float vig = vigUv.x * vigUv.y * 16.0;
                vig = saturate(pow(max(vig, 0.0001), _VignetteIntensity));
                col *= vig;

                col *= _BrightnessBoost;
                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
