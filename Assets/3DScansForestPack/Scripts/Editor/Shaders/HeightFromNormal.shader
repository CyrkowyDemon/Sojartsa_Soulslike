
Shader "Hidden/HeightFromNormal" 
{
   Properties {
      _MainTex ("Base (RGB)", 2D) = "bump" {}
   }

   CGINCLUDE
      
      #include "UnityCG.cginc"
   
      struct v2f {
         float4 pos : SV_POSITION;
         float2 uv : TEXCOORD0;
      };
      
      sampler2D _MainTex;
      float4 _MainTex_TexelSize;
          
	  float4 frag(v2f_img i) : SV_Target
      {
		 float2 n = tex2Dlod(_MainTex, float4(i.uv, 0, 0)).xy;
		 return (n.x < 0.5 ? (2.0 * n.x * n.y) : (1.0 - 2.0 * (1.0 - n.x) * (1.0 - n.y)));
      }

      ENDCG

   SubShader {
      Pass {
         ZTest Always Cull Off ZWrite Off
            
         CGPROGRAM
         #pragma vertex vert_img
         #pragma fragment frag
         #include "UnityCG.cginc"
         ENDCG
      }

   }

   Fallback off

}