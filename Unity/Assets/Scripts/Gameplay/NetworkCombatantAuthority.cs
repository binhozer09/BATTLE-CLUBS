using Unity.Netcode;
using UnityEngine;

namespace BattleClubs.Gameplay
{
    /// <summary>
    /// Server-authoritative combat bridge for a spawned player NetworkObject.
    /// Attach this beside NetworkObject and Combatant on the player prefab.
    /// The server validates attack timing, life state, range and team before
    /// applying a hit; life state and hit count are replicated to observers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Combatant))]
    public sealed class NetworkCombatantAuthority : NetworkBehaviour
    {
        [SerializeField] private Transform hitOrigin;
        [SerializeField, Min(0.1f)] private float hitRadius = 1.25f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 0.8f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private readonly NetworkVariable<int> replicatedHits =
            new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> replicatedLifeState =
            new NetworkVariable<int>((int)PlayerLifeState.Alive, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Combatant combatant;
        private float nextServerAttackTime;

        public int ReplicatedHits => replicatedHits.Value;
        public PlayerLifeState ReplicatedLifeState => (PlayerLifeState)replicatedLifeState.Value;

        private void Awake()
        {
            combatant = GetComponent<Combatant>();
            if (hitOrigin == null) hitOrigin = transform;
        }

        public override void OnNetworkSpawn()
        {
            if (combatant == null) combatant = GetComponent<Combatant>();
            replicatedHits.OnValueChanged += OnReplicatedStateChanged;
            replicatedLifeState.OnValueChanged += OnReplicatedStateChanged;

            if (IsServer)
                PublishCurrentState();
            else
                ApplyReplicatedState();
        }

        public override void OnNetworkDespawn()
        {
            replicatedHits.OnValueChanged -= OnReplicatedStateChanged;
            replicatedLifeState.OnValueChanged -= OnReplicatedStateChanged;
        }

        [ServerRpc]
        public void RequestAttackServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer || combatant == null || combatant.State != PlayerLifeState.Alive)
                return;
            if (Time.time < nextServerAttackTime)
                return;

            nextServerAttackTime = Time.time + attackCooldown;
            Combatant target = FindServerValidatedTarget();
            if (target == null) return;

            NetworkCombatantAuthority targetAuthority = target.GetComponent<NetworkCombatantAuthority>();
            if (targetAuthority == null || !targetAuthority.IsSpawned || !targetAuthority.IsServer)
                return;

            targetAuthority.ApplyServerHit();
        }

        /// <summary>Call only from server-owned match/team setup.</summary>
        public void ApplyServerElimination()
        {
            if (!IsServer || combatant == null) return;
            combatant.Eliminate();
            PublishCurrentState();
        }

        private Combatant FindServerValidatedTarget()
        {
            if (hitOrigin == null) return null;

            Collider[] overlaps = Physics.OverlapSphere(
                hitOrigin.position, hitRadius, targetLayers, QueryTriggerInteraction.Ignore);

            Combatant nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.transform.root == transform.root) continue;
                Combatant candidate = overlap.GetComponentInParent<Combatant>();
                if (candidate == null || candidate.State != PlayerLifeState.Alive) continue;

                if (combatant.HasTeamAssignment &&
                    (!candidate.HasTeamAssignment || candidate.TeamId == combatant.TeamId))
                    continue;

                NetworkObject targetObject = candidate.GetComponent<NetworkObject>();
                if (targetObject == null || !targetObject.IsSpawned) continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        private void ApplyServerHit()
        {
            if (!IsServer || combatant == null || combatant.State != PlayerLifeState.Alive) return;
            combatant.ReceiveBatHit();
            PublishCurrentState();
        }

        private void PublishCurrentState()
        {
            if (!IsServer || combatant == null) return;
            replicatedHits.Value = combatant.HitsReceived;
            replicatedLifeState.Value = (int)combatant.State;
            ApplyReplicatedState();
        }

        private void OnReplicatedStateChanged(int previous, int current)
        {
            ApplyReplicatedState();
        }

        private void ApplyReplicatedState()
        {
            if (combatant == null) combatant = GetComponent<Combatant>();
            if (combatant == null) return;

            int safeState = Mathf.Clamp(replicatedLifeState.Value,
                (int)PlayerLifeState.Alive, (int)PlayerLifeState.Eliminated);
            combatant.ApplyReplicatedState(replicatedHits.Value, (PlayerLifeState)safeState);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            hitRadius = Mathf.Max(0.1f, hitRadius);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
        }
#endif
    }
}
