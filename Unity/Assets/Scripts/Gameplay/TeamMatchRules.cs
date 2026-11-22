using System.Collections.Generic;
using UnityEngine;
using BattleClubs.Characters;
using BattleClubs.World;

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
                // A team is not eliminated until its complete four-player roster
                // exists and every member has reached Eliminated.
                if (players == null || players.Count != 4) return false;
                foreach (Combatant player in players)
                {
                    if (player == null || player.State != PlayerLifeState.Eliminated)
                        return false;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Match format defined by GAME_DESIGN.md: five teams with four players each.
    /// This is local match bookkeeping only; an online authority must own these rules.
    /// </summary>
    public sealed class TeamMatchRules : MonoBehaviour
    {
        [SerializeField] private List<MatchTeam> teams = new List<MatchTeam>();
        [SerializeField] private TeamSpawnManager spawnManager;
        [SerializeField, Min(1)] private int playersPerTeam = 4;
        [SerializeField] private int pointsPerEliminatedTeam = 3;

        private const int RequiredTeamCount = 5;

        private void Awake()
        {
            playersPerTeam = 4;
            if (spawnManager == null) spawnManager = GetComponent<TeamSpawnManager>();
            EnsureDefaultTeams();
        }

        private void EnsureDefaultTeams()
        {
            if (teams == null) teams = new List<MatchTeam>();
            while (teams.Count < RequiredTeamCount)
            {
                int number = teams.Count + 1;
                teams.Add(new MatchTeam
                {
                    teamId = "team-" + number,
                    clubId = string.Empty,
                    players = new List<Combatant>()
                });
            }

            // Do not silently delete Inspector data; gameplay APIs expose only the
            // five canonical teams and report the configured match capacity.
            for (int i = 0; i < RequiredTeamCount; i++)
            {
                if (teams[i] == null)
                    teams[i] = new MatchTeam();
                if (string.IsNullOrWhiteSpace(teams[i].teamId))
                    teams[i].teamId = "team-" + (i + 1);
                if (teams[i].players == null)
                    teams[i].players = new List<Combatant>();
            }
        }

        public IReadOnlyList<MatchTeam> Teams => teams;
        public int PlayersPerTeam => 4;
        public int TeamCount => teams == null ? 0 : Mathf.Min(teams.Count, RequiredTeamCount);
        public int MaxPlayers => TeamCount * PlayersPerTeam;

        public bool TryAddPlayer(string teamId, Combatant player)
        {
            if (player == null || teams == null) return false;

            for (int i = 0; i < TeamCount; i++)
            {
                MatchTeam existing = teams[i];
                if (existing != null && existing.players != null && existing.players.Contains(player))
                    return false;
            }

            MatchTeam team = null;
            for (int i = 0; i < TeamCount; i++)
            {
                if (teams[i] != null && teams[i].teamId == teamId)
                {
                    team = teams[i];
                    break;
                }
            }

            if (team == null) return false;
            if (team.players == null) team.players = new List<Combatant>();
            if (team.players.Count >= PlayersPerTeam) return false;

            team.players.Add(player);
            player.AssignTeam(team.teamId);

            int teamIndex = teams.IndexOf(team);
            if (spawnManager != null)
                spawnManager.TrySpawnPlayer(teamIndex, team.players.Count - 1, player.transform);
            ClubKitApplier kitApplier = player.GetComponentInChildren<ClubKitApplier>(true);
            if (kitApplier != null)
                kitApplier.ApplyKit(teamIndex, team.players.Count);

            return true;
        }

        public int CountActiveTeams()
        {
            int active = 0;
            for (int i = 0; i < TeamCount; i++)
                if (teams[i] != null && !teams[i].IsEliminated) active++;
            return active;
        }

        /// <summary>
        /// Awards points only for eliminated opposing teams. A team's own
        /// elimination must never increase its score.
        /// </summary>
        public int GetPointsForTeam(MatchTeam team)
        {
            if (team == null || team.IsEliminated) return 0;

            int eliminatedOpponents = 0;
            for (int i = 0; i < TeamCount; i++)
            {
                MatchTeam candidate = teams[i];
                if (candidate != null && candidate != team && candidate.IsEliminated)
                    eliminatedOpponents++;
            }

            return eliminatedOpponents * pointsPerEliminatedTeam;
        }

        /// <summary>
        /// Returns the sole surviving team's score, or zero while no unique
        /// winner exists. The winning team receives three points per defeated
        /// opposing team (four opponents in the canonical five-team match).
        /// </summary>
        public int GetWinnerPoints()
        {
            MatchTeam winner = GetSoleSurvivingTeam();
            return winner == null ? 0 : GetPointsForTeam(winner);
        }

        public MatchTeam GetSoleSurvivingTeam()
        {
            MatchTeam survivor = null;
            for (int i = 0; i < TeamCount; i++)
            {
                MatchTeam team = teams[i];
                if (team == null || team.IsEliminated) continue;
                if (survivor != null) return null;
                survivor = team;
            }
            return survivor;
        }
    }
}