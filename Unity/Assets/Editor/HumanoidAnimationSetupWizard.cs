#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using BattleClubs.Player;
using BattleClubs.Gameplay;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Creates the BATTLE CLUBS Animator state-machine template and validates
    /// the selected humanoid hierarchy without inventing or bundling animation assets.
    /// </summary>
    public static class HumanoidAnimationSetupWizard
    {
        private const string ControllerFolder = "Assets/Animation";
        private const string ControllerPath = ControllerFolder + "/BC_Humanoid.controller";

        [MenuItem("BATTLE CLUBS/Characters/Create Humanoid Animator Template")]
        public static void CreateAnimatorTemplate()
        {
            EnsureFolder("Assets", "Animation");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            EnsureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Downed", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Eliminated", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindOrCreateState(machine, "Idle", new Vector3(260f, 80f, 0f));
            AnimatorState locomotion = FindOrCreateState(machine, "Locomotion", new Vector3(500f, 80f, 0f));
            AnimatorState airborne = FindOrCreateState(machine, "Jump_Fall", new Vector3(500f, 230f, 0f));
            AnimatorState attack = FindOrCreateState(machine, "Attack", new Vector3(760f, 80f, 0f));
            AnimatorState downed = FindOrCreateState(machine, "Downed", new Vector3(760f, 230f, 0f));
            AnimatorState eliminated = FindOrCreateState(machine, "Eliminated", new Vector3(1010f, 230f, 0f));

            machine.defaultState = idle;

            EnsureTransition(idle, locomotion, false, 0f,
                new ConditionSpec("MoveSpeed", AnimatorConditionMode.Greater, 0.1f));
            EnsureTransition(locomotion, idle, false, 0f,
                new ConditionSpec("MoveSpeed", AnimatorConditionMode.Less, 0.1f));

            EnsureTransition(idle, airborne, false, 0f,
                new ConditionSpec("Grounded", AnimatorConditionMode.IfNot, 0f));
            EnsureTransition(locomotion, airborne, false, 0f,
                new ConditionSpec("Grounded", AnimatorConditionMode.IfNot, 0f));
            EnsureTransition(airborne, locomotion, false, 0f,
                new ConditionSpec("Grounded", AnimatorConditionMode.If, 0f));

            EnsureAnyStateTransition(machine, attack, new ConditionSpec("Attack", AnimatorConditionMode.If, 0f));
            EnsureTransition(attack, locomotion, true, 0.85f);

            EnsureAnyStateTransition(machine, downed, new ConditionSpec("Downed", AnimatorConditionMode.If, 0f));
            EnsureTransition(downed, locomotion, false, 0f,
                new ConditionSpec("Downed", AnimatorConditionMode.IfNot, 0f));

            EnsureAnyStateTransition(machine, eliminated, new ConditionSpec("Eliminated", AnimatorConditionMode.If, 0f));

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Selection.activeObject = controller;

            EditorUtility.DisplayDialog("BATTLE CLUBS",
                "Animator Controller criado/atualizado em " + ControllerPath +
                "\n\nOs estados estão sem clipes de propósito. Atribua Idle, locomotion, jump/fall, attack, downed e eliminated usando animações licenciadas antes de testar.",
                "OK");
        }

        [MenuItem("BATTLE CLUBS/Characters/Validate Selected Humanoid")]
        public static void ValidateSelectedHumanoid()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS", "Selecione o objeto raiz do personagem ou o prefab na Hierarchy.", "OK");
                return;
            }

            Animator animator = selected.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS", "Nenhum Animator foi encontrado nos filhos do objeto selecionado.", "OK");
                return;
            }

            if (animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "O Animator foi encontrado, mas o Avatar não é Humanoid válido. No importador do FBX, abra Rig, selecione Humanoid e aplique/valide o Avatar.",
                    "OK");
                return;
            }

            GameObject root = selected;
            ThirdPersonMotor motor = root.GetComponent<ThirdPersonMotor>();
            Combatant combatant = root.GetComponent<Combatant>();
            if (motor == null || combatant == null)
            {
                string missing = (motor == null ? "ThirdPersonMotor " : "") +
                                 (combatant == null ? "Combatant" : "");
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "O Avatar Humanoid é válido, mas faltam componentes no objeto raiz: " + missing +
                    ". Coloque o Animator no filho visual e os componentes de gameplay no Player_Root.",
                    "OK");
                return;
            }

            HumanoidAnimationDriver driver = root.GetComponent<HumanoidAnimationDriver>();
            if (driver == null) driver = Undo.AddComponent<HumanoidAnimationDriver>(root);

            SerializedObject serialized = new SerializedObject(driver);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("motor").objectReferenceValue = motor;
            serialized.FindProperty("combatant").objectReferenceValue = combatant;
            serialized.ApplyModifiedProperties();
            animator.applyRootMotion = false;

            AnimatorController assignedController = animator.runtimeAnimatorController as AnimatorController;
            bool hasController = assignedController != null;
            string controllerStatus = hasController
                ? "Controller: " + assignedController.name
                : "ATENÇÃO: nenhum Animator Controller está atribuído.";

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(driver);
            if (PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.RecordPrefabInstancePropertyModifications(driver);

            EditorUtility.DisplayDialog("BATTLE CLUBS",
                "Validação estrutural aprovada. Avatar Humanoid válido e referências do driver configuradas.\n" +
                controllerStatus +
                "\n\nAinda é necessário atribuir clipes aos estados e testar a animação em Play Mode.",
                "OK");
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                if (parameter.name == name) return;
            controller.AddParameter(name, type);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string name, Vector3 position)
        {
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == name) return child.state;
            return machine.AddState(name, position);
        }

        private struct ConditionSpec
        {
            public string parameter;
            public AnimatorConditionMode mode;
            public float threshold;

            public ConditionSpec(string parameter, AnimatorConditionMode mode, float threshold)
            {
                this.parameter = parameter;
                this.mode = mode;
                this.threshold = threshold;
            }
        }

        private static void EnsureTransition(AnimatorState from, AnimatorState to,
            bool hasExitTime, float exitTime, params ConditionSpec[] conditions)
        {
            foreach (AnimatorStateTransition existing in from.transitions)
            {
                if (existing.destinationState != to) continue;
                ApplyTransition(existing, hasExitTime, exitTime, conditions);
                return;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            ApplyTransition(transition, hasExitTime, exitTime, conditions);
        }

        private static void EnsureAnyStateTransition(AnimatorStateMachine machine,
            AnimatorState to, params ConditionSpec[] conditions)
        {
            foreach (AnimatorStateTransition existing in machine.anyStateTransitions)
            {
                if (existing.destinationState != to) continue;
                ApplyTransition(existing, false, 0f, conditions);
                return;
            }

            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            ApplyTransition(transition, false, 0f, conditions);
        }

        private static void ApplyTransition(AnimatorStateTransition transition,
            bool hasExitTime, float exitTime, ConditionSpec[] conditions)
        {
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.conditions = new AnimatorCondition[0];
            foreach (ConditionSpec condition in conditions)
                transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
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
