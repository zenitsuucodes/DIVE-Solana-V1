using UnityEngine;
public sealed class DiveAthlete : MonoBehaviour {
    Animation motion;string swim,idle,shot;Vector3 previous;float strikeUntil;bool hands;
    public void Setup(bool blue,bool firstPerson=false){
        hands=firstPerson;previous=transform.position;
        foreach(var r in GetComponentsInChildren<Renderer>()){
            foreach(var m in r.materials){
                if(m.name.Contains("Team")){m.color=blue?new Color(.025f,.32f,.7f):new Color(.9f,.19f,.085f);m.SetFloat("_Glossiness",.58f);}
                if(m.name.Contains("Glass")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.015f,.17f,.2f));m.SetFloat("_Glossiness",.93f);m.SetFloat("_Metallic",.65f);}
                if(m.name.Contains("Metal")){m.SetFloat("_Metallic",.8f);m.SetFloat("_Glossiness",.7f);}
            }
        }
        motion=GetComponentInChildren<Animation>();if(motion!=null){foreach(AnimationState a in motion){if(a.name.Contains("Swim"))swim=a.name;if(a.name.Contains("Idle"))idle=a.name;if(a.name.Contains("Shoot"))shot=a.name;a.wrapMode=WrapMode.Loop;}if(swim!=null){motion.Play(swim);motion[swim].normalizedTime=Random.value;}}
    }
    public void Strike(){strikeUntil=Time.time+.55f;if(motion!=null&&shot!=null){motion[shot].wrapMode=WrapMode.Once;motion[shot].speed=2.6f;motion.CrossFade(shot,.12f);}}
    void LateUpdate(){float speed=(transform.position-previous).magnitude/Mathf.Max(Time.deltaTime,.001f);previous=transform.position;if(motion==null||Time.time<strikeUntil)return;string clip=(speed>.1f||hands)?swim:idle;if(clip!=null){motion.CrossFade(clip,.3f);motion[clip].speed=hands?.65f:Mathf.Clamp(speed*.35f,.6f,1.5f);}}
}
