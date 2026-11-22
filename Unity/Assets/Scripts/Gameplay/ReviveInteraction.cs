using UnityEngine;

namespace BattleClubs.Gameplay
{
    /// <summary>
    /// Local prototype for a teammate holding an interaction to revive a downed player.
    /// A network authority/server must validate and replicate this action in multiplayer.
    /// </summary>
    public sealed class ReviveInteraction : MonoBehaviour
    {
        [SerializeField] private Transform reviver;
        [SerializeField] private float reviveRange = 2.5f;
        [SerializeField, Min(0.5f)] private float reviveDuration = 4f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private Combatant target;
        private float progress;
        private bool holding;

        public float Progress => reviveDuration <= 0f ? 0f : Mathf.Clamp01(progress / reviveDuration);
        public bool IsReviving => holding && target != null;

        private void Awake()
        {
            if (reviver == null) reviver = transform;
        }

        private void Update()
        {
            if (!holding) return;

            if (!IsValidTarget(target))
            {
                CancelRevive();
                return;
            }

            progress += Time.deltaTime;
            if (progress >= reviveDuration)
            {
                target.Revive();
                CancelRevive();
            }
        }

        public bool BeginRevive()
        {
            if (holding) return target != null && IsValidTarget(target);

            Combatant reviverCombatant = GetComponent<Combatant>();
            if (reviverCombatant != null && reviverCombatant.State != PlayerLifeState.Alive)
                return false;

            Collider[] hits = Physics.OverlapSphere(reviver.position, reviveRange, targetLayers,
                QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            Combatant candidate = null;
            foreach (Collider hit in hits)
            {
                Combatant found = hit.GetComponentInParent<Combatant>();
                if (found == null || found == GetComponent<Combatant>() || found.State != PlayerLifeState.Downed)
                    continue;

                if (!AreEligibleTeammates(reviverCombatant, found)) continue;

                float distance = Vector3.Distance(reviver.position, found.transform.position);
                if (distance < nearest)
                {
                    nearest = distance;
                    candidate = found;
                }
            }

            if (candidate == null) return false;

            target = candidate;
            progress = 0f;
            holding = true;
            return true;
        }

        public void CancelRevive()
        {
            holding = false;
            progress = 0f;
            target = null;
        }

        private bool IsValidTarget(Combatant candidate)
        {
            Combatant reviverCombatant = GetComponent<Combatant>();
            return candidate != null &&
                   candidate.State == PlayerLifeState.Downed &&
                   (reviverCombatant == null || reviverCombatant.State == PlayerLifeState.Alive) &&
                   AreEligibleTeammates(reviverCombatant, candidate) &&
                   Vector3.Distance(reviver.position, candidate.transform.position) <= reviveRange;
        }

        private static bool AreEligibleTeammates(Combatant reviverCombatant, Combatant candidate)
        {
            // Training characters may interact when neither has a team.
            // If either character is assigned, both must be assigned to the same team.
            if (reviverCombatant == null || candidate == null) return false;

            bool reviverAssigned = reviverCombatant.HasTeamAssignment;
            bool candidateAssigned = candidate.HasTeamAssignment;
            if (!reviverAssigned && !candidateAssigned) return true;
            if (!reviverAssigned || !candidateAssigned) return false;

            return candidate.TeamId == reviverCombatant.TeamId;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = reviver != null ? reviver : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin.position, reviveRange);
        }
    }
}