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
    }
}
