using UnityEngine;
using System.Collections.Generic;

// One shared Meshy skeleton, posed in world space so import bone axes do not matter.
public sealed class DiveHuman : MonoBehaviour {
    readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
    readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();
    bool standing; float strike,phase;
    public void Setup(bool blue,bool portrait=false){
        standing=portrait;phase=Random.value*6.28f;
        foreach(var bone in GetComponentsInChildren<Transform>()){bones[bone.name]=bone;rest[bone]=bone.localRotation;}
        foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        gameObject.AddComponent<DiveTeamAppearance>().Apply(blue);
        Pose();
    }
    public void Strike(){strike=Time.time+.45f;}
    void Aim(string name,string child,Vector3 direction){
        if(!bones.TryGetValue(name,out var bone)||!bones.TryGetValue(child,out var end))return;
        bone.rotation=Quaternion.FromToRotation(end.position-bone.position,direction)*bone.rotation;
    }
    void LateUpdate(){Pose();}
    void Pose(){
        foreach(var item in rest)if(item.Key!=transform)item.Key.localRotation=item.Value;
        var forward=standing?transform.forward:transform.parent.forward;var right=standing?transform.right:transform.parent.right;
        float kick=Mathf.Sin(Time.time*5+phase)*.2f;
        foreach(string side in new[]{"Left","Right"}){
            float sign=side=="Left"?-1:1;
            if(standing){Aim(side+"Arm",side+"ForeArm",Vector3.down+right*sign*.14f);Aim(side+"ForeArm",side+"Hand",Vector3.down+forward*.12f);}
            else {
                Aim(side+"Arm",side+"ForeArm",forward*.7f+right*sign*.38f-Vector3.up*.12f);
                Aim(side+"ForeArm",side+"Hand",forward+Vector3.up*(Time.time<strike?.25f:-.08f));
                Aim(side+"UpLeg",side+"Leg",-forward+Vector3.up*kick*sign);
                Aim(side+"Leg",side+"Foot",-forward-Vector3.up*kick*sign);
            }
        }
    }
}
