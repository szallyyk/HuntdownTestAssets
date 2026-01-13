using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_SERVICES || STEAM_SERVICES
using System.Threading.Tasks;
using Unity.Netcode;
#if UNITY_SERVICES
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages multiplayer session lifecycle, including scene loading and player readiness.
    /// </summary>
    public class SessionManager : NetworkBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("Session Settings")]
        [Tooltip("Load scenes asynchronously.")]
        public bool loadAsync = false;

        [Tooltip("Maximum wait time for players to load (seconds).")]
        public int waitTime = 45;

        private int playersLoaded = 0;
        private int totalPlayers = 0;

        public CharacterData SelectedCharacter { get; private set; }

        public static event Action OnAllPlayersLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        private void OnEnable()
        {
            LobbyManager.OnLobbyUpdated += OnLobbyUpdated;
        }

        private void OnDisable()
        {
            LobbyManager.OnLobbyUpdated -= OnLobbyUpdated;
        }

        bool alreadyrun = false;

        private async void OnLobbyUpdated(LobbyData data)
        {
            if (alreadyrun) return;

            if (data == null)
            {
                Debug.LogError("LobbyData is null in OnLobbyUpdated.");
                return;
            }

            if (!data.Data.TryGetValue(LobbyDataKeys.GameId, out var gameId) || string.IsNullOrEmpty(gameId))
            {
                return;
            }

            alreadyrun = true;

            LoadingScreen.Instance?.ShowLoading();

            var localPlayer = data.Players.FirstOrDefault(p => p.IsLocalPlayer);
            if (localPlayer == null)
            {
                Debug.LogError("Local player not found in lobby.");
                return;
            }

            totalPlayers = data.Players.Count;

            localPlayer.Data.TryGetValue(PlayerDataKeys.PlayerCharacter, out var characterId);
            SelectedCharacter = !string.IsNullOrEmpty(characterId)
                ? PlayerProfileManager.Instance.CharacterData.Find(c => c.CharacterId == characterId)
                : PlayerProfileManager.Instance.CharacterData.FirstOrDefault();

            if (SelectedCharacter == null)
            {
                Debug.LogWarning("No character selected or available. Please select a character before starting the game.");
            }

#if UNITY_SERVICES
            if (data.HostId != PlayerProfileManager.Instance.LocalPlayerStats.PlayerId)
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(gameId);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
            }
#elif STEAM_SERVICES
            NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>().ConnectToSteamID = ulong.Parse(gameId);
#endif

            if (data.HostId == PlayerProfileManager.Instance.LocalPlayerStats.PlayerId)
            {
                NetworkManager.Singleton.StartHost();
                Debug.Log("Starting host... + ID :" + gameId);
                await WaitForPlayersToLoad();
            }
            else
            {
                await Task.Delay(2000); // Small delay to ensure transport is ready
                Debug.Log("Starting client... + ID :" + gameId);
                NetworkManager.Singleton.StartClient();
            }
        }

        private async Task WaitForPlayersToLoad()
        {
            var timeout = TimeSpan.FromSeconds(waitTime);
            var startTime = DateTime.UtcNow;

            while (playersLoaded < totalPlayers && (DateTime.UtcNow - startTime) < timeout)
            {
                await Task.Delay(1000);

                playersLoaded = NetworkManager.Singleton.ConnectedClientsList.Count;

                Debug.Log($"Waiting for all players to load... {playersLoaded}/{totalPlayers} loaded.");
            }

            if (playersLoaded < totalPlayers)
            {
                Debug.LogWarning("Timeout reached. Not all players loaded, proceeding.");
            }
            else
            {
                Debug.Log("All players loaded.");
            }

            await Task.Delay(1000); // Ensure all clients are ready

            LoadNewScene("Game"); // Replace with dynamic scene selection if needed (e.g., from lobby data > GameMode)
        }

        private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            Debug.Log($"Scene : {sceneName}, loaded for {clientsCompleted.Count} clients.");

            if (loadAsync && IsServer)
            {
                playersLoaded = clientsCompleted.Count;
                totalPlayers = NetworkManager.ConnectedClients.Count;

                if (playersLoaded >= totalPlayers)
                {
                    HideLoadingRpc();
                }
            }
        }

        public void LoadNewScene(string sceneName)
        {
            if (!IsServer)
            {
                return;
            }

            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        [Rpc(SendTo.Everyone)]
        private void HideLoadingRpc()
        {
            OnAllPlayersLoaded?.Invoke();
            LoadingScreen.Instance?.HideLoading();

            playersLoaded = 0;
            totalPlayers = 0;
        }
    }
}
#endif