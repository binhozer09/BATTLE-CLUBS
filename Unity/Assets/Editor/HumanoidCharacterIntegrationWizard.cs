#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleClubs.Characters;
using BattleClubs.Gameplay;
using BattleClubs.Player;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Replaces the generated capsule visual with a licensed Humanoid model selected
    /// from the Project window. Gameplay components remain on Player_Root.
    /// </summary>
    public static class HumanoidCharacterIntegrationWizard
    {
        private const string ControllerPath = "Assets/Animation/BC_Humanoid.controller";

        [MenuItem("BATTLE CLUBS/Characters/Integrate Selected Model Into Player Root")]
        public static void IntegrateSelectedModel()
        {
            GameObject playerRoot = null;
            GameObject modelAsset = null;

            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null) continue;

                if (selected.GetComponent<ThirdPersonMotor>() != null &&
                    selected.GetComponent<Combatant>() != null)
                    playerRoot = selected;

                if (AssetDatabase.Contains(selected) &&
                    selected.GetComponentInChildren<Animator>(true) != null)
                    modelAsset = selected;
            }

            if (playerRoot == null || modelAsset == null)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "Selecione dois objetos: o Player_Root da Hierarchy (com ThirdPersonMotor e Combatant) e o modelo humanoide FBX/prefab na janela Project.",
                    "OK");
                return;
            }

            Animator sourceAnimator = modelAsset.GetComponentInChildren<Animator>(true);
            if (sourceAnimator == null || sourceAnimator.avatar == null ||
                !sourceAnimator.avatar.isValid || !sourceAnimator.isHuman)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "O modelo selecionado não possui um Avatar Humanoid válido. No importador FBX, configure Rig > Animation Type = Humanoid e aplique as alterações.",
                    "OK");
                return;
            }

            if (EditorUtility.IsPersistent(playerRoot))
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "O Player_Root precisa ser uma instância na Hierarchy, não um asset direto da janela Project.",
                    "OK");
                return;
            }

            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, playerRoot.scene) as GameObject;
            if (visual == null)
            {
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "Não foi possível instanciar o modelo selecionado. Verifique se ele é um prefab/modelo FBX importado.",
                    "OK");
                return;
            }

            Undo.RegisterCreatedObjectUndo(visual, "Integrate BATTLE CLUBS Humanoid");
            visual.name = "Visual_Humanoid";
            visual.transform.SetParent(playerRoot.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
            {
                Undo.DestroyObjectImmediate(visual);
                EditorUtility.DisplayDialog("BATTLE CLUBS",
                    "O Avatar do modelo instanciado não passou na validação. Nenhuma alteração no visual provisório foi mantida.",
                    "OK");
                return;
            }

            if (controller != null) animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            HumanoidAnimationDriver driver = playerRoot.GetComponent<HumanoidAnimationDriver>();
            if (driver == null) driver = Undo.AddComponent<HumanoidAnimationDriver>(playerRoot);

            SerializedObject driverData = new SerializedObject(driver);
            driverData.FindProperty("animator").objectReferenceValue = animator;
            driverData.FindProperty("motor").objectReferenceValue = playerRoot.GetComponent<ThirdPersonMotor>();
            driverData.FindProperty("combatant").objectReferenceValue = playerRoot.GetComponent<Combatant>();
            driverData.ApplyModifiedProperties();

            BatAttack attack = playerRoot.GetComponent<BatAttack>();
            if (attack != null)
            {
                SerializedObject attackData = new SerializedObject(attack);
                SerializedProperty animatorProperty = attackData.FindProperty("animator");
                if (animatorProperty != null)
                {
                    animatorProperty.objectReferenceValue = animator;
                    attackData.ApplyModifiedProperties();
                }
            }

            ClubKitApplier kit = visual.GetComponent<ClubKitApplier>();
            if (kit == null) kit = Undo.AddComponent<ClubKitApplier>(visual);

            RemoveGeneratedPlaceholder(playerRoot.transform, "Player_Body_Visual");
            RemoveGeneratedPlaceholder(playerRoot.transform, "Player_Head_Visual");

            EditorUtility.SetDirty(playerRoot);
            EditorUtility.SetDirty(visual);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(driver);
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerRoot);
            EditorSceneManager.MarkSceneDirty(playerRoot.scene);
            Selection.activeGameObject = visual;

            string controllerMessage = controller != null
                ? "Animator Controller BC_Humanoid atribuído."
                : "ATENÇÃO: BC_Humanoid.controller não foi encontrado; crie-o pelo menu Characters.";

            EditorUtility.DisplayDialog("BATTLE CLUBS",
                "Modelo Humanoid integrado ao Player_Root.\n\n" +
                "• Visual provisório removido.\n" +
                "• Animator ligado ao HumanoidAnimationDriver.\n" +
                "• BatAttack ligado ao Animator.\n" +
                "• ClubKitApplier adicionado ao modelo.\n" +
                "• Root Motion desativado.\n\n" +
                controllerMessage +
                "\n\nSalve a cena e atribua clipes licenciados aos estados do Animator antes de testar.",
                "OK");
        }

        private static void RemoveGeneratedPlaceholder(Transform root, string objectName)
        {
            Transform child = root.Find(objectName);
            if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
        }

        [MenuItem("BATTLE CLUBS/Characters/Integrate Selected Model Into Player Root", true)]
        private static bool ValidateIntegrateSelectedModel()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length >= 2;
        }
    }
}
#endif
