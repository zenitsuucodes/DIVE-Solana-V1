Shader "Dive/UnderwaterSurface" {
 Properties { _Reflection("Reflection",2D)="black"{} _Color("Water tint",Color)=(.025,.32,.78,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off
 GrabPass {"_DiveRefraction"}
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _DiveRefraction,_Reflection;float4 _Color;
 struct appdata {float4 vertex:POSITION;};struct v2f {float4 pos:SV_POSITION;float4 grab:TEXCOORD0;float4 screen:TEXCOORD1;float3 world:TEXCOORD2;};
 v2f vert(appdata v){v2f o;float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;w.y+=sin(w.x*1.6+_Time.y*.8)*.035+sin(w.z*2.1-_Time.y)*.025;o.world=w;o.pos=UnityWorldToClipPos(w);o.grab=ComputeGrabScreenPos(o.pos);o.screen=ComputeScreenPos(o.pos);return o;}
 fixed4 frag(v2f i):SV_Target {
 float2 p=i.world.xz;float t=_Time.y;
 float2 ripple=float2(cos(p.x*1.6+t*.8)*.07+sin(p.y*4+t)*.025,cos(p.y*2.1-t)*.07+sin(p.x*3-t*.7)*.025);
 float3 n=normalize(float3(ripple.x,-1,ripple.y));float3 view=normalize(_WorldSpaceCameraPos-i.world);
 float facing=saturate(abs(dot(n,view)));float fresnel=.06+.85*pow(1-facing,3);
 float2 uv=i.grab.xy/i.grab.w+ripple*.055;
 float3 refractColor=tex2D(_DiveRefraction,uv).rgb;
 float3 reflection=tex2D(_Reflection,i.screen.xy/i.screen.w+ripple*.035).rgb;
 float shine=pow(saturate(dot(reflect(-normalize(float3(.4,-1,.3)),n),view)),110)*.9;
 float3 clearBlue=lerp(refractColor*float3(.62,.88,1.12),_Color.rgb,.16);
 float3 reflectedBlue=lerp(reflection,_Color.rgb,.13);
 float3 color=lerp(clearBlue,reflectedBlue,fresnel)+shine*float3(.75,.9,1);
 return float4(color,1);
 }
 ENDCG }
 } }
