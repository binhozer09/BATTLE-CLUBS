using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using BattleClubs.World;

namespace BattleClubs.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkTeamRosterAuthority : MonoBehaviour
    {
        [SerializeField] private TeamSpawnManager spawnManager;
        private const int MaxPlayers = 20;
        private const int PlayersPerTeam = 4;

        private NetworkManager networkManager;
        private readonly bool[] occupiedSeats = new bool[MaxPlayers];
        private readonly Dictionary<ulong, int> clientSeats = new Dictionary<ulong, int>();
        private bool callbacksRegistered;

        public int AssignedPlayers => clientSeats.Count;
        public int AvailableSeats => MaxPlayers - clientSeats.Count;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            if (spawnManager == null) spawnManager = FindObjectOfType<TeamSpawnManager>();
        }

        private void OnEnable() => RegisterCallbacks();

        private void OnDisable()
        {
            UnregisterCallbacks();
            if (networkManager != null && networkManager.IsServer) ClearRoster();
        }

        private void RegisterCallbacks()
        {
            if (networkManager == null) networkManager = GetComponent<NetworkManager>();
            if (networkManager == null || callbacksRegistered) return;
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (networkManager == null || !callbacksRegistered) return;
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            callbacksRegistered = false;
        }

        private void OnServerStarted()
        {
            if (!networkManager.IsServer) return;
            foreach (ulong clientId in networkManager.ConnectedClientsIds)
                AssignConnectedClient(clientId);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (networkManager != null && networkManager.IsServer)
                AssignConnectedClient(clientId);
        }

        private void AssignConnectedClient(ulong clientId)
        {
            if (clientSeats.ContainsKey(clientId)) return;
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
                client.PlayerObject == null)
            {
                Debug.LogWarning("[BATTLE CLUBS] Player NetworkObject is not ready for team assignment.", this);
                return;
            }

            int seat = FindFreeSeat();
            if (seat < 0)
            {
                Debug.LogWarning("[BATTLE CLUBS] All 20 team seats are occupied.", this);
                return;
            }

            NetworkTeamAssignment assignment = client.PlayerObject.GetComponent<NetworkTeamAssignment>();
            if (assignment == null || !assignment.AssignTeamServer(seat))
            {
                Debug.LogError("[BATTLE CLUBS] Player Prefab needs NetworkTeamAssignment and Combatant.", client.PlayerObject);
                return;
            }

            occupiedSeats[seat] = true;
            clientSeats.Add(clientId, seat);
            int teamIndex = seat / PlayersPerTeam;
            int playerIndex = seat % PlayersPerTeam;

            if (spawnManager != null)
                spawnManager.TrySpawnPlayer(teamIndex, playerIndex, client.PlayerObject.transform);
            else
                Debug.LogWarning("[BATTLE CLUBS] TeamSpawnManager not found; player assigned without team spawn placement.", this);

            Debug.Log("[BATTLE CLUBS] Client " + clientId + " assigned to team-" +
                      (teamIndex + 1) + ", seat " + (playerIndex + 1) + ". " +
                      AssignedPlayers + "/" + MaxPlayers + " players.", this);
        }

        private int FindFreeSeat()
        {
            for (int i = 0; i < occupiedSeats.Length; i++)
                if (!occupiedSeats[i]) return i;
            return -1;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!clientSeats.TryGetValue(clientId, out int seat)) return;
            occupiedSeats[seat] = false;
            clientSeats.Remove(clientId);
            Debug.Log("[BATTLE CLUBS] Released team seat " + (seat + 1) +
                      " after client " + clientId + " disconnected.", this);
        }

        private void ClearRoster()
        {
            clientSeats.Clear();
            for (int i = 0; i < occupiedSeats.Length; i++) occupiedSeats[i] = false;
        }
    }
}
