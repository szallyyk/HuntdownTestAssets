using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages friends, friend requests, and lobby invites for multiplayer functionality.
    /// Provides methods to interact with the friends system and raises events for UI updates.
    /// </summary>
    public class FriendsManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the FriendsManager.
        /// </summary>
        public static FriendsManager Instance { get; private set; }

        /// <summary>
        /// Raised when the friends list is updated.
        /// </summary>
        public static event System.Action<List<Friend>> OnFriendsListUpdated;

        /// <summary>
        /// Raised when a friend's data is updated.
        /// </summary>
        public static event System.Action<Friend> OnFriendDataUpdated;

        /// <summary>
        /// Raised when a friend's presence is updated.
        /// </summary>
        public static event System.Action<(string friendId, FriendPresence presence)> OnFriendPresenceUpdated;

        /// <summary>
        /// Raised when a lobby invite is received.
        /// </summary>
        public static event System.Action<LobbyInvite> OnLobbyInviteReceived;

        /// <summary>
        /// Raised when localPlayer's presence is updated.
        /// </summary>
        public static event System.Action<FriendPresence> OnLocalPlayerPresenceUpdated;

        public static event System.Action<FriendRequest> OnFriendRequestReceived;

        public static event System.Action<Friend> OnFriendAdded;

        public List<Friend> FriendsList { get; private set; } = new List<Friend>();

        private IFriends friendsService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_SERVICES
            friendsService = new UnityFriends();
#elif STEAM_SERVICES
            friendsService = new SteamFriends();
#endif

            AuthenticationManager.OnProfileSetupCompleted += OnSignInCompletedHandler;

            friendsService.OnFriendsListUpdated += (list) =>
            {
                FriendsList = list;
                OnFriendsListUpdated?.Invoke(list);
            };

            friendsService.OnFriendDataUpdated += (friend) =>
            {
                // Update the specific friend in FriendsList
                var index = FriendsList.FindIndex(f => f.PlayerId == friend.PlayerId);
                if (index >= 0)
                {
                    FriendsList[index] = friend;
                }
                else
                {
                    FriendsList.Add(friend);
                }
                OnFriendDataUpdated?.Invoke(friend);
            };

            friendsService.OnFriendPresenceUpdated += (data) =>
            {
                // Update presence for the specific friend
                var (friendId, presence) = data;
                var friend = FriendsList.Find(f => f.PlayerId == friendId);
                if (friend != null)
                {
                    friend.Presence = presence;
                }
                OnFriendPresenceUpdated?.Invoke(data);
            };
            friendsService.OnLobbyInviteReceived += (invite) => OnLobbyInviteReceived?.Invoke(invite);
            friendsService.OnFriendRequestReceived += (request) => OnFriendRequestReceived?.Invoke(request);
            friendsService.OnLocalPlayerPresenceUpdated += (presence) => OnLocalPlayerPresenceUpdated?.Invoke(presence);
            friendsService.OnFriendAdded += (friend) => OnFriendAdded?.Invoke(friend);
        }

        private void OnSignInCompletedHandler(bool sucess)
        {
            _ = friendsService.InitializeAsync();
        }

        /// <summary>
        /// Retrieves the current list of friends asynchronously.
        /// </summary>
        /// <returns>A list of friends.</returns>
        public async Task<List<Friend>> GetFriendsList()
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return new List<Friend>();
            }
            FriendsList =  await friendsService.GetFriendListAsync();
            return FriendsList;
        }

        /// <summary>
        /// Retrieves the current list of friend requests asynchronously.
        /// </summary>
        /// <returns>A list of friend requests.</returns>
        public async Task<List<FriendRequest>> GetFriendRequests()
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return new List<FriendRequest>();
            }
            return await friendsService.GetFriendRequestsAsync();
        }

        /// <summary>
        /// Sends a friend request to the specified player.
        /// </summary>
        /// <param name="playerId">The unique identifier of the player to send a request to.</param>
        /// <returns>True if the request was sent successfully; otherwise, false.</returns>
        public async Task<bool> SendFriendRequest(string playerId)
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return false;
            }
            return await friendsService.SendFriendRequest(playerId);
        }

        /// <summary>
        /// Cancels an outgoing friend request to the specified player.
        /// </summary>
        /// <param name="playerId">The unique identifier of the player whose request should be canceled.</param>
        /// <returns>True if the request was canceled successfully; otherwise, false.</returns>
        public async Task<bool> CancelOutgoingRequest(string playerId)
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return false;
            }
            return await friendsService.CancelOutgoingRequest(playerId);
        }

        /// <summary>
        /// Accepts a friend request from the specified player.
        /// </summary>
        /// <param name="playerId">The unique identifier of the player whose request should be accepted.</param>
        /// <returns>True if the request was accepted successfully; otherwise, false.</returns>
        public async Task<bool> AcceptFriendRequest(string playerId)
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return false;
            }
            return await friendsService.AcceptFriendRequest(playerId);
        }

        /// <summary>
        /// Declines a friend request from the specified player.
        /// </summary>
        /// <param name="playerId">The unique identifier of the player whose request should be declined.</param>
        /// <returns>True if the request was declined successfully; otherwise, false.</returns>
        public async Task<bool> DeclineFriendRequest(string playerId)
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return false;
            }
            return await friendsService.DeclineFriendRequest(playerId);
        }

        /// <summary>
        /// Removes a friend relationship with the specified player.
        /// </summary>
        /// <param name="playerId">The unique identifier of the friend to remove.</param>
        /// <returns>True if the friend was removed successfully; otherwise, false.</returns>
        public async Task<bool> Unfriend(string playerId)
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return false;
            }
            return await friendsService.Unfriend(playerId);
        }

        public async Task<bool> InviteToGame(string playerId)
        {
            if (friendsService == null)
            {
                Debug.LogError("Friends service not initialized.");
                return false;
            }
            return await friendsService.SendInvite(playerId, LobbyManager.Instance.LobbyData.LobbyId);
        }

        public async Task SetPresence(FriendPresence presence)
        {
            await friendsService.SetPresence(presence);
        }
    }

    /// <summary>
    /// Represents a friend in the multiplayer system.
    /// </summary>
    public class Friend
    {
        /// <summary>
        /// The display name of the friend.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The unique player ID of the friend.
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// The current presence status of the friend.
        /// </summary>
        public FriendPresence Presence;

        /// <summary>
        /// The avatar image of the friend.
        /// </summary>
        public Sprite Avatar;
    }

    /// <summary>
    /// Represents a friend request in the multiplayer system.
    /// </summary>
    public class FriendRequest
    {
        /// <summary>
        /// The display name of the player who sent or received the request.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The unique player ID of the request sender or receiver.
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// The avatar image of the player.
        /// </summary>
        public Sprite Avatar;

        /// <summary>
        /// The type of the friend request (incoming or outgoing).
        /// </summary>
        public FriendRequestType RequestType;
    }

    /// <summary>
    /// Represents a lobby invite in the multiplayer system.
    /// </summary>
    public class LobbyInvite
    {
        /// <summary>
        /// The player ID of the sender.
        /// </summary>
        public string FromPlayerId;

        /// <summary>
        /// The display name of the sender.
        /// </summary>
        public string FromPlayerName;

        /// <summary>
        /// The lobby ID for the invite.
        /// </summary>
        public string LobbyId;

        /// <summary>
        /// The avatar image of the sender.
        /// </summary>
        public Sprite FromAvatar;

        /// <summary>
        /// The type of the invite.
        /// </summary>
        public InviteType InviteType;
    }

    /// <summary>
    /// Specifies the type of a lobby invite.
    /// </summary>
    public enum InviteType
    {
        /// <summary>
        /// Standard invite to join a lobby.
        /// </summary>
        Invite,

        /// <summary>
        /// Request to join a lobby.
        /// </summary>
        RequestToJoin,

        /// <summary>
        /// Accepted join request.
        /// </summary>
        AcceptedRequest
    }

    /// <summary>
    /// Specifies the type of a friend request.
    /// </summary>
    public enum FriendRequestType
    {
        /// <summary>
        /// Incoming friend request.
        /// </summary>
        Incoming,

        /// <summary>
        /// Outgoing friend request.
        /// </summary>
        Outgoing
    }

    /// <summary>
    /// Specifies the presence status of a friend.
    /// </summary>
    public enum FriendPresence
    {
        /// <summary>
        /// The friend is away.
        /// </summary>
        Away,

        /// <summary>
        /// The friend is online.
        /// </summary>
        Online,

        /// <summary>
        /// The friend is offline.
        /// </summary>
        Offline,

        /// <summary>
        /// The friend is in a game.
        /// </summary>
        InGame,

        /// <summary>
        /// The friend is in Lobby.
        /// </summary>
        InLobby
    }

    /// <summary>
    /// Represents lobby list data for UI or matchmaking.
    /// </summary>
    public class LobbyListData
    {
        /// <summary>
        /// The name of the lobby.
        /// </summary>
        public string lobbyName;

        /// <summary>
        /// The unique identifier of the lobby.
        /// </summary>
        public string lobbyId;

        /// <summary>
        /// The current number of players in the lobby.
        /// </summary>
        public int currentPlayers;

        /// <summary>
        /// The maximum number of players allowed in the lobby.
        /// </summary>
        public int maxPlayers;

        /// <summary>
        /// The game mode of the lobby.
        /// </summary>
        public string gameMode;
    }
}