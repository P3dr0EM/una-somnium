using UnityEngine;
using UnityEngine.Rendering;

public class MainCamera : MonoBehaviour {

    Portal[] portals;

    void Awake () {
        portals = FindObjectsByType<Portal> ();
    }

    void OnEnable () {
        RenderPipelineManager.beginCameraRendering += OnCameraRender;
    }

    void OnDisable () {
        RenderPipelineManager.beginCameraRendering -= OnCameraRender;
    }

    void OnCameraRender (ScriptableRenderContext context, Camera camera) {
        if (camera != Camera.main) return;

        for (int i = 0; i < portals.Length; i++) {
            portals[i].PrePortalRender();
        }
        for (int i = 0; i < portals.Length; i++) {
            portals[i].Render(context);
        }

        for (int i = 0; i < portals.Length; i++) {
            portals[i].PostPortalRender();
        }

    }

}