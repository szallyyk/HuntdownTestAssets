using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages the character selection UI in the multiplayer lobby.
    /// </summary>
    public class LobbyPlayerItem : MonoBehaviour
    {
        public void UpdatePlayer(PlayerData playerData, LobbyData lobbyData)
        {
            playerNameText.text = playerData.Data.ContainsKey(PlayerDataKeys.PlayerName) ? playerData.Data[PlayerDataKeys.PlayerName] : "Unknown";
            readyImage.gameObject.SetActive(playerData.Data.ContainsKey(PlayerDataKeys.PlayerReady) && playerData.Data[PlayerDataKeys.PlayerReady] == "true");
            ownerImage.gameObject.SetActive(playerData.IsLobbyHost);

            kickPlayer.gameObject.SetActive(!playerData.IsLocalPlayer && lobbyData.HostId == PlayerProfileManager.Instance.LocalPlayerStats.PlayerId);
            characterImage.sprite = PlayerProfileManager.Instance.CharacterData.Find(CharacterData => CharacterData.CharacterId == playerData.Data[PlayerDataKeys.PlayerCharacter])?.CharacterIcon;
            playerId = playerData.PlayerId;
        }

        [SerializeField] private Image characterImage;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private Image readyImage;  
        [SerializeField] private Image ownerImage;
        [SerializeField] private Button kickPlayer;

        private string playerId;

        private void Awake()
        {
            kickPlayer.onClick.AddListener(async () =>
            {
                kickPlayer.interactable = false;
                await LobbyManager.Instance.KickPlayer(playerId);
                kickPlayer.interactable = true;
            });
        }
    }
}
