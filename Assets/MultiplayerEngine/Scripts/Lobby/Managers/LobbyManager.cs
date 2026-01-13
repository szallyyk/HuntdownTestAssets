using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages multiplayer lobby functionalities such as creating, joining, leaving, and updating lobbies and player data.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        public static event Action<LobbyData> OnLobbyCreated;
        public static event Action<LobbyData> OnLobbyJoined;
        public static event Action OnLobbyLeft;
        public static event Action<LobbyData> OnLobbyUpdated;
        public static event Action<LobbyData> OnLobbyPlayerDataUpdated;
        public static event Action<bool> OnAllPlayersReady;
        public static event Action<LobbyData> OnGameStarted;

        public LobbyData LobbyData { get; private set; }

        private ILobby lobbyService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_SERVICES
            lobbyService = new UnityLobby();
#elif STEAM_SERVICES
            lobbyService = new SteamLobby();
#endif

            AuthenticationManager.OnProfileSetupCompleted += HandleProfileCompleted;
        }

        private void OnDestroy()
        {
            AuthenticationManager.OnProfileSetupCompleted -= HandleProfileCompleted;
            if (lobbyService != null)
            {
                lobbyService.LobbyCreated -= HandleLobbyCreated;
                lobbyService.LobbyJoined -= HandleLobbyJoined;
                lobbyService.LobbyLeft -= HandleLobbyLeft;
                lobbyService.LobbyUpdated -= HandleLobbyUpdated;
                lobbyService.LobbyPlayerDataUpdated -= OnLobbyPlayerDataUpdated;
                lobbyService.GameStarted -= OnGameStarted;
            }
        }

        private void HandleProfileCompleted(bool success)
        {
            _ = lobbyService?.InitializeAsync();
        }

        private void Start()
        {
            if (lobbyService == null)
            {
                Debug.LogError("No lobby service implementation found. Please ensure a lobby service is configured.");
                return;
            }

            lobbyService.LobbyCreated += HandleLobbyCreated;
            lobbyService.LobbyJoined += HandleLobbyJoined;
            lobbyService.LobbyLeft += HandleLobbyLeft;
            lobbyService.LobbyUpdated += HandleLobbyUpdated;
            lobbyService.LobbyPlayerDataUpdated += OnLobbyPlayerDataUpdated;
            lobbyService.GameStarted += OnGameStarted;
        }

        private void HandleLobbyCreated(LobbyData lobbyData)
        {
            OnLobbyCreated?.Invoke(lobbyData);
            SetPresenceBasedOnPrivacy(lobbyData);
        }

        private void HandleLobbyJoined(LobbyData lobbyData)
        {
            OnLobbyJoined?.Invoke(lobbyData);
            SetPresenceBasedOnPrivacy(lobbyData);
        }

        private void HandleLobbyLeft()
        {
            OnLobbyLeft?.Invoke();
            FriendsManager.Instance?.SetPresence(FriendPresence.Online);
        }

        private void HandleLobbyUpdated(LobbyData lobbyData)
        {
            OnLobbyUpdated?.Invoke(lobbyData);
            SetPresenceBasedOnPrivacy(lobbyData);
        }

        private async void SetPresenceBasedOnPrivacy(LobbyData lobbyData)
        {
            if (FriendsManager.Instance == null || lobbyData == null)
                return;
            await FriendsManager.Instance.SetPresence(lobbyData.IsPrivate ? FriendPresence.Online : FriendPresence.InLobby);
        }

        /// <summary>
        /// Creates a new lobby with the specified parameters.
        /// </summary>
        public async Task<LobbyData> CreateLobby(string lobbyName, bool isPrivate, int maxPlayers, GameMode gameMode)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var playerStats = PlayerProfileManager.Instance?.LocalPlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("Player profile not available.");
                return null;
            }

            var playerData = new Dictionary<PlayerDataKeys, string>
            {
                [PlayerDataKeys.PlayerName] = playerStats.DisplayName,
                [PlayerDataKeys.PlayerID] = playerStats.PlayerId,
                [PlayerDataKeys.PlayerCharacter] = string.Empty,
                [PlayerDataKeys.PlayerReady] = "true"
            };

            var lobbyData = new Dictionary<LobbyDataKeys, string>
            {
                [LobbyDataKeys.GameMode] = gameMode.ToString()
            };

            var lobby = await lobbyService.CreateLobbyAsync(lobbyName, isPrivate, maxPlayers, lobbyData, playerData);
            LobbyData = lobby;
            SetPresenceBasedOnPrivacy(lobby);
            return lobby;
        }

        /// <summary>
        /// Joins an existing lobby using a lobby ID.
        /// </summary>
        public async Task<LobbyData> JoinLobby(string lobbyId)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var playerStats = PlayerProfileManager.Instance?.LocalPlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("Player profile not available.");
                return null;
            }

            var playerData = new Dictionary<PlayerDataKeys, string>
            {
                [PlayerDataKeys.PlayerName] = playerStats.DisplayName,
                [PlayerDataKeys.PlayerID] = playerStats.PlayerId,
                [PlayerDataKeys.PlayerCharacter] = string.Empty,
                [PlayerDataKeys.PlayerReady] = "false"
            };

            var lobby = await lobbyService.JoinLobbyAsync(lobbyId, playerData);
            LobbyData = lobby;
            return lobby;
        }

        /// <summary>
        /// Joins an existing lobby using a join code.
        /// </summary>
        public async Task<LobbyData> JoinLobbyByCode(string joinCode)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var playerStats = PlayerProfileManager.Instance?.LocalPlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("Player profile not available.");
                return null;
            }

            var playerData = new Dictionary<PlayerDataKeys, string>
            {
                [PlayerDataKeys.PlayerName] = playerStats.DisplayName,
                [PlayerDataKeys.PlayerID] = playerStats.PlayerId,
                [PlayerDataKeys.PlayerCharacter] = string.Empty,
                [PlayerDataKeys.PlayerReady] = "false"
            };

            var lobby = await lobbyService.JoinLobbyByCodeAsync(joinCode, playerData);
            LobbyData = lobby;
            return lobby;
        }

        /// <summary>
        /// Leaves the currently joined lobby.
        /// </summary>
        public async Task<bool> LeaveLobby(LobbyData lobbyData = null)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return false;
            }

            var result = await lobbyService.LeaveLobbyAsync();

            if (FriendsManager.Instance != null)
                await FriendsManager.Instance.SetPresence(FriendPresence.Online);

            if (result)
                OnLobbyLeft?.Invoke();

            return result;
        }

        /// <summary>
        /// Updates the current player's data in the lobby.
        /// </summary>
        public async Task<LobbyData> UpdatePlayerData(IDictionary<PlayerDataKeys, string> playerData)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var lobby = await lobbyService.UpdatePlayerDataAsync(playerData);
            LobbyData = lobby;
            OnLobbyPlayerDataUpdated?.Invoke(lobby);
            return lobby;
        }

        /// <summary>
        /// Updates the privacy setting of the current lobby.
        /// </summary>
        public async Task<LobbyData> UpdateLobbyPrivacy(bool isPrivate)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var value = await lobbyService.UpdateLobbyPrivacyAsync(isPrivate);
            return value;
        }

        /// <summary>
        /// Updates the lobby's metadata.
        /// </summary>
        public async Task<LobbyData> UpdateLobbyData(IDictionary<LobbyDataKeys, string> lobbyData)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var lobby = await lobbyService.UpdateLobbyDataAsync(lobbyData);
            LobbyData = lobby;
            OnLobbyUpdated?.Invoke(lobby);
            return lobby;
        }

        /// <summary>
        /// Updates the game mode for the current lobby.
        /// </summary>
        public async Task<LobbyData> UpdateGameMode(GameMode gameMode)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }
            var lobby = await UpdateLobbyData(new Dictionary<LobbyDataKeys, string>
            {
                { LobbyDataKeys.GameMode, gameMode.ToString() }
            });
            LobbyData = lobby;
            return lobby;
        }

        /// <summary>
        /// Updates the current player's character selection in the lobby.
        /// </summary>
        public async Task<LobbyData> UpdateCharacter(string characterId)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }
            var lobby = await UpdatePlayerData(new Dictionary<PlayerDataKeys, string>
            {
                { PlayerDataKeys.PlayerCharacter, characterId }
            });

            LobbyData = lobby;

            return lobby;
        }

        /// <summary>
        /// Kicks a player from the lobby by their player ID.
        /// </summary>
        public async Task<LobbyData> KickPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return null;
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }

            var result = await lobbyService.KickPlayerAsync(playerId);

            LobbyData = result;
            return result;
        }

        /// <summary>
        /// Retrieves a list of available lobbies.
        /// </summary>
        public async Task<List<LobbyListData>> GetLobbyListAsync()
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }
            var lobbies = await lobbyService.GetLobbyListAsync();
            return lobbies;
        }

        /// <summary>
        /// Updates the current player's ready state in the lobby.
        /// </summary>
        public async Task<LobbyData> UpdateReadyState(string readyState)
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return null;
            }
            var lobby = await UpdatePlayerData(new Dictionary<PlayerDataKeys, string>
            {
                { PlayerDataKeys.PlayerReady, readyState }
            });

            LobbyData = lobby;

            return lobby;
        }

        /// <summary>
        /// Starts the game from the lobby.
        /// </summary>
        public async Task StartGameAsync()
        {
            if (lobbyService == null)
            {
                Debug.LogError("Lobby service not initialized.");
                return;
            }

            await lobbyService.StartGameAsync();
        }
    }

    /// <summary>
    /// Represents the data for a multiplayer lobby.
    /// </summary>
    public class LobbyData
    {
        public string LobbyId;
        public string JoinCode;
        public string LobbyName;
        public bool IsPrivate;
        public string HostId;
        public IDictionary<LobbyDataKeys, string> Data;
        public IList<PlayerData> Players;
    }

    /// <summary>
    /// Represents the data for a player in a lobby.
    /// </summary>
    public class PlayerData
    {
        public string PlayerId;
        public bool IsLobbyHost;
        public string PlayerName;
        public bool IsLocalPlayer;
        public IDictionary<PlayerDataKeys, string> Data;
    }

    /// <summary>
    /// Keys for lobby-level metadata stored in the lobby.
    /// </summary>
    public enum LobbyDataKeys
    {
        GameMode,
        GameId  // the ID use to connect to the game Host (In Unity : Relay allocation ID, in Steam : Host player ID)
    }

    /// <summary>
    /// Keys for player-specific metadata stored in the lobby.
    /// </summary>
    public enum PlayerDataKeys
    {
        PlayerName,
        PlayerCharacter,
        PlayerReady,
        PlayerID
    }

    /// <summary>
    /// Supported game modes for the lobby.
    /// </summary>
    public enum GameMode
    {
        FreeForAll,
        TeamDeathmatch,
        CaptureTheFlag
    }
}