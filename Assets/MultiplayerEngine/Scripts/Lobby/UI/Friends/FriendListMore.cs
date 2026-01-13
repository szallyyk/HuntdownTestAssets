using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Holds character-related data for multiplayer functionality.
    /// </summary>
    public class FriendListMore : MonoBehaviour
    {
        [SerializeField] private Button showProfile;
        [SerializeField] private Button inviteToGame;
        [SerializeField] private Button asktoJoin;
        [SerializeField] private Button removeFriend;

        private string friendId;

        public void Awake()
        {
            showProfile.onClick.AddListener(() =>
            {
                PlayerProfileUI.Instance.ShowFriendProfileUI(friendId);
            });

            inviteToGame.onClick.AddListener(async() =>
            {
                inviteToGame.interactable = false;
                await FriendsManager.Instance.InviteToGame(friendId);
                inviteToGame.interactable = true;
            });

            asktoJoin.onClick.AddListener(() =>
            {
                
            });

            removeFriend.onClick.AddListener(async() =>
            {
                removeFriend.interactable = false;
                bool sucess  = await FriendsManager.Instance.Unfriend(friendId);
                gameObject.SetActive(!sucess);
            });
        }

        public void Show(string friendId)
        {
            this.friendId = friendId;
            gameObject.SetActive(true);

            var lobbyAvailable = LobbyManager.Instance != null && LobbyManager.Instance.LobbyData != null;
            var friend = FriendsManager.Instance?.FriendsList.Find(f => f.PlayerId == friendId);

            // Invite to game: only if lobby is available and friend is not offline
            inviteToGame.interactable = lobbyAvailable && friend != null && friend.Presence != FriendPresence.Offline;

            // Ask to join: only if friend is in a lobby
            asktoJoin.interactable = friend != null && friend.Presence == FriendPresence.InLobby;
        }
    }
}
