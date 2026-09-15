Shader "Hidden/Dive/Grade" {Properties { _MainTex ("Source", 2D) = "white" {} } SubShader{Cull Off ZWrite Off ZTest Always Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 fixed4 frag(v2f_img i):SV_Target{float2 uv=i.uv;float3 c=tex2D(_MainTex,uv).rgb;float vignette=smoothstep(.85,.18,length((uv-.5)*float2(1.2,1)));c*=lerp(.72,1,vignette);c=c/(c+.55)*1.15;return float4(c,1);}
 ENDCG}}}

