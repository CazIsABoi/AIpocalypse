Shader "Custom/HexBarrier"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (4, 0.25, 0.2, 1)
        _HexSize ("Hex Size (world units)", Float) = 0.35
        _LineWidth ("Line Width", Range(0.01, 0.25)) = 0.06
        _LineGlow ("Line Glow", Range(0, 1)) = 0.5
        _Fill ("Fill Inside Hexes", Range(0, 0.5)) = 0.06
        _Flicker ("Cell Flicker", Range(0, 1)) = 0.6
        _PulseSpeed ("Pulse Speed", Float) = 2
        _ScanSpeed ("Scan Band Speed", Float) = 1.5
        _ScanStrength ("Scan Band Strength", Range(0, 2)) = 0.6
        _BottomGlow ("Bottom Glow", Range(0, 4)) = 1.5
        _EdgeGlow ("Edge Glow (fresnel)", Range(0, 2)) = 0.6
        _Intensity ("Intensity (fade in/out)", Range(0, 1)) = 1
        _Flash ("Hit Flash", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One      // additive
        ZWrite Off
        Cull Off           // both sides

        Pass
        {
            Name "HexBarrier"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _HexSize, _LineWidth, _LineGlow, _Fill, _Flicker, _PulseSpeed;
                float _ScanSpeed, _ScanStrength, _BottomGlow, _EdgeGlow, _Intensity, _Flash;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  heightOS   : TEXCOORD2;   
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.heightOS = v.positionOS.y;
                return o;
            }

            float2 Mod(float2 x, float2 y) { return x - y * floor(x / y); }   

            float HexDist(float2 p)
            {
                p = abs(p);
                return max(dot(p, normalize(float2(1, 1.7320508))), p.x);
            }

            // edge = 0 on a hex edge, 0.5 in its middle. id = which hex (for per-cell randomness)
            float HexGrid(float2 uv, out float2 id)
            {
                const float2 r = float2(1, 1.7320508);
                const float2 h = r * 0.5;
                float2 a = Mod(uv, r) - h;
                float2 b = Mod(uv - h, r) - h;
                float2 gv = dot(a, a) < dot(b, b) ? a : b;
                id = uv - gv;
                return 0.5 - HexDist(gv);
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 an = abs(n);
                // pick the world plane that faces this surface (top face uses XZ, walls use XY or ZY)
                float2 uv = an.y > max(an.x, an.z) ? i.positionWS.xz
                          : (an.x > an.z ? i.positionWS.zy : i.positionWS.xy);
                uv /= _HexSize;

                float2 id;
                float edge = HexGrid(uv, id);

                float lines = 1 - smoothstep(_LineWidth * 0.5, _LineWidth, edge);     // crisp line
                float glow  = exp(-edge * 14) * _LineGlow;                            // soft glow around it

                float rnd = Hash(id);
                float flicker = lerp(1, 0.55 + 0.45 * sin(_Time.y * 3 + rnd * 6.2832), _Flicker);
                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);

                float scan = pow(saturate(sin(i.positionWS.y * 2.5 - _Time.y * _ScanSpeed) * 0.5 + 0.5), 10) * _ScanStrength;
                float bottom = pow(saturate(0.5 - i.heightOS), 3) * _BottomGlow;

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = pow(1 - saturate(abs(dot(n, viewDir))), 3) * _EdgeGlow;

                float brightness = (lines + glow) * flicker + _Fill + scan * (lines + 0.3) + fresnel + bottom;
                brightness *= pulse * (1 + _Flash * 2);

                return half4(_Color.rgb * brightness * _Intensity, 1);
            }
            ENDHLSL
        }
    }
}
