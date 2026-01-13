using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Holds character-related data for multiplayer functionality.
    /// </summary>
    public class FriendListItem : MonoBehaviour
    {
        public string FriendName { get; private set; }
        public string FriendId { get; private set; }
        public FriendPresence Presence { get; private set; }

        private void Start()
        {
            profileButton.onClick.AddListener(() =>
            {
                if (PlayerProfileUI.Instance != null)
                {
                    PlayerProfileUI.Instance.ShowFriendProfileUI(FriendId);
                }
            });

            moreButton.onClick.AddListener(() =>
            {
                FriendsUI.Instance?.ShowExpandOptionsPanel(FriendId);
            });
        }

        public void SetFriendData(Friend friend)
        {
            FriendName = friend.DisplayName;
            FriendId = friend.PlayerId;
            friendNameText.text = FriendName;
            profileImage.sprite = friend.Avatar != null ? friend.Avatar : null; // Set a default sprite if Avatar is null
            UpdatePresence(friend.Presence);
        }

        public void UpdatePresence(FriendPresence presence)
        {
            precenseText.text = presence.ToString();
            Presence = presence;
            switch (presence)
            {
                case FriendPresence.Online:
                    precenseIcon.color = Color.green;
                    precenseText.color = Color.green;
                    break;
                case FriendPresence.Away:
                    precenseText.color = Color.red;
                    break;
                case FriendPresence.Offline:
                    precenseText.color = Color.grey;
                    break;
                case FriendPresence.InLobby:
                    precenseIcon.color = Color.purple;
                    precenseText.color = Color.purple;
                    break;
                case FriendPresence.InGame:
                    precenseIcon.color = Color.cyan;
                    precenseText.color = Color.cyan;
                    break;
                default:
                    precenseIcon.color = Color.gray;
                    precenseText.color = Color.gray;
                    break;
            }
        }

        [SerializeField] private TMP_Text friendNameText;
        [SerializeField] private TMP_Text precenseText;
        [SerializeField] private Image profileImage;
        [SerializeField] private Button profileButton;
        [SerializeField] private Button moreButton;
        [SerializeField] private Image precenseIcon;
    }
}
