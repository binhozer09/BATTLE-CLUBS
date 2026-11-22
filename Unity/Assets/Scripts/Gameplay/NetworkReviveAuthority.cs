using Unity.Netcode;
using UnityEngine;

namespace BattleClubs.Gameplay
{
    /// <summary>
    /// Server-authoritative teammate revive. Attach to the same player prefab
    /// as NetworkObject, Combatant and NetworkCombatantAuthority.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Combatant))]
    public sealed class NetworkReviveAuthority : NetworkBehaviour
    {
        [SerializeField] private Transform reviveOrigin;
        [SerializeField, Min(0.5f)] private float reviveRange = 2.5f;
        [SerializeField, Min(0.5f)] private float reviveDuration = 4f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private readonly NetworkVariable<float> replicatedProgress =
            new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> replicatedReviving =
            new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Combatant reviverCombatant;
        private NetworkCombatantAuthority targetAuthority;
        private float serverProgress;

        public float Progress => replicatedProgress.Value;
        public bool IsReviving => replicatedReviving.Value;

        private void Awake()
        {
            reviverCombatant = GetComponent<Combatant>();
            if (reviveOrigin == null) reviveOrigin = transform;
        }

        private void Update()
        {
            if (!IsServer || !replicatedReviving.Value) return;

            if (reviverCombatant == null || reviverCombatant.State != PlayerLifeState.Alive ||
                targetAuthority == null || !targetAuthority.IsSpawned ||
                targetAuthority.ReplicatedLifeState != PlayerLifeState.Downed ||
                !AreTeammates(reviverCombatant, targetAuthority.GetComponent<Combatant>()) ||
                Vector3.Distance(reviveOrigin.position, targetAuthority.transform.position) > reviveRange)
            {
                StopServerRevive();
                return;
            }

            serverProgress += Time.deltaTime;
            replicatedProgress.Value = Mathf.Clamp01(serverProgress / reviveDuration);

            if (serverProgress < reviveDuration) return;

            targetAuthority.ApplyServerRevive();
            StopServerRevive();
        }

        [ServerRpc]
        public void BeginReviveServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer || replicatedReviving.Value || reviverCombatant == null ||
                reviverCombatant.State != PlayerLifeState.Alive || reviveOrigin == null)
                return;

            Collider[] hits = Physics.OverlapSphere(reviveOrigin.position, reviveRange,
                targetLayers, QueryTriggerInteraction.Ignore);

            float nearestDistance = float.MaxValue;
            NetworkCombatantAuthority nearest = null;
            foreach (Collider hit in hits)
            {
                if (hit == null || hit.transform.root == transform.root) continue;
                Combatant candidate = hit.GetComponentInParent<Combatant>();
                if (candidate == null || candidate.State != PlayerLifeState.Downed ||
                    !AreTeammates(reviverCombatant, candidate))
                    continue;

                NetworkCombatantAuthority authority = candidate.GetComponent<NetworkCombatantAuthority>();
                if (authority == null || !authority.IsSpawned ||
                    authority.ReplicatedLifeState != PlayerLifeState.Downed)
                    continue;

                float distance = (candidate.transform.position - reviveOrigin.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = authority;
                }
            }

            if (nearest == null) return;
            targetAuthority = nearest;
            serverProgress = 0f;
            replicatedProgress.Value = 0f;
            replicatedReviving.Value = true;
        }

        [ServerRpc]
        public void CancelReviveServerRpc(ServerRpcParams rpcParams = default)
        {
            if (IsServer) StopServerRevive();
        }

        private void StopServerRevive()
        {
            serverProgress = 0f;
            targetAuthority = null;
            replicatedProgress.Value = 0f;
            replicatedReviving.Value = false;
        }

        private static bool AreTeammates(Combatant first, Combatant second)
        {
            return first != null && second != null &&
                   first.HasTeamAssignment && second.HasTeamAssignment &&
                   first.TeamId == second.TeamId;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            reviveRange = Mathf.Max(0.5f, reviveRange);
            reviveDuration = Mathf.Max(0.5f, reviveDuration);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = reviveOrigin != null ? reviveOrigin : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin.position, reviveRange);
        }
#endif
    }
}
