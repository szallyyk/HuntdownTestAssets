using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages the player profile UI, including local and friend profiles.
    /// Handles display, editing, and custom stats presentation.
    /// </summary>
    public class PlayerProfileUI : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of PlayerProfileUI.
        /// </summary>
        public static PlayerProfileUI Instance { get; private set; }

        [Header("Profile UI References")]
        [SerializeField] private TMP_Text displayName;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Button editProfileButton;
        [SerializeField] private RectTransform editProfilePanel;
        [SerializeField] private Button backButton;

        [Header("Custom Stats UI")]
        [SerializeField] private Image playerTimeImage;
        [SerializeField] private TMP_Text playTimeText;
        [SerializeField] private Image extraDataImage;
        [SerializeField] private TMP_Text extraDataText;

        private static readonly List<string> CustomStatsKeys = new() { "playTime", "exampleData" };

        /// <summary>
        /// Initializes the profile UI, sets up singleton, button listeners, and panel state.
        /// </summary>
        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            backButton.onClick.AddListener(HideProfilePanel);
            editProfilePanel.gameObject.SetActive(false);
            editProfileButton.onClick.AddListener(ShowEditProfilePanel);
        }

        /// <summary>
        /// Displays the local player's profile UI and loads stats asynchronously.
        /// </summary>
        public async Task ShowPlayerProfileUI()
        {
            gameObject.SetActive(true);
            try
            {
                var stats = await PlayerProfileManager.Instance.GetLocalPlayerStatsAsync(CustomStatsKeys);
                UpdateProfileUI(stats, isLocalPlayer: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading local player profile: {ex}");
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Displays a friend's profile UI and loads stats asynchronously.
        /// </summary>
        /// <param name="friendId">The friend's player ID.</param>
        public async void ShowFriendProfileUI(string friendId)
        {
            gameObject.SetActive(true);
            try
            {
                var stats = await PlayerProfileManager.Instance.GetRemotePlayerStatsAsync(friendId, CustomStatsKeys);
                UpdateProfileUI(stats, isLocalPlayer: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading friend profile ({friendId}): {ex}");
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the profile UI elements with the provided stats.
        /// </summary>
        /// <param name="stats">Player stats to display.</param>
        /// <param name="isLocalPlayer">True if displaying local player, false for friend.</param>
        private void UpdateProfileUI(PlayerStats stats, bool isLocalPlayer)
        {
            if (stats == null)
            {
                displayName.text = "Unknown Player";
                avatarImage.sprite = null;
                playerTimeImage.gameObject.SetActive(false);
                extraDataImage.gameObject.SetActive(false);
                editProfileButton.gameObject.SetActive(false);
                return;
            }

            displayName.text = stats.DisplayName;
            avatarImage.sprite = stats.PlayerAvatar;

            if (stats.CustomStats != null)
            {
                if (stats.CustomStats.TryGetValue("playTime", out string playTime))
                {
                    playerTimeImage.gameObject.SetActive(true);
                    playTimeText.text = $"Play Time: {playTime} hrs";
                }
                else
                {
                    playerTimeImage.gameObject.SetActive(false);
                }

                if (stats.CustomStats.TryGetValue("exampleData", out string exampleData))
                {
                    extraDataImage.gameObject.SetActive(true);
                    extraDataText.text = $"Extra Data: {exampleData}";
                }
                else
                {
                    extraDataImage.gameObject.SetActive(false);
                }
            }
            else
            {
                playerTimeImage.gameObject.SetActive(false);
                extraDataImage.gameObject.SetActive(false);
            }

            editProfileButton.gameObject.SetActive(isLocalPlayer);
        }

        /// <summary>
        /// Hides the profile UI panel.
        /// </summary>
        private void HideProfilePanel()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the edit profile panel.
        /// </summary>
        private void ShowEditProfilePanel()
        {
            editProfilePanel.gameObject.SetActive(true);
        }
    }
}