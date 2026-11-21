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

        private float nextAttackTime;
        private int attackTriggerHash;

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

            // Play the configured attack animation when an Animator Controller is assigned.
            if (animator != null && HasAttackTrigger())
                animator.SetTrigger(attackTriggerHash);

            // The attack button also advances the third-person camera smoothly around the player.
            if (cameraRig != null) cameraRig.AddAttackOrbit();

            Combatant nearestTarget = FindNearestTarget();
            if (nearestTarget == null) return;

            Vector3 direction = nearestTarget.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(direction), turnToTargetSpeed * Time.deltaTime);

            nearestTarget.ReceiveBatHit();
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