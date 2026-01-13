#if UNITY_SERVICES
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Provides Unity Authentication integration for user sign-in, sign-up, and session management.
    /// </summary>
    public class UnityAuthentication : IAuthentication
    {
        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized.");
        }

        /// <inheritdoc/>
        public async Task<bool> AutoSignInAsync()
        {
            Debug.Log("Attempting automatic sign-in...");
            // Attempt automatic sign-in if a session token exists.
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("Auto sign-in successful.");
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Auto sign-in failed: " + ex);
                    return false;
                }
            }
            Debug.LogWarning("No session token found for auto sign-in.");
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> SignUpAsync(string email, string password, bool rememberMe)
        {
            if (!rememberMe)
            {
                AuthenticationService.Instance.ClearSessionToken();
            }

            try
            {
                Debug.Log("trying to sign In with " + email);
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(email, password);
                Debug.Log("Sign-up successful for: " + email);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Sign-up failed for " + email + ": " + ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SignInAsync(string email, string password, bool rememberMe)
        {
            if (!rememberMe)
            {
                AuthenticationService.Instance.ClearSessionToken();
            }

            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(email, password);
                Debug.Log("Sign-in successful for: " + email);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Sign-in failed for " + email + ": " + ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SignInWithCodeAsync(string authCode)
        {
            Debug.LogError("SignInWithCodeAsync is not implemented for Unity Authentication.");
            await Task.CompletedTask;
            return false;
        }

        /// <inheritdoc/>
        public Task<bool> SignOutAsync()
        {
            Debug.Log("Signing out and clearing session token...");
            AuthenticationService.Instance.ClearSessionToken();
            try
            {
                AuthenticationService.Instance.SignOut();
                Debug.Log("Sign-out successful.");
                return Task.FromResult(true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Sign-out failed: " + ex);
                return Task.FromResult(false);
            }
        }
    }
}
#endif