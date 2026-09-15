using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DiveSceneBuilder {
    static DiveSceneBuilder(){EditorApplication.delayCall+=PrepareFirstScene;}
    static void PrepareFirstScene(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
        if(!File.Exists("Assets/Dive/Scenes/FirstPerson.unity")&&!EditorSceneManager.GetActiveScene().isDirty)Create();
    }
    [MenuItem("DIVE/Create first-person preview")]
    public static void Create(){
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("DIVE");var game=root.AddComponent<DiveFirstPerson>();game.floorShader=Shader.Find("Dive/PoolFloor");
        Directory.CreateDirectory("Assets/Dive/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Dive/Scenes/FirstPerson.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Dive/Scenes/FirstPerson.unity",true)};
        PlayerSettings.companyName="DIVE";PlayerSettings.productName="DIVE";PlayerSettings.colorSpace=ColorSpace.Linear;
        AssetDatabase.SaveAssets();Debug.Log("DIVE_FIRST_PERSON_READY: Press Play, then Click to swim.");
    }
    [MenuItem("DIVE/Build browser preview")]
    public static void Build(){
        var normalImporter=AssetImporter.GetAtPath("Assets/Dive/Resources/DiverNormal.png") as TextureImporter;
        if(normalImporter!=null&&normalImporter.textureType!=TextureImporterType.NormalMap){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();}
        // Runtime-created materials need explicit shader references in the player.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(string name in new[]{"Standard","Skybox/Procedural","Dive/PoolFloor","Dive/UnderwaterSurface","Dive/Bubble","Dive/AthleteTeam"}){var shader=Shader.Find(name);if(shader==null)throw new System.Exception("Missing shader: "+name);bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=shader;}}
        graphics.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template="APPLICATION:Minimal";
        PlayerSettings.runInBackground=true;
        Directory.CreateDirectory("Builds/Web");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Dive/Scenes/FirstPerson.unity"},locationPathName="Builds/Web",target=BuildTarget.WebGL,options=BuildOptions.None});
        Directory.CreateDirectory("../artifacts");File.WriteAllText("../artifacts/web-build-result.txt",result.summary.result+" bytes="+result.summary.totalSize+" duration="+result.summary.totalTime);
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Web build failed: "+result.summary.result);
        File.Copy("../web/unity.html","Builds/Web/index.html",true);
        File.Copy("../web/devnet.js","Builds/Web/devnet.js",true);
        Debug.Log("DIVE_WEB_BUILD_READY");
    }
}
