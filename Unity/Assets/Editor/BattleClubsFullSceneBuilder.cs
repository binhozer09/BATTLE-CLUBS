#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BattleClubs.Player;
using BattleClubs.UI;
using BattleClubs.Gameplay;
using BattleClubs.World;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Creates the main playable Battle Clubs community blockout scene.
    /// This is the project's actual game entry scene, not the old isolated test scene.
    /// Character meshes and online multiplayer still require their production assets/systems.
    /// </summary>
    public static class BattleClubsFullSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BattleClubs_Main.unity";
        private const string DamageColorPath = "Assets/Textures/AsphaltDamage001/AsphaltDamage001_2K-JPG_Color.jpg";
        private const string DamageOpacityPath = "Assets/Textures/AsphaltDamage001/AsphaltDamage001_2K-JPG_Opacity.jpg";
        private const string DamageCombinedPath = "Assets/Textures/AsphaltDamage001/AsphaltDamage001_RGBA.png";

        [MenuItem("BATTLE CLUBS/Create Main Game Scene")]
        public static void CreateMainGameScene()
        {
            EnsureFolder("Assets", "Scenes");
            ConfigureLighting();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject world = new GameObject("BATTLE_CLUBS_WORLD");
            UrbanCommunityBlockout map = world.AddComponent<UrbanCommunityBlockout>();

            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(DamageColorPath);
            if (color == null)
                throw new System.Exception("Damage 001 Color map missing: " + DamageColorPath);

            Texture2D combined = BuildDamageTexture();
            SerializedObject mapData = new SerializedObject(map);
            mapData.FindProperty("roadSurfaceTexture").objectReferenceValue = color;
            if (combined != null)
                mapData.FindProperty("asphaltDamageTexture").objectReferenceValue = combined;
            mapData.ApplyModifiedPropertiesWithoutUndo();

            GameObject localPlayer = CreatePlayer(new Vector3(4f, 0.15f, 4f));
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraRig rig = cameraObject.AddComponent<ThirdPersonCameraRig>();
            SetObjectReference(rig, "target", localPlayer.transform);
            SetObjectReference(localPlayer.GetComponent<ThirdPersonMotor>(), "cameraTransform", cameraObject.transform);

            BatAttack attack = localPlayer.AddComponent<BatAttack>();
            Transform hitOrigin = new GameObject("Bat_Hit_Origin").transform;
            hitOrigin.SetParent(localPlayer.transform, false);
            hitOrigin.localPosition = new Vector3(0f, 1.05f, 1.05f);
            SetObjectReference(attack, "hitOrigin", hitOrigin);
            SetObjectReference(attack, "cameraRig", rig);
            localPlayer.AddComponent<BattleClubsMobileHUD>();

            CreateTeamSetup(localPlayer.GetComponent<Combatant>());
            CreateSpawnMarkers();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BATTLE_CLUBS_MAIN_SCENE_CREATED=" + ScenePath);
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.38f, 0.37f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.57f, 0.63f, 0.67f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0018f;
            GameObject sun = new GameObject("Sun_Directional_Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.91f, 0.78f);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = light;
        }

        private static GameObject CreatePlayer(Vector3 position)
        {
            GameObject root = new GameObject("Player_Local");
            root.transform.position = position;
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.42f;
            controller.center = new Vector3(0f, 1f, 0f);
            root.AddComponent<ThirdPersonMotor>();
            root.AddComponent<Combatant>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Player_Body_Blockout";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.78f, 0.92f, 0.78f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            SetColor(body, new Color(0.13f, 0.29f, 0.52f));

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Player_Head_Blockout";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.03f, 0f);
            head.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            SetColor(head, new Color(0.57f, 0.39f, 0.28f));

            GameObject bat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bat.name = "Bat_Visual";
            bat.transform.SetParent(root.transform, false);
            bat.transform.localPosition = new Vector3(0.55f, 1.05f, 0.35f);
            bat.transform.localRotation = Quaternion.Euler(0f, 0f, 68f);
            bat.transform.localScale = new Vector3(0.09f, 0.58f, 0.09f);
            Object.DestroyImmediate(bat.GetComponent<Collider>());
            SetColor(bat, new Color(0.36f, 0.22f, 0.12f));
            return root;
        }

        private static void CreateTeamSetup(Combatant localPlayer)
        {
            GameObject rulesObject = new GameObject("Team_Match_Rules");
            TeamMatchRules rules = rulesObject.AddComponent<TeamMatchRules>();
            SerializedObject serialized = new SerializedObject(rules);
            serialized.FindProperty("playersPerTeam").intValue = 4;
            serialized.FindProperty("pointsPerEliminatedTeam").intValue = 3;
            SerializedProperty teams = serialized.FindProperty("teams");
            teams.arraySize = 5;
            string[] teamNames = { "Azul", "Vermelho", "Verde", "Amarelo", "Roxo" };
            for (int i = 0; i < teams.arraySize; i++)
            {
                SerializedProperty team = teams.GetArrayElementAtIndex(i);
                team.FindPropertyRelative("teamId").stringValue = "TEAM_" + (i + 1);
                team.FindPropertyRelative("clubId").stringValue = teamNames[i];
                team.FindPropertyRelative("players").arraySize = 0;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            rules.TryAddPlayer("TEAM_1", localPlayer);
        }

        private static void CreateSpawnMarkers()
        {
            GameObject root = new GameObject("TEAM_SPAWN_POINTS_5x4");
            Color[] colors = {
                new Color(0.12f, 0.35f, 0.85f), new Color(0.8f, 0.16f, 0.14f),
                new Color(0.15f, 0.62f, 0.25f), new Color(0.9f, 0.68f, 0.12f),
                new Color(0.48f, 0.2f, 0.72f)
            };
            for (int team = 0; team < 5; team++)
            {
                GameObject teamRoot = new GameObject("Team_" + (team + 1) + "_Spawns");
                teamRoot.transform.SetParent(root.transform, false);
                for (int member = 0; member < 4; member++)
                {
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = "Spawn_" + (member + 1);
                    marker.transform.SetParent(teamRoot.transform, false);
                    float angle = (team * 72f + member * 12f) * Mathf.Deg2Rad;
                    marker.transform.position = new Vector3(38f + Mathf.Cos(angle) * (14f + team * 3f), 0.08f,
                        38f + Mathf.Sin(angle) * (14f + team * 3f));
                    marker.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
                    SetColor(marker, colors[team]);
                    Collider collider = marker.GetComponent<Collider>();
                    if (collider != null) Object.DestroyImmediate(collider);
                }
            }
        }

        private static Texture2D BuildDamageTexture()
        {
            if (!File.Exists(DamageColorPath) || !File.Exists(DamageOpacityPath))
                throw new System.Exception("Damage 001 Color/Opacity maps are required.");
            SetReadable(DamageColorPath, true);
            SetReadable(DamageOpacityPath, true);
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(DamageColorPath);
            Texture2D opacity = AssetDatabase.LoadAssetAtPath<Texture2D>(DamageOpacityPath);
            if (color == null || opacity == null || color.width != opacity.width || color.height != opacity.height)
                throw new System.Exception("Damage 001 maps could not be loaded or dimensions differ.");
            Color[] pixels = color.GetPixels();
            Color[] mask = opacity.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i].a = mask[i].grayscale;
            Texture2D combined = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false, true);
            combined.SetPixels(pixels);
            combined.Apply(false, false);
            File.WriteAllBytes(DamageCombinedPath, combined.EncodeToPNG());
            Object.DestroyImmediate(combined);
            AssetDatabase.ImportAsset(DamageCombinedPath, ImportAssetOptions.ForceUpdate);
            SetReadable(DamageColorPath, false);
            SetReadable(DamageOpacityPath, false);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(DamageCombinedPath);
        }

        private static void SetReadable(string path, bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.isReadable != readable)
            {
                importer.isReadable = readable;
                importer.SaveAndReimport();
            }
        }

        private static void SetObjectReference(Object target, string property, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new System.Exception("Serialized field not found: " + property);
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            Material material = new Material(shader) { color = color };
            renderer.sharedMaterial = material;
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
