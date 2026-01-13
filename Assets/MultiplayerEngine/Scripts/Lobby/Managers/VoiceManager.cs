using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages voice chat functionality within the lobby.
    /// </summary>
    public class VoiceManager : MonoBehaviour
    {
        public static VoiceManager Instance { get; private set; }

        // Voice chat events
        public static event Action<string> ChannelJoined;
        public static event Action<string> PositionalChannelJoined;
        public static event Action ChannelLeft;

        public static event Action<List<VoiceMemeberSettings>> MembersUpdated;
        public static event Action<VoiceMemeberSettings> MemberDataUpdated;

        public static event Action<bool> MicStatusUpdated;
        public static event Action<bool> SpeakerStatusUpdated;
        public static event Action<int> MicVolumeUpdated;

        // Text chat events
        public static event Action<string, string> OnMessageReceived;

        private IVoiceChat voiceChatService;

        public string CurrentChannelId { get; private set; }

        // PlayerPrefs keys (centralized for consistency)
        private const string MicMutedKey = "LocalPlayer_MicMuted";
        private const string MicVolumeKey = "LocalPlayer_MicVolume";
        private const string SpeakerMutedKey = "LocalPlayer_SpeakerMuted";

        private void Awake()
        {
            // Ensure singleton instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

#if STEAM_SERVICES
            voiceChatService = new SteamVoiceServices();
#elif UNITY_SERVICES
            voiceChatService = new VivoxServices();
#endif

            // Subscribe to voice chat service events
            if (voiceChatService != null)
            {
                voiceChatService.ChannelJoined += (channelId) => ChannelJoined?.Invoke(channelId);
                voiceChatService.PositionalChannelJoined += (channelId) => PositionalChannelJoined?.Invoke(channelId);
                voiceChatService.ChannelLeft += () => ChannelLeft?.Invoke();
                voiceChatService.MembersUpdated += (members) => MembersUpdated?.Invoke(members);
                voiceChatService.MemberDataUpdated += (member) => MemberDataUpdated?.Invoke(member);
                voiceChatService.MicStatusUpdated += (isMuted) => MicStatusUpdated?.Invoke(isMuted);
                voiceChatService.SpeakerStatusUpdated += (isMuted) => SpeakerStatusUpdated?.Invoke(isMuted);
                voiceChatService.MicVolumeUpdated += (volume) => MicVolumeUpdated?.Invoke(volume);
                voiceChatService.OnMessageRecived += (senderId, message) => OnMessageReceived?.Invoke(senderId, message);
            }

            AuthenticationManager.OnProfileSetupCompleted += (boolean) =>
            {
                voiceChatService?.Initialize();
            };
        }

        private void Update()
        {
            voiceChatService?.Update();
        }

        private void LateUpdate()
        {
            voiceChatService?.LateUpdate();
        }

        /// <summary>
        /// Joins the voice chat channel for the specified lobby.
        /// </summary>
        /// <param name="lobbyId">The lobby identifier.</param>
        public async Task JoinVoiceChat(string lobbyId)
        {
            if (voiceChatService == null) return;

            if (lobbyId == string.Empty && CurrentChannelId != null)
                lobbyId = CurrentChannelId;
            else if (lobbyId == null)
                return;

            CurrentChannelId = await voiceChatService.JoinVoiceChat(lobbyId);
            ApplySavedSettings();
        }

        public async Task JoinPositionalChannel(string lobbyId)
        {
            if (voiceChatService == null) return;

            if (lobbyId == null && CurrentChannelId != null)
                lobbyId = CurrentChannelId;
            else if (lobbyId == null)
                return;

            CurrentChannelId = await voiceChatService.JoinPositionalChannel(lobbyId);
            ApplySavedSettings();
        }

        /// <summary>
        /// Applies saved audio settings from PlayerPrefs.
        /// </summary>
        private void ApplySavedSettings()
        {
            // Apply mic mute state (1 = muted, 0 = unmuted)
            bool isMicMuted = PlayerPrefs.GetInt(MicMutedKey, 0) == 1;
            if (isMicMuted)
                voiceChatService.MuteMic();
            else
                voiceChatService.UnmuteMic();

            // Apply mic volume
            int micVolume = PlayerPrefs.GetInt(MicVolumeKey, 50);
            voiceChatService.SetMicVolume(micVolume);

            // Apply speaker mute state (1 = muted, 0 = unmuted)
            bool isSpeakerMuted = PlayerPrefs.GetInt(SpeakerMutedKey, 0) == 1;
            if (isSpeakerMuted)
                voiceChatService.MuteSpeaker();
            else
                voiceChatService.UnmuteSpeaker();
        }

        /// <summary>
        /// Gets the saved settings for UI initialization.
        /// </summary>
        public (bool micMuted, bool speakerMuted, int micVolume) GetSavedSettings()
        {
            bool micMuted = PlayerPrefs.GetInt(MicMutedKey, 0) == 1;
            bool speakerMuted = PlayerPrefs.GetInt(SpeakerMutedKey, 0) == 1;
            int micVolume = PlayerPrefs.GetInt(MicVolumeKey, 50);
            return (micMuted, speakerMuted, micVolume);
        }

        public async Task LeaveVoiceChat()
        {
            if (CurrentChannelId == null || voiceChatService == null) return;
            await voiceChatService.LeaveVoiceChat();
            CurrentChannelId = null;
            Debug.Log("Left voice chat channel.");
        }

        public void MuteMic()
        {
            voiceChatService?.MuteMic();
            PlayerPrefs.SetInt(MicMutedKey, 1);
            PlayerPrefs.Save();
        }

        public void UnmuteMic()
        {
            voiceChatService?.UnmuteMic();
            PlayerPrefs.SetInt(MicMutedKey, 0);
            PlayerPrefs.Save();
        }

        public void SetMicVolume(float volume)
        {
            voiceChatService?.SetMicVolume((int)volume);
            PlayerPrefs.SetInt(MicVolumeKey, (int)volume);
            PlayerPrefs.Save();
        }

        public void MuteSpeaker()
        {
            voiceChatService?.MuteSpeaker();
            PlayerPrefs.SetInt(SpeakerMutedKey, 1);
            PlayerPrefs.Save();
        }

        public void UnmuteSpeaker()
        {
            voiceChatService?.UnmuteSpeaker();
            PlayerPrefs.SetInt(SpeakerMutedKey, 0);
            PlayerPrefs.Save();
        }

        public void SetMemberVolume(float volume, string playerId)
        {
            voiceChatService?.SetMemberVolume((int)volume, playerId);
            PlayerPrefs.SetInt($"VoiceMember_{playerId}_Volume", (int)volume);
            PlayerPrefs.Save();
        }

        public void MuteMember(string playerId)
        {
            voiceChatService?.MuteMember(playerId);
            PlayerPrefs.SetInt($"VoiceMember_{playerId}_Muted", 1);
            PlayerPrefs.Save();
        }

        public void UnmuteMember(string playerId)
        {
            voiceChatService?.UnmuteMember(playerId);
            PlayerPrefs.SetInt($"VoiceMember_{playerId}_Muted", 0);
            PlayerPrefs.Save();
        }

        public int GetMemberVolume(string memberId)
        {
            return PlayerPrefs.GetInt($"VoiceMember_{memberId}_Volume", 50);
        }

        public bool IsMemberMuted(string memberId)
        {
            return PlayerPrefs.GetInt($"VoiceMember_{memberId}_Muted", 0) == 1;
        }

        /// <summary>
        /// Sends a text chat message to the current channel.
        /// </summary>
        public void SendTextMessage(string message)
        {
            if (string.IsNullOrEmpty(message) || voiceChatService == null) return;
            voiceChatService.SendMessage(message);
        }
    }


    public class VoiceMemeberSettings
    {
        public string MemberId { get; set; }
        public int Volume { get; set; }
        public bool IsMuted { get; set; }
        public Sprite Sprite { get; set; }
        public string DisplayName { get; set; }
    }
}