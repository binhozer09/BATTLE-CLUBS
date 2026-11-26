#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BattleClubs.Player;
using BattleClubs.UI;
using BattleClubs.Gameplay;
using BattleClubs.Characters;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Creates a temporary source scene containing only the configured humanoid player.
    /// The medieval village builder transfers Player_Local into the final game scene.
    /// </summary>
    public static class BattleClubsFullSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BattleClubs_HumanoidSource.unity";

        [MenuItem("BATTLE CLUBS/Characters/Create Humanoid Source Scene")]
        public static void CreateHumanoidSourceScene()
        {
            EnsureFolder("Assets", "Scenes");
            string humanoidModelPath = HumanoidAssetBootstrap.EnsureHumanoidModelImported();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject localPlayer = CreatePlayer(new Vector3(4f, 1.0f, 4f), humanoidModelPath);
            BatAttack attack = localPlayer.AddComponent<BatAttack>();
            Transform hitOrigin = new GameObject("Bat_Hit_Origin").transform;
            hitOrigin.SetParent(localPlayer.transform, false);
            hitOrigin.localPosition = new Vector3(0f, 1.05f, 1.05f);
            SetObjectReference(attack, "hitOrigin", hitOrigin);
            localPlayer.AddComponent<BattleClubsMobileHUD>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BATTLE_CLUBS_MAIN_SCENE_CREATED=" + ScenePath);
        }

        private static GameObject CreatePlayer(Vector3 position, string humanoidModelPath)
        {
            GameObject root = new GameObject("Player_Local");
            root.transform.position = position;
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.95f, 0f);
            controller.minMoveDistance = 0f;
            controller.skinWidth = 0.04f;
            controller.stepOffset = 0.25f;
            controller.slopeLimit = 50f;
            controller.detectCollisions = true;
            root.AddComponent<PlayerGroundSafety>();
            root.AddComponent<PlayerGroundDiagnostics>();

            ThirdPersonMotor motor = root.AddComponent<ThirdPersonMotor>();
            Combatant combatant = root.AddComponent<Combatant>();

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(humanoidModelPath);
            if (modelAsset == null)
                throw new System.Exception("Modelo humanoide não pôde ser carregado: " + humanoidModelPath);

            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (visual == null) visual = Object.Instantiate(modelAsset);
            visual.name = "Visual_Humanoid";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
                throw new System.Exception("O personagem KinemIQ não possui Avatar Humanoid válido.");

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds modelBounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (!hasBounds) { modelBounds = renderer.bounds; hasBounds = true; }
                else modelBounds.Encapsulate(renderer.bounds);
            }
            if (hasBounds && modelBounds.size.y > 0.01f)
                visual.transform.localScale = Vector3.one * (1.8f / modelBounds.size.y);

            animator.applyRootMotion = false;
            HumanoidGroundAlignment alignment = visual.AddComponent<HumanoidGroundAlignment>();
            SerializedObject alignmentData = new SerializedObject(alignment);
            alignmentData.FindProperty("owner").objectReferenceValue = controller;
            alignmentData.ApplyModifiedPropertiesWithoutUndo();

            HumanoidAnimationDriver animationDriver = root.AddComponent<HumanoidAnimationDriver>();
            SerializedObject driverData = new SerializedObject(animationDriver);
            driverData.FindProperty("animator").objectReferenceValue = animator;
            driverData.FindProperty("motor").objectReferenceValue = motor;
            driverData.FindProperty("combatant").objectReferenceValue = combatant;
            driverData.ApplyModifiedPropertiesWithoutUndo();

            visual.AddComponent<ClubKitApplier>();

            GameObject bat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bat.name = "Bat_Visual";
            bat.transform.SetParent(root.transform, false);
            bat.transform.localPosition = new Vector3(0.42f, 1.0f, 0.32f);
            bat.transform.localRotation = Quaternion.Euler(0f, 0f, 68f);
            bat.transform.localScale = new Vector3(0.07f, 0.52f, 0.07f);
            Object.DestroyImmediate(bat.GetComponent<Collider>());
            SetColor(bat, new Color(0.36f, 0.22f, 0.12f));
            return root;
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
