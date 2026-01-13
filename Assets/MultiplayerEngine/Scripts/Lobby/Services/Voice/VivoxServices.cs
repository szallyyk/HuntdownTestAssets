#if UNITY_SERVICES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Provides Vivox-based voice chat services.
    /// </summary>
    public class VivoxServices : IVoiceChat
    {
        public event Action<string> ChannelJoined;
        public event Action<string> PositionalChannelJoined;
        public event Action ChannelLeft;

        public event Action<List<VoiceMemeberSettings>> MembersUpdated;
        public event Action<VoiceMemeberSettings> MemberDataUpdated;

        public event Action<bool> MicStatusUpdated;
        public event Action<bool> SpeakerStatusUpdated;
        public event Action<int> MicVolumeUpdated;
        public event Action<string, string> OnMessageRecived;

        private string currentChannelId;

        private List<VoiceMemeberSettings> voiceMembers = new List<VoiceMemeberSettings>();

        private bool isLoggedIn = false;
        private string queuedChannelId = null;
        private bool joinCancelled = false;

        /// <inheritdoc />
        public async void Initialize()
        {
            try
            {
                await VivoxService.Instance.InitializeAsync();

                LoginOptions loginOptions = new LoginOptions
                {
                    PlayerId = AuthenticationService.Instance.PlayerId,
                    DisplayName = AuthenticationService.Instance.PlayerName,
                };

                await VivoxService.Instance.LoginAsync(loginOptions);
                isLoggedIn = true;

                // Process queued join if any and not cancelled
                if (queuedChannelId != null && !joinCancelled)
                {
                    await JoinVoiceChat(queuedChannelId);
                    queuedChannelId = null;
                }

                VivoxService.Instance.ParticipantAddedToChannel += (participant) =>
                {
                    // Check if participant already exists in the voiceMembers list
                    var existingMember = voiceMembers.Find(m => m.MemberId == participant.PlayerId);
                    
                    if (participant.PlayerId == AuthenticationService.Instance.PlayerId)
                        return;

                    if (existingMember == null)
                    {
                        Sprite sprite = null;
                        if (FriendsManager.Instance != null)
                        {
                            sprite = FriendsManager.Instance.FriendsList.Find(spriteData => spriteData.PlayerId == participant.PlayerId)?.Avatar;
                        }

                        // Create a new VoiceMemeberSettings for the participant
                        var newMember = new VoiceMemeberSettings
                        {
                            MemberId = participant.PlayerId,
                            Volume = VoiceManager.Instance.GetMemberVolume(participant.PlayerId),
                            IsMuted = VoiceManager.Instance.IsMemberMuted(participant.PlayerId),
                            Sprite = sprite,
                            DisplayName = participant.DisplayName
                        };
                        voiceMembers.Add(newMember);
                        MembersUpdated?.Invoke(voiceMembers);
                    }
                };

                VivoxService.Instance.ParticipantRemovedFromChannel += (participant) =>
                {
                    // Find and remove the participant from the voiceMembers list
                    var memberToRemove = voiceMembers.Find(m => m.MemberId == participant.PlayerId);
                    if (memberToRemove != null)
                    {
                        voiceMembers.Remove(memberToRemove);
                        MembersUpdated?.Invoke(voiceMembers);
                    }
                };

                // Subscribe to text message events
                VivoxService.Instance.ChannelMessageReceived += (args) =>
                {
                    // Skip messages from ourselves (we already show them locally)
                    if (args.FromSelf) return;
                    
                    OnMessageRecived?.Invoke(args.SenderPlayerId, args.MessageText);
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Vivox initialization failed: {ex.Message}");
            }
        }

        //not sure about working condition yet, need to test
        public void LogParticipantsInChannel(string channelName)
        {
            if (VivoxService.Instance.ActiveChannels.TryGetValue(channelName, out var channelSession))
            {
                
                channelSession.Cast<VivoxParticipant>().ToList().ForEach(participant =>
                {
                    Debug.Log($"Participant in channel {channelName}: {participant.PlayerId})");
                });
            }
            else
            {
                Debug.LogWarning($"Not connected to channel: {channelName}");
            }
        }

        /// <inheritdoc />
        public async Task<string> JoinVoiceChat(string channelId)
        {
            if (!isLoggedIn)
            {
                queuedChannelId = channelId;
                joinCancelled = false;
                return null;
            }

            try
            {
                if (VivoxService.Instance.ActiveChannels.Count != 0)
                    await VivoxService.Instance.LeaveAllChannelsAsync();

                await VivoxService.Instance.JoinGroupChannelAsync(channelId, ChatCapability.TextAndAudio);
                ChannelJoined?.Invoke(channelId);
                currentChannelId = channelId;
                return channelId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to join voice chat: {ex.Message}");
            }
            return null;
        }

        /// <inheritdoc />
        public async Task LeaveVoiceChat()
        {
            if (!isLoggedIn && queuedChannelId != null)
            {
                // Cancel queued join
                joinCancelled = true;
                queuedChannelId = null;
                return;
            }

            try
            {
                await VivoxService.Instance.LeaveAllChannelsAsync();
                currentChannelId = null;
                ChannelLeft?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to leave voice chat: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void MuteMember(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;

            var channel = VivoxService.Instance.ActiveChannels.GetValueOrDefault(currentChannelId);
            var participant = channel?.First(p => p.PlayerId == playerId);
            participant?.MutePlayerLocally();
        }

        /// <inheritdoc />
        public void UnmuteMember(string playerId)
        {
            if (string.IsNullOrEmpty(currentChannelId) || string.IsNullOrEmpty(playerId)) return;

            var channel = VivoxService.Instance.ActiveChannels.GetValueOrDefault(currentChannelId);
            var participant = channel?.First(p => p.PlayerId == playerId);
            participant?.UnmutePlayerLocally();
        }

        /// <inheritdoc />
        public void SetMemberVolume(int volume, string memberId)
        {
            if (string.IsNullOrEmpty(currentChannelId) || string.IsNullOrEmpty(memberId)) return;

            var channel = VivoxService.Instance.ActiveChannels.GetValueOrDefault(currentChannelId);
            var participant = channel?.First(p => p.PlayerId == memberId);
            participant?.SetLocalVolume(volume - 50);
        }

        /// <inheritdoc />
        public void SetMicVolume(int volume)
        {
            VivoxService.Instance.SetInputDeviceVolume(volume -50);
            MicVolumeUpdated?.Invoke(volume);
        }

        /// <inheritdoc />
        public void Set3DPosition(GameObject participantObject)
        {
            if (participantObject == null || string.IsNullOrEmpty(currentChannelId)) return;
            VivoxService.Instance.Set3DPosition(participantObject, currentChannelId);
        }

        /// <inheritdoc />
        public async Task<string> JoinPositionalChannel(string channelId)
        {
            try
            {
                if (VivoxService.Instance.ActiveChannels.Count != 0)
                    await VivoxService.Instance.LeaveAllChannelsAsync();

                var channel3DProperties = new Channel3DProperties();
                await VivoxService.Instance.JoinPositionalChannelAsync(channelId, ChatCapability.TextAndAudio, channel3DProperties);
                currentChannelId = channelId;
                PositionalChannelJoined?.Invoke(channelId);
                return channelId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to join positional channel: {ex.Message}");
            }
            return null;
        }

        /// <inheritdoc />
        public void Update() { }

        /// <inheritdoc />
        public void LateUpdate() { }

        public void MuteMic()
        {
            VivoxService.Instance.MuteInputDevice();
            MicStatusUpdated?.Invoke(true);
        }

        public void UnmuteMic()
        {
            VivoxService.Instance.UnmuteInputDevice();
            VivoxService.Instance.UnmuteOutputDevice();
            MicStatusUpdated?.Invoke(false);
            SpeakerStatusUpdated?.Invoke(false);
        }

        public void MuteSpeaker()
        {
            VivoxService.Instance.MuteInputDevice();
            VivoxService.Instance.MuteOutputDevice();
            SpeakerStatusUpdated?.Invoke(true);
            MicStatusUpdated?.Invoke(true);
        }

        public void UnmuteSpeaker()
        {
            VivoxService.Instance.UnmuteOutputDevice();
            SpeakerStatusUpdated?.Invoke(false);
        }

        public async void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(currentChannelId) || string.IsNullOrEmpty(message)) return;
            
            try
            {
                await VivoxService.Instance.SendChannelTextMessageAsync(currentChannelId, message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send text message: {ex.Message}");
            }
        }
    }
}
#endif