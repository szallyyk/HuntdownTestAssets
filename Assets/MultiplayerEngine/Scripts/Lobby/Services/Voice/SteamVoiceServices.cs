#if STEAM_SERVICES
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Provides Steam-based voice chat services for multiplayer lobbies.
    /// </summary>
    public class SteamVoiceServices : IVoiceChat
    {
        // Fields
        private bool isRecording;
        private readonly byte[] voiceBuffer = new byte[1024 * 10];
        private readonly byte[] receiveBuffer = new byte[1024 * 10];
        private readonly byte[] pcmBuffer = new byte[48000];
        private CSteamID lobbyID = CSteamID.Nil;
        private readonly Dictionary<CSteamID, AudioSource> playerSources = new();
        private HashSet<CSteamID> trackedMembers = new();

        // Events
        public event Action<string> ChannelJoined;
        public event Action<string> PositionalChannelJoined;
        public event Action ChannelLeft;
        public event Action<List<VoiceMemeberSettings>> MembersUpdated;
        public event Action<VoiceMemeberSettings> MemberDataUpdated;
        public event Action<bool> MicStatusUpdated;
        public event Action<bool> SpeakerStatusUpdated;
        public event Action<int> MicVolumeUpdated;

        public event Action<string, string> OnMessageRecived;

        private Callback<LobbyChatMsg_t> lobbyChatMsgCallback;

        /// <inheritdoc />
        public void Initialize()
        {
            // Register callback for lobby chat messages
            lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
        }

        /// <inheritdoc />
        public async Task<string> JoinVoiceChat(string channelId)
        {
            lobbyID = new CSteamID(ulong.Parse(channelId));
            StartVoice();
            ChannelJoined?.Invoke(channelId);
            return await Task.FromResult(channelId);
        }

        /// <inheritdoc />
        public Task LeaveVoiceChat()
        {
            StopVoice();
            playerSources.Clear();
            ChannelLeft?.Invoke();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<string> JoinPositionalChannel(string channelId)
        {
            lobbyID = new CSteamID(ulong.Parse(channelId));
            foreach (var src in playerSources.Values)
            {
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 1f;
                src.maxDistance = 20f;
            }
            StartVoice();
            PositionalChannelJoined?.Invoke(channelId);
            return await Task.FromResult(channelId);
        }

        /// <inheritdoc />
        public void Set3DPosition(GameObject participantObject)
        {
            var pos = participantObject.transform.position;
            var posData = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, posData, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, posData, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos.z), 0, posData, 8, 4);

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            for (int i = 0; i < memberCount; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
                if (member == SteamUser.GetSteamID()) continue;
                SteamNetworking.SendP2PPacket(member, posData, (uint)posData.Length, EP2PSend.k_EP2PSendUnreliable, 2);
            }
        }

        /// <inheritdoc />
        public void Update()
        {
            CheckAndHandleMemberChanges();

            if (!isRecording) return;

            if (SteamUser.GetAvailableVoice(out uint bytesAvailable) != EVoiceResult.k_EVoiceResultOK || bytesAvailable == 0)
                return;

            if (SteamUser.GetVoice(true, voiceBuffer, (uint)voiceBuffer.Length, out uint bytesWritten) == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0)
            {
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                for (int i = 0; i < memberCount; i++)
                {
                    var member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
                    if (member == SteamUser.GetSteamID()) continue;
                    SteamNetworking.SendP2PPacket(member, voiceBuffer, bytesWritten, EP2PSend.k_EP2PSendUnreliable, 1);
                }
            }
        }

        /// <inheritdoc />
        public void LateUpdate()
        {
            while (SteamNetworking.IsP2PPacketAvailable(out uint dataSize, 1))
            {
                if (SteamNetworking.ReadP2PPacket(receiveBuffer, (uint)receiveBuffer.Length, out uint bytesRead, out CSteamID remote, 1))
                {
                    if (SteamUser.DecompressVoice(receiveBuffer, bytesRead, pcmBuffer, (uint)pcmBuffer.Length, out uint bytesDecompressed, 48000) == EVoiceResult.k_EVoiceResultOK && bytesDecompressed > 0)
                    {
                        var samples = new float[bytesDecompressed / 2];
                        for (int i = 0; i < samples.Length; i++)
                            samples[i] = BitConverter.ToInt16(pcmBuffer, i * 2) / 32768f;

                        var clip = AudioClip.Create("voice", samples.Length, 1, 48000, false);
                        clip.SetData(samples, 0);

                        PlayVoice(remote, clip);
                    }
                }
            }
            HandlePositionPackets();
        }

        /// <inheritdoc />
        public void MuteMic()
        {
            StopVoice();
            MicStatusUpdated?.Invoke(true);
        }

        /// <inheritdoc />
        public void UnmuteMic()
        {
            StartVoice();
            UnmuteSpeaker();
            MicStatusUpdated?.Invoke(false);
            SpeakerStatusUpdated?.Invoke(false);
        }

        /// <inheritdoc />
        public void MuteSpeaker()
        {
            foreach (var src in playerSources.Values)
                src.mute = true;

            StopVoice();
            SpeakerStatusUpdated?.Invoke(true);
            MicStatusUpdated?.Invoke(true);
        }

        /// <inheritdoc />
        public void UnmuteSpeaker()
        {
            foreach (var src in playerSources.Values)
                src.mute = false;

            SpeakerStatusUpdated?.Invoke(false);
        }

        /// <inheritdoc />
        public void SetMicVolume(int volume)
        {
            MicVolumeUpdated?.Invoke(volume);
        }

        /// <inheritdoc />
        public void SetMemberVolume(int volume, string memberId)
        {
            var steamID = new CSteamID(ulong.Parse(memberId));
            if (playerSources.TryGetValue(steamID, out var src))
                src.volume = Mathf.Clamp01(volume);

            MemberDataUpdated?.Invoke(CreateMemberSettings(steamID));
        }

        /// <inheritdoc />
        public void MuteMember(string playerId)
        {
            var steamID = new CSteamID(ulong.Parse(playerId));
            if (playerSources.TryGetValue(steamID, out var src))
                src.mute = true;

            MemberDataUpdated?.Invoke(CreateMemberSettings(steamID));
        }

        /// <inheritdoc />
        public void UnmuteMember(string playerId)
        {
            var steamID = new CSteamID(ulong.Parse(playerId));
            if (playerSources.TryGetValue(steamID, out var src))
                src.mute = false;

            MemberDataUpdated?.Invoke(CreateMemberSettings(steamID));
        }
        private void StartVoice()
        {
            if (lobbyID == CSteamID.Nil) return;

            if (isRecording) return;
            isRecording = true;
            SteamUser.StartVoiceRecording();
            Debug.Log("Steam voice started");
        }

        private void StopVoice()
        {
            if (!isRecording) return;
            isRecording = false;
            SteamUser.StopVoiceRecording();

            lobbyID = CSteamID.Nil;

            Debug.Log("Steam voice stopped");
        }

        private void PlayVoice(CSteamID speaker, AudioClip clip)
        {
            var source = GetAudioSourceForSpeaker(speaker);
            source.clip = clip;
            source.Play();
        }

        private AudioSource GetAudioSourceForSpeaker(CSteamID id)
        {
            if (!playerSources.TryGetValue(id, out var src))
            {
                var go = new GameObject($"Voice_{id}");
                src = go.AddComponent<AudioSource>();
                src.spatialBlend = 0;
                playerSources[id] = src;
            }
            return src;
        }

        private void HandlePositionPackets()
        {
            while (SteamNetworking.IsP2PPacketAvailable(out uint dataSize, 2))
            {
                var posBuffer = new byte[12];
                if (SteamNetworking.ReadP2PPacket(posBuffer, (uint)posBuffer.Length, out uint bytesRead, out CSteamID remote, 2) && bytesRead == 12)
                {
                    float x = BitConverter.ToSingle(posBuffer, 0);
                    float y = BitConverter.ToSingle(posBuffer, 4);
                    float z = BitConverter.ToSingle(posBuffer, 8);
                    if (playerSources.TryGetValue(remote, out var src))
                        src.transform.position = new Vector3(x, y, z);
                }
            }
        }

        private void CheckAndHandleMemberChanges()
        {
            var currentMembers = new HashSet<CSteamID>();
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            for (int i = 0; i < memberCount; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
                if (member == SteamUser.GetSteamID()) continue;
                currentMembers.Add(member);
            }

            if (!currentMembers.SetEquals(trackedMembers))
            {
                OnLobbyMembersChanged(currentMembers);
                trackedMembers = currentMembers;
            }
        }

        private void OnLobbyMembersChanged(HashSet<CSteamID> currentMembers)
        {
            // Remove AudioSources for members who left
            foreach (var member in trackedMembers)
            {
                if (!currentMembers.Contains(member) && playerSources.TryGetValue(member, out var src))
                {
                    UnityEngine.Object.Destroy(src.gameObject);
                    playerSources.Remove(member);
                }
            }

            // Add AudioSources for new members
            foreach (var member in currentMembers)
            {
                if (!trackedMembers.Contains(member))
                    GetAudioSourceForSpeaker(member);
            }

            var memberSettingsList = new List<VoiceMemeberSettings>();
            foreach (var member in currentMembers)
                memberSettingsList.Add(CreateMemberSettings(member));

            MembersUpdated?.Invoke(memberSettingsList);
        }

        private VoiceMemeberSettings CreateMemberSettings(CSteamID steamID)
        {
            Sprite sprite = null;
            if (FriendsManager.Instance!= null)
            {
                sprite = FriendsManager.Instance.FriendsList.Find(spriteData => spriteData.PlayerId == steamID.ToString())?.Avatar;
            }

            return new VoiceMemeberSettings
            {
                MemberId = steamID.ToString(),
                IsMuted = VoiceManager.Instance.IsMemberMuted(steamID.ToString()),
                Volume = VoiceManager.Instance.GetMemberVolume(steamID.ToString()),
                DisplayName = Steamworks.SteamFriends.GetFriendPersonaName(steamID),
                Sprite = sprite
            };
        }

        public void SendMessage(string message)
        {
             SteamMatchmaking.SendLobbyChatMsg(lobbyID, System.Text.Encoding.UTF8.GetBytes(message), message.Length);
        }

        private void OnLobbyChatMessage(LobbyChatMsg_t callback)
        {
            CSteamID user;
            byte[] data = new byte[4096];
            EChatEntryType type;

            int length = SteamMatchmaking.GetLobbyChatEntry(
                new CSteamID(callback.m_ulSteamIDLobby),
                (int)callback.m_iChatID,
                out user,
                data,
                data.Length,
                out type
            );

            if (length > 0)
            {
                // Skip messages from ourselves (we already show them locally)
                if (user == SteamUser.GetSteamID()) return;
                
                string message = System.Text.Encoding.UTF8.GetString(data, 0, length);
                OnMessageRecived?.Invoke(user.ToString(), message);
            }
        }
    }
}
#endif