using UnityEngine;
using System.Collections.Generic;

// Lightweight authored scenery, generated once; shared materials keep draw costs modest.
public sealed class DivePoolside : MonoBehaviour {
    Material paving, grout, wood, dark, plaster, glass, leaves, grass;
    Material Mat(Color c,float gloss=.25f){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",gloss);return m;}
    GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material){var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(transform);o.transform.position=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;if(name=="Garden lawn")o.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Destroy(o.GetComponent<Collider>());return o;}
    void Box(string name,Vector3 p,Vector3 size,Material m){Shape(name,PrimitiveType.Cube,p,size,m);}
    void TextureMaterial(Material material,bool timber=false){
        var texture=new Texture2D(128,128,TextureFormat.RGB24,true);texture.wrapMode=TextureWrapMode.Repeat;
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){
            float grain=Mathf.PerlinNoise(x*(timber?.12f:.2f),y*(timber?.012f:.2f));
            float fine=Mathf.PerlinNoise(x*.7f,y*.7f);
            float value=Mathf.Lerp(.74f,1f,grain*.65f+fine*.35f);
            texture.SetPixel(x,y,new Color(value,value,value));
        }
        texture.Apply();material.mainTexture=texture;material.mainTextureScale=timber?new Vector2(3,1):new Vector2(5,5);
    }
    public void Build(){
        paving=Mat(new Color(.67f,.67f,.57f),.4f);grout=Mat(new Color(.4f,.43f,.39f));wood=Mat(new Color(.37f,.2f,.085f));dark=Mat(new Color(.075f,.12f,.12f),.6f);plaster=Mat(new Color(.82f,.79f,.65f));glass=Mat(new Color(.09f,.32f,.36f),.92f);leaves=Mat(new Color(.11f,.29f,.085f));grass=Mat(new Color(.23f,.31f,.13f));
        TextureMaterial(paving);TextureMaterial(plaster);TextureMaterial(wood,true);TextureMaterial(grass);
        foreach(int side in new[]{-1,1}){Box("Garden lawn",new Vector3(side*60,4.35f,0),new Vector3(80,.7f,200),grass);Box("Garden lawn",new Vector3(0,4.35f,side*60),new Vector3(40,.7f,80),grass);}
        foreach(int s in new[]{-1,1}){
            Box("Wide stone promenade",new Vector3(s*16,4.9f,0),new Vector3(8,.5f,40),paving);
            Box("End terrace",new Vector3(0,4.9f,s*18),new Vector3(40,.5f,6),paving);
            for(int z=-18;z<=18;z+=2)Box("Paving joint",new Vector3(s*16,5.155f,z),new Vector3(7.98f,.008f,.018f),grout);
            for(int x=13;x<20;x+=2)Box("Paving joint",new Vector3(s*x,5.155f,0),new Vector3(.018f,.008f,40),grout);
            for(int z=-12;z<=14;z+=13){Palm(new Vector3(s*15.6f,5.15f,z),1+(z+12)*.006f);Bench(new Vector3(s*11,5.18f,z+3),s);}
            Box("Garden border",new Vector3(s*20,5,0),new Vector3(.2f,.45f,43),plaster);
            for(int z=-20;z<=23;z+=6){Shape("Garden shrub",PrimitiveType.Sphere,new Vector3(s*23,5.5f,z),new Vector3(3,1.8f,2.8f),leaves);}
        }
        Box("Clubhouse",new Vector3(0,7.9f,24),new Vector3(38,5.8f,5),plaster);
        Box("Clubhouse roof",new Vector3(0,10.9f,23.5f),new Vector3(40,.35f,7),paving);
        for(int x=-16;x<=16;x+=4){Box("Clubhouse glazing",new Vector3(x,7.6f,21.45f),new Vector3(3.55f,3.8f,.08f),glass);Box("Window mullion",new Vector3(x+1.85f,7.6f,21.35f),new Vector3(.09f,4,.16f),dark);}
        var sign=new GameObject("Aquatic club sign");sign.transform.SetParent(transform);sign.transform.position=new Vector3(0,10,21.3f);sign.transform.rotation=Quaternion.identity;var text=sign.AddComponent<TextMesh>();text.text="D I V E   /   A Q U A T I C   C L U B";text.anchor=TextAnchor.MiddleCenter;text.characterSize=.17f;text.fontSize=64;text.color=new Color(.075f,.17f,.16f);
        // Timber shade pavilion along the far terrace.
        foreach(int x in new[]{-8,8})foreach(int z in new[]{16,20})Box("Pergola support",new Vector3(x,7.1f,z),new Vector3(.16f,4,.16f),wood);
        for(int x=-8;x<=8;x++)Box("Pergola roof slat",new Vector3(x,9.15f,18),new Vector3(.12f,.18f,5),wood);
        for(int z=16;z<=20;z+=4)Box("Pergola beam",new Vector3(0,9.1f,z),new Vector3(16.4f,.22f,.18f),wood);
        foreach(int x in new[]{-5,0,5}){Bench(new Vector3(x,5.15f,18),0);}
    }
    void Bench(Vector3 p,int side){
        for(int i=0;i<5;i++)Box("Timber bench slat",p+new Vector3(0,.45f,(i-2)*.13f),new Vector3(2,.07f,.1f),wood);
        foreach(float x in new[]{-.7f,.7f})Box("Bench leg",p+new Vector3(x,.2f,0),new Vector3(.09f,.45f,.6f),dark);
        for(int i=0;i<3;i++)Box("Bench back slat",p+new Vector3(0,.7f+i*.16f,.35f),new Vector3(2,.11f,.065f),wood);
    }
    void Palm(Vector3 p,float size){
        Shape("Stone palm planter",PrimitiveType.Cylinder,p+Vector3.up*.45f,new Vector3(1.45f,.45f,1.45f),plaster);
        Shape("Planter soil",PrimitiveType.Cylinder,p+Vector3.up*.91f,new Vector3(1.25f,.015f,1.25f),wood);
        float height=5*size;
        Shape("Palm trunk",PrimitiveType.Cylinder,p+Vector3.up*(height/2+.9f),new Vector3(.24f,height/2,.24f),wood);
        Vector3 top=p+Vector3.up*(height+.9f);
        for(int f=0;f<9;f++){
            float a=f*Mathf.PI*2/9;Vector3 forward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),right=Vector3.Cross(Vector3.up,forward);
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int j=0;j<=10;j++){float t=j/10f;Vector3 mid=top+forward*(t*3.2f*size)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.9f-t*t*.9f);float width=Mathf.Sin(t*Mathf.PI)*.43f*(j%2==0?1:.65f);vertices.Add(mid-right*width);vertices.Add(mid+right*width);if(j<10){int k=j*2;triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3,k+1,k+2,k,k+3,k+2,k+1});}}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var o=new GameObject("Palm frond");o.transform.SetParent(transform);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=leaves;
        }
    }
}
