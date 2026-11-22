using Unity.Netcode;
using UnityEngine;
using BattleClubs.Gameplay;

namespace BattleClubs.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Combatant))]
    public sealed class NetworkTeamAssignment : NetworkBehaviour
    {
        private readonly NetworkVariable<int> teamIndex = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private Combatant combatant;

        public int TeamIndex => teamIndex.Value;
        public int PlayerIndex => teamIndex.Value < 0 ? -1 : teamIndex.Value % 4;
        public string TeamId => teamIndex.Value < 0 ? string.Empty : "team-" + (teamIndex.Value / 4 + 1);

        public override void OnNetworkSpawn()
        {
            combatant = GetComponent<Combatant>();
            teamIndex.OnValueChanged += OnTeamIndexChanged;
            ApplyTeam(teamIndex.Value);
        }

        public override void OnNetworkDespawn()
        {
            teamIndex.OnValueChanged -= OnTeamIndexChanged;
        }

        public bool AssignTeamServer(int seat)
        {
            if (!IsServer || seat < 0 || seat >= 20 || !IsSpawned) return false;
            teamIndex.Value = seat;
            ApplyTeam(seat);
            return true;
        }

        private void OnTeamIndexChanged(int previous, int current) => ApplyTeam(current);

        private void ApplyTeam(int value)
        {
            if (combatant == null) combatant = GetComponent<Combatant>();
            if (combatant == null || value < 0 || value >= 20) return;
            combatant.AssignTeam("team-" + (value / 4 + 1));
        }
    }
}
