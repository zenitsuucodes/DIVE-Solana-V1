using UnityEngine;
using UnityEngine.Rendering;
public sealed class DiveAtmosphere : MonoBehaviour {
    Transform[] bubbles=new Transform[90];Material grade;Camera eye;
    Material Mat(Color c,float metal=0){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.65f);return m;}
    GameObject Box(string n,Vector3 p,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;Destroy(o.GetComponent<Collider>());o.transform.SetParent(transform);return o;}
    void Start(){
        eye=GetComponent<Camera>();eye.allowHDR=true;eye.clearFlags=CameraClearFlags.Skybox;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.65f,.72f);RenderSettings.ambientEquatorColor=new Color(.09f,.23f,.28f);RenderSettings.ambientGroundColor=new Color(.025f,.09f,.12f);
        RenderSettings.fogColor=new Color(.025f,.22f,.42f);RenderSettings.fogDensity=.034f;
        var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.35f,.62f,.76f));sky.SetFloat("_Exposure",1.2f);RenderSettings.skybox=sky;
        var deck=Mat(new Color(.65f,.64f,.52f));var steel=Mat(new Color(.5f,.58f,.61f),.8f);var dark=Mat(new Color(.025f,.1f,.14f));
        // These world objects are detached from the camera after assembly.
        var root=new GameObject("Pool architecture").transform;
        for(int s=-1;s<=1;s+=2){
            Box("Limestone deck",new Vector3(s*10,5,0),new Vector3(4,.35f,30),deck).transform.SetParent(root);
            for(int z=-12;z<=12;z+=3){Box("Coping stone",new Vector3(s*7.9f,4.9f,z),new Vector3(.45f,.25f,2.94f),deck).transform.SetParent(root);Box("Tile seam",new Vector3(s*7.83f,2.5f,z),new Vector3(.015f,4.7f,.028f),dark).transform.SetParent(root);}
            for(int y=1;y<5;y++)Box("Horizontal grout",new Vector3(s*7.82f,y,0),new Vector3(.02f,.025f,26),dark).transform.SetParent(root);
            for(int z=-10;z<=10;z+=5){Box("Courtyard column",new Vector3(s*10,8,z),new Vector3(.55f,6,.55f),deck).transform.SetParent(root);}
            Box("Roof beam",new Vector3(s*10,11,0),new Vector3(.7f,.5f,26),deck).transform.SetParent(root);
            for(int x=-1;x<=1;x+=2){Box("Ladder rail",new Vector3(s*7.5f,3.8f,4+x*.45f),new Vector3(.06f,3,.06f),steel).transform.SetParent(root);}
            for(int y=0;y<6;y++)Box("Ladder rung",new Vector3(s*7.5f,2.7f+y*.4f,4),new Vector3(.08f,.055f,.95f),steel).transform.SetParent(root);
            // Thin goal net cords stay readable without opaque goal backs.
            for(int x=-4;x<=4;x++)Box("Goal net vertical",new Vector3(x*.5f,1.25f,s*12.35f),new Vector3(.015f,2.5f,.015f),deck).transform.SetParent(root);
            for(int y=1;y<=5;y++)Box("Goal net horizontal",new Vector3(0,y*.45f,s*12.35f),new Vector3(4,.015f,.015f),deck).transform.SetParent(root);
        }
        var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="Refractive water surface";water.layer=4;water.transform.position=new Vector3(0,4.95f,0);water.transform.localScale=new Vector3(1.6f,1,2.6f);water.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Dive/UnderwaterSurface"));Destroy(water.GetComponent<Collider>());water.AddComponent<DiveWaterReflection>();water.transform.SetParent(root);
        var bubbleMat=new Material(Shader.Find("Dive/Bubble"));
        var rng=new System.Random(83);
        for(int i=0;i<bubbles.Length;i++){var b=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.name="Rising air bubble";Destroy(b.GetComponent<Collider>());b.transform.SetParent(root);b.transform.position=new Vector3((float)rng.NextDouble()*15-7.5f,(float)rng.NextDouble()*5,(float)rng.NextDouble()*24-12);b.transform.localScale=Vector3.one*(.025f+(float)rng.NextDouble()*.06f);b.GetComponent<Renderer>().sharedMaterial=bubbleMat;bubbles[i]=b.transform;}
        if(Application.platform!=RuntimePlatform.WebGLPlayer){var probeObject=new GameObject("Pool reflection probe");probeObject.transform.SetParent(root);probeObject.transform.position=new Vector3(0,2,0);var probe=probeObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.resolution=128;probe.size=new Vector3(18,12,28);probe.cullingMask=~(1<<4);probe.RenderProbe();}

    }
    void Update(){for(int i=0;i<bubbles.Length;i++)if(bubbles[i]!=null){var p=bubbles[i].position;p.y+=Time.deltaTime*(.18f+i%5*.035f);p.x+=Mathf.Sin(Time.time+i)*Time.deltaTime*.02f;if(p.y>4.8f)p.y=.2f;bubbles[i].position=p;}}
    void OnRenderImage(RenderTexture source,RenderTexture destination){Graphics.Blit(source,destination);}
}


