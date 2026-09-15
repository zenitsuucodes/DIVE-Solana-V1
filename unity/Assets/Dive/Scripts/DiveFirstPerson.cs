using UnityEngine;

// Local gameplay with sponsored V1 Devnet event receipts in the browser.
public sealed class DiveFirstPerson : MonoBehaviour {
    #if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void DiveCapture(int enabled);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern int DiveCaptured();
    #endif
    void Capture(bool enabled){
        #if UNITY_WEBGL && !UNITY_EDITOR
        DiveCapture(enabled?1:0);
        #else
        Cursor.lockState=enabled?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!enabled;
        #endif
    }
    public Shader floorShader;
    Camera eye;
    DiveAudio sound;
    DiveChain chain;
    bool showingSplash=true;
    Transform puck;
    readonly Transform[] players=new Transform[6];
    readonly float[] actionDelay=new float[6];
    Vector3 puckVelocity;
    float yaw, pitch;
    int possession=-1, blue, coral;
    bool playing, matchStarted;
    int kickoffTeam;
    float kickoffTime;
    float stamina=1,charge;int lastTouch=-1,shots,passes,steals;bool charging;
    Material Material(Color color, bool glow=false) {
        var m=new Material(Shader.Find("Standard")); m.color=color;
        m.SetFloat("_Glossiness",.65f);
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.8f);} return m;
    }
    GameObject Shape(string label, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent=null) {
        var o=GameObject.CreatePrimitive(type);o.name=label;o.transform.SetParent(parent==null?transform:parent,false);
        o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
        Destroy(o.GetComponent<Collider>());return o;
    }
    void Start() {
        QualitySettings.antiAliasing=4;Application.targetFrameRate=60;
        RenderSettings.ambientLight=new Color(.16f,.4f,.46f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.025f,.25f,.32f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.045f;
        var lamp=new GameObject("Surface sunlight").AddComponent<Light>();lamp.transform.SetParent(transform);lamp.type=LightType.Directional;lamp.intensity=1.5f;lamp.color=new Color(.72f,.94f,1);lamp.shadows=LightShadows.Soft;lamp.transform.rotation=Quaternion.Euler(65,-25,0);
        var tile= floorShader!=null?new Material(floorShader):Material(new Color(.1f,.5f,.58f));
        Shape("Caustic tiles",PrimitiveType.Cube,new Vector3(0,-.15f,0),new Vector3(16,.3f,26),tile);
        var wall=Material(new Color(.08f,.33f,.4f));
        for(int s=-1;s<=1;s+=2){
            Shape("Pool side",PrimitiveType.Cube,new Vector3(s*8,2.5f,0),new Vector3(.3f,5,26),wall);
            Shape("End wall",PrimitiveType.Cube,new Vector3(0,2.5f,s*13),new Vector3(16,5,.3f),wall);
            var goal=Material(s>0?new Color(1,.22f,.12f):new Color(.05f,.55f,1),true);
            for(int x=-1;x<=1;x+=2)Shape("Goal post",PrimitiveType.Cube,new Vector3(x*2,1.25f,s*12),new Vector3(.12f,2.5f,.12f),goal);
            Shape("Goal crossbar",PrimitiveType.Cube,new Vector3(0,2.5f,s*12),new Vector3(4,.12f,.12f),goal);
            for(int z=-10;z<=10;z+=4)Shape("Lane light",PrimitiveType.Cube,new Vector3(s*7.8f,.45f,z),new Vector3(.08f,.08f,1.5f),Material(new Color(.08f,.8f,.9f),true));
        }

        eye=new GameObject("First person camera").AddComponent<Camera>();eye.transform.SetParent(transform);eye.tag="MainCamera";eye.fieldOfView=78;eye.nearClipPlane=.04f;eye.farClipPlane=60;eye.backgroundColor=RenderSettings.fogColor;eye.clearFlags=CameraClearFlags.SolidColor;
        eye.gameObject.AddComponent<DiveAtmosphere>();
        eye.gameObject.AddComponent<AudioListener>();sound=eye.gameObject.AddComponent<DiveAudio>();
        var handPrefab=Resources.Load<GameObject>("SwimHands");
        if(handPrefab!=null){var hands=Instantiate(handPrefab,eye.transform);hands.name="Animated swim hands";hands.transform.localScale=Vector3.one;hands.transform.localPosition=new Vector3(0,-.27f,-.05f);hands.transform.localRotation=Quaternion.Euler(0,180,0);hands.AddComponent<DiveAthlete>().Setup(true,true);}
        players[0]=eye.transform;
        var athlete=Resources.Load<GameObject>("MeshyDiver");
        for(int id=1;id<6;id++){
            var rival=new GameObject(id<3?"Blue teammate "+id:"Coral opponent "+(id-2)).transform;rival.SetParent(transform);players[id]=rival;
            if(athlete!=null){var model=Instantiate(athlete,rival);model.transform.localPosition=new Vector3(0,0,-1.15f);model.transform.localRotation=Quaternion.Euler(90,0,0);model.transform.localScale=Vector3.one;model.AddComponent<DiveHuman>().Setup(id<3);}
        }
        puck=Shape("Golden puck",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.38f,.07f,.38f),Material(new Color(1,.65f,.06f),true)).transform;
        chain=gameObject.AddComponent<DiveChain>();ResetRound();
        var garden=new GameObject("Poolside gardens");garden.transform.SetParent(transform);garden.AddComponent<DivePoolside>().Build();
        var opening=new GameObject("DIVE opening");opening.transform.SetParent(transform);opening.AddComponent<DiveSplash>().Setup(this,eye,sound);
    }
    public void BeginFromSplash(){foreach(var sign in GetComponentsInChildren<TextMesh>())sign.gameObject.SetActive(false);showingSplash=false;playing=true;matchStarted=true;chain.Begin(kickoffTeam*3);sound.SetActive(true);sound.Announce(kickoffTeam==0?"BlueStart":"CoralStart");Capture(true);}
    void ResetRound(int team=-1){
        charging=false;charge=0;stamina=1;
        kickoffTeam=team<0?Random.Range(0,2):team;kickoffTime=4;
        Vector3[] starts={new Vector3(0,1.3f,-6),new Vector3(-3,1.3f,-3),new Vector3(2,1.3f,-9),new Vector3(0,1.3f,4),new Vector3(3,1.3f,6),new Vector3(-2,1.3f,9)};
        for(int i=0;i<6;i++){players[i].position=starts[i];players[i].rotation=Quaternion.Euler(0,i<3?0:180,0);actionDelay[i]=1;}
        possession=kickoffTeam==0?0:3;
        players[possession].position=new Vector3(0,1.3f,kickoffTeam==0?-.9f:.9f);
        lastTouch=possession;puck.position=players[possession].position+players[possession].forward*.9f-Vector3.up*.4f;puckVelocity=Vector3.zero;yaw=pitch=0;
    }
    bool SameTeam(int a,int b)=>a>=0&&b>=0&&a/3==b/3;
    int PassTarget(){int best=-1;float alignment=-2;for(int i=1;i<3;i++){Vector3 delta=players[i].position-eye.transform.position;float dot=Vector3.Dot(eye.transform.forward,delta.normalized);if(dot>0&&dot>alignment){alignment=dot;best=i;}}return best;}
    void Launch(int id,Vector3 direction,float speed){lastTouch=id;if(id==0){if(speed>=12)shots++;else passes++;}var human=players[id].GetComponentInChildren<DiveHuman>();if(human!=null)human.Strike();chain.Record(speed>=12?"shot":"pass",id,blue,coral);sound.At(speed>=12?"Shot":"Pass",players[id].position);var athlete=players[id].GetComponentInChildren<DiveAthlete>();if(athlete!=null)athlete.Strike();puck.position=players[id].position+direction.normalized*.85f;possession=-1;puckVelocity=direction.normalized*speed;actionDelay[id]=.8f;}
    int Chaser(int team){int best=-1;float distance=float.MaxValue;for(int i=team*3;i<team*3+3;i++){if(i%3==2&&puck.position.z*(team==0?1:-1)>-6)continue;float d=(players[i].position-puck.position).sqrMagnitude;if(d<distance){distance=d;best=i;}}return best;}
    void Bots(float dt){
        int blueChaser=Chaser(0),coralChaser=Chaser(1);
        for(int i=1;i<6;i++){
            float attack=i<3?1:-1;bool defending=i==2||i==5;
            Vector3 target;
            if(possession==i)target=new Vector3(Mathf.Sin(Time.time*.7f+i)*1.5f,1.3f,attack*10);
            else if(i==(i<3?blueChaser:coralChaser)&&!SameTeam(i,possession))target=possession>=0?players[possession].position:puck.position;
            else if(defending)target=new Vector3(Mathf.Clamp(puck.position.x*.6f,-3,3),Mathf.Clamp(puck.position.y,1,2.2f),-attack*(SameTeam(i,possession)?5:9));
            else target=new Vector3((i%2==0?1:-1)*3.5f,1.5f,Mathf.Clamp(puck.position.z+attack*(SameTeam(i,possession)?3:-2),-9,9));
            Vector3 separation=Vector3.zero;for(int j=0;j<6;j++)if(j!=i){Vector3 delta=players[i].position-players[j].position;if(delta.sqrMagnitude<2.25f)separation+=delta.normalized*(1.5f-delta.magnitude);}
            players[i].position=Bound(Vector3.MoveTowards(players[i].position,target,dt*2.9f)+separation*dt*2);
            Vector3 look=(possession==i?new Vector3(0,1.2f,attack*13):puck.position)-players[i].position;
            if(look.sqrMagnitude>.01f)players[i].rotation=Quaternion.Slerp(players[i].rotation,Quaternion.LookRotation(look),dt*6);
            if(possession==i&&actionDelay[i]<=0){
                if(players[i].position.z*attack>3)Launch(i,new Vector3(0,1.2f,attack*13)-players[i].position,12);
                else {int recipient=-1;float gain=2;for(int j=i<3?0:3;j<(i<3?3:6);j++)if(j!=i){float g=(players[j].position.z-players[i].position.z)*attack;if(g>gain&&Vector3.Distance(players[i].position,players[j].position)<10){recipient=j;gain=g;}}if(recipient>=0)Launch(i,players[recipient].position-players[i].position,10);}
            }
            else if(possession>=0&&!SameTeam(i,possession)&&actionDelay[i]<=0&&actionDelay[possession]<=0&&Vector3.Distance(players[i].position,players[possession].position)<1.15f){actionDelay[possession]=1;possession=i;actionDelay[i]=.7f;chain.Record("steal",i,blue,coral);}
        }
    }
    Vector3 Bound(Vector3 p){p.x=Mathf.Clamp(p.x,-7.3f,7.3f);p.y=Mathf.Clamp(p.y,.65f,4.3f);p.z=Mathf.Clamp(p.z,-11.5f,11.5f);return p;}
    void Update(){
        if(eye==null||showingSplash)return;
        if(Input.GetKeyDown(KeyCode.Escape)){playing=false;sound.SetActive(false);Capture(false);}
        if(Input.GetKeyDown(KeyCode.M))sound.Toggle();
        if(!playing||blue>=3||coral>=3)return;
        float dt=Time.deltaTime;for(int i=0;i<6;i++)actionDelay[i]-=dt;
        bool canLook=true;
        #if UNITY_WEBGL && !UNITY_EDITOR
        canLook=DiveCaptured()!=0||Input.GetMouseButton(1);
        #endif
        if(canLook){yaw+=Input.GetAxis("Mouse X")*2;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,-75,75);eye.transform.rotation=Quaternion.Euler(pitch,yaw,0);}
        if(kickoffTime>0){kickoffTime=Mathf.Max(0,kickoffTime-dt);if(kickoffTime==0){chain.Record("kickoff",kickoffTeam*3,blue,coral);sound.Cue("Start");actionDelay[possession]=.3f;}sound.Movement(0);return;}
        Vector3 movement=eye.transform.forward*Input.GetAxisRaw("Vertical")+eye.transform.right*Input.GetAxisRaw("Horizontal");
        if(Input.GetKey(KeyCode.Space))movement+=Vector3.up;if(Input.GetKey(KeyCode.LeftControl))movement+=Vector3.down;
        sound.Movement(movement.magnitude);
        bool sprint=Input.GetKey(KeyCode.LeftShift)&&stamina>.05f&&movement.sqrMagnitude>.1f;
        stamina=Mathf.Clamp01(stamina+dt*(sprint?-.24f:.16f));
        eye.fieldOfView=Mathf.Lerp(eye.fieldOfView,sprint?83:78,dt*5);
        eye.transform.position=Bound(eye.transform.position+Vector3.ClampMagnitude(movement,1)*dt*(sprint?6:4.2f));
        if(Input.GetKeyDown(KeyCode.E)&&possession==0&&actionDelay[0]<=0){int receiver=PassTarget();if(receiver>=0)Launch(0,players[receiver].position-eye.transform.position,11);}
        if(Input.GetMouseButtonDown(0)&&actionDelay[0]<=0){
            if(possession==0){charging=true;charge=0;}
            else if(possession>=3&&Vector3.Distance(eye.transform.position,players[possession].position)<2){actionDelay[possession]=1.3f;possession=0;lastTouch=0;steals++;actionDelay[0]=.6f;chain.Record("steal",0,blue,coral);sound.At("Pickup",eye.transform.position);}
        }
        if(charging){if(possession!=0){charging=false;charge=0;}else{charge=Mathf.Min(1,charge+dt);if(Input.GetMouseButtonUp(0)){Launch(0,eye.transform.forward,Mathf.Lerp(12,19,charge));charging=false;charge=0;}}}
        Bots(dt);
        if(possession==-1){
            puck.position+=puckVelocity*dt;puckVelocity*=Mathf.Exp(-.3f*dt);
            if(Mathf.Abs(puck.position.z)>=12&&Mathf.Abs(puck.position.x)<2&&puck.position.y<2.5f){bool scored=puck.position.z>0;if(scored)blue++;else coral++;chain.Record("goal",lastTouch>=0?lastTouch:(scored?0:3),blue,coral);sound.Cue(blue>=3?"Win":coral>=3?"Lose":scored?"Goal":"Concede");sound.Announce(blue>=3?"BlueWin":coral>=3?"CoralWin":scored?"BlueGoal":"CoralGoal");if(blue>=3||coral>=3){chain.Record("match_end",blue>=3?0:3,blue,coral);sound.SetActive(false);Capture(false);}else ResetRound(scored?1:0);return;}
            Vector3 p=puck.position;if(Mathf.Abs(p.x)>7.6f){p.x=Mathf.Clamp(p.x,-7.6f,7.6f);puckVelocity.x*=-.75f;sound.At("Impact",p);}if(Mathf.Abs(p.z)>12.5f){p.z=Mathf.Clamp(p.z,-12.5f,12.5f);puckVelocity.z*=-.75f;sound.At("Impact",p);}if(p.y<.3f||p.y>4.7f){p.y=Mathf.Clamp(p.y,.3f,4.7f);puckVelocity.y*=-.6f;sound.At("Impact",p);}puck.position=p;
            int closest=-1;float nearest=1.2f;
            for(int i=0;i<6;i++){float d=Vector3.Distance(p,players[i].position);if(actionDelay[i]<=0&&d<nearest){nearest=d;closest=i;}}
            if(closest>=0){possession=closest;lastTouch=closest;actionDelay[closest]=.3f;sound.At("Pickup",players[closest].position,.6f);}
        }
        if(possession==0)puck.position=eye.transform.position+eye.transform.forward*1.05f+eye.transform.right*.3f-eye.transform.up*.43f;
        if(possession>0)lastTouch=possession;puck.position=players[possession].position+players[possession].forward*.9f-Vector3.up*.4f;
    }
    void OnGUI(){
        if(showingSplash)return;
        GUI.skin.label.fontSize=20;GUI.color=Color.white;
        GUI.Label(new Rect(24,20,520,35),$"DIVE     BLUE {blue} : {coral} CORAL  /  3v3");
        if(eye!=null){int receiver=PassTarget();for(int i=1;i<3;i++){var screen=eye.WorldToScreenPoint(players[i].position+Vector3.up);if(screen.z>0){GUI.color=new Color(.3f,.85f,1);GUI.Label(new Rect(screen.x-65,Screen.height-screen.y,160,30),possession==0&&receiver==i?"[E] PASS HERE":"TEAMMATE "+i);}}GUI.color=Color.white;}

        if(sound!=null&&GUI.Button(new Rect(Screen.width-170,300,145,30),sound.Muted?"Sound off [M]":"Sound on [M]"))sound.Toggle();
        if(playing&&kickoffTime>0&&blue<3&&coral<3){GUI.Box(new Rect(Screen.width/2-200,130,400,75),(kickoffTeam==0?"BLUE RESTART  -  YOUR PUCK":"CORAL RESTART")+"\n"+Mathf.CeilToInt(kickoffTime));}
        GUI.Label(new Rect(24,65,470,30),possession==0?"YOUR PUCK - Hold / release to shoot":possession<0?"LOOSE PUCK":possession<3?"BLUE POSSESSION":"CORAL POSSESSION");
        GUI.Box(new Rect(24,102,180,12),"");GUI.color=new Color(.25f,.8f,1);GUI.DrawTexture(new Rect(26,104,176*stamina,8),Texture2D.whiteTexture);GUI.color=Color.white;
        if(charging){GUI.Box(new Rect(Screen.width/2-80,Screen.height/2+40,160,12),"");GUI.DrawTexture(new Rect(Screen.width/2-78,Screen.height/2+42,156*charge,8),Texture2D.whiteTexture);}
        if(possession!=0&&eye!=null){var mark=eye.WorldToScreenPoint(puck.position);if(mark.z>0){GUI.color=new Color(1,.8f,.2f);GUI.Label(new Rect(Mathf.Clamp(mark.x-24,20,Screen.width-65),Mathf.Clamp(Screen.height-mark.y+18,130,Screen.height-100),80,25),"PUCK");GUI.color=Color.white;}}
        GUI.Label(new Rect(Screen.width/2-8,Screen.height/2-16,30,30),"+");
        GUI.Label(new Rect(24,Screen.height-55,900,50),"WASD swim  /  Mouse look  /  Space rise  /  Ctrl dive  /  Hold click shoot / Click steal  /  E pass  /  Esc pause");
        if(!playing||blue>=3||coral>=3){GUI.Box(new Rect(Screen.width/2-180,Screen.height/2-90,360,160),blue>=3?"YOU WIN":coral>=3?"CORAL WINS":"DIVE  -  3 versus 3");
            GUI.Label(new Rect(Screen.width/2-175,Screen.height/2+70,380,32),$"Shots {shots}   /   Passes {passes}   /   Steals {steals}");
            GUI.enabled=sound!=null&&sound.Ready;
            if(!GUI.enabled)GUI.Label(new Rect(Screen.width/2-100,Screen.height/2+35,220,30),"Preparing audio...");
            if(GUI.Button(new Rect(Screen.width/2-100,Screen.height/2-20,200,50),blue>=3||coral>=3?"Play again":"Click to swim")){if(blue>=3||coral>=3){blue=coral=shots=passes=steals=0;matchStarted=false;ResetRound();}playing=true;sound.SetActive(true);if(!matchStarted){chain.Begin(kickoffTeam*3);sound.Announce(kickoffTeam==0?"BlueStart":"CoralStart");matchStarted=true;}Capture(true);}GUI.enabled=true;}
    }
    void OnDisable(){if(sound!=null)sound.SetActive(false);Capture(false);}
}





