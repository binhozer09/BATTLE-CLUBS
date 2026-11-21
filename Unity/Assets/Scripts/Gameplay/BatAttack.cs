using UnityEngine;

namespace BattleClubs.Gameplay
{
    public sealed class BatAttack : MonoBehaviour
    {
        [SerializeField] private Transform hitOrigin;
        [SerializeField] private float hitRadius = 1.25f;
        [SerializeField] private float attackCooldown = 0.8f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private float nextAttackTime;

        public void Attack()
        {
            if (Time.time < nextAttackTime || hitOrigin == null) return;
            nextAttackTime = Time.time + attackCooldown;

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
