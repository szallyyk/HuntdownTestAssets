using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    public class LobbyListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text lobbyName;
        [SerializeField] private TMP_Text playerCount;
        [SerializeField] private TMP_Text gameMode;
        [SerializeField] private Button joinLobby;

        private string lobbyId;
        public void SetUp(LobbyListData lobbyListData)
        {
            lobbyId = lobbyListData.lobbyId;
            lobbyName.text = lobbyListData.lobbyName;
            playerCount.text = $"{lobbyListData.currentPlayers}/{lobbyListData.maxPlayers}";
            gameMode.text = lobbyListData.gameMode;
            joinLobby.onClick.AddListener(async() =>
            {
                  await LobbyManager.Instance.JoinLobby(lobbyId);
            });
        }
    }
}
