#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
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
            map.AddComponent<UrbanCommunityBlockout>();
            map.AddComponent<CommunityArtDetails>();
            map.AddComponent<FavelaStreetLifeDetails>();
            map.AddComponent<TeamSpawnManager>();
            map.AddComponent<TeamMatchRules>();

            GameObject player = CreatePlayer(new Vector3(4f, -0.15f, 4f));
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
            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.42f;
            characterController.center = new Vector3(0f, 1f, 0f);
            root.AddComponent<ThirdPersonMotor>();
            root.AddComponent<Combatant>();
            root.AddComponent<HumanoidAnimationDriver>();

            GameObject mannequin = LoadHumanoidMannequin();
            GameObject visual = PrefabUtility.InstantiatePrefab(mannequin) as GameObject;
            if (visual == null)
                throw new System.Exception("Não foi possível instanciar o FBX humanoide KinemIQ.");
            visual.name = "Visual_Humanoid";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // FBX pivots are not guaranteed to sit at the soles of the feet.
            // Align the visible humanoid's lowest rendered point with the
            // CharacterController's bottom so the mesh cannot start below the floor.
            Renderer[] bodyRenderers = visual.GetComponentsInChildren<Renderer>(true);
            if (bodyRenderers.Length > 0)
            {
                Bounds bodyBounds = bodyRenderers[0].bounds;
                for (int i = 1; i < bodyRenderers.Length; i++)
                    bodyBounds.Encapsulate(bodyRenderers[i].bounds);
                float feetOffset = characterController.bounds.min.y - bodyBounds.min.y;
                visual.transform.position += Vector3.up * feetOffset;
            }

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
                throw new System.Exception("O FBX KinemIQ foi encontrado, mas não possui Avatar Humanoid válido.");
            animator.applyRootMotion = false;
            RuntimeAnimatorController controller = EnsureHumanoidController();
            animator.runtimeAnimatorController = controller;

            HumanoidAnimationDriver animationDriver = root.GetComponent<HumanoidAnimationDriver>();
            SerializedObject driverData = new SerializedObject(animationDriver);
            driverData.FindProperty("animator").objectReferenceValue = animator;
            driverData.FindProperty("motor").objectReferenceValue = root.GetComponent<ThirdPersonMotor>();
            driverData.FindProperty("combatant").objectReferenceValue = root.GetComponent<Combatant>();
            driverData.ApplyModifiedProperties();
            if (visual.GetComponent<BattleClubs.Characters.ClubKitApplier>() == null)
                visual.AddComponent<BattleClubs.Characters.ClubKitApplier>();

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

        private static RuntimeAnimatorController EnsureHumanoidController()
        {
            const string folder = "Assets/Animation";
            const string path = folder + "/BC_Humanoid.controller";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Animation");
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            EnsureAnimatorParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            EnsureAnimatorParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
            EnsureAnimatorParameter(controller, "Downed", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(controller, "Eliminated", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);

            AnimationClip idleClip = FindAnimationClip("idle", "stand");
            AnimationClip moveClip = FindAnimationClip("run", "walk", "locomotion");
            AnimationClip jumpClip = FindAnimationClip("jump", "fall");
            AnimationClip attackClip = FindAnimationClip("punch", "attack", "jab");
            AnimationClip downClip = FindAnimationClip("hit", "down", "fall");
            AnimationClip deathClip = FindAnimationClip("death", "die");

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindOrCreateAnimatorState(machine, "Idle", new Vector3(250, 80, 0), idleClip);
            AnimatorState locomotion = FindOrCreateAnimatorState(machine, "Locomotion", new Vector3(500, 80, 0), moveClip);
            AnimatorState airborne = FindOrCreateAnimatorState(machine, "Jump_Fall", new Vector3(500, 230, 0), jumpClip);
            AnimatorState attack = FindOrCreateAnimatorState(machine, "Attack", new Vector3(760, 80, 0), attackClip);
            AnimatorState downed = FindOrCreateAnimatorState(machine, "Downed", new Vector3(760, 230, 0), downClip);
            AnimatorState eliminated = FindOrCreateAnimatorState(machine, "Eliminated", new Vector3(1010, 230, 0), deathClip);
            machine.defaultState = idle;

            EnsureAnimatorTransition(idle, locomotion, "MoveSpeed", AnimatorConditionMode.Greater, 0.1f);
            EnsureAnimatorTransition(locomotion, idle, "MoveSpeed", AnimatorConditionMode.Less, 0.1f);
            EnsureAnimatorTransition(idle, airborne, "Grounded", AnimatorConditionMode.IfNot, 0f);
            EnsureAnimatorTransition(locomotion, airborne, "Grounded", AnimatorConditionMode.IfNot, 0f);
            EnsureAnimatorTransition(airborne, locomotion, "Grounded", AnimatorConditionMode.If, 0f);
            EnsureAnimatorTransition(machine, attack, "Attack", AnimatorConditionMode.If, 0f);
            // Any-State attack/downed transitions must have a route back out.
            // Without these, the generated player can remain stuck in the
            // attack or downed state even after the trigger/state is cleared.
            EnsureExitTimeTransition(attack, locomotion, 0.85f);
            EnsureAnimatorTransition(machine, downed, "Downed", AnimatorConditionMode.If, 0f);
            EnsureAnimatorTransition(downed, locomotion, "Downed", AnimatorConditionMode.IfNot, 0f);
            EnsureAnimatorTransition(machine, eliminated, "Eliminated", AnimatorConditionMode.If, 0f);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip FindAnimationClip(params string[] keywords)
        {
            string[] models = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/KinemIQ" });
            foreach (string guid in models)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    AnimationClip clip = asset as AnimationClip;
                    if (clip == null || clip.name.StartsWith("__preview__")) continue;
                    string name = clip.name.ToLowerInvariant();
                    foreach (string keyword in keywords)
                        if (name.Contains(keyword)) return clip;
                }
            }
            return null;
        }

        private static AnimatorState FindOrCreateAnimatorState(AnimatorStateMachine machine,
            string name, Vector3 position, Motion motion)
        {
            AnimatorState state = null;
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == name) { state = child.state; break; }
            if (state == null) state = machine.AddState(name, position);
            state.motion = motion;
            return state;
        }

        private static void EnsureAnimatorParameter(AnimatorController controller,
            string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                if (parameter.name == name) return;
            controller.AddParameter(name, type);
        }

        private static void EnsureAnimatorTransition(AnimatorState from, AnimatorState to,
            string parameter, AnimatorConditionMode mode, float threshold)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
                if (transition.destinationState == to) return;
            AnimatorStateTransition created = from.AddTransition(to);
            created.hasExitTime = false;
            created.duration = 0.08f;
            created.AddCondition(mode, threshold, parameter);
        }

        private static void EnsureAnimatorTransition(AnimatorStateMachine machine, AnimatorState to,
            string parameter, AnimatorConditionMode mode, float threshold)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
                if (transition.destinationState == to) return;
            AnimatorStateTransition created = machine.AddAnyStateTransition(to);
            created.hasExitTime = false;
            created.duration = 0.08f;
            created.AddCondition(mode, threshold, parameter);
        }

        private static void EnsureExitTimeTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
                if (transition.destinationState == to) return;

            AnimatorStateTransition created = from.AddTransition(to);
            created.hasExitTime = true;
            created.exitTime = Mathf.Clamp01(exitTime);
            created.duration = 0.08f;
        }

        private static GameObject LoadHumanoidMannequin()
        {
            string[] guids = AssetDatabase.FindAssets("KinemIQ_Mannequin t:Model",
                new[] { "Assets/ThirdParty/KinemIQ" });
            if (guids.Length == 0)
                throw new System.Exception("Modelo humanoide ausente. O pacote CC0 KinemIQ precisa ser baixado antes de gerar a cena; o build foi interrompido para não criar outro boneco de cápsula.");
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null) throw new System.Exception("FBX humanoide não pôde ser carregado: " + assetPath);
            return model;
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

        private static void EnsureFolder(string parent, string folder)
        {
            string path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
