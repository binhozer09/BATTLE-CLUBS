using System.Collections;
using UnityEngine;
using BattleClubs.Player;

namespace BattleClubs.Gameplay
{
    public sealed class BatAttack : MonoBehaviour
    {
        [SerializeField] private Transform hitOrigin;
        [SerializeField] private float hitRadius = 1.25f;
        [SerializeField] private float attackCooldown = 0.8f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private ThirdPersonCameraRig cameraRig;
        [Header("Attack animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private float turnToTargetSpeed = 900f;
        [SerializeField] private float fallbackImpactDelay = 0.35f;

        private float nextAttackTime;
        private int attackTriggerHash;
        private Combatant pendingTarget;
        private bool pendingHitApplied;
        private Coroutine fallbackRoutine;

        private void Awake()
        {
            if (cameraRig == null && Camera.main != null)
                cameraRig = Camera.main.GetComponent<ThirdPersonCameraRig>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            attackTriggerHash = Animator.StringToHash(attackTrigger);
        }

        public void Attack()
        {
            if (Time.time < nextAttackTime) return;
            nextAttackTime = Time.time + attackCooldown;

            pendingTarget = FindNearestTarget();
            pendingHitApplied = false;

            if (pendingTarget != null)
            {
                Vector3 direction = pendingTarget.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, Quaternion.LookRotation(direction),
                        turnToTargetSpeed * Time.deltaTime);
            }

            if (cameraRig != null) cameraRig.AddAttackOrbit();

            bool hasAnimation = animator != null && HasAttackTrigger();
            if (hasAnimation)
            {
                animator.SetTrigger(attackTriggerHash);
                // The animation should call ApplyAttackHit() at the contact frame.
                // This fallback prevents attacks from getting stuck if no Animation Event exists.
                if (fallbackRoutine != null) StopCoroutine(fallbackRoutine);
                fallbackRoutine = StartCoroutine(FallbackImpact());
            }
            else
            {
                ApplyAttackHit();
            }
        }

        // Add an Animation Event to the bat-contact frame and set its function to ApplyAttackHit.
        public void ApplyAttackHit()
        {
            if (pendingHitApplied) return;
            pendingHitApplied = true;

            if (fallbackRoutine != null)
            {
                StopCoroutine(fallbackRoutine);
                fallbackRoutine = null;
            }

            if (pendingTarget != null && pendingTarget.State == PlayerLifeState.Alive)
                pendingTarget.ReceiveBatHit();

            pendingTarget = null;
        }

        private IEnumerator FallbackImpact()
        {
            yield return new WaitForSeconds(fallbackImpactDelay);
            fallbackRoutine = null;
            ApplyAttackHit();
        }

        private Combatant FindNearestTarget()
        {
            if (hitOrigin == null) return null;

            Collider[] hits = Physics.OverlapSphere(
                hitOrigin.position, hitRadius, targetLayers, QueryTriggerInteraction.Ignore);

            Combatant nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Collider hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                Combatant candidate = hit.GetComponentInParent<Combatant>();
                if (candidate == null || candidate.State != PlayerLifeState.Alive) continue;
                Combatant attacker = GetComponent<Combatant>();
                if (attacker != null && attacker.HasTeamAssignment &&
                    candidate.TeamId == attacker.TeamId) continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        private bool HasAttackTrigger()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == attackTriggerHash &&
                    parameter.type == AnimatorControllerParameterType.Trigger)
                    return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (hitOrigin == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitOrigin.position, hitRadius);
        }
#endif
    }
}