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

        private float nextAttackTime;

        private void Awake()
        {
            if (cameraRig == null && Camera.main != null)
                cameraRig = Camera.main.GetComponent<ThirdPersonCameraRig>();
        }

        public void Attack()
        {
            if (Time.time < nextAttackTime || hitOrigin == null) return;
            nextAttackTime = Time.time + attackCooldown;

            // The attack button also advances the third-person camera smoothly around the player.
            if (cameraRig != null) cameraRig.AddAttackOrbit();

            Collider[] hits = Physics.OverlapSphere(hitOrigin.position, hitRadius, targetLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                if (hit.transform.root == transform.root) continue;
                Combatant target = hit.GetComponentInParent<Combatant>();
                if (target != null)
                {
                    target.ReceiveBatHit();
                    break;
                }
            }
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