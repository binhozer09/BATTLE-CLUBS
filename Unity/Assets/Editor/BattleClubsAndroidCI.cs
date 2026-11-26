#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleClubs.Editor
{
    /// <summary>Builds an Android APK using the exact user-provided VillageBuilder.</summary>
    public static class BattleClubsAndroidCI
    {
        public static void BuildAndroid()
        {
            const string obsoleteTestScene = "Assets/Scenes/BattleClubs_Community_Test.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(obsoleteTestScene) != null)
                AssetDatabase.DeleteAsset(obsoleteTestScene);

            const string mainScene = "Assets/Scenes/BattleClubs_MedievalVillage.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VillageBuilder.BuildVillage();
            if (!EditorSceneManager.SaveScene(scene, mainScene))
                throw new Exception("Não foi possível salvar a cena do vilarejo: " + mainScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(mainScene, true) };
            string[] scenes = EditorBuildSettings.scenes
                .Where(item => item.enabled)
                .Select(item => item.path)
                .ToArray();

            if (scenes.Length == 0 || !System.IO.File.Exists(mainScene))
                throw new Exception("A cena principal do vilarejo não foi criada: " + mainScene);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new Exception("O módulo Android não está instalado no Unity Editor.");

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.binhozer09.battleclubs");
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.Android.bundleVersionCode = 2;
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
            Debug.Log("BATTLE_CLUBS_BUILD_SCENE=" + mainScene);
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