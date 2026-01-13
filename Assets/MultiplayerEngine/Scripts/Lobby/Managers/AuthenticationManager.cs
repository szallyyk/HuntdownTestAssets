using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages user authentication and delegates to the selected authentication provider.
    /// Handles sign-in, sign-out, sign-up, and profile completion checks.
    /// </summary>
    public class AuthenticationManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the AuthenticationManager.
        /// </summary>
        public static AuthenticationManager Instance { get; private set; }

        private IAuthentication authenticationProvider;

        /// <summary>
        /// Invoked when automatic sign-in completes.
        /// </summary>
        public static event System.Action<bool> OnAutoSignInCompleted;

        /// <summary>
        /// Invoked when sign-in completes.
        /// </summary>
        public static event System.Action<bool> OnSignInCompleted;

        /// <summary>
        /// Invoked when sign-out completes.
        /// </summary>
        public static event System.Action<bool> OnSignOutCompleted;

        /// <summary>
        /// Invoked when sign-up completes.
        /// </summary>
        public static event System.Action<bool> OnSignUpCompleted;

        /// <summary>
        /// Invoked when player profile completion is checked.
        /// </summary>
        public static event System.Action<bool> OnProfileSetupCompleted;

        private void Awake()
        {
            // Ensure a single instance persists across scenes
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

#if UNITY_SERVICES
            authenticationProvider = new UnityAuthentication();
#elif STEAM_SERVICES
            authenticationProvider = new SteamAuthentication();
#endif
        }

        private async void Start()
        {
            if (authenticationProvider == null)
            {
                Debug.LogError("Authentication provider not set.");
                return;
            }

            await authenticationProvider.InitializeAsync();

            // Attempt automatic sign-in if possible
            bool result = await authenticationProvider.AutoSignInAsync();
            OnAutoSignInCompleted?.Invoke(result);
        }

        /// <summary>
        /// Signs in a user with the provided credentials.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <param name="rememberMe">Whether to persist the session.</param>
        /// <returns>True if sign-in was successful; otherwise, false.</returns>
        public async Task<bool> SignInAsync(string email, string password, bool rememberMe)
        {
            if (authenticationProvider == null)
            {
                Debug.LogError("Authentication provider not set.");
                OnSignInCompleted?.Invoke(false);
                return false;
            }

            bool result = await authenticationProvider.SignInAsync(email, password, rememberMe);
            OnSignInCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns>True if sign-out was successful; otherwise, false.</returns>
        public async Task<bool> SignOutAsync()
        {
            if (authenticationProvider == null)
            {
                Debug.LogError("Authentication provider not set.");
                OnSignOutCompleted?.Invoke(false);
                return false;
            }

            bool result = await authenticationProvider.SignOutAsync();
            OnSignOutCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Registers a new user with the provided credentials.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <param name="rememberMe">Whether to persist the session.</param>
        /// <returns>True if sign-up was successful; otherwise, false.</returns>
        public async Task<bool> SignUpAsync(string email, string password, bool rememberMe)
        {
            if (authenticationProvider == null)
            {
                Debug.LogError("Authentication provider not set.");
                OnSignUpCompleted?.Invoke(false);
                return false;
            }

            bool result = await authenticationProvider.SignUpAsync(email, password, rememberMe);
            OnSignUpCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Checks if the player's profile is complete (DisplayName and Avatar are set).
        /// </summary>
        /// <returns>True if profile is complete; otherwise, false.</returns>
        public async Task<bool> CheckForPlayerProfile()
        {
            PlayerProfileManager profileManager = PlayerProfileManager.Instance;
            if (profileManager == null)
            {
                Debug.LogError("PlayerProfileManager not found.");
                OnProfileSetupCompleted?.Invoke(false);
                return false;
            }

            var stats = await profileManager.GetLocalPlayerStatsAsync(null);

            if (stats == null)
            {
                Debug.LogError("Player stats not found.");
                OnProfileSetupCompleted?.Invoke(false);
                return false;
            }

            bool isComplete = !string.IsNullOrEmpty(stats.DisplayName);
            OnProfileSetupCompleted?.Invoke(isComplete);
            return isComplete;
        }
    }
}