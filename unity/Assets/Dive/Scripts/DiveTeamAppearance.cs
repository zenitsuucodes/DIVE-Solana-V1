using UnityEngine;
// Applied to the imported Meshy character, independently of its animation rig.
public sealed class DiveTeamAppearance : MonoBehaviour {
 public void Apply(bool blue) {
  var shader=Shader.Find("Dive/AthleteTeam");if(shader==null)return;
  foreach(var renderer in GetComponentsInChildren<Renderer>()) {
   var materials=renderer.materials;
   for(int i=0;i<materials.Length;i++) {
    var source=materials[i];Texture texture=Resources.Load<Texture2D>("DiverColor");
    if(texture==null)texture=source.mainTexture;
    if(texture==null)continue;
    var team=new Material(shader);team.name=blue?"DIVE Blue athlete":"DIVE Coral athlete";
    team.SetTexture("_MainTex",texture);
    var normal=Resources.Load<Texture2D>("DiverNormal");if(normal!=null)team.SetTexture("_BumpMap",normal);

    team.SetColor("_TeamTint",blue?new Color(.08f,.42f,1f):new Color(1f,.19f,.09f));
    materials[i]=team;
   }
   renderer.materials=materials;
  }
 }
}
