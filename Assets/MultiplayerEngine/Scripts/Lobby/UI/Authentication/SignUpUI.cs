using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles the sign-up UI logic, input validation, and user feedback for authentication.
    /// </summary>
    public class SignUpUI : MonoBehaviour
    {
        [Header("Sign Up Methods")]
        [Tooltip("Button to sign up using email and password.")]
        [SerializeField] private Button signUpUsingEmail;
        [Tooltip("Button to sign up using Google account.")]
        [SerializeField] private Button signUpUsingGoogle;
        [Tooltip("Button to sign up using Apple account.")]
        [SerializeField] private Button signUpUsingApple;
        [Tooltip("Button to sign up using Steam account.")]
        [SerializeField] private Button signUpUsingSteam;

        [Header("Input Fields")]
        [Tooltip("Input field for the user's email address.")]
        [SerializeField] private TMP_InputField emailInput;
        [Tooltip("Input field for the user's password.")]
        [SerializeField] private TMP_InputField passwordInput;
        [Tooltip("Input field to confirm the user's password.")]
        [SerializeField] private TMP_InputField confirmPasswordInput;
        [Tooltip("Button to submit the sign-up form.")]
        [SerializeField] private Button signUpButton;

        [Header("Navigation")]
        [Tooltip("Button to switch to the sign-in UI.")]
        [SerializeField] private Button switchToSignInButton;
        [Tooltip("Text field to display error messages.")]
        [SerializeField] private TMP_Text errorText;

        private AuthenticationUI authenticationUI;
        private const float ExpandDelay = 0.05f;
        private bool isExpanded = false;
        private Coroutine expandCoroutine;

        // Email must be in a valid format (simple regex).
        private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        // Password must be at least 6 chars, with upper, lower, and special char.
        private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).{6,}$";

        private void Awake()
        {
            // Ensure all serialized fields are assigned in the inspector.
            if (signUpUsingEmail == null || signUpUsingGoogle == null || signUpUsingApple == null ||
                signUpUsingSteam == null || emailInput == null || passwordInput == null ||
                confirmPasswordInput == null || signUpButton == null || switchToSignInButton == null ||
                errorText == null)
            {
                Debug.LogError("SignUpUI: One or more serialized fields are not assigned.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            authenticationUI = GetComponentInParent<AuthenticationUI>();
            if (authenticationUI == null)
            {
                Debug.LogError("SignUpUI: AuthenticationUI not found in parent.");
                enabled = false;
                return;
            }

            signUpUsingEmail.onClick.AddListener(OnSignUpClicked);
            signUpButton.onClick.AddListener(SignUpUsingEmail);
            switchToSignInButton.onClick.AddListener(() => authenticationUI.ChangeToSignIn());

            // Hide input fields and sign-up button initially.
            emailInput.gameObject.SetActive(false);
            passwordInput.gameObject.SetActive(false);
            confirmPasswordInput.gameObject.SetActive(false);
            signUpButton.gameObject.SetActive(false);

            AuthenticationManager.OnSignInCompleted += OnSignInFeedback;

            // Validate inputs on value change.
            passwordInput.onValueChanged.AddListener(_ => ValidateInputs());
            confirmPasswordInput.onValueChanged.AddListener(_ => ValidateInputs());
            emailInput.onValueChanged.AddListener(_ => ValidateInputs());

            errorText.gameObject.SetActive(false);
            signUpButton.interactable = false;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events and input listeners to prevent memory leaks.
            AuthenticationManager.OnSignInCompleted -= OnSignInFeedback;
            passwordInput.onValueChanged.RemoveAllListeners();
            confirmPasswordInput.onValueChanged.RemoveAllListeners();
            emailInput.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Handles feedback from the authentication manager after sign-up.
        /// </summary>
        /// <param name="success">True if sign-up succeeded, false otherwise.</param>
        private void OnSignInFeedback(bool success)
        {
            if (!success)
            {
                ShowError("Sign Up Failed. Please try again.");
            }
            else
            {
                errorText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Expands or collapses the email sign-up form.
        /// </summary>
        private void OnSignUpClicked()
        {
            signUpUsingEmail.interactable = false;
            if (expandCoroutine != null)
                StopCoroutine(expandCoroutine);

            expandCoroutine = StartCoroutine(isExpanded ? HideInputFields() : ShowInputFields());
        }

        /// <summary>
        /// Shows the email/password input fields with a short delay for UI effect.
        /// </summary>
        private IEnumerator ShowInputFields()
        {
            signUpUsingGoogle.gameObject.SetActive(false);
            signUpUsingApple.gameObject.SetActive(false);
            signUpUsingSteam.gameObject.SetActive(false);
            emailInput.gameObject.SetActive(true);
            yield return new WaitForSeconds(ExpandDelay);
            passwordInput.gameObject.SetActive(true);
            yield return new WaitForSeconds(ExpandDelay);
            confirmPasswordInput.gameObject.SetActive(true);
            yield return new WaitForSeconds(ExpandDelay);
            signUpButton.gameObject.SetActive(true);
            isExpanded = true;
            signUpUsingEmail.interactable = true;
        }

        /// <summary>
        /// Hides the email/password input fields with a short delay for UI effect.
        /// </summary>
        private IEnumerator HideInputFields()
        {
            confirmPasswordInput.gameObject.SetActive(false);
            yield return new WaitForSeconds(ExpandDelay);
            passwordInput.gameObject.SetActive(false);
            yield return new WaitForSeconds(ExpandDelay);
            emailInput.gameObject.SetActive(false);
            yield return new WaitForSeconds(ExpandDelay);
            signUpButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(ExpandDelay);
            signUpUsingSteam.gameObject.SetActive(true);
            signUpUsingGoogle.gameObject.SetActive(true);
            signUpUsingApple.gameObject.SetActive(true);
            isExpanded = false;
            signUpUsingEmail.interactable = true;
        }

        /// <summary>
        /// Attempts to sign up using the provided email and password.
        /// </summary>
        private async void SignUpUsingEmail()
        {
            signUpButton.interactable = false;
            try
            {
                await AuthenticationManager.Instance.SignUpAsync(emailInput.text.Trim(), passwordInput.text, false);
                signUpButton.interactable = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SignUpUI: Exception during sign up: {ex.Message}");
                ShowError("An unexpected error occurred. Please try again.");
                signUpButton.interactable = true;
            }
        }

        /// <summary>
        /// Validates user input and updates the UI accordingly.
        /// </summary>
        private void ValidateInputs()
        {
            string email = emailInput.text.Trim();
            string password = passwordInput.text;
            string confirmPassword = confirmPasswordInput.text;

            if (string.IsNullOrEmpty(email))
            {
                SetValidationState(false, "Email is required.");
                return;
            }
            if (!Regex.IsMatch(email, EmailPattern))
            {
                SetValidationState(false, "Enter a valid email address.");
                return;
            }

            // Only validate password fields if user is interacting with them or they are not empty.
            if (passwordInput.isFocused || confirmPasswordInput.isFocused || !string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword))
            {
                if (string.IsNullOrEmpty(password))
                {
                    SetValidationState(false, "Password is required.");
                    return;
                }
                if (!Regex.IsMatch(password, PasswordPattern))
                {
                    SetValidationState(false, "Password must be at least 6 characters, include uppercase, lowercase, and a special character.");
                    return;
                }
                if (string.IsNullOrEmpty(confirmPassword))
                {
                    SetValidationState(false, "Please confirm your password.");
                    return;
                }
                if (password != confirmPassword)
                {
                    SetValidationState(false, "Passwords do not match.");
                    return;
                }
            }
            else
            {
                errorText.gameObject.SetActive(false);
                signUpButton.interactable = false;
                return;
            }

            errorText.gameObject.SetActive(false);
            signUpButton.interactable = true;
        }

        /// <summary>
        /// Sets the validation state and displays an error message if needed.
        /// </summary>
        /// <param name="isValid">Whether the input is valid.</param>
        /// <param name="message">Error message to display if invalid.</param>
        private void SetValidationState(bool isValid, string message)
        {
            signUpButton.interactable = isValid;
            ShowError(message, !isValid);
        }

        /// <summary>
        /// Shows or hides the error message.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        /// <param name="show">Whether to show the error message.</param>
        private void ShowError(string message, bool show = true)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(show);
        }
    }
}