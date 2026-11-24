using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Somnium.EditorTools
{
    /// <summary>
    /// Gera os executáveis do jogo com as cenas marcadas na lista do build (a "persistencia" primeiro).
    /// Menus: Somnium → Build. Em batch:
    ///   Unity.exe -batchmode -quit -buildTarget Android -executeMethod Somnium.EditorTools.SomniumBuild.Android -buildOut "pasta"
    ///   Unity.exe -batchmode -quit -buildTarget Win64   -executeMethod Somnium.EditorTools.SomniumBuild.Windows -buildOut "pasta"
    /// </summary>
    public static class SomniumBuild
    {
        const string DefaultOut = @"D:\Projetos\Somnium\Claude\builds";
        const string FirstScene = "Assets/Scenes/persistencia.unity";

        [MenuItem("Somnium/Build/APK (Android, Cardboard)")]
        public static void Android() => Build(BuildTarget.Android, "Android", "Somnium.apk");

        [MenuItem("Somnium/Build/PC (Windows, mouse e teclado)")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "PC", "Somnium.exe");

        const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        /// <summary>
        /// PC com VR (OpenXR: Meta Quest via Link/Air Link, SteamVR...). Liga o OpenXR no Standalone só durante este build
        /// e desliga em seguida, para a versão PC normal continuar só de monitor. Sem óculos conectado, abre no monitor.
        /// </summary>
        [MenuItem("Somnium/Build/PC VR (Windows, OpenXR)")]
        public static void WindowsVR()
        {
            var perTarget = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (perTarget == null || perTarget.AssignedSettings == null)
            {
                Debug.LogError("[Somnium] Configuração de XR do Standalone não encontrada.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            bool assigned = UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.AssignLoader(perTarget.AssignedSettings, OpenXRLoader, BuildTargetGroup.Standalone);
            perTarget.InitManagerOnStart = true;
            // Desliga o "Mock Runtime" (runtime falso de testes do pacote): sem isso o build ignora o óculos de verdade.
            var oxr = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (oxr != null)
                foreach (var feature in oxr.GetFeatures())
                    if (feature.GetType().Name.Contains("Mock") || feature.GetType().Name.Contains("ConformanceAutomation"))
                    {
                        if (feature.enabled) Debug.Log($"[Somnium] OpenXR: desligando {feature.GetType().Name}");
                        feature.enabled = false;
                        EditorUtility.SetDirty(feature);
                    }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] OpenXR no Standalone: {(assigned ? "ligado" : "já estava ligado")}.");
            try
            {
                Build(BuildTarget.StandaloneWindows64, "PC_VR", "Somnium_VR.exe");
                // Tira o runtime falso de testes do pacote (mock_runtime): no PC do jogador vale o runtime real
                // (Quest Link, SteamVR...); sem óculos, o jogo abre no monitor.
                string plugins = Path.Combine(DefaultOut, "PC_VR", "Somnium_VR_Data", "Plugins", "x86_64");
                foreach (var f in new[] { "mock_runtime.dll", "mock_api.dll" })
                {
                    string p = Path.Combine(plugins, f);
                    if (File.Exists(p)) { File.Delete(p); Debug.Log($"[Somnium] Removido do build: {f}"); }
                }
            }
            finally
            {
                UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.RemoveLoader(perTarget.AssignedSettings, OpenXRLoader, BuildTargetGroup.Standalone);
                AssetDatabase.SaveAssets();
                Debug.Log("[Somnium] OpenXR desligado de novo no Standalone (o PC normal fica só de monitor).");
            }
        }

        static void Build(BuildTarget target, string subfolder, string fileName)
        {
            string outRoot = DefaultOut;
            var args = System.Environment.GetCommandLineArgs();
            int idx = System.Array.IndexOf(args, "-buildOut");
            if (idx >= 0 && idx + 1 < args.Length) outRoot = args[idx + 1];
            string dir = Path.Combine(outRoot, subfolder);
            Directory.CreateDirectory(dir);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            scenes.Remove(FirstScene);
            scenes.Insert(0, FirstScene); // o jogo abre pela persistencia, que carrega o Saguão

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Path.Combine(dir, fileName),
                target = target,
                options = BuildOptions.None,
            };

            Debug.Log($"[Somnium] Build {target}: {string.Join(", ", scenes)}");
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Somnium] Build {target}: {s.result}, {s.totalErrors} erro(s), {s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalMinutes:0.0} min → {options.locationPathName}");

            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
            if (!Application.isBatchMode && s.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(options.locationPathName);
        }
    }
}
