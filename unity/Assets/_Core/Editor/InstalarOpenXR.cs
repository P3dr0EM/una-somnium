using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Somnium.EditorTools
{
    /// <summary>
    /// Instala o pacote OpenXR (VR no PC: Meta Quest via Link/Air Link, SteamVR e outros).
    /// Batch (sem -quit): Unity.exe -batchmode -projectPath ... -executeMethod Somnium.EditorTools.InstalarOpenXR.Run
    /// </summary>
    public static class InstalarOpenXR
    {
        static AddRequest request;

        public static void Run()
        {
            request = Client.Add("com.unity.xr.openxr");
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!request.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"[Somnium] OpenXR instalado: {request.Result.packageId}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Somnium] Falha ao instalar o OpenXR: {request.Error?.message}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
