Shader "Dive/PoolFloor" {
 Properties { _Color("Tile color",Color)=(.10,.46,.53,1) _Smoothness("Smoothness",Range(0,1))=.72 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color; half _Smoothness;
 struct Input { float3 worldPos; };
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float caustic(float2 p){float2 g=floor(p),f=frac(p);float a=8,b=8;
 for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){float2 o=float2(x,y);float2 h=float2(hash(g+o),hash(g+o+19));float2 r=o+.5+.28*sin(_Time.y*.45+6.28*h)-f;float d=dot(r,r);if(d<a){b=a;a=d;}else b=min(b,d);}return pow(1-smoothstep(.01,.14,b-a),2);}
 void surf(Input IN,inout SurfaceOutputStandard o){float2 p=IN.worldPos.xz;float2 f=frac((p+float2(6.5,3.9))/1.3);float grout=1-smoothstep(.008,.02,min(min(f.x,1-f.x),min(f.y,1-f.y)));float c=caustic(p*1.5)*.15+caustic(p*2.6+5)*.05;
 float mid=1-smoothstep(.015,.03,abs(p.x));float ring=1-smoothstep(.02,.035,abs(length(p)-1.1));
 o.Albedo=_Color.rgb*(1-grout*.25)+mid*.1+ring*.14;o.Emission=c*float3(.45,.85,.65);o.Smoothness=_Smoothness;o.Metallic=.08;o.Alpha=1;}
 ENDCG
 } FallBack "Diffuse"
}

