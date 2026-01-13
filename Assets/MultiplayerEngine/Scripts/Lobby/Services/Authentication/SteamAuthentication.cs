#if STEAM_SERVICES
using Steamworks;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Provides Steam-based authentication for multiplayer services.
    /// </summary>
    public class SteamAuthentication : IAuthentication
    {
        /// <inheritdoc/>
        public Task InitializeAsync()
        {
            Debug.Log("[SteamAuthentication] InitializeAsync called (no operation for Steam).");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> AutoSignInAsync()
        {
            Debug.Log("[SteamAuthentication] Attempting automatic Steam sign-in...");
            try
            {
                // SteamAPI.Init() is synchronous; wrap in Task.Run for async compatibility
                bool initialized = await Task.Run(() => SteamAPI.Init());
                if (!initialized)
                {
                    Debug.LogWarning("[SteamAuthentication] SteamAPI.Init() failed. Trying to launch Steam client...");

                    TryToOpenSteam();

                    Debug.Log("[SteamAuthentication] Waiting 10 seconds before retrying SteamAPI.Init()...");
                    await Task.Delay(10000); // Wait 10 seconds before retrying
                    initialized = await Task.Run(() => SteamAPI.Init());
                    if (!initialized)
                    {
                        Debug.LogWarning("[SteamAuthentication] SteamAPI.Init() failed after retry.");
                    }
                }
                else
                {
                    Debug.Log("[SteamAuthentication] SteamAPI.Init() succeeded.");
                }
                return initialized;
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogWarning("[SteamAuthentication] Could not load steam_api.dll: " + e);
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SteamAuthentication] Unexpected exception during AutoSignInAsync: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Attempts to launch the Steam client using protocol or default install path.
        /// </summary>
        private void TryToOpenSteam()
        {
            Debug.Log("[SteamAuthentication] Attempting to launch Steam client...");
            try
            {
                // Method 1: Use steam:// protocol (works if Windows has Steam installed correctly)
                System.Diagnostics.Process.Start("steam://open/main");
                Debug.Log("[SteamAuthentication] Launched Steam via protocol.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SteamAuthentication] Failed to launch Steam via protocol: " + ex);
            }

            // Method 2 (fallback): Launch steam.exe from default path
            const string steamPath = @"C:\Program Files (x86)\Steam\Steam.exe";
            if (File.Exists(steamPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(steamPath);
                    Debug.Log("[SteamAuthentication] Launched Steam via default path.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[SteamAuthentication] Failed to launch Steam via default path: " + ex);
                }
            }
            else
            {
                Debug.LogWarning("[SteamAuthentication] Steam.exe not found at default path.");
            }
        }

        /// <inheritdoc/>
        public Task<bool> SignInAsync(string email, string password, bool rememberMe)
        {
            Debug.LogWarning("[SteamAuthentication] SignInAsync called, but not supported for Steam. Returning true.");
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SignInWithCodeAsync(string authCode)
        {
            Debug.LogWarning("[SteamAuthentication] SignInWithCodeAsync is not implemented for Steam.");
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> SignOutAsync()
        {
            Debug.Log("[SteamAuthentication] SignOutAsync called (no operation for Steam).");
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SignUpAsync(string email, string password, bool rememberMe)
        {
            Debug.LogWarning("[SteamAuthentication] SignUpAsync called, but not supported for Steam. Returning true.");
            return Task.FromResult(true);
        }
    }
}
#endif