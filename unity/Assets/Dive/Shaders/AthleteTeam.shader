Shader "Dive/AthleteTeam" {
 Properties {
  _MainTex ("Skin and wetsuit", 2D) = "white" {}
  _BumpMap ("Normal detail", 2D) = "bump" {}
  _TeamTint ("Team accent", Color) = (0.06,0.38,0.9,1)
  _Glossiness ("Smoothness", Range(0,1)) = 0.3
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex, _BumpMap;
  fixed4 _TeamTint;
  half _Glossiness;
  struct Input { float2 uv_MainTex; float2 uv_BumpMap; };
  void surf(Input IN, inout SurfaceOutputStandard o) {
   fixed4 base=tex2D(_MainTex,IN.uv_MainTex);
   // Only saturated blue fabric becomes team-colored; skin and neutral rubber retain their texture.
   half blueExcess=base.b-max(base.r,base.g*0.86);
   half skin=smoothstep(0.005,0.035,base.r-base.b);
   half mask=1-skin;
   half brightness=max(base.r,max(base.g,base.b));
   o.Albedo=lerp(base.rgb,_TeamTint.rgb*max(brightness,0.6),mask);
   o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));
   o.Metallic=0; o.Smoothness=_Glossiness; o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
