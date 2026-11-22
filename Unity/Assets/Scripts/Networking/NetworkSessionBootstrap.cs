using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BattleClubs.Networking
{
    /// <summary>
    /// Starts a direct NGO host, dedicated server, or client session.
    /// This is the transport/session foundation; matchmaking and authoritative
    /// match gameplay are implemented in subsequent networking layers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public sealed class NetworkSessionBootstrap : MonoBehaviour
    {
        [SerializeField, Min(1)] private int protocolVersion = 1;
        [SerializeField, Range(1, 20)] private int maxPlayers = 20;
        [SerializeField] private ushort port = 7777;

        private NetworkManager networkManager;
        private UnityTransport transport;
        private bool callbacksRegistered;

        public bool IsListening => networkManager != null && networkManager.IsListening;
        public int ConnectedPlayers => networkManager == null ? 0 : networkManager.ConnectedClientsIds.Count;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            ConfigureNetworkManager();
        }

        private void OnEnable()
        {
            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        public bool StartHost()
        {
            if (!CanStart()) return false;
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            return networkManager.StartHost();
        }

        public bool StartDedicatedServer()
        {
            if (!CanStart()) return false;
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            return networkManager.StartServer();
        }

        public bool StartClient(string serverAddress)
        {
            if (!CanStart() || string.IsNullOrWhiteSpace(serverAddress)) return false;

            transport.SetConnectionData(serverAddress.Trim(), port);
            networkManager.NetworkConfig.ConnectionData = BitConverter.GetBytes(protocolVersion);
            return networkManager.StartClient();
        }

        public void Shutdown()
        {
            if (networkManager != null && networkManager.IsListening)
                networkManager.Shutdown();
        }

        private bool CanStart()
        {
            if (networkManager == null || transport == null)
            {
                Debug.LogError("[BATTLE CLUBS] NetworkManager or UnityTransport is missing.", this);
                return false;
            }

            if (networkManager.IsListening)
            {
                Debug.LogWarning("[BATTLE CLUBS] A network session is already running.", this);
                return false;
            }

            ConfigureNetworkManager();
            return true;
        }

        private void ConfigureNetworkManager()
        {
            if (networkManager == null) networkManager = GetComponent<NetworkManager>();
            if (transport == null) transport = GetComponent<UnityTransport>();
            if (networkManager == null || transport == null) return;

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = ApproveConnection;
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            bool hasRoom = networkManager.ConnectedClientsIds.Count < Mathf.Clamp(maxPlayers, 1, 20);
            bool validProtocol = request.Payload != null &&
                                 request.Payload.Length == sizeof(int) &&
                                 BitConverter.ToInt32(request.Payload, 0) == protocolVersion;

            response.Approved = hasRoom && validProtocol;
            response.CreatePlayerObject = response.Approved;
            response.Pending = false;
            response.Reason = !hasRoom
                ? "MATCH_FULL"
                : !validProtocol ? "PROTOCOL_VERSION_MISMATCH" : string.Empty;
        }

        private void RegisterCallbacks()
        {
            if (networkManager == null) networkManager = GetComponent<NetworkManager>();
            if (networkManager == null || callbacksRegistered) return;

            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (networkManager == null || !callbacksRegistered) return;

            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            callbacksRegistered = false;
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[BATTLE CLUBS] Client connected: {clientId}. Players: {ConnectedPlayers}/{maxPlayers}", this);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[BATTLE CLUBS] Client disconnected: {clientId}. Players: {ConnectedPlayers}/{maxPlayers}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxPlayers = Mathf.Clamp(maxPlayers, 1, 20);
            protocolVersion = Mathf.Max(1, protocolVersion);
        }
#endif
    }
}
