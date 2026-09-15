using UnityEngine;

// Built-in render pipeline: a small reflection pass, never rendered recursively.
public sealed class DiveWaterReflection : MonoBehaviour {
    Camera reflectionCamera;
    RenderTexture reflection;
    bool rendering;
    void OnWillRenderObject() {
        var source=Camera.current;
        if(rendering || source==null || source.cameraType!=CameraType.Game) return;
        if(reflectionCamera==null){var go=new GameObject("Water reflection camera");go.hideFlags=HideFlags.HideAndDontSave;reflectionCamera=go.AddComponent<Camera>();reflectionCamera.enabled=false;reflection=new RenderTexture(512,512,16);reflection.name="DIVE reflection";}
        rendering=true;
        try {
            reflectionCamera.CopyFrom(source);reflectionCamera.enabled=false;reflectionCamera.targetTexture=reflection;
            // Water is layer 4 and is excluded from its own reflected render.
            reflectionCamera.cullingMask=source.cullingMask & ~(1<<4);
            float height=transform.position.y;
            Matrix4x4 mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=2*height;
            reflectionCamera.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
            Vector3 p=reflectionCamera.worldToCameraMatrix.MultiplyPoint(new Vector3(0,height+.03f,0));
            Vector3 n=reflectionCamera.worldToCameraMatrix.MultiplyVector(source.transform.position.y>height?Vector3.up:Vector3.down).normalized;
            reflectionCamera.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(n.x,n.y,n.z,-Vector3.Dot(p,n)));
            bool previous=GL.invertCulling;
            try{GL.invertCulling=!previous;reflectionCamera.Render();}finally{GL.invertCulling=previous;}
            GetComponent<Renderer>().sharedMaterial.SetTexture("_Reflection",reflection);
        } finally {rendering=false;}
    }
    void OnDestroy(){if(reflectionCamera!=null)Destroy(reflectionCamera.gameObject);if(reflection!=null){reflection.Release();Destroy(reflection);}}
}
