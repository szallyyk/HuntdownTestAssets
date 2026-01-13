using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles the main home screen UI logic, including menu navigation and lobby UI transitions.
    /// </summary>
    public class HomeScreenUI : MonoBehaviour
    {
        public static HomeScreenUI Instance { get; private set; }

        [Header("Main Menu Buttons")]
        [Tooltip("Button to open the play menu.")]
        [SerializeField] private Button play;

        [Tooltip("Button to open the character selection screen.")]
        [SerializeField] private Button charcterSelection;

        [Tooltip("Button to open the settings menu.")]
        [SerializeField] private Button settings;

        [Tooltip("Button to quit the application.")]
        [SerializeField] private Button quit;

        [Header("Lobby UI References")]
        [Tooltip("UI for creating a new lobby.")]
        [SerializeField] private CreateLobby createLobbyUI;

        [Tooltip("UI for joining an existing lobby.")]
        [SerializeField] private JoinLobby joinLobbyUI;

        [Tooltip("UI for viewing recent games.")]
        [SerializeField] private RecentGames recentGamesUI;

        [Header("Menu Navigation")]
        [Tooltip("RectTransform for the main menu panel.")]
        [SerializeField] private RectTransform menu;

        [Tooltip("Button to return to the previous menu.")]
        [SerializeField] private Button backButton;

        [Tooltip("Button to open the create lobby UI.")]
        [SerializeField] private Button createLobbyButton;

        [Tooltip("Button to open the join lobby UI.")]
        [SerializeField] private Button joinLobbyButton;

        [Tooltip("Button to open the recent games UI.")]
        [SerializeField] private Button recentGamesButton;

        [Tooltip("add profile card to show player's profile")]
        [SerializeField] private PlayerProfileCard playerProfileCard;

        /// <summary>
        /// Initializes the home screen UI and sets up button listeners.
        /// Should be called after user authentication.
        /// </summary>
        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            AuthenticationManager.OnProfileSetupCompleted += OnSignInCompleted;

            play.onClick.AddListener(() =>
            {
                menu.gameObject.SetActive(true);
                createLobbyUI.gameObject.SetActive(false);
                joinLobbyUI.gameObject.SetActive(false);
                recentGamesUI.gameObject.SetActive(false);
            });

            backButton.onClick.AddListener(() =>
            {
                menu.gameObject.SetActive(false);
                createLobbyUI.gameObject.SetActive(false);
                joinLobbyUI.gameObject.SetActive(false);
                recentGamesUI.gameObject.SetActive(false);
            });

            createLobbyButton.onClick.AddListener(() =>
            {
                createLobbyUI.gameObject.SetActive(true);
                joinLobbyUI.gameObject.SetActive(false);
                recentGamesUI.gameObject.SetActive(false);
                menu.gameObject.SetActive(false);
            });

            joinLobbyButton.onClick.AddListener(() =>
            {
                joinLobbyUI.gameObject.SetActive(true);
                createLobbyUI.gameObject.SetActive(false);
                recentGamesUI.gameObject.SetActive(false);
                menu.gameObject.SetActive(false);
            });

            recentGamesButton.onClick.AddListener(() =>
            {
                recentGamesUI.gameObject.SetActive(true);
                createLobbyUI.gameObject.SetActive(false);
                joinLobbyUI.gameObject.SetActive(false);
                menu.gameObject.SetActive(false);
            });

            quit.onClick.AddListener(() =>
            {
                Application.Quit();
            });

            playerProfileCard?.Initialize();
        }

        /// <summary>
        /// Callback for when user sign-in is completed.
        /// Shows or hides the home screen based on authentication success.
        /// </summary>
        /// <param name="success">True if sign-in succeeded, false otherwise.</param>
        private void OnSignInCompleted(bool success)
        {
            gameObject.SetActive(success);
        }

        public void ShowMenu()
        {
            menu.gameObject.SetActive(true);
            createLobbyUI.gameObject.SetActive(false);
            joinLobbyUI.gameObject.SetActive(false);
            recentGamesUI.gameObject.SetActive(false);
        }
    }
}