Shader "Story/FlashlightBeam"
{
    Properties
    {
        _Color ("Beam colour", Color) = (1, 0.95, 0.8, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _Falloff ("Distance falloff", Range(0.2, 4)) = 1.4
        _EdgeSoftness ("Edge softness", Range(0.2, 6)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "FlashlightBeam"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Falloff;
                half _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float along : TEXCOORD2;
                half weight : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(vp.positionWS);
                OUT.along = IN.uv.x;
                OUT.weight = IN.color.a;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);
                float facing = saturate(abs(dot(n, v)));
                float edge = pow(facing, _EdgeSoftness);   // fades to nothing at the silhouette so there is no hard triangle outline
                float along = saturate(IN.along);
                float fade = pow(1.0 - along, _Falloff) * smoothstep(0.0, 0.08, along);
                float a = edge * fade * IN.weight * _Intensity * _Color.a;
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
