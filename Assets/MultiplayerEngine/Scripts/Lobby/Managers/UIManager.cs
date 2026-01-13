using Ignitives.MultiplayerEngine;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages the overall UI state and transitions between different UI screens in the multiplayer lobby system.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private AuthenticationUI authenticationUI;
        [SerializeField] private HomeScreenUI homeUI;
        [SerializeField] private LobbyUI lobbyUI;
        [SerializeField] private PlayerProfileUI playerProfileUI;
        [SerializeField] private FriendsUI friendsUI;

        private void Awake()
        {
            authenticationUI?.gameObject.SetActive(true);
            homeUI?.gameObject.SetActive(false);
            lobbyUI?.gameObject.SetActive(false);
            playerProfileUI?.gameObject.SetActive(false);
            friendsUI?.gameObject.SetActive(true);

            authenticationUI?.Initialize();
            homeUI?.Initialize();
            lobbyUI?.Initialize();
            playerProfileUI?.Initialize();
            friendsUI?.Initialize();
        }
    }
}
