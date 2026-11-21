using System.Collections.Generic;
using UnityEngine;

namespace BattleClubs.Gameplay
{
    [System.Serializable]
    public sealed class MatchTeam
    {
        public string teamId;
        public string clubId;
        public List<Combatant> players = new List<Combatant>();
        public bool IsEliminated
        {
            get
            {
                if (players == null || players.Count == 0) return false;
                foreach (Combatant player in players)
                    if (player != null && player.State != PlayerLifeState.Eliminated) return false;
                return true;
            }
        }
    }

    public sealed class TeamMatchRules : MonoBehaviour
    {
        [SerializeField] private List<MatchTeam> teams = new List<MatchTeam>();
        [SerializeField] private int playersPerTeam = 4;
        [SerializeField] private int pointsPerEliminatedTeam = 3;

        public IReadOnlyList<MatchTeam> Teams => teams;

        public bool TryAddPlayer(string teamId, Combatant player)
        {
            if (player == null) return false;
            MatchTeam team = teams.Find(t => t.teamId == teamId);
            if (team == null || team.players.Count >= playersPerTeam || team.players.Contains(player))
                return false;
            team.players.Add(player);
            return true;
        }

        public int CountActiveTeams()
        {
            int active = 0;
            foreach (MatchTeam team in teams)
                if (team != null && !team.IsEliminated) active++;
            return active;
        }

        public int GetWinnerPoints()
        {
            int eliminated = 0;
            foreach (MatchTeam team in teams)
                if (team != null && team.IsEliminated) eliminated++;
            return eliminated * pointsPerEliminatedTeam;
        }

        public MatchTeam GetSoleSurvivingTeam()
        {
            MatchTeam survivor = null;
            foreach (MatchTeam team in teams)
            {
                if (team == null || team.IsEliminated) continue;
                if (survivor != null) return null;
                survivor = team;
            }
            return survivor;
        }
    }
}
