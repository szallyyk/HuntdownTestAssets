using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages the UI for setting the player's profile after authentication.
    /// Handles avatar selection and display name input.
    /// </summary>
    public class EditPlayerProfileUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Displays the currently selected avatar.")]
        [SerializeField] private Image avatarImage;

        [Tooltip("Parent container for avatar icon buttons.")]
        [SerializeField] private RectTransform iconsHolder;

        [Tooltip("Input field for entering the display name.")]
        [SerializeField] private TMP_InputField displayNameInput;

        [Tooltip("Text field for displaying error messages.")]
        [SerializeField] private TMP_Text errorText;

        [Tooltip("Button to confirm and set the profile.")]
        [SerializeField] private Button setProfileButton;

        // Index of the currently selected avatar icon.
        private int selectedAvatarIndex = 0;

        private void Start()
        {
            // Validate profile manager and icons availability.
            if (PlayerProfileManager.Instance == null || PlayerProfileManager.Instance.PlayerIcons == null)
            {
                Debug.LogWarning("PlayerProfileManager or PlayerIcons not set.");
                setProfileButton.interactable = false;
                errorText.text = "Profile system unavailable.";
                return;
            }

            // Remove any existing avatar icon buttons.
            foreach (Transform child in iconsHolder)
            {
                Destroy(child.gameObject);
            }

            // Dynamically create avatar icon buttons.
            for (int i = 0; i < PlayerProfileManager.Instance.PlayerIcons.Count; i++)
            {
                Sprite sprite = PlayerProfileManager.Instance.PlayerIcons[i];
                GameObject iconButtonObj = new GameObject("IconButton", typeof(RectTransform), typeof(Button), typeof(Image));
                iconButtonObj.transform.SetParent(iconsHolder, false);

                Image iconImage = iconButtonObj.GetComponent<Image>();
                iconImage.sprite = sprite;

                Button iconButton = iconButtonObj.GetComponent<Button>();
                int index = i; // Capture index for closure
                iconButton.onClick.AddListener(() =>
                {
                    selectedAvatarIndex = index;
                    avatarImage.sprite = sprite;
                });
            }

            // Ensure only one listener is attached to the set profile button.
            setProfileButton.onClick.RemoveAllListeners();
            setProfileButton.onClick.AddListener(OnSetProfileClicked);
        }

        /// <summary>
        /// Handles the profile set button click event.
        /// Validates input and updates player data asynchronously.
        /// </summary>
        private async void OnSetProfileClicked()
        {
            setProfileButton.interactable = false;
            errorText.text = "";

            string displayName = displayNameInput.text?.Trim();
            if (string.IsNullOrEmpty(displayName) || displayName.Length < 3)
            {
                errorText.text = "Display name must be at least 3 characters.";
                setProfileButton.interactable = true;
                return;
            }

            try
            {
                var results = await PlayerProfileManager.Instance.UpdatePlayerDataAsync(displayName, selectedAvatarIndex.ToString());
                if (results.userName && results.avatar)
                {
                    errorText.text = "";
                    gameObject.SetActive(false);
                }
                else if(!results.userName)
                {
                    errorText.text = "Error setting userName. Please try again.";
                }
                else if(!results.avatar)
                {
                    errorText.text = "Error setting avatar. Please try again.";
                }
                else
                {
                    errorText.text = "Error setting profile. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Exception in UpdatePlayerDataAsync: {ex}");
                errorText.text = "Unexpected error. Please try again.";
            }
            finally
            {
                setProfileButton.interactable = true;
            }
        }
    }
}