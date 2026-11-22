using UnityEngine;

namespace BattleClubs.Gameplay
{
    public enum PlayerLifeState
    {
        Alive,
        Downed,
        Eliminated
    }

    public sealed class Combatant : MonoBehaviour
    {
        [SerializeField, Min(1)] private int hitsToDown = 4;
        [SerializeField] private PlayerLifeState state = PlayerLifeState.Alive;

        public int HitsReceived { get; private set; }
        public PlayerLifeState State => state;
        public string TeamId { get; private set; } = string.Empty;
        public bool HasTeamAssignment => !string.IsNullOrWhiteSpace(TeamId);

        public void AssignTeam(string teamId)
        {
            TeamId = teamId == null ? string.Empty : teamId.Trim();
        }

        public void ReceiveBatHit()
        {
            if (state != PlayerLifeState.Alive) return;

            HitsReceived++;
            if (HitsReceived >= hitsToDown)
                state = PlayerLifeState.Downed;
        }

        public bool Revive()
        {
            if (state != PlayerLifeState.Downed) return false;

            state = PlayerLifeState.Alive;
            HitsReceived = 0;
            return true;
        }

        public void Eliminate()
        {
            state = PlayerLifeState.Eliminated;
        }

        /// <summary>Applies state received from the authoritative network player object.</summary>
        public void ApplyReplicatedState(int hitsReceived, PlayerLifeState replicatedState)
        {
            HitsReceived = Mathf.Max(0, hitsReceived);
            state = replicatedState;
        }
    }
}
