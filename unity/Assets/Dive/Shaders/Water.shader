Shader "Dive/Water" {
 Properties {_Tint("Water tint",Color)=(.08,.65,.71,.13) _Reflection("Reflection",2D)="black"{} }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _Reflection;float4 _Tint;
 struct v2f {float4 pos:SV_POSITION;float4 screen:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.pos);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {float2 uv=i.screen.xy/i.screen.w;float2 p=i.world.xz;float2 ripple=float2(sin(p.x*4+_Time.y*.7+p.y),cos(p.y*5+_Time.y*.55+p.x))*.003;float3 reflected=tex2D(_Reflection,uv+ripple).rgb;float f=pow(1-saturate(dot(normalize(_WorldSpaceCameraPos-i.world),float3(0,1,0))),3);float glitter=pow(saturate(sin(p.x*38+sin(p.y*8+_Time.y)*2)),30)*.025;return float4(lerp(_Tint.rgb,reflected,.4+f*.4)+glitter,_Tint.a+f*.25);}
 ENDCG }
 } FallBack Off
}
