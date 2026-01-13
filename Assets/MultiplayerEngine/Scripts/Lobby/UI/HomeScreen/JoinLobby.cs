using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    public class JoinLobby : MonoBehaviour
    {
        [SerializeField] private RectTransform lobbyList;
        [SerializeField] private LobbyListItem joinLobbyPanel;
        [SerializeField] private Button refreshButton;

        [SerializeField] private TMP_InputField joinCodeInputField;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button backButton;

        private async void Start()
        {
            var lobbylist = await LobbyManager.Instance.GetLobbyListAsync();

            foreach(Transform child in lobbyList)
            {
                Destroy(child.gameObject);
            }

            foreach (var lobby in lobbylist)
            {
                var item = Instantiate(joinLobbyPanel, lobbyList);
                item.SetUp(lobby);
            }

            joinLobbyButton.onClick.AddListener(async () =>
            {
                joinLobbyButton.interactable = false;
                await LobbyManager.Instance.JoinLobbyByCode(joinCodeInputField.text);
                joinLobbyButton.interactable = true;
            });

            backButton.onClick.AddListener(() =>
            {
                HomeScreenUI.Instance.ShowMenu();
            });

            refreshButton.onClick.AddListener(async () =>
            {
                refreshButton.interactable = false;
                var updatedLobbyList = await LobbyManager.Instance.GetLobbyListAsync();
                foreach(Transform child in lobbyList)
                {
                    Destroy(child.gameObject);
                }
                foreach (var lobby in updatedLobbyList)
                {
                    var item = Instantiate(joinLobbyPanel, lobbyList);
                    item.SetUp(lobby);
                }
                refreshButton.interactable = true;
            });
        }
    }
}
