using UnityEngine;
using System.Collections.Generic;

// Baked clips work in Web builds; no generation service or credentials ship to players.
public sealed class DiveAudio : MonoBehaviour {
    readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
    readonly AudioSource[] voices=new AudioSource[8];
    AudioSource ambience, swim, ui, announcer;
    readonly AudioSource[] music=new AudioSource[2];
    int musicLead;
    float musicTime;
    int nextVoice;
    bool active, focused=true;
    float movement, nextImpact;
    public bool Muted {get;private set;}
    bool reportedReady;
    public bool Ready {get{if(clips.Count!=18)return false;foreach(var clip in clips.Values)if(clip.loadState!=AudioDataLoadState.Loaded)return false;return true;}}
    AudioSource Source(){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=0;return source;}
    void Awake(){
        foreach(var name in new[]{"Ambience","Swim","Shot","Pass","Impact","Pickup","Start","Goal","Concede","Win","Lose"}){
            var clip=Resources.Load<AudioClip>("Audio/"+name);if(clip!=null)clips[name]=clip;else Debug.LogError("Missing DIVE audio: "+name);
        }
        foreach(var name in new[]{"Music","BlueStart","CoralStart","BlueGoal","CoralGoal","BlueWin","CoralWin"}){var clip=Resources.Load<AudioClip>("Audio/"+name);if(clip!=null)clips[name]=clip;}
        foreach(var clip in clips.Values)clip.LoadAudioData();
        ambience=Source();swim=Source();ui=Source();announcer=Source();ui.volume=1;announcer.volume=.8f;
        for(int i=0;i<2;i++){music[i]=Source();if(clips.TryGetValue("Music",out var track))music[i].clip=track;}
        for(int i=0;i<voices.Length;i++)voices[i]=Source();
        ambience.loop=swim.loop=true;
        if(clips.TryGetValue("Ambience",out var bed))ambience.clip=bed;
        if(clips.TryGetValue("Swim",out var strokes))swim.clip=strokes;
        Muted=PlayerPrefs.GetInt("DiveMuted",0)==1;
        ApplyMute();
    }
    public void SetActive(bool value){active=value;if(value){if(!ambience.isPlaying)ambience.Play();if(!swim.isPlaying)swim.Play();if(!music[musicLead].isPlaying&&music[musicLead].clip!=null){music[musicLead].Play();musicTime=0;}}else{ambience.Stop();swim.Stop();foreach(var voice in voices)voice.Stop();foreach(var source in music)source.Stop();musicTime=0;}}
    public void Movement(float value){movement=Mathf.Clamp01(value);}
    public void Toggle(){Muted=!Muted;PlayerPrefs.SetInt("DiveMuted",Muted?1:0);PlayerPrefs.Save();ApplyMute();}
    void ApplyMute(){bool mute=Muted||!focused;ambience.mute=swim.mute=ui.mute=announcer.mute=mute;foreach(var voice in voices)voice.mute=mute;foreach(var source in music)source.mute=mute;}
    void Update(){
        ApplyMute();if(!Ready)return;if(!reportedReady){Debug.Log("DIVE_AUDIO_READY: 18 clips decoded");reportedReady=true;}float dt=Time.unscaledDeltaTime;
        ambience.volume=Mathf.MoveTowards(ambience.volume,active?.22f:0,dt);swim.volume=Mathf.MoveTowards(swim.volume,active?movement*.3f:0,dt*.7f);
        if(active&&music[musicLead].clip!=null){
            musicTime+=dt;float length=music[musicLead].clip.length;
            if(musicTime>=length-1){musicLead=1-musicLead;music[musicLead].Play();musicTime=0;}
            float gain=announcer.isPlaying?.055f:.18f;
            music[musicLead].volume=Mathf.MoveTowards(music[musicLead].volume,gain,dt*.2f);
            music[1-musicLead].volume=Mathf.MoveTowards(music[1-musicLead].volume,0,dt*.2f);
        }
    }
    void OnApplicationFocus(bool value){focused=value;ApplyMute();}
    public void Cue(string name){if(!clips.TryGetValue(name,out var clip))return;ui.pitch=1;ui.PlayOneShot(clip,.42f);}
    public void Announce(string name){if(!clips.TryGetValue(name,out var clip))return;announcer.Stop();announcer.clip=clip;announcer.Play();}
    public void At(string name,Vector3 position,float gain=1){
        if(!active||!clips.TryGetValue(name,out var clip))return;
        if(name=="Impact"){if(Time.time<nextImpact)return;nextImpact=Time.time+.1f;}
        var delta=position-transform.position;float distance=delta.magnitude;
        var voice=voices[nextVoice++%voices.Length];voice.Stop();voice.clip=clip;voice.volume=gain*.48f/(1+distance*.22f);voice.pitch=Random.Range(.94f,1.06f);voice.panStereo=Mathf.Clamp(Vector3.Dot(transform.right,delta.normalized)*.7f,-.7f,.7f);voice.Play();
    }
}
