Shader "Dive/Bubble" { Properties {_Color("Color",Color)=(.65,.92,1,.3)} SubShader { Tags{"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};float4 _Color;
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target{float f=pow(1-saturate(abs(dot(normalize(i.normal),normalize(i.view)))),3);return float4(_Color.rgb,.035+f*_Color.a);}
 ENDCG } } }
