#if UNITY_SERVICES
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Holds character-related data for multiplayer functionality.
    /// </summary>
    public class UnityFriends : IFriends
    {
        public event Action<List<Friend>> OnFriendsListUpdated;
        public event Action<Friend> OnFriendDataUpdated;
        public event Action<(string playerId, FriendPresence presence)> OnFriendPresenceUpdated;

        public event Action<Friend> OnFriendAdded;
        public event Action<string> OnFriendRemoved;
        public event Action<FriendRequest> OnFriendRequestReceived;
        public event Action<LobbyInvite> OnLobbyInviteReceived;
        public event Action<LobbyInvite> OnJoinRequestAccepted;
        public event Action<FriendPresence> OnLocalPlayerPresenceUpdated;

        private List<Friend> currentFriends = new List<Friend>();

        public async Task InitializeAsync()
        {
            await FriendsService.Instance.InitializeAsync();
            FriendsService.Instance.PresenceUpdated += HandlePresenceUpdated;
            FriendsService.Instance.RelationshipAdded += HandleRelationshipAdded;
            FriendsService.Instance.MessageReceived += HandleMessageReceived;
            await SetPresence(FriendPresence.Online);
            await GetFriendListAsync();
        }

        public async Task<bool> AcceptFriendRequest(string playerId)
        {
            var relationship = await FriendsService.Instance.AddFriendAsync(playerId);
            return relationship.Type == RelationshipType.Friend ? true : false;
        }

        public async Task<bool> CancelOutgoingRequest(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteOutgoingFriendRequestAsync(playerId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeclineFriendRequest(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteIncomingFriendRequestAsync(playerId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Friend>> GetFriendListAsync()
        {
            var friends = FriendsService.Instance.Friends;
            List<Friend> friendList = new List<Friend>();
            foreach (var friend in friends)
            {
                FriendPresence presence = MapPresence(friend.Member.Presence);

                var newFriend = new Friend
                {
                    PlayerId = friend.Member.Id,
                    DisplayName = friend.Member.Profile.Name,
                    Avatar = await GetProfilePictureAsync(friend.Member.Id),
                    Presence = presence
                };

                friendList.Add(newFriend);
            }

            currentFriends = friendList;

            OnFriendsListUpdated?.Invoke(currentFriends);
            return currentFriends;
        }

        private async Task<Sprite> GetProfilePictureAsync(string playerId)
        {
            try
            {
                var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { "avatarId" },
                    new LoadOptions(new PublicReadAccessClassOptions(playerId))
                );

                if (playerData.TryGetValue("avatarId", out var playerIconValue))
                {
                    int playerIconId = playerIconValue.Value.GetAs<int>();
                    return PlayerProfileManager.Instance.GetIconById(playerIconId.ToString());
                }
               
            } 
            catch{ }
            return PlayerProfileManager.Instance.GetIconById("0");
        }

        private FriendPresence MapPresence(Presence presence)
        {

            if (presence == null)
                return FriendPresence.Offline;

            if(presence.Availability == Availability.Offline)
                return FriendPresence.Offline;

            if (presence.Availability == Availability.Away)
                return FriendPresence.Away;

            return presence.GetActivity<Activity>()?.friendPresence ?? FriendPresence.Online;
        }

        public async Task<List<FriendRequest>> GetFriendRequestsAsync()
        {
            var friendRequests = new List<FriendRequest>();

            foreach (var request in FriendsService.Instance.IncomingFriendRequests)
            {
                friendRequests.Add(new FriendRequest
                {
                    PlayerId = request.Member.Id,
                    DisplayName = request.Member.Profile.Name,
                    Avatar = await GetProfilePictureAsync(request.Member.Id.ToString()),
                    RequestType = FriendRequestType.Incoming
                });
            }

            foreach (var request in FriendsService.Instance.OutgoingFriendRequests)
            {
                friendRequests.Add(new FriendRequest
                {
                    PlayerId = request.Member.Id,
                    DisplayName = request.Member.Profile.Name,
                    Avatar = await GetProfilePictureAsync(request.Member.Id.ToString()),
                    RequestType = FriendRequestType.Outgoing
                });
            }

            return friendRequests;
        }

        private void HandleMessageReceived(IMessageReceivedEvent @event)
        {
            InviteData inviteData = @event.GetAs<InviteData>();
            if (inviteData != null)
            {
                if (inviteData.Type == InviteType.Invite || inviteData.Type == InviteType.RequestToJoin)
                {
                    LobbyInvite invite = new LobbyInvite
                    {
                        FromPlayerId = @event.UserId,
                        LobbyId = inviteData.JoinId,
                        InviteType = inviteData.Type
                    };
                    OnLobbyInviteReceived?.Invoke(invite);
                }
                else if (inviteData.Type == InviteType.AcceptedRequest)
                {
                   OnJoinRequestAccepted?.Invoke(new LobbyInvite
                   {
                       FromPlayerId = @event.UserId,
                       LobbyId = inviteData.JoinId,
                       InviteType = inviteData.Type
                   });
                }
            }
        }

        public async Task<bool> SendFriendRequest(string playerId)
        {
            var relationship = await FriendsService.Instance.AddFriendByNameAsync(playerId);
            return relationship.Type == RelationshipType.FriendRequest ? true : false;
        }

        public async Task<bool> Unfriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteRelationshipAsync(playerId);

                _ = GetFriendListAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public class InviteData
        {
            public string JoinId;
            public InviteType Type;
        }

        public async Task<bool> SendInvite(string playerId, string joinId) 
        {
            var inviteData = new InviteData
            {
               JoinId = joinId,
                Type = InviteType.Invite
            };
            await FriendsService.Instance.MessageAsync(playerId, inviteData);
            return true;
        }

        public async Task<bool> AskToJoin(string playerId)
        {
            var inviteData = new InviteData
            {
                JoinId = "",
                Type = InviteType.RequestToJoin
            };
            await FriendsService.Instance.MessageAsync(playerId, inviteData);
            return true;
        }

        public async Task<bool> AcceptJoinRequest(string playerId, string joinId) 
        { 
            var inviteData = new InviteData
            {
                JoinId = joinId,
                Type = InviteType.AcceptedRequest
            };
            await FriendsService.Instance.MessageAsync(playerId, inviteData);
            return true;
        }

        private void HandlePresenceUpdated(IPresenceUpdatedEvent friend)
        {
           FriendPresence presence =  MapPresence(friend.Presence);
           OnFriendPresenceUpdated?.Invoke((friend.ID, presence));
        }

        private async void HandleRelationshipAdded(IRelationshipAddedEvent evt)
        {
            if (evt.Relationship.Type == RelationshipType.Friend)
            {
                OnFriendAdded?.Invoke(new Friend
                {
                    PlayerId = evt.Relationship.Member.Id,
                    DisplayName = evt.Relationship.Member.Profile.Name,
                    Avatar = await GetProfilePictureAsync(evt.Relationship.Member.Id.ToString()),
                    Presence = MapPresence(evt.Relationship.Member.Presence)
                });

                _ = GetFriendListAsync();
            }
            else if (evt.Relationship.Type == RelationshipType.FriendRequest)
            {
                OnFriendRequestReceived?.Invoke(new FriendRequest
                {
                    PlayerId = evt.Relationship.Member.Id,
                    DisplayName = evt.Relationship.Member.Profile.Name,
                    Avatar = await GetProfilePictureAsync(evt.Relationship.Member.Id.ToString()),
                    RequestType = FriendRequestType.Incoming
                });
            }
        }

        public async Task SetPresence(FriendPresence presence)
        {
            var availability = Availability.Offline;
            var activity = new Activity();
            activity.friendPresence = presence;

            if (presence == FriendPresence.Offline)
            {
                availability = Availability.Offline;
            }
            else if (presence == FriendPresence.Online)
            {
                availability = Availability.Online;
            }
            else if (presence == FriendPresence.Away)
            {
                availability = Availability.Away;
            }
            else if (presence == FriendPresence.InGame)
            {
                availability = Availability.Online;
            }
            else if(presence == FriendPresence.InLobby)
            {
                availability = Availability.Online;
            }

            await FriendsService.Instance.SetPresenceAsync(availability, activity);

            OnLocalPlayerPresenceUpdated?.Invoke(presence);
        }

        private class Activity
        {
            public FriendPresence friendPresence;
        }
    }
}
#endif
