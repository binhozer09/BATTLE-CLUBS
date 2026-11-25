using System.Collections.Generic;
using UnityEngine;
using BattleClubs.Gameplay;

namespace BattleClubs.Player
{
    /// <summary>
    /// Drives a Humanoid Animator from the character motor and combat life state.
    /// The visual model must be a rigged Humanoid with an Animator component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HumanoidAnimationDriver : MonoBehaviour
    {
        [Header("Character references")]
        [SerializeField] private Animator animator;
        [SerializeField] private ThirdPersonMotor motor;
        [SerializeField] private Combatant combatant;

        [Header("Animator parameter names")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string groundedParameter = "Grounded";
        [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
        [SerializeField] private string downedParameter = "Downed";
        [SerializeField] private string eliminatedParameter = "Eliminated";

        [Header("Animation tuning")]
        [SerializeField, Min(0f)] private float speedDampTime = 0.12f;

        private readonly HashSet<int> floatParameters = new HashSet<int>();
        private readonly HashSet<int> boolParameters = new HashSet<int>();

        private int moveSpeedHash;
        private int groundedHash;
        private int verticalSpeedHash;
        private int downedHash;
        private int eliminatedHash;

        public Animator CharacterAnimator => animator;
        public bool HasHumanoidRig => animator != null && animator.avatar != null && animator.avatar.isValid && animator.isHuman;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (motor == null) motor = GetComponent<ThirdPersonMotor>();
            if (combatant == null) combatant = GetComponent<Combatant>();

            moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
            groundedHash = Animator.StringToHash(groundedParameter);
            verticalSpeedHash = Animator.StringToHash(verticalSpeedParameter);
            downedHash = Animator.StringToHash(downedParameter);
            eliminatedHash = Animator.StringToHash(eliminatedParameter);

            CacheAnimatorParameters();

            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;

            if (motor != null)
            {
                SetFloatIfAvailable(moveSpeedHash, motor.CurrentPlanarSpeed, speedDampTime);
                SetBoolIfAvailable(groundedHash, motor.IsGrounded);
                SetFloatIfAvailable(verticalSpeedHash, motor.VerticalSpeed, 0f);
            }

            if (combatant != null)
            {
                SetBoolIfAvailable(downedHash, combatant.State == PlayerLifeState.Downed);
                SetBoolIfAvailable(eliminatedHash, combatant.State == PlayerLifeState.Eliminated);
            }
        }

        private void CacheAnimatorParameters()
        {
            floatParameters.Clear();
            boolParameters.Clear();
            if (animator == null) return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float)
                    floatParameters.Add(parameter.nameHash);
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                    boolParameters.Add(parameter.nameHash);
            }
        }

        private void SetFloatIfAvailable(int hash, float value, float dampTime)
        {
            if (!floatParameters.Contains(hash)) return;
            if (dampTime > 0f)
                animator.SetFloat(hash, value, dampTime, Time.deltaTime);
            else
                animator.SetFloat(hash, value);
        }

        private void SetBoolIfAvailable(int hash, bool value)
        {
            if (boolParameters.Contains(hash))
                animator.SetBool(hash, value);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            speedDampTime = Mathf.Max(0f, speedDampTime);
        }
#endif
    }
}
