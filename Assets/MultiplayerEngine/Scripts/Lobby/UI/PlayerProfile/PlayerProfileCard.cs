using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Holds character-related data for multiplayer functionality.
    /// </summary>
    public class PlayerProfileCard : MonoBehaviour
    {
        [SerializeField] private Image profilePicture;
        [SerializeField] private Image precenseImage;
        [SerializeField] private TMP_Text displayName;
        [SerializeField] private TMP_Text precense;
        [SerializeField] private Button hideShowPrecense;
        [SerializeField] private Button showLocalPlayerProfile;

        private FriendPresence currentPresence = FriendPresence.Online;

        public void Initialize()
        {
            PlayerProfileManager.OnLocalPlayerUpdated += UpdateLocalPlayerProfile;
            FriendsManager.OnLocalPlayerPresenceUpdated += OnLocalPlayerPresenceUpdated;
            if (hideShowPrecense != null)
            {
                hideShowPrecense.onClick.AddListener(async () =>
                {
                    hideShowPrecense.interactable = false;
                    await FriendsManager.Instance.SetPresence(currentPresence == FriendPresence.Online ? FriendPresence.Away : FriendPresence.Online);
                    hideShowPrecense.interactable = true;
                });
            }
            if(showLocalPlayerProfile != null)
            {
                showLocalPlayerProfile.onClick.AddListener(async() =>
                {
                    showLocalPlayerProfile.interactable = false;
                    await PlayerProfileUI.Instance.ShowPlayerProfileUI();
                    showLocalPlayerProfile.interactable = true;
                });
            }
        }

        private void OnLocalPlayerPresenceUpdated(FriendPresence presence)
        {
            if(presence == FriendPresence.Offline)
            {
                precenseImage.color = Color.gray;
                precense.color = Color.gray;
            }
            else if(presence == FriendPresence.Online)
            {
                precenseImage.color = Color.green;
                precense.color = Color.green;
            }
            else if(presence == FriendPresence.Away)
            {
                precenseImage.color = Color.red;
                precense.color = Color.red;
            }
            else if(presence == FriendPresence.InGame)
            {
                precenseImage.color = Color.cyan;
                precense.color = Color.cyan;
            }
            else if(presence == FriendPresence.InLobby)
            {
                precenseImage.color = Color.purple;
                precense.color = Color.purple;
            }
            precense.text = presence.ToString();
            currentPresence = presence;
        }

        private void UpdateLocalPlayerProfile()
        {
            PlayerStats stats = PlayerProfileManager.Instance.LocalPlayerStats;
            displayName.text = stats.DisplayName;
            profilePicture.sprite = stats.PlayerAvatar;
        }
    }
}
