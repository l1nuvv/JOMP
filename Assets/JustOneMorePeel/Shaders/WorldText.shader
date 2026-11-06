Shader "JustOneMorePeel/WorldText"
{
 Properties { _MainTex("Font",2D)="white"{} _Color("Color",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest LEqual
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _Color;
   CBUFFER_END
   V vert(A input) { V o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;o.color=input.color*_Color;return o; }
   half4 frag(V input):SV_Target { return half4(input.color.rgb,input.color.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a); }
   ENDHLSL
  }
 }
}