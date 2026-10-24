// Plain additive glow (URP, unlit): an HDR color times the vertex color. For lightning arcs, lasers, sparks.
// Bright HDR values (above 1) + Bloom in the Volume = glow.
Shader "Custom/AdditiveGlow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.6, 0.9, 4, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "AdditiveGlow"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(_Color.rgb * i.color.rgb * i.color.a, 1);
            }
            ENDHLSL
        }
    }
}
