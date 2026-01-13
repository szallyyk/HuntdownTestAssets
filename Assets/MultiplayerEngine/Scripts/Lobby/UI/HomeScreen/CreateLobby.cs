using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles the UI and logic for creating a new multiplayer lobby.
    /// </summary>
    public class CreateLobby : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Input field for entering the lobby name.")]
        [SerializeField] private TMP_InputField lobbyNameInputField;

        [Tooltip("Button to cycle through max player options.")]
        [SerializeField] private Button maxPlayers;

        [Tooltip("Button to cycle through available game modes.")]
        [SerializeField] private Button gameMode;

        [Tooltip("Button to create the lobby with the selected settings.")]
        [SerializeField] private Button createLobbyButton;

        [Tooltip("Button to return to the previous screen.")]
        [SerializeField] private Button backButton;

        [Tooltip("Button to toggle lobby visibility (Public/Private).")]
        [SerializeField] private Button visibility;

        // Cached references to the TMP_Text components for dynamic UI updates
        private TMP_Text maxPlayersText;
        private TMP_Text gameModeText;
        private TMP_Text visibilityText;

        private GameMode gameModeEnum = GameMode.FreeForAll;

        /// <summary>
        /// Initializes UI elements and sets up button listeners.
        /// </summary>
        private void Awake()
        {
            // Cache TMP_Text components for updating button labels
            maxPlayersText = maxPlayers.GetComponentInChildren<TMP_Text>();
            gameModeText = gameMode.GetComponentInChildren<TMP_Text>();
            visibilityText = visibility.GetComponentInChildren<TMP_Text>();

            // Set default values
            maxPlayersText.text = "5";
            gameModeText.text = "Deathmatch";
            visibilityText.text = "Public";

            // Cycle through max player options: 2, 3, 4, 5
            maxPlayers.onClick.AddListener(() =>
            {
                switch (maxPlayersText.text)
                {
                    case "2":
                        maxPlayersText.text = "3";
                        break;
                    case "3":
                        maxPlayersText.text = "4";
                        break;
                    case "4":
                        maxPlayersText.text = "5";
                        break;
                    default:
                        maxPlayersText.text = "2";
                        break;
                }
            });

            // Toggle lobby visibility between Public and Private
            visibility.onClick.AddListener(() =>
            {
                visibilityText.text = visibilityText.text == "Public" ? "Private" : "Public";
            });

            // Cycle through game modes
            gameMode.onClick.AddListener(() =>
            {
               switch (gameModeEnum)
               {
                   case GameMode.FreeForAll:
                       gameModeEnum = GameMode.TeamDeathmatch;
                       gameModeText.text = "Deathmatch";
                       break;
                   case GameMode.TeamDeathmatch:
                       gameModeEnum = GameMode.CaptureTheFlag;
                       gameModeText.text = "Capture The Flag";
                       break;
                   case GameMode.CaptureTheFlag:
                       gameModeEnum = GameMode.FreeForAll;
                       gameModeText.text = "Free For All";
                       break;
                }
            });

            // Handle lobby creation
            createLobbyButton.onClick.AddListener(async () =>
            {
                createLobbyButton.interactable = false;

                int maxPlayersValue = int.Parse(maxPlayersText.text);
                bool isPrivate = visibilityText.text == "Private";
                string lobbyName = lobbyNameInputField.text == string.Empty ? "new Lobby" : lobbyNameInputField.text;

                // Attempt to create the lobby
                await LobbyManager.Instance.CreateLobby(lobbyName, isPrivate, maxPlayersValue, gameModeEnum);

                Debug.Log("Lobby created successfully.");

                createLobbyButton.interactable = true;
            });

            // Handle back button to close the create lobby UI
            backButton.onClick.AddListener(() =>
            {
                HomeScreenUI.Instance.ShowMenu();
            });
        }
    }
}