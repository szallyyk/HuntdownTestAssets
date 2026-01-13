using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles session data for multiplayer games, including player information and session management.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        public GameObject loadingScreenUI;

        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            loadingScreenUI?.SetActive(false);
        }
        /// <summary>
        /// Shows the loading screen.
        /// </summary>
        public void ShowLoading()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the loading screen.
        /// </summary>
        public void HideLoading()
        {
           gameObject.SetActive(false);
        }

    }
}
