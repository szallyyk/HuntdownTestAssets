using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages player profile data, including local and remote player statistics, icons, and profile updates.
    /// </summary>
    public class PlayerProfileManager : MonoBehaviour
    {
        // --- Fields ---
        [Header("Profile Icon and Character Data")]
        [SerializeField] private List<Sprite> profileIcons;
        [SerializeField] private List<CharacterData> characterData;

        private IPlayerProfile playerProfileService;

        // --- Singleton ---
        /// <summary>
        /// Singleton instance of the PlayerProfileManager.
        /// </summary>
        public static PlayerProfileManager Instance { get; private set; }

        // --- Properties ---
        /// <summary>
        /// List of available player icons.
        /// </summary>
        public List<Sprite> PlayerIcons { get; private set; }

        /// <summary>
        /// List of available character data.
        /// </summary>
        public List<CharacterData> CharacterData { get; private set; }

        /// <summary>
        /// The local player's statistics.
        /// </summary>
        public PlayerStats LocalPlayerStats { get; private set; }

        // --- Events ---
        /// <summary>
        /// Raised when the local player's profile is updated.
        /// </summary>
        public static event System.Action OnLocalPlayerUpdated;

        // --- Unity Lifecycle ---
        /// <summary>
        /// Initializes the singleton instance and sets up the player profile service.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_SERVICES
            playerProfileService = new UnityPlayerProfile();
#elif STEAM_SERVICES
            playerProfileService = new SteamPlayerProfile();
#endif

            PlayerIcons = profileIcons;
            CharacterData = characterData;
        }

        // --- Public Methods ---

        /// <summary>
        /// Retrieves player stats by player ID. If player ID is null or empty, retrieves local player stats.
        /// </summary>
        /// <param name="playerId">The player ID to retrieve stats for. If null, retrieves local player stats.</param>
        /// <param name="customStatRequest">Optional list of custom statistic keys to retrieve.</param>
        /// <returns>A <see cref="PlayerStats"/> instance containing the requested statistics.</returns>
        public async Task<PlayerStats> GetPlayerStatsAsync(string playerId = null, IList<string> customStatRequest = null)
        {
            if (playerProfileService == null)
            {
                Debug.LogError("Player profile service not initialized.");
                return null;
            }

            if (string.IsNullOrEmpty(playerId))
            {
                return await GetLocalPlayerStatsAsync(customStatRequest);
            }
            else
            {
                return await GetRemotePlayerStatsAsync(playerId, customStatRequest);
            }
        }

        /// <summary>
        /// Retrieves the local player's statistics.
        /// </summary>
        /// <param name="customStatRequest">Optional list of custom statistic keys to retrieve.</param>
        /// <returns>A <see cref="PlayerStats"/> instance containing the local player's statistics.</returns>
        public async Task<PlayerStats> GetLocalPlayerStatsAsync(IList<string> customStatRequest)
        {
            if (playerProfileService == null)
            {
                Debug.LogError("Player profile service not initialized.");
                return null;
            }

            var profile = await playerProfileService.GetPlayerDataAsync(customStatRequest);
            if (profile == null)
                return null;

            LocalPlayerStats = profile;
            OnLocalPlayerUpdated?.Invoke();
            return profile;
        }

        /// <summary>
        /// Retrieves statistics for a remote player.
        /// </summary>
        /// <param name="playerId">The unique identifier of the remote player.</param>
        /// <param name="customStatRequest">Optional list of custom statistic keys to retrieve.</param>
        /// <returns>A <see cref="PlayerStats"/> instance containing the remote player's statistics.</returns>
        public async Task<PlayerStats> GetRemotePlayerStatsAsync(string playerId, IList<string> customStatRequest = null)
        {
            if (playerProfileService == null)
            {
                Debug.LogError("Player profile service not initialized.");
                return null;
            }

            var profile = await playerProfileService.GetRemotePlayerDataAsync(playerId, customStatRequest);
            if (profile == null)
                return null;

            return profile;
        }

        /// <summary>
        /// Updates the local player's profile data.
        /// </summary>
        /// <param name="displayName">The new display name for the player.</param>
        /// <param name="avatarId">The identifier for the new avatar image.</param>
        /// <returns>True if the profile was updated successfully; otherwise, false.</returns>
        public async Task<(bool userName, bool avatar)> UpdatePlayerDataAsync(string displayName, string avatarId)
        {
            if (playerProfileService == null)
            {
                Debug.LogError("Player profile service not initialized.");
                return (false,false);
            }
            return await playerProfileService.UpdatePlayerProfileAsync(displayName, avatarId);
        }

        /// <summary>
        /// Retrieves a player icon by its identifier.
        /// </summary>
        /// <param name="iconId">The identifier of the icon.</param>
        /// <returns>The <see cref="Sprite"/> corresponding to the icon ID, or the first icon if not found.</returns>
        public Sprite GetIconById(string iconId)
        {
            if (PlayerIcons == null || PlayerIcons.Count == 0)
                return null;

            if (int.TryParse(iconId, out int index) && index >= 0 && index < PlayerIcons.Count)
            {
                return PlayerIcons[index];
            }
            return PlayerIcons[0];
        }
    }

    /// <summary>
    /// Represents player statistics, including basic information and a flexible dictionary for custom attributes.
    /// </summary>
    [System.Serializable]
    public class PlayerStats
    {
        /// <summary>
        /// The unique identifier for the player.
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// The display name of the player.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The avatar image of the player.
        /// </summary>
        public Sprite PlayerAvatar;

        /// <summary>
        /// Flexible attributes for any additional stats.
        /// </summary>
        public Dictionary<string, string> CustomStats = new Dictionary<string, string>();
    }
}