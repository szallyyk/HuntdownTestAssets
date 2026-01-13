using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Holds character-related data for multiplayer functionality.
    /// </summary>
    public class FriendsUI : MonoBehaviour
    {
        public static FriendsUI Instance { get; private set; }

        [SerializeField] private FriendListItem friendListItemPrefab;
        [SerializeField] private Transform friendListHolder;
        [SerializeField] private LobbyInviteItem lobbyInviteItemPrefab;
        [SerializeField] private Transform lobbyInviteHolder;
        [SerializeField] private FriendListMore friendListMoreItem;

        [SerializeField] private Button cancelSearch;
        [SerializeField] private Button SearchButton;
        [SerializeField] private TMP_InputField searchInputField;

        [SerializeField] private Button addFriend;
        [SerializeField] private RectTransform addFriendsPanel;

        [SerializeField] private Button toggleFriendsButton;
        [SerializeField] private UITransformMove friendsPanelMover;

        [SerializeField] private PlayerProfileCard playerProfile;

        private List<FriendListItem> friendListItems = new List<FriendListItem>();
        private List<LobbyInviteItem> lobbyInviteItems = new List<LobbyInviteItem>();
        private string expandedFriendId; // Add this as a private field in FriendsUI
        private Color defaultColor;

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

#if UNITY_SERVICES
            defaultColor = addFriend.image.color;
            addFriend.onClick.AddListener(() =>
            {
                addFriendsPanel.gameObject.SetActive(!addFriendsPanel.gameObject.activeSelf);
                addFriend.image.color = defaultColor;
            });
#elif STEAM_SERVICES
            addFriend.gameObject.SetActive(false);
#endif

            FriendsManager.OnFriendDataUpdated += FriendsManager_OnFriendDataUpdated;
            FriendsManager.OnFriendsListUpdated += FriendsManager_OnFriendsListUpdated;
            FriendsManager.OnLobbyInviteReceived += FriendsManager_OnLobbyInviteReceived;
            FriendsManager.OnFriendRequestReceived += FriendsManager_OnFriendRequestReceived;
            FriendsManager.OnFriendPresenceUpdated += FriendsManager_OnPresenceUpdate;

            cancelSearch.onClick.AddListener(() => {
               searchInputField.text = string.Empty;
               searchInputField.gameObject.SetActive(false);
               cancelSearch.gameObject.SetActive(false);
               foreach(var item in friendListItems)
               {
                   item.gameObject.SetActive(true);
                }
            });

            searchInputField.onValueChanged.AddListener((value) => {
                if (string.IsNullOrEmpty(value))
                {
                    // Show all friend items if the search bar is empty
                    foreach (var item in friendListItems)
                    {
                        item.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Filter friend items based on the search input
                    foreach (var item in friendListItems)
                    {
                        bool isMatch = item.FriendName.ToLower().Contains(value.ToLower());
                        item.gameObject.SetActive(isMatch);
                    }
                }
            });

            toggleFriendsButton.onClick.AddListener(async () =>
            {
                toggleFriendsButton.interactable = false;
                cancelSearch.onClick.Invoke();
                addFriendsPanel.gameObject.SetActive(false);
                HideExapndOptionspanel();
                await friendsPanelMover.ToggleMoveAsync();
                toggleFriendsButton.interactable = true;
            });

            SearchButton.onClick.AddListener(() => {
                searchInputField.gameObject.SetActive(true);
                cancelSearch.gameObject.SetActive(true);
                searchInputField.ActivateInputField();
            });

            playerProfile?.Initialize();
        }

        private void FriendsManager_OnFriendRequestReceived(FriendRequest request)
        {
            addFriend.image.color = Color.yellow;
        }

        private void FriendsManager_OnPresenceUpdate((string friendId, FriendPresence presence) tuple)
        {
            friendListItems.Find(item => item.FriendId == tuple.friendId)?.UpdatePresence(tuple.presence);
            SortFriendList();
        }

        private void SortFriendList()
        {
            int GetStatusPriority(FriendPresence status) => status switch
            {
                FriendPresence.Online => 0,
                FriendPresence.InLobby => 1,
                FriendPresence.InGame => 2,
                FriendPresence.Away => 3,
                FriendPresence.Offline => 5,
                _ => 4,
            };

            friendListItems.Sort((a, b) => GetStatusPriority(a.Presence).CompareTo(GetStatusPriority(b.Presence)));

            for (int i = 0; i < friendListItems.Count; i++)
            {
                friendListItems[i].transform.SetSiblingIndex(i + 1);
            }

            if (!string.IsNullOrEmpty(expandedFriendId))
            {
                ShowExpandOptionsPanel(expandedFriendId);
            }
        }

        private void FriendsManager_OnFriendsListUpdated(List<Friend> list)
        {
            foreach (var friend in list)
            {
                var friendItem = friendListItems.Find(item => item.FriendId == friend.PlayerId);
                if (friendItem == null)
                {
                    var newFriendItem = Instantiate(friendListItemPrefab, friendListHolder);
                    newFriendItem.SetFriendData(friend);
                    friendListItems.Add(newFriendItem);
                }
                else
                {
                    friendItem.SetFriendData(friend);
                }
            }

            // Remove items that are no longer in the friends list
            friendListItems.RemoveAll(item =>
            {
                if (!list.Exists(f => f.PlayerId == item.FriendId))
                {
                    Destroy(item.gameObject);
                    return true;
                }
                return false;
            });

            SortFriendList();
        }

        private void FriendsManager_OnFriendDataUpdated(Friend obj)
        {
            var friendItem = friendListItems.Find(item => item.FriendId == obj.PlayerId);

            if (friendItem != null)
            {
                friendItem.SetFriendData(obj);
            }
        }

        private void FriendsManager_OnLobbyInviteReceived(LobbyInvite obj)
        {
            var inviteItem = lobbyInviteItems.Find(item => item.FromPlayerId == obj.FromPlayerId);

            if (inviteItem == null)
            {
                var newInviteItem = Instantiate(lobbyInviteItemPrefab, lobbyInviteHolder);
                newInviteItem.SetInviteData(obj);
                lobbyInviteItems.Add(newInviteItem);
            }
        }

        public void ShowExpandOptionsPanel(string friendId)
        {
            if(friendListMoreItem.gameObject.activeSelf && expandedFriendId == friendId)
            {
                HideExapndOptionspanel();
                return;
            }
            expandedFriendId = friendId; // Track the currently expanded friend
            var friendItem = friendListItems.Find(item => item.FriendId == friendId);
            // Ensure the more item is parented correctly
            friendListMoreItem.transform.SetParent(friendListHolder, false);

            // Place the more item right after the friend item in the hierarchy
            friendListMoreItem.transform.SetAsLastSibling();
            int friendIndex = friendItem.transform.GetSiblingIndex();
            friendListMoreItem.transform.SetSiblingIndex(friendIndex + 1);

            // Show the panel for the correct friend
            friendListMoreItem.Show(friendId);
        }

        public void HideExapndOptionspanel()
        {
            expandedFriendId = null; // Clear the expanded friend
            friendListMoreItem.gameObject.SetActive(false);
        }
    }
}
