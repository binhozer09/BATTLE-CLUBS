#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleClubs.Player;
using BattleClubs.UI;
using BattleClubs.Gameplay;
using BattleClubs.World;

namespace BattleClubs.Editor
{
    /// <summary>Creates a first visual test scene using the project's procedural map and player scripts.</summary>
    public static class BattleClubsSceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/BattleClubs_Community_Test.unity";

        [MenuItem("BATTLE CLUBS/Create Community Test Scene")]
        public static void CreateCommunityTestScene()
        {
            EnsureFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.38f, 0.37f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.57f, 0.63f, 0.67f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0018f;

            GameObject sun = new GameObject("Sun_Directional_Light");
            Light sunlight = sun.AddComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.intensity = 1.25f;
            sunlight.color = new Color(1f, 0.91f, 0.78f);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = sunlight;

            GameObject map = new GameObject("BATTLE_CLUBS_Community_Map");
            UrbanCommunityBlockout mapGenerator = map.AddComponent<UrbanCommunityBlockout>();
            Texture2D damageTexture = BuildAsphaltDamageTexture();
            Texture2D roadTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DamageColorPath);
            if (roadTexture == null)
                throw new System.Exception("Textura AsphaltDamage001 Color não encontrada: " + DamageColorPath);

            SerializedObject mapSettings = new SerializedObject(mapGenerator);
            mapSettings.FindProperty("roadSurfaceTexture").objectReferenceValue = roadTexture;
            if (damageTexture != null)
                mapSettings.FindProperty("asphaltDamageTexture").objectReferenceValue = damageTexture;
            mapSettings.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = CreatePlayer(new Vector3(4f, 0.1f, 4f));
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 350f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraRig rig = cameraObject.AddComponent<ThirdPersonCameraRig>();
            SerializedObject rigSerialized = new SerializedObject(rig);
            rigSerialized.FindProperty("target").objectReferenceValue = player.transform;
            rigSerialized.ApplyModifiedPropertiesWithoutUndo();

            ThirdPersonMotor motor = player.GetComponent<ThirdPersonMotor>();
            SerializedObject motorSerialized = new SerializedObject(motor);
            motorSerialized.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            motorSerialized.ApplyModifiedPropertiesWithoutUndo();

            BatAttack attack = player.AddComponent<BatAttack>();
            Transform hitOrigin = new GameObject("Bat_Hit_Origin").transform;
            hitOrigin.SetParent(player.transform, false);
            hitOrigin.localPosition = new Vector3(0f, 1.05f, 1.05f);
            SerializedObject attackSerialized = new SerializedObject(attack);
            attackSerialized.FindProperty("hitOrigin").objectReferenceValue = hitOrigin;
            attackSerialized.FindProperty("cameraRig").objectReferenceValue = rig;
            attackSerialized.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<BattleClubsMobileHUD>();

            CreateTrainingDummy(new Vector3(7f, 0.1f, 7f), new Color(0.64f, 0.22f, 0.18f), "Training_Dummy_01");
            CreateTrainingDummy(new Vector3(10f, 0.1f, 9f), new Color(0.18f, 0.32f, 0.68f), "Training_Dummy_02");

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = map;
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "Cena de teste criada em " + ScenePath +
                    "\n\nPressione Play para gerar o mapa procedural e testar a câmera e os controles.",
                    "OK");
            }
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

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Player_Body_Visual";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.78f, 0.92f, 0.78f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            SetColor(body, new Color(0.13f, 0.29f, 0.52f));

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Player_Head_Visual";
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

        private static void CreateTrainingDummy(Vector3 position, Color color, string objectName)
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = objectName;
            dummy.transform.position = position + Vector3.up;
            dummy.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
            SetColor(dummy, color);
            dummy.AddComponent<Combatant>();
        }

        private static void SetColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            Material material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private const string DamageColorPath = "Assets/Textures/AsphaltDamage001/AsphaltDamage001_2K-JPG_Color.jpg";
        private const string DamageOpacityPath = "Assets/Textures/AsphaltDamage001/AsphaltDamage001_2K-JPG_Opacity.jpg";
        private const string DamageCombinedPath = "Assets/Textures/AsphaltDamage001/AsphaltDamage001_RGBA.png";

        private static Texture2D BuildAsphaltDamageTexture()
        {
            if (!File.Exists(DamageColorPath) || !File.Exists(DamageOpacityPath))
            {
                Debug.LogError("Asphalt Damage 001 maps are missing. Expected Color and Opacity JPG files in " +
                    "Assets/Textures/AsphaltDamage001.");
                return null;
            }

            MakeTextureReadable(DamageColorPath);
            MakeTextureReadable(DamageOpacityPath);
            Texture2D colorMap = AssetDatabase.LoadAssetAtPath<Texture2D>(DamageColorPath);
            Texture2D opacityMap = AssetDatabase.LoadAssetAtPath<Texture2D>(DamageOpacityPath);
            if (colorMap == null || opacityMap == null ||
                colorMap.width != opacityMap.width || colorMap.height != opacityMap.height)
            {
                Debug.LogError("Asphalt Damage 001 Color/Opacity maps could not be loaded or have different sizes.");
                return null;
            }

            Color[] colors = colorMap.GetPixels();
            Color[] opacity = opacityMap.GetPixels();
            for (int i = 0; i < colors.Length; i++)
                colors[i].a = opacity[i].grayscale;

            Texture2D combined = new Texture2D(colorMap.width, colorMap.height, TextureFormat.RGBA32, false, true);
            combined.SetPixels(colors);
            combined.Apply(false, false);
            File.WriteAllBytes(DamageCombinedPath, combined.EncodeToPNG());
            Object.DestroyImmediate(combined);
            AssetDatabase.ImportAsset(DamageCombinedPath, ImportAssetOptions.ForceUpdate);

            TextureImporter outputImporter = AssetImporter.GetAtPath(DamageCombinedPath) as TextureImporter;
            if (outputImporter != null)
            {
                outputImporter.textureType = TextureImporterType.Default;
                outputImporter.alphaIsTransparency = true;
                outputImporter.wrapMode = TextureWrapMode.Clamp;
                outputImporter.mipmapEnabled = true;
                outputImporter.SaveAndReimport();
            }

            MakeTextureNotReadable(DamageColorPath);
            MakeTextureNotReadable(DamageOpacityPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(DamageCombinedPath);
        }

        private static void MakeTextureReadable(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        private static void MakeTextureNotReadable(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.isReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
