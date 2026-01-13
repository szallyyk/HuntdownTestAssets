using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Holds character-related data for multiplayer functionality.
    /// </summary>
    public class LobbyInviteItem : MonoBehaviour
    {

        [SerializeField] private Button accept;
        [SerializeField] private Button decline;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text fromPlayerName;
        [SerializeField] private Image fromPlayerAvatar;

        public string LobbyId { get; private set; }
        public string FromPlayerId { get; private set; }

        public InviteType InviteType { get; private set; }

        public void SetInviteData(LobbyInvite invite)
        {
            FromPlayerId = invite.FromPlayerId;
            LobbyId = invite.LobbyId;
            fromPlayerName.text = invite.FromPlayerName;
            description.text = invite.InviteType == InviteType.Invite ? "invited you to a lobby" : "requested to join your lobby";
            fromPlayerAvatar.sprite = invite.FromAvatar;

            Debug.Log($"[LobbyInviteItem] SetInviteData: FromPlayerId={FromPlayerId}, LobbyId={LobbyId}, FromPlayerName={invite.FromPlayerName}, InviteType={invite.InviteType}");
        }

        private void Awake()
        {
            accept.onClick.AddListener(async () =>
            {
                accept.interactable = false;
                await LobbyManager.Instance.JoinLobby(LobbyId);
                if (LobbyManager.Instance.LobbyData != null)
                    Destroy(this.gameObject);
                else
                    accept.interactable = true;
            });

            decline.onClick.AddListener(() =>
            {
                Destroy(this.gameObject);
            });
        }
    }
}
