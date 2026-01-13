using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages the lobby UI elements and interactions.
    /// Handles player list, game mode, privacy, and button events.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the LobbyUI.
        /// </summary>
        public static LobbyUI Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("Button to leave the lobby.")]
        [SerializeField] private Button leaveButton;

        [Tooltip("Button for the host to start the game.")]
        [SerializeField] private Button startGameButton;

        [Tooltip("Button for players to mark themselves as ready.")]
        [SerializeField] private Button readyGameButton;

        [Tooltip("Button to toggle lobby privacy between public and private.")]
        [SerializeField] private Button privateButton;

        [Tooltip("Button to change the game mode.")]
        [SerializeField] private Button gameModeButton;

        [Tooltip("Displays the lobby join code.")]
        [SerializeField] private TMP_Text joinCode;

        [Header("Lobby Components")]
        [Tooltip("Handles character selection in the lobby. Attach character selection panel if there is any character selection panels avalable in lobby, other than you can skip this")]
        [SerializeField] private LobbyCharacterSelection characterSelection;

        [Tooltip("Controls voice chat features in the lobby.")]
        [SerializeField] private VoiceControlUI voiceControlUI;

        [Tooltip("UI for displays and manages the lobby chat view.")]
        [SerializeField] private GeneralChatUI generalChatUI;

        [Tooltip("Prefab for displaying each player in the lobby.")]
        [SerializeField] private LobbyPlayerItem lobbyPlayerItemPrefab;

        [Tooltip("UI container for player items.")]
        [SerializeField]
        private RectTransform playerItemHolder;

        private bool isLobbyPrivate = true;
        private GameMode currentGameMode = GameMode.FreeForAll;

        private readonly Dictionary<string, LobbyPlayerItem> lobbyPlayers = new();

        private const string PrivateText = "Private";
        private const string PublicText = "Public";
        private const string JoinCodePrefix = "Join Code: ";

        /// <summary>
        /// Ensures singleton instance and destroys duplicate objects.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Initializes the lobby UI, validates references, sets up listeners, and subscribes to events.
        /// </summary>
        public void Initialize()
        {
            if (!ValidateUIReferences())
            {
                enabled = false;
                return;
            }

            SetupButtonListeners();
            SubscribeLobbyEvents();

            characterSelection?.Initialize();
            voiceControlUI?.Initialize();
            generalChatUI?.Initialize();
        }

        /// <summary>
        /// Validates that all required UI references are set in the inspector.
        /// </summary>
        /// <returns>True if all references are valid; otherwise, false.</returns>
        private bool ValidateUIReferences()
        {
            if (leaveButton == null || startGameButton == null || readyGameButton == null ||
                privateButton == null || gameModeButton == null || joinCode == null ||
                lobbyPlayerItemPrefab == null || playerItemHolder == null)
            {
                Debug.LogError("LobbyUI: One or more UI references are not set in the inspector.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets up button click listeners for lobby actions.
        /// </summary>
        private void SetupButtonListeners()
        {
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
            readyGameButton.onClick.AddListener(OnReadyGameButtonClicked);
            gameModeButton.onClick.AddListener(OnGameModeButtonClicked);
            privateButton.onClick.AddListener(OnPrivateButtonClicked);
        }

        /// <summary>
        /// Subscribes to lobby-related events for UI updates.
        /// </summary>
        private void SubscribeLobbyEvents()
        {
            LobbyManager.OnLobbyCreated += InitializeLobby;
            LobbyManager.OnLobbyJoined += InitializeLobby;
            LobbyManager.OnLobbyUpdated += UpdateLobbyData;
            LobbyManager.OnLobbyPlayerDataUpdated += UpdatePlayerData;
        }

        /// <summary>
        /// Unsubscribes from lobby-related events to prevent memory leaks.
        /// </summary>
        private void UnsubscribeLobbyEvents()
        {
            LobbyManager.OnLobbyCreated -= InitializeLobby;
            LobbyManager.OnLobbyJoined -= InitializeLobby;
            LobbyManager.OnLobbyUpdated -= UpdateLobbyData;
            LobbyManager.OnLobbyPlayerDataUpdated -= UpdatePlayerData;
        }

        /// <summary>
        /// Called when the object is destroyed; unsubscribes from events.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeLobbyEvents();
        }

        /// <summary>
        /// Handles the leave button click event and leaves the lobby.
        /// </summary>
        private async void OnLeaveButtonClicked()
        {
            leaveButton.interactable = false;

            if (voiceControlUI != null)
                VoiceManager.Instance?.LeaveVoiceChat();

            try
            {
                if (LobbyManager.Instance != null)
                {
                    await LobbyManager.Instance.LeaveLobby();
                }
                else
                {
                    Debug.LogError("LobbyManager.Instance is null.");
                }
                gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error leaving lobby: {ex}");
            }
            finally
            {
                leaveButton.interactable = true;
            }
        }

        /// <summary>
        /// Handles the start game button click event and starts the game.
        /// </summary>
        private async void OnStartGameButtonClicked()
        {
            startGameButton.enabled = false;
            try
            {
                if (LobbyManager.Instance != null)
                {
                    await LobbyManager.Instance.StartGameAsync();
                }
                else
                {
                    Debug.LogError("LobbyManager.Instance is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error starting game: {ex}");
            }
        }

        /// <summary>
        /// Handles the ready button click event and updates the player's ready state.
        /// </summary>
        private async void OnReadyGameButtonClicked()
        {
            readyGameButton.interactable = false;
            try
            {
                if (LobbyManager.Instance != null)
                {
                    await LobbyManager.Instance.UpdateReadyState("true");
                }
                else
                {
                    Debug.LogWarning("LobbyManager.Instance is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error updating player ready state: {ex}");
            }
        }

        /// <summary>
        /// Handles the privacy button click event and toggles lobby privacy.
        /// </summary>
        private async void OnPrivateButtonClicked()
        {
            privateButton.interactable = false;
            try
            {
                isLobbyPrivate = !isLobbyPrivate;
                UpdatePrivateButtonText();
                if (LobbyManager.Instance != null)
                {
                    await LobbyManager.Instance.UpdateLobbyPrivacy(isLobbyPrivate);
                }
                else
                {
                    Debug.LogWarning("LobbyManager.Instance is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error updating lobby privacy: {ex}");
            }
            finally
            {
                privateButton.interactable = true;
            }
        }

        /// <summary>
        /// Handles the game mode button click event and cycles through available game modes.
        /// </summary>
        private async void OnGameModeButtonClicked()
        {
            gameModeButton.interactable = false;
            try
            {
                currentGameMode = GetNextGameMode(currentGameMode);
                UpdateGameModeButtonText();
                if (LobbyManager.Instance != null)
                {
                    await LobbyManager.Instance.UpdateGameMode(currentGameMode);
                }
                else
                {
                    Debug.LogWarning("LobbyManager.Instance is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error updating game mode: {ex}");
            }
            finally
            {
                gameModeButton.interactable = true;
            }
        }

        /// <summary>
        /// Returns the next game mode in the cycle.
        /// </summary>
        /// <param name="mode">Current game mode.</param>
        /// <returns>Next game mode.</returns>
        private static GameMode GetNextGameMode(GameMode mode)
        {
            return mode switch
            {
                GameMode.TeamDeathmatch => GameMode.CaptureTheFlag,
                GameMode.CaptureTheFlag => GameMode.FreeForAll,
                GameMode.FreeForAll => GameMode.TeamDeathmatch,
                _ => GameMode.FreeForAll
            };
        }

        /// <summary>
        /// Initializes the lobby UI with the provided lobby data.
        /// </summary>
        /// <param name="lobbyData">Lobby data to initialize UI.</param>
        private void InitializeLobby(LobbyData lobbyData)
        {
            gameObject.SetActive(true);

            if (voiceControlUI != null)
                VoiceManager.Instance?.JoinVoiceChat(lobbyData.LobbyId);

            if (lobbyData == null)
            {
                Debug.LogWarning("InitializeLobby called with null lobbyData.");
                return;
            }

            ClearPlayerItems();
            lobbyPlayers.Clear();

            UpdateLobbyData(lobbyData);
            UpdatePlayerData(lobbyData);
        }

        /// <summary>
        /// Updates the player list UI based on the latest lobby data.
        /// Adds, updates, or removes player items as needed.
        /// </summary>
        /// <param name="data">Latest lobby data.</param>
        private void UpdatePlayerData(LobbyData data)
        {
            if (data?.Players == null) return;

            foreach (var player in data.Players)
            {
                if (!lobbyPlayers.ContainsKey(player.PlayerId))
                {
                    var newPlayerItem = Instantiate(lobbyPlayerItemPrefab, playerItemHolder);
                    newPlayerItem.UpdatePlayer(player, data);
                    lobbyPlayers.Add(player.PlayerId, newPlayerItem);
                }
                else
                {
                    lobbyPlayers[player.PlayerId].UpdatePlayer(player, data);
                }
            }

            var playerIdsToRemove = lobbyPlayers.Keys
                .Where(id => !data.Players.Any(p => p.PlayerId == id))
                .ToList();

            foreach (var playerId in playerIdsToRemove)
            {
                Destroy(lobbyPlayers[playerId].gameObject);
                lobbyPlayers.Remove(playerId);
            }

            CheckAllPlayersReady(data);
        }

        /// <summary>
        /// Updates the lobby UI elements (join code, privacy, game mode, button states) based on lobby data.
        /// </summary>
        /// <param name="lobbyData">Latest lobby data.</param>
        private void UpdateLobbyData(LobbyData lobbyData)
        {
            if (lobbyData == null) return;

            joinCode.text = $"{JoinCodePrefix}{lobbyData.JoinCode}";

            isLobbyPrivate = lobbyData.IsPrivate;
            currentGameMode = ParseGameModeFromLobbyData(lobbyData);

            UpdatePrivateButtonText();
            UpdateGameModeButtonText();

            if(lobbyData.HostId == PlayerProfileManager.Instance.LocalPlayerStats.PlayerId)
                SetHostUIState();
            else
                SetPlayerUIState();
        }

        /// <summary>
        /// Checks if all non-host players are ready and enables the start game button if so.
        /// </summary>
        /// <param name="lobbyData">Lobby data to check readiness.</param>
        private void CheckAllPlayersReady(LobbyData lobbyData)
        {
            if (lobbyData == null || PlayerProfileManager.Instance == null) return;

            var localPlayerId = PlayerProfileManager.Instance.LocalPlayerStats.PlayerId;
            if (lobbyData.HostId == localPlayerId)
            {
                bool allReady = lobbyData.Players
                    .Where(p => p.PlayerId != lobbyData.HostId)
                    .All(p => p.Data.ContainsKey(PlayerDataKeys.PlayerReady) && p.Data[PlayerDataKeys.PlayerReady] == "true");

                startGameButton.interactable = allReady;
            }
        }

        /// <summary>
        /// Updates the privacy button text to reflect the current lobby privacy state.
        /// </summary>
        private void UpdatePrivateButtonText()
        {
            var textComponent = privateButton.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = isLobbyPrivate ? PrivateText : PublicText;
        }

        /// <summary>
        /// Updates the game mode button text to reflect the current game mode.
        /// </summary>
        private void UpdateGameModeButtonText()
        {
            var textComponent = gameModeButton.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = currentGameMode.ToString();
        }

        /// <summary>
        /// Removes all player items from the player item holder UI.
        /// </summary>
        private void ClearPlayerItems()
        {
            foreach (Transform child in playerItemHolder)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Parses the game mode from the lobby data dictionary.
        /// </summary>
        /// <param name="lobbyData">Lobby data containing game mode info.</param>
        /// <returns>Parsed GameMode value, or FreeForAll if parsing fails.</returns>
        private static GameMode ParseGameModeFromLobbyData(LobbyData lobbyData)
        {
            if (lobbyData.Data != null && lobbyData.Data.TryGetValue(LobbyDataKeys.GameMode, out var modeStr))
            {
                return Enum.TryParse(modeStr, out GameMode mode) ? mode : GameMode.FreeForAll;
            }
            return GameMode.FreeForAll;
        }

        /// <summary>
        /// Sets the UI state for the host (enables host controls).
        /// </summary>
        private void SetHostUIState()
        {
            gameModeButton.interactable = true;
            privateButton.interactable = true;
            readyGameButton.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Sets the UI state for non-host players (enables ready controls).
        /// </summary>
        private void SetPlayerUIState()
        {
            gameModeButton.interactable = false;
            privateButton.interactable = false;
            readyGameButton.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
        }
    }
}