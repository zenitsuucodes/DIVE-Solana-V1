using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class DiveVisualScene : MonoBehaviour {
    public GameObject diverPrefab;
    public Shader floorShader;
    public Shader waterShader;
    readonly Transform[] divers=new Transform[4];
    readonly Vector3[] targets=new Vector3[4];
    Transform puck;
    Camera view;
    int owner=0;
    Vector3 puckTarget;
    float clock;
    [Serializable] public class DiverState {public int id,x,z;public bool recover;}
    [Serializable] public class VisualState {public DiverState[] players;public int owner=-1;public int puckX,puckZ;}
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void DiveTileClicked(int x,int z);
    #endif
    static Vector3 At(int x,int z,float y=.2f)=>new Vector3((x-4.5f)*1.3f,y,(z-2.5f)*1.3f);
    Material Mat(Color c,float smooth=.55f,float metal=.05f){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",metal);return m;}
    GameObject Box(string name,Vector3 pos,Vector3 size,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(transform);o.transform.position=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
    void Start(){
        QualitySettings.antiAliasing=4;QualitySettings.shadowDistance=30;Application.targetFrameRate=60;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.58f,.64f);RenderSettings.ambientEquatorColor=new Color(.1f,.24f,.29f);RenderSettings.ambientGroundColor=new Color(.025f,.05f,.065f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.025f,.09f,.12f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.018f;
        var cameraObject=new GameObject("Pool camera");view=cameraObject.AddComponent<Camera>();view.tag="MainCamera";view.orthographic=true;view.orthographicSize=5.7f;view.transform.position=new Vector3(0,16,12.5f);view.transform.LookAt(Vector3.zero);view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=RenderSettings.fogColor;view.allowHDR=false;view.allowMSAA=true;view.farClipPlane=80;
        var lightObject=new GameObject("Sun");var sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.94f,.81f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.55f;sun.transform.rotation=Quaternion.Euler(52,-28,0);
        var stone=Mat(new Color(.77f,.81f,.74f),.42f);var wall=Mat(new Color(.12f,.46f,.52f),.8f,.12f);
        Box("Basin foundation",new Vector3(0,-.4f,0),new Vector3(14.2f,.65f,9),Mat(new Color(.035f,.12f,.15f)));
        var floor=Box("Tiled pool",new Vector3(0,-.08f,0),new Vector3(13,.12f,7.8f),new Material(floorShader));
        for(int i=0;i<20;i++)for(int side=-1;side<=1;side+=2){float x=-6.5f+(i+.5f)*.65f;Box("Coping",new Vector3(x,.66f,side*4.12f),new Vector3(.627f,.2f,.5f),stone);Box("Wall tile",new Vector3(x,.22f,side*3.94f),new Vector3(.638f,.7f,.13f),wall);}
        for(int i=0;i<12;i++)for(int side=-1;side<=1;side+=2){float z=-3.9f+(i+.5f)*.65f;Box("Coping",new Vector3(side*6.75f,.66f,z),new Vector3(.5f,.2f,.627f),stone);Box("Wall tile",new Vector3(side*6.54f,.22f,z),new Vector3(.13f,.7f,.638f),wall);}
        var water=GameObject.CreatePrimitive(PrimitiveType.Quad);water.name="Water";water.layer=4;water.transform.SetParent(transform);water.transform.position=new Vector3(0,.64f,0);water.transform.rotation=Quaternion.Euler(90,0,0);water.transform.localScale=new Vector3(13,7.8f,1);Destroy(water.GetComponent<Collider>());water.GetComponent<Renderer>().sharedMaterial=new Material(waterShader);water.AddComponent<DiveWaterReflection>();
        Color[] colors={new Color(.01f,.44f,.83f),new Color(.96f,.29f,.12f)};
        for(int t=0;t<2;t++){float side=t==0?-1:1;var m=Mat(colors[t],.76f,.35f);Box("Goal back",new Vector3(side*6.25f,.32f,0),new Vector3(.11f,.64f,2.6f),m);Box("Goal bar",new Vector3(side*5.72f,.66f,0),new Vector3(.1f,.1f,2.6f),m);for(int end=-1;end<=1;end+=2)Box("Goal post",new Vector3(side*5.72f,.32f,end*1.25f),new Vector3(.1f,.64f,.1f),m);}
        int[,] start={{2,3},{3,1},{7,2},{6,4}};
        for(int i=0;i<4;i++){var o=Instantiate(diverPrefab,At(start[i,0],start[i,1]),Quaternion.Euler(0,i<2?90:-90,0),transform);o.name="Diver "+i;o.transform.localScale=Vector3.one*.77f;foreach(var r in o.GetComponentsInChildren<Renderer>()){var materials=r.materials;foreach(var m in materials)if(m.name.StartsWith("Team"))m.color=colors[i/2];r.materials=materials;}divers[i]=o.transform;targets[i]=o.transform.position;}
        var p=GameObject.CreatePrimitive(PrimitiveType.Cylinder);p.name="Puck";p.transform.localScale=new Vector3(.25f,.035f,.25f);p.GetComponent<Renderer>().material=Mat(new Color(1,.81f,.1f),.8f,.3f);puck=p.transform;
        Box("Sea floor",new Vector3(0,-1,0),new Vector3(50,.2f,40),Mat(new Color(.025f,.10f,.12f),.1f));
    }
    // A JS host can call unityInstance.SendMessage('DIVE','SetState',JSON.stringify(state)).
    public void SetState(string json){var s=JsonUtility.FromJson<VisualState>(json);if(s==null||s.players==null)return;foreach(var p in s.players)if(p.id>=0&&p.id<4)targets[p.id]=At(p.x,p.z,p.recover?.8f:.2f);owner=s.owner;puckTarget=At(s.puckX,s.puckZ,.1f);}
    void Update(){if(view==null)return;clock+=Time.deltaTime;view.orthographicSize=Mathf.Max(5.7f,8.75f/view.aspect);
        for(int i=0;i<4;i++){divers[i].position=Vector3.Lerp(divers[i].position,targets[i]+Vector3.up*Mathf.Sin(clock*2+i)*.018f,Time.deltaTime*6);}
        Vector3 goal=owner>=0&&owner<4?divers[owner].position+divers[owner].forward*.55f:puckTarget;goal.y=.12f;puck.position=Vector3.Lerp(puck.position,goal,Time.deltaTime*10);
        if(Input.GetMouseButtonDown(0)){var ray=view.ScreenPointToRay(Input.mousePosition);if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float d)){var p=ray.GetPoint(d);int x=Mathf.RoundToInt(p.x/1.3f+4.5f),z=Mathf.RoundToInt(p.z/1.3f+2.5f);if(x>=0&&x<10&&z>=0&&z<6){
            #if UNITY_WEBGL && !UNITY_EDITOR
            DiveTileClicked(x,z);
            #else
            Debug.Log($"DIVE tile {x},{z}");
            #endif
        }}}
    }
}
