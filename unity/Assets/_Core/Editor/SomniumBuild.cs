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
