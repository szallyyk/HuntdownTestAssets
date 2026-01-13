using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles the sign-in UI logic, input validation, and user feedback for authentication.
    /// </summary>
    public class SignInUI : MonoBehaviour
    {
        [Header("Sign In Methods")]
        [Tooltip("Button to expand email/password sign-in fields.")]
        [SerializeField] private Button signInUsingEmail;
        [Tooltip("Button to sign in using Google.")]
        [SerializeField] private Button signInUsingGoogle;
        [Tooltip("Button to sign in using Apple.")]
        [SerializeField] private Button signInUsingApple;
        [Tooltip("Button to sign in using Steam.")]
        [SerializeField] private Button signInUsingSteam;

        [Header("Input Fields")]
        [Tooltip("Input field for the user's email address.")]
        [SerializeField] private TMP_InputField emailInput;
        [Tooltip("Input field for the user's password.")]
        [SerializeField] private TMP_InputField passwordInput;
        [Tooltip("Button to submit email/password sign-in.")]
        [SerializeField] private Button signInButton;

        [Header("Navigation")]
        [Tooltip("Button to switch to the sign-up UI.")]
        [SerializeField] private Button switchToSignUpButton;
        [Tooltip("Displays error messages to the user.")]
        [SerializeField] private TMP_Text errorText;

        private AuthenticationUI authenticationUI;
        private const float expandDelay = 0.05f;
        private bool isExpanded = false;
        private bool isSigningIn = false;

        private void Start()
        {
            authenticationUI = GetComponentInParent<AuthenticationUI>();

            // Register button listeners
            signInUsingEmail.onClick.AddListener(OnSignInClicked);
            signInButton.onClick.AddListener(SignInUsingEmail);
            switchToSignUpButton.onClick.AddListener(() => authenticationUI.ChangeToSignUp());

            // Hide input fields initially
            emailInput.gameObject.SetActive(false);
            passwordInput.gameObject.SetActive(false);
            signInButton.gameObject.SetActive(false);

            // Subscribe to sign-in feedback
            AuthenticationManager.OnSignInCompleted += OnSignInFeedback;

            // Validate inputs on change
            passwordInput.onValueChanged.AddListener(_ => ValidateInputs());
            emailInput.onValueChanged.AddListener(_ => ValidateInputs());

            errorText.gameObject.SetActive(false);
            signInButton.interactable = false;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events and listeners to prevent memory leaks
            AuthenticationManager.OnSignInCompleted -= OnSignInFeedback;
            passwordInput.onValueChanged.RemoveAllListeners();
            emailInput.onValueChanged.RemoveAllListeners();
            signInUsingEmail.onClick.RemoveListener(OnSignInClicked);
            signInButton.onClick.RemoveListener(SignInUsingEmail);
            switchToSignUpButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Handles feedback from the authentication manager after a sign-in attempt.
        /// </summary>
        /// <param name="success">True if sign-in succeeded, false otherwise.</param>
        private void OnSignInFeedback(bool success)
        {
            isSigningIn = false;
            signInButton.interactable = true;

            if (!success)
            {
                errorText.text = "Sign In Failed. Please check your credentials and try again.";
                errorText.gameObject.SetActive(true);
            }
            else
            {
                errorText.gameObject.SetActive(false);
                // Clear fields on success for security
                emailInput.text = string.Empty;
                passwordInput.text = string.Empty;
            }
        }

        /// <summary>
        /// Handles the click event for expanding/collapsing the email sign-in fields.
        /// </summary>
        private void OnSignInClicked()
        {
            signInUsingEmail.interactable = false;
            if (!isExpanded)
                StartCoroutine(ShowInputFields());
            else
                StartCoroutine(HideInputFields());
        }

        /// <summary>
        /// Animates and shows the email/password input fields.
        /// </summary>
        private IEnumerator ShowInputFields()
        {
            signInUsingGoogle.gameObject.SetActive(false);
            signInUsingApple.gameObject.SetActive(false);
            signInUsingSteam.gameObject.SetActive(false);
            emailInput.gameObject.SetActive(true);
            yield return new WaitForSeconds(expandDelay);
            passwordInput.gameObject.SetActive(true);
            yield return new WaitForSeconds(expandDelay);
            signInButton.gameObject.SetActive(true);
            isExpanded = true;
            signInUsingEmail.interactable = true;
        }

        /// <summary>
        /// Animates and hides the email/password input fields.
        /// </summary>
        private IEnumerator HideInputFields()
        {
            passwordInput.gameObject.SetActive(false);
            yield return new WaitForSeconds(expandDelay);
            emailInput.gameObject.SetActive(false);
            yield return new WaitForSeconds(expandDelay);
            signInButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(expandDelay);
            signInUsingSteam.gameObject.SetActive(true);
            signInUsingGoogle.gameObject.SetActive(true);
            signInUsingApple.gameObject.SetActive(true);
            isExpanded = false;
            signInUsingEmail.interactable = true;
            // Clear fields and errors for security and UX
            emailInput.text = string.Empty;
            passwordInput.text = string.Empty;
            errorText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Attempts to sign in using the provided email and password.
        /// </summary>
        private async void SignInUsingEmail()
        {
            if (isSigningIn)
                return;

            isSigningIn = true;
            signInButton.interactable = false;

            // Defensive: check for nulls
            if (AuthenticationManager.Instance == null)
            {
                errorText.text = "Authentication service unavailable.";
                errorText.gameObject.SetActive(true);
                isSigningIn = false;
                signInButton.interactable = true;
                return;
            }

            await AuthenticationManager.Instance.SignInAsync(emailInput.text.Trim(), passwordInput.text, false);
            signInButton.interactable = true;
        }

        /// <summary>
        /// Validates the email and password input fields and updates the UI accordingly.
        /// </summary>
        private void ValidateInputs()
        {
            string email = emailInput.text.Trim();
            string password = passwordInput.text;

            // Email validation
            if (string.IsNullOrEmpty(email))
            {
                signInButton.interactable = false;
                errorText.text = "Email is required.";
                errorText.gameObject.SetActive(true);
                return;
            }
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                signInButton.interactable = false;
                errorText.text = "Enter a valid email address.";
                errorText.gameObject.SetActive(true);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                signInButton.interactable = false;
                errorText.text = "Password is required.";
                errorText.gameObject.SetActive(true);
                return;
            }

            // Optionally: add password strength checks here

            errorText.gameObject.SetActive(false);
            signInButton.interactable = !isSigningIn;
        }
    }
}