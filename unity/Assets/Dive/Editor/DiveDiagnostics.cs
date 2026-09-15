using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
[InitializeOnLoad]
public static class DiveDiagnostics {
 static DiveDiagnostics(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Inspect;};}
 [MenuItem("DIVE/Inspect running visuals")]
 public static void Inspect(){var s=new StringBuilder();foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))s.AppendLine($"CAMERA {c.name} pos={c.transform.position} enabled={c.enabled} target={c.targetTexture}");foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){if(r.name.Contains("Athlete")||r.name.Contains("Hands")||r.name.Contains("water"))s.AppendLine($"MESH {r.name} bounds={r.bounds} scale={r.transform.lossyScale} shader={r.sharedMaterial.shader.name}");}foreach(var a in Object.FindObjectsByType<Animation>(FindObjectsSortMode.None)){s.AppendLine($"ANIMATION {a.name} playing={a.isPlaying}");foreach(AnimationState state in a)s.AppendLine($"CLIP {state.name} length={state.length}");}foreach(var skin in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)){var mesh=new Mesh();skin.BakeMesh(mesh);s.AppendLine($"BAKED {skin.name} enabled={skin.enabled} bounds={mesh.bounds} bone0pos={skin.bones[0].position} bone0scale={skin.bones[0].lossyScale}");Object.DestroyImmediate(mesh);}Directory.CreateDirectory("../artifacts");File.WriteAllText("../artifacts/visual-diagnostics.txt",s.ToString());Debug.Log("DIVE_DIAGNOSTICS_READY");}
}

