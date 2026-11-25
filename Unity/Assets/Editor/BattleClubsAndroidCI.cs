#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BattleClubs.Editor
{
    /// <summary>Builds the scenes explicitly configured for the Android player.</summary>
    public static class BattleClubsAndroidCI
    {
        public static void BuildAndroid()
        {
            // Never generate or switch to the procedural community test scene here.
            // CI must build the project's actual scenes as configured in Build Settings.
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new Exception(
                    "Nenhuma cena de jogo está habilitada em File > Build Settings. " +
                    "Adicione a cena principal do BATTLE CLUBS antes de gerar o APK.");

            foreach (string scene in scenes)
            {
                if (!System.IO.File.Exists(scene))
                    throw new Exception("Cena configurada não encontrada: " + scene);
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new Exception("O módulo Android não está instalado no Unity Editor.");

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.binhozer09.battleclubs");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            string outputPath = System.IO.Path.GetFullPath("Build/Android/BATTLE-CLUBS.apk");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("BATTLE_CLUBS_BUILD_RESULT=" + report.summary.result);
            Debug.Log("BATTLE_CLUBS_BUILD_OUTPUT=" + outputPath);
            Debug.Log("BATTLE_CLUBS_BUILD_ERRORS=" + report.summary.totalErrors);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build Android falhou: " + report.summary.result +
                                    " (" + report.summary.totalErrors + " erros).");
            if (!System.IO.File.Exists(outputPath) || new System.IO.FileInfo(outputPath).Length == 0)
                throw new Exception("Unity informou sucesso, mas o APK não existe ou está vazio.");
        }
    }
}
#endif
