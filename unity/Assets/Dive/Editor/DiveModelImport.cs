using UnityEditor;
using UnityEngine;
public class DiveModelImport : AssetPostprocessor {
    void OnPreprocessModel(){if(!assetPath.Contains("Dive/Resources/"))return;var m=(ModelImporter)assetImporter;m.animationType=ModelImporterAnimationType.Legacy;m.importAnimation=true;m.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;m.globalScale=1;m.addCollider=false;}
    void OnPreprocessAnimation(){if(!assetPath.Contains("Dive/Resources/"))return;var m=(ModelImporter)assetImporter;var clips=m.defaultClipAnimations;foreach(var c in clips){c.loopTime=true;c.wrapMode=WrapMode.Loop;}m.clipAnimations=clips;}
    [MenuItem("DIVE/Refresh athlete assets")]
    public static void RefreshAthletes(){AssetDatabase.ImportAsset("Assets/Dive/Resources/Athlete.fbx",ImportAssetOptions.ForceUpdate);AssetDatabase.ImportAsset("Assets/Dive/Resources/SwimHands.fbx",ImportAssetOptions.ForceUpdate);}
}
