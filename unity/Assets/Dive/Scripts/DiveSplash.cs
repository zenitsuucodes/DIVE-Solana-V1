using UnityEngine;
using System.Collections;

public sealed class DiveSplash : MonoBehaviour {
    Camera view, player;
    GameObject diver, hands;
    DiveFirstPerson game;
    DiveAudio audioMix;
    Light sun;
    Color sunColor;
    ShadowQuality savedShadows;
    bool entering;
    float progress;
    Texture2D shade, button;
    Vector3 cameraHome=new Vector3(9.2f,7.4f,-6);
    public void Setup(DiveFirstPerson owner,Camera eye,DiveAudio sound){
        game=owner;player=eye;audioMix=sound;savedShadows=QualitySettings.shadows;QualitySettings.shadows=ShadowQuality.Disable;
        hands=eye.transform.Find("Animated swim hands")?.gameObject;if(hands!=null)hands.SetActive(false);
        view=new GameObject("Poolside title camera").AddComponent<Camera>();view.transform.SetParent(transform);view.fieldOfView=58;view.nearClipPlane=.08f;view.farClipPlane=100;view.allowHDR=true;view.clearFlags=CameraClearFlags.Skybox;eye.enabled=false;
        foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;sunColor=light.color;break;}
        var model=Resources.Load<GameObject>("MeshyDiver");
        if(model!=null){
            diver=Instantiate(model,transform);diver.name="Poolside diver";
            diver.transform.position=new Vector3(8.55f,5.2f,0);
            diver.transform.rotation=Quaternion.Euler(0,200,0);
            diver.transform.localScale=Vector3.one*1.2f;
            diver.AddComponent<DiveHuman>().Setup(true,true);
        }
        var stone=new Material(Shader.Find("Standard"));stone.color=new Color(.63f,.64f,.56f);stone.SetFloat("_Glossiness",.4f);
        for(int sign=-1;sign<=1;sign+=2){var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Pool end terrace";deck.transform.SetParent(game.transform);deck.transform.position=new Vector3(0,5,sign*14);deck.transform.localScale=new Vector3(24,.35f,2);deck.GetComponent<Renderer>().sharedMaterial=stone;Destroy(deck.GetComponent<Collider>());}
        shade=new Texture2D(128,1);for(int x=0;x<128;x++)shade.SetPixel(x,0,new Color(.015f,.075f,.095f,Mathf.Lerp(.88f,0,x/127f)));shade.Apply();
        button=new Texture2D(1,1);button.SetPixel(0,0,new Color(.64f,.98f,.79f));button.Apply();
    }
    void PortraitDetails(){
        var suit=new Material(Shader.Find("Standard"));suit.color=new Color(.018f,.045f,.055f);suit.SetFloat("_Glossiness",.5f);
        var lens=new Material(Shader.Find("Standard"));lens.color=new Color(.12f,.68f,.78f);lens.SetFloat("_Metallic",.65f);lens.SetFloat("_Glossiness",.95f);
        foreach(string side in new[]{"L","R"}){var shoulder=Bone("UpperArm"+side);if(shoulder==null)continue;float sign=side=="L"?-1:1;Vector3 elbow=shoulder.position+new Vector3(sign*.08f,-.43f,-.06f),hand=elbow+new Vector3(sign*.03f,-.4f,-.12f);PortraitLimb(shoulder.position,elbow,.1f,suit);PortraitLimb(elbow,hand,.075f,suit);PortraitShape("Poolside glove",PrimitiveType.Sphere,hand,new Vector3(.15f,.2f,.12f),suit);}
        var head=Bone("Head");if(head!=null){Vector3 face=head.position+new Vector3(0,.26f,-.16f);PortraitShape("Poolside dive mask",PrimitiveType.Cube,face,new Vector3(.31f,.14f,.1f),suit);foreach(float x in new[]{-.078f,.078f})PortraitShape("Mask lens",PrimitiveType.Cube,face+new Vector3(x,0,-.057f),new Vector3(.13f,.095f,.025f),lens);PortraitShape("Regulator",PrimitiveType.Sphere,face+new Vector3(0,-.13f,-.035f),Vector3.one*.11f,suit);}
    }
    GameObject PortraitShape(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material){var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(diver.transform,true);o.transform.position=p;o.transform.rotation=Quaternion.identity;o.transform.localScale=scale/1.2f;o.GetComponent<Renderer>().sharedMaterial=material;Destroy(o.GetComponent<Collider>());return o;}
    void PortraitLimb(Vector3 a,Vector3 b,float radius,Material m){var o=PortraitShape("Relaxed arm",PrimitiveType.Capsule,(a+b)/2,new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),m);o.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    Transform Bone(string name){foreach(var t in diver.GetComponentsInChildren<Transform>())if(t.name==name)return t;return null;}
    void LateUpdate(){
        if(!entering){view.transform.position=cameraHome+new Vector3(Mathf.Sin(Time.time*.17f)*.22f,Mathf.Sin(Time.time*.22f)*.06f,0);view.transform.LookAt(new Vector3(3.5f,5.35f,3));RenderSettings.fog=false;if(sun!=null)sun.color=new Color(1,.88f,.68f);}
    }
    IEnumerator Enter(){
        entering=true;audioMix.Cue("Start");var start=view.transform.position;var rotation=view.transform.rotation;
        for(float t=0;t<1.4f;t+=Time.deltaTime){progress=t/1.4f;float smooth=Mathf.SmoothStep(0,1,progress);view.transform.position=Vector3.Lerp(start,player.transform.position,smooth);view.transform.rotation=Quaternion.Slerp(rotation,player.transform.rotation,smooth);RenderSettings.fog=view.transform.position.y<4.95f;yield return null;}
        QualitySettings.shadows=savedShadows;player.enabled=true;if(hands!=null)hands.SetActive(true);if(sun!=null)sun.color=sunColor;RenderSettings.fog=true;
        view.gameObject.SetActive(false);if(diver!=null)diver.SetActive(false);game.BeginFromSplash();Destroy(gameObject);
    }
    void OnGUI(){
        if(entering){GUI.color=new Color(.02f,.1f,.13f,Mathf.Sin(progress*Mathf.PI)*.3f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;return;}
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),shade);
        float scale=Mathf.Min(Screen.width/1000f,Screen.height/800f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        float h=Screen.height/scale,w=Screen.width/scale,x=64,y=h*.29f;
        var label=new GUIStyle(GUI.skin.label){fontSize=15,fontStyle=FontStyle.Bold};label.normal.textColor=new Color(.65f,.98f,.82f);
        GUI.Label(new Rect(x,45,500,30),"D /  AQUATIC SPORTS CLUB    /    BUILT FOR SOLANA V1",label);
        var title=new GUIStyle(label){fontSize=118};title.normal.textColor=Color.white;GUI.Label(new Rect(x,y,500,145),"DIVE",title);
        GUI.Label(new Rect(x,y+143,500,30),"TAKE THE GAME BELOW THE SURFACE.",label);
        var body=new GUIStyle(GUI.skin.label){fontSize=20,wordWrap=true};body.normal.textColor=new Color(.8f,.9f,.9f);
        GUI.Label(new Rect(x,y+190,385,80),"One pool. Two teams.\nA whole new way to play.",body);
        var start=new GUIStyle(GUI.skin.button){fontSize=22,fontStyle=FontStyle.Bold};start.normal.background=button;start.hover.background=button;start.active.background=button;start.normal.textColor=start.hover.textColor=start.active.textColor=new Color(.035f,.16f,.16f);
        GUI.enabled=audioMix.Ready;if(GUI.Button(new Rect(x,y+295,290,65),audioMix.Ready?"Start game   >":"Preparing audio...",start))StartCoroutine(Enter());GUI.enabled=true;
        label.fontSize=13;GUI.Label(new Rect(x,y+380,500,30),"3v3   /   FIRST TO THREE   /   SOLO + BOTS",label);
        body.fontSize=16;GUI.Label(new Rect(x,y+413,520,48),"Uses the Solana blockchain and the V1 transaction update\nto record game actions.",body);
        body.fontSize=14;GUI.Label(new Rect(x,y+465,500,26),"Live on Devnet  /  No wallet needed",body);
        GUI.Label(new Rect(x,h-62,550,30),"WASD to swim    /    Mouse to look    /    E to pass",body);
        GUI.matrix=Matrix4x4.identity;
    }
    void OnDestroy(){QualitySettings.shadows=savedShadows;if(shade!=null)Destroy(shade);if(button!=null)Destroy(button);}
}
