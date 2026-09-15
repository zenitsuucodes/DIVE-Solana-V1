using UnityEngine;
public sealed class DiveChain : MonoBehaviour {
 #if UNITY_WEBGL && !UNITY_EDITOR
 [System.Runtime.InteropServices.DllImport("__Internal")] static extern void DiveEvent(string payload);
 #endif
 [System.Serializable] class Event {public string match,type;public int seq,actor,blue,coral;}
 string match;int sequence;
 public void Begin(int actor){match=System.Guid.NewGuid().ToString("N");sequence=0;Record("match_start",actor,0,0);}
 public void Record(string type,int actor,int blue,int coral){
  if(match==null)return;
  var payload=JsonUtility.ToJson(new Event{match=match,seq=++sequence,type=type,actor=actor,blue=blue,coral=coral});
  #if UNITY_WEBGL && !UNITY_EDITOR
  DiveEvent(payload);
  #endif
 }
}
