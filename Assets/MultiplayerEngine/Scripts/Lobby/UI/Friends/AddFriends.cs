using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    public class AddFriends : MonoBehaviour
    {
        [SerializeField] private FriendRequestItem friendRequestItemPrefab;
        [SerializeField] private Transform friendRequestHolder;

        [SerializeField] private TMP_InputField friendNameInputField;
        [SerializeField] private Button addFriendButton;
        [SerializeField] private Button sendRequest;
        [SerializeField] private Button recieveRequest;
        [SerializeField] private Button closeButton;

        private List<FriendRequestItem> friendRequestItems = new List<FriendRequestItem>();

        private void Start()
        {
            sendRequest.onClick.AddListener(ShowOnlySentRequests);
            recieveRequest.onClick.AddListener(ShowOnlyReceivedRequests);
            FriendsManager.OnFriendRequestReceived += async(recieveRequest) => await RefreshRequests();
            FriendsManager.OnFriendAdded += async(friend) => await RefreshRequests();

            addFriendButton.onClick.AddListener(async() =>
            {
                var playerName = friendNameInputField.text;
                if (!string.IsNullOrEmpty(playerName))
                {
                    addFriendButton.interactable = false;
                    bool sucess = await FriendsManager.Instance.SendFriendRequest(playerName);
                    await RefreshRequests();
                    friendNameInputField.text = sucess ? "request sent sucessfully" : "failed";
                    addFriendButton.interactable = true;
                }
            });
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            RefreshRequests();
        }

        private async Task RefreshRequests()
        {
            List<FriendRequest> friendRequests = await  FriendsManager.Instance.GetFriendRequests();
            foreach(var items in friendRequestItems)
            {
                Destroy(items.gameObject);
            }
            friendRequestItems.Clear();
            foreach(var request in friendRequests)
            {
                var item = Instantiate(friendRequestItemPrefab, friendRequestHolder);
                item.SetRequest(request);
                friendRequestItems.Add(item);
            }
        }

        private void ShowOnlyReceivedRequests()
        {
            foreach(var item in friendRequestItems)
            {
                item.gameObject.SetActive(item.RequestType == FriendRequestType.Incoming);
            }
        }

        private void ShowOnlySentRequests()
        {
           foreach(var item in friendRequestItems)
            {
                item.gameObject.SetActive(item.RequestType == FriendRequestType.Outgoing);
            }
        }
    }
}
