Shader "JustOneMorePeel/BasementFallingPaper"
{
    Properties { _BaseMap ("Surface", 2D) = "white" {} _BaseColor ("Colour", Color) = (1,1,1,1) _DoorOpen ("Door open", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _DoorOpen;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.uv=TRANSFORM_TEX(input.uv,_BaseMap);return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 surface=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*_BaseColor;
                clip(surface.a-.001h);
                float depth=max(0,input.positionWS.z-3.35);
                float width=1.2+depth*.45;
                float doorway=saturate(1-depth/8.0)*saturate(1-abs(input.positionWS.x)/width)*_DoorOpen;
                return half4(surface.rgb*(1.15h*doorway)*half3(.78h,.87h,1),surface.a);
            }
            ENDHLSL
        }
    }
}
