using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages the authentication UI elements and user flow between sign-in, sign-up, and profile setup.
    /// </summary>
    public class AuthenticationUI : MonoBehaviour
    {
        [Header("Authentication UI References")]
        [Tooltip("Reference to the Sign Up UI component.")]
        [SerializeField] private SignUpUI signUp;

        [Tooltip("Reference to the Sign In UI component.")]
        [SerializeField] private SignInUI signIn;

        [Tooltip("Reference to the Set Player Profile UI component.")]
        [SerializeField] private SetPlayerProfileUI setPlayerProfileUI;

        [Tooltip("Loading icon displayed during authentication operations.")]
        [SerializeField] private RectTransform loadingIcon;

        [Tooltip("Error Screen will show if unknown error happens")]
        [SerializeField] private RectTransform errorPanel;

        /// <summary>
        /// Singleton instance for global access.
        /// </summary>
        public static AuthenticationUI Instance { get; private set; }

        /// <summary>
        /// Initializes the authentication UI and subscribes to authentication events.
        /// </summary>
        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Null checks for serialized fields
            if (signUp == null) Debug.LogWarning("SignUpUI reference is missing.");
            if (signIn == null) Debug.LogWarning("SignInUI reference is missing.");
            if (setPlayerProfileUI == null) Debug.LogWarning("SetPlayerProfileUI reference is missing.");
            if (loadingIcon == null) Debug.LogWarning("LoadingIcon reference is missing.");

            signIn?.gameObject.SetActive(false);
            signUp?.gameObject.SetActive(false);
            setPlayerProfileUI?.gameObject.SetActive(false);
            loadingIcon?.gameObject.SetActive(true);
            gameObject.SetActive(true);

            AuthenticationManager.OnAutoSignInCompleted += HandleAuthenticationResult;
            AuthenticationManager.OnSignInCompleted += HandleAuthenticationResult;
            AuthenticationManager.OnSignUpCompleted += HandleAuthenticationResult;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            AuthenticationManager.OnAutoSignInCompleted -= HandleAuthenticationResult;
            AuthenticationManager.OnSignInCompleted -= HandleAuthenticationResult;
            AuthenticationManager.OnSignUpCompleted -= HandleAuthenticationResult;
        }

        /// <summary>
        /// Handles feedback from sign-in or sign-up events and updates UI accordingly.
        /// </summary>
        /// <param name="isSuccess">Indicates if authentication was successful.</param>
        private async void HandleAuthenticationResult(bool isSuccess)
        {
            if (isSuccess)
            {
                try 
                {
                    await PlayerProfileCompleted();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error checking player profile: {ex.Message}");

                    signIn?.gameObject.SetActive(true);
                    loadingIcon?.gameObject.SetActive(false);
                }
            }
            else
            {
#if UNITY_SERVICES
                signIn?.gameObject.SetActive(true);
#elif STEAM_SERVICES
                errorPanel?.gameObject.SetActive(true);
#endif
                loadingIcon?.gameObject.SetActive(false);
            }
        }

        public async Task PlayerProfileCompleted()
        {
            bool isAvailable = await AuthenticationManager.Instance?.CheckForPlayerProfile();

            if (isAvailable)
            {
                gameObject.SetActive(false);
                loadingIcon?.gameObject.SetActive(false);
            }
            else
            {
#if UNITY_SERVICES
                setPlayerProfileUI?.gameObject.SetActive(true);
                loadingIcon?.gameObject.SetActive(false);
#elif STEAM_SERVICES
                gameObject.SetActive(false);
                loadingIcon?.gameObject.SetActive(false);
#endif
            }
        }

        /// <summary>
        /// Switches the UI to the sign-in screen.
        /// </summary>
        public void ChangeToSignIn()
        {
            signUp?.gameObject.SetActive(false);
            signIn?.gameObject.SetActive(true);
        }

        /// <summary>
        /// Switches the UI to the sign-up screen.
        /// </summary>
        public void ChangeToSignUp()
        {
            signIn?.gameObject.SetActive(false);
            signUp?.gameObject.SetActive(true);
        }
    }
}