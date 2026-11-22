#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;
using BattleClubs.World;

namespace BattleClubs.Editor
{
    /// <summary>Builds a map-only 3D preview scene for browser inspection on mobile.</summary>
    public static class BattleClubsMapPreviewBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/BattleClubs_MapPreview.unity";
        private const string BuildPath = "Build/WebGL";

        public static void BuildWebGLMapPreview()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.38f, 0.37f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.57f, 0.63f, 0.67f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0012f;

            var sunObject = new GameObject("MapPreview_Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.91f, 0.78f);
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = sun;

            var map = new GameObject("BATTLE_CLUBS_Favela_Map");
            map.AddComponent<UrbanCommunityBlockout>();
            map.AddComponent<CommunityArtDetails>();
            map.AddComponent<FavelaStreetLifeDetails>();
            map.AddComponent<TeamSpawnManager>();

            var cameraObject = new GameObject("MapPreview_Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            cameraObject.transform.position = new Vector3(0f, 92f, -112f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Directory.CreateDirectory(BuildPath);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("BATTLE CLUBS WebGL map preview failed: " + report.summary.result);

            Debug.Log("BATTLE_CLUBS_MAP_PREVIEW_BUILD_OK: " + report.summary.totalSize + " bytes");
        }
    }
}
#endif
