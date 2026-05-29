
Shader "Hidden/NormalFromDiffuse" 
{
   Properties {
      _MainTex ("Base (RGB)", 2D) = "white" {}
   }

   CGINCLUDE
      
      #include "UnityCG.cginc"
   
      struct v2f {
         float4 pos : SV_POSITION;
         float2 uv : TEXCOORD0;
      };
      
      sampler2D _MainTex;
      float4 _MainTex_TexelSize;

      float4 CreateNormal(float2 uv, float uvOff, float amp, float b)
      {
         float tX = _MainTex_TexelSize.x;
         float tY = _MainTex_TexelSize.y;

         float3 nx = tex2Dbias(_MainTex, float4(uv + float2(tX, 0), 0, b)).rgb;
         float3 ny = tex2Dbias(_MainTex, float4(uv + float2(0, tY), 0, b)).rgb; 
         float3 nxb = tex2Dbias(_MainTex, float4(uv + float2(-tX, 0), 0, b)).rgb;
         float3 nyb = tex2Dbias(_MainTex, float4(uv + float2(0, -tY), 0, b)).rgb;

         nx  = saturate( Luminance(nx + uvOff));
         ny  = saturate( Luminance(ny + uvOff));
         nxb = saturate( Luminance(nxb + uvOff));
         nyb = saturate( Luminance(nyb + uvOff));

         nx = (nx - nxb) * -1;
         ny = (ny - nyb) * -1;

		 float4 ret = float4(0.5, 0.5, 0, 1);

         float len = sqrt( nx * nx + ny * ny + 1 );

         if(len > 0)
         {
            ret.r = 10*amp*nx/len * 0.5 + 0.5;
            ret.g = 10*amp*ny/len * 0.5 + 0.5;
            ret.b = 1.0 / len;
         } 
         return ret;
      }
          
	  float4 frag(v2f_img i) : SV_Target
      {
         float4 normal = 0;
         normal += CreateNormal(i.uv, 0.1, 0.7, 6) * 6;
         normal += CreateNormal(i.uv, 0.2, 0.6, 5) * 5;
         normal += CreateNormal(i.uv, 0.3, 0.5, 4) * 4;
         normal += CreateNormal(i.uv, 0.4, 0.4, 3) * 3;
         normal += CreateNormal(i.uv, 0.5, 0.3, 2) * 2;
         normal += CreateNormal(i.uv, 0.6, 0.2, 1) * 1;
         normal += CreateNormal(i.uv, 0.7, 0.1, 0) * 1;
         normal /= 22.0;

         normal.xy -= 0.5;
         normal.xy *= 14;
         normal.xy += 0.5;
         normal.w = sqrt(1 - saturate(dot(normal.xy - 0.5, normal.xy - 0.5)));
         normal.z = 0.1 * (normal.z + ((normal.x + normal.y)/2));
         return normal.yxwz;
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