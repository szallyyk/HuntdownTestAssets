using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles the UI controls for voice chat features, including microphone, speaker, connection, and panel expansion.
    /// </summary>
    public class VoiceControlUI : MonoBehaviour
    {
        // ------------------------------------------------------------------------
        // UI References
        // ------------------------------------------------------------------------
        [Header("Microphone Controls")]
        [SerializeField] private Button micButton;
        [SerializeField] private Image micIcon;
        [SerializeField] private Slider micVolumeSlider;
        [SerializeField] private Sprite micActiveSprite;
        [SerializeField] private Sprite micMutedSprite;

        [Header("Speaker Controls")]
        [SerializeField] private Button speakerButton;
        [SerializeField] private Image speakerIcon;
        [SerializeField] private Sprite speakerActiveSprite;
        [SerializeField] private Sprite speakerMutedSprite;

        [Header("Panel Controls")]
        [SerializeField] private Button expandButton;
        [SerializeField] private Image expandIcon;
        [SerializeField] private GameObject expandedPanel;
        [SerializeField] private Sprite expandPanelSprite;
        [SerializeField] private Sprite collapsePanelSprite;

        [Header("Connection Controls")]
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Image disconnectIcon;
        [SerializeField] private Sprite connectedSprite;
        [SerializeField] private Sprite disconnectedSprite;

        [Header("Voice Members")]
        [SerializeField] private Transform membersHolder;
        [SerializeField] private VoiceMemberItem voiceMemberItem;

        // ------------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------------
        private bool isExpanded;
        private bool isConnected;
        private bool isMicMuted;
        private bool isSpeakerMuted;

        // ------------------------------------------------------------------------
        // Initialization
        // ------------------------------------------------------------------------

        /// <summary>
        /// Initializes the voice control UI and sets up event listeners.
        /// </summary>
        public void Initialize()
        {
            SetupButtonListeners();
            SetupVoiceManagerEvents();
            LoadInitialState();
        }

        // ------------------------------------------------------------------------
        // UI Event Setup
        // ------------------------------------------------------------------------

        /// <summary>
        /// Sets up button click listeners for all UI controls.
        /// </summary>
        private void SetupButtonListeners()
        {
            micButton.onClick.AddListener(OnMicButtonClicked);
            speakerButton.onClick.AddListener(OnSpeakerButtonClicked);
            expandButton.onClick.AddListener(OnExpandButtonClicked);
            disconnectButton.onClick.AddListener(() => { OnDisconnectButtonClickedAsync(); });

            expandedPanel.gameObject.SetActive(false);

            micVolumeSlider.onValueChanged.AddListener(OnMicVolumeChanged);
        }

        /// <summary>
        /// Subscribes to VoiceManager events for UI updates.
        /// </summary>
        private void SetupVoiceManagerEvents()
        {
            VoiceManager.ChannelJoined += OnChannelJoined;
            VoiceManager.ChannelLeft += OnChannelLeft;
            VoiceManager.PositionalChannelJoined += OnChannelJoined;

            VoiceManager.MicStatusUpdated += OnMicStatusUpdated;
            VoiceManager.SpeakerStatusUpdated += OnSpeakerStatusUpdated;
            VoiceManager.MicVolumeUpdated += OnMicVolumeUpdated;
            VoiceManager.MembersUpdated += VoiceManager_MembersUpdated;
            VoiceManager.MemberDataUpdated += VoiceManager_MemberDataUpdated;
        }

        /// <summary>
        /// Unsubscribes from VoiceManager events to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            VoiceManager.ChannelJoined -= OnChannelJoined;
            VoiceManager.ChannelLeft -= OnChannelLeft;
            VoiceManager.PositionalChannelJoined -= OnChannelJoined;

            VoiceManager.MicStatusUpdated -= OnMicStatusUpdated;
            VoiceManager.SpeakerStatusUpdated -= OnSpeakerStatusUpdated;
            VoiceManager.MicVolumeUpdated -= OnMicVolumeUpdated;
            VoiceManager.MembersUpdated -= VoiceManager_MembersUpdated;
            VoiceManager.MemberDataUpdated -= VoiceManager_MemberDataUpdated;
        }

        /// <summary>
        /// Loads initial state from saved PlayerPrefs.
        /// </summary>
        private void LoadInitialState()
        {
            if (VoiceManager.Instance == null) return;

            var settings = VoiceManager.Instance.GetSavedSettings();
            
            // Set initial mic state
            isMicMuted = settings.micMuted;
            micIcon.sprite = isMicMuted ? micMutedSprite : micActiveSprite;
            micIcon.color = isMicMuted ? Color.yellow : Color.cyan;

            // Set initial speaker state
            isSpeakerMuted = settings.speakerMuted;
            speakerIcon.sprite = isSpeakerMuted ? speakerMutedSprite : speakerActiveSprite;
            speakerIcon.color = isSpeakerMuted ? Color.yellow : Color.cyan;

            // Set initial volume
            micVolumeSlider.maxValue = 100;
            micVolumeSlider.minValue = 0;
            micVolumeSlider.wholeNumbers = true;
            micVolumeSlider.SetValueWithoutNotify(settings.micVolume);

            // Set initial connection state (disconnected by default)
            isConnected = false;
            disconnectIcon.sprite = disconnectedSprite;
            disconnectIcon.color = Color.red;
        }

        // ------------------------------------------------------------------------
        // Button Event Handlers
        // ------------------------------------------------------------------------

        /// <summary>
        /// Handles microphone button click.
        /// </summary>
        private void OnMicButtonClicked()
        {
            if (isMicMuted)
                VoiceManager.Instance.UnmuteMic();
            else
                VoiceManager.Instance.MuteMic();
        }

        /// <summary>
        /// Handles speaker button click.
        /// </summary>
        private void OnSpeakerButtonClicked()
        {
            if (isSpeakerMuted)
                VoiceManager.Instance.UnmuteSpeaker();
            else
                VoiceManager.Instance.MuteSpeaker();
        }

        /// <summary>
        /// Handles expand/collapse panel button click.
        /// </summary>
        private void OnExpandButtonClicked()
        {
            isExpanded = !isExpanded;
            expandedPanel.SetActive(isExpanded);
            expandIcon.sprite = isExpanded ? collapsePanelSprite : expandPanelSprite;
            expandIcon.color = isExpanded ? Color.cyan : Color.white;
        }

        /// <summary>
        /// Handles disconnect/connect button click asynchronously.
        /// </summary>
        private async void OnDisconnectButtonClickedAsync()
        {
            disconnectButton.interactable = false;
            Debug.Log("Toggling voice chat connection...");
            if (isConnected)
            {
                await VoiceManager.Instance.LeaveVoiceChat();
                Debug.Log("Left voice chat.");
            }
            else
            {
                await VoiceManager.Instance.JoinVoiceChat(string.Empty);
                Debug.Log("Joined voice chat.");
            }
            disconnectButton.interactable = true;
        }

        /// <summary>
        /// Handles microphone volume slider value change.
        /// </summary>
        private void OnMicVolumeChanged(float value)
        {
            VoiceManager.Instance.SetMicVolume((int)value);
        }

        // ------------------------------------------------------------------------
        // VoiceManager Event Handlers
        // ------------------------------------------------------------------------

        /// <summary>
        /// Updates microphone UI when mute status changes.
        /// </summary>
        private void OnMicStatusUpdated(bool isMuted)
        {
            isMicMuted = isMuted;
            micIcon.sprite = isMuted ? micMutedSprite : micActiveSprite;
            micIcon.color = isMuted ? Color.yellow : Color.cyan;
        }

        /// <summary>
        /// Updates speaker UI when mute status changes.
        /// </summary>
        private void OnSpeakerStatusUpdated(bool isMuted)
        {
            isSpeakerMuted = isMuted;
            speakerIcon.sprite = isMuted ? speakerMutedSprite : speakerActiveSprite;
            speakerIcon.color = isMuted ? Color.yellow : Color.cyan;
        }

        /// <summary>
        /// Updates microphone volume slider when volume changes.
        /// </summary>
        private void OnMicVolumeUpdated(int volume)
        {
            micVolumeSlider.maxValue = 100;
            micVolumeSlider.minValue = 0;
            micVolumeSlider.wholeNumbers = true;
            micVolumeSlider.value = volume;
        }

        /// <summary>
        /// Handles when a voice channel is joined.
        /// </summary>
        private void OnChannelJoined(string channelId)
        {
            disconnectIcon.sprite = connectedSprite;
            disconnectIcon.color = Color.cyan;
            isConnected = true;
        }

        /// <summary>
        /// Handles when a voice channel is left.
        /// </summary>
        private void OnChannelLeft()
        {
            disconnectIcon.sprite = disconnectedSprite;
            disconnectIcon.color = Color.red;
            isConnected = false;

            foreach (Transform child in membersHolder)
            {
                if(child.GetComponent<VoiceMemberItem>() != null)
                    Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Updates member UI when member data changes.
        /// </summary>
        private void VoiceManager_MemberDataUpdated(VoiceMemeberSettings obj)
        {
            foreach (Transform child in membersHolder)
            {
                var item = child.GetComponent<VoiceMemberItem>();
                if (item != null && item.PlayerId == obj.MemberId)
                {
                    item.SetMutedState(obj.IsMuted);
                    item.SetVolume(obj.Volume);
                    break;
                }
            }
        }

        /// <summary>
        /// Updates the members list UI when members are updated.
        /// </summary>
        private void VoiceManager_MembersUpdated(System.Collections.Generic.List<VoiceMemeberSettings> obj)
        {
            foreach (Transform child in membersHolder)
            {
                if(child.GetComponent<VoiceMemberItem>() != null)
                    Destroy(child.gameObject);
            }

            foreach (var member in obj)
            {
                var item = Instantiate(voiceMemberItem, membersHolder);
                item.Initialize(member.DisplayName, member.MemberId, member.Sprite);
                item.SetMutedState(member.IsMuted);
                item.SetVolume(member.Volume);
            }
        }
    }
}