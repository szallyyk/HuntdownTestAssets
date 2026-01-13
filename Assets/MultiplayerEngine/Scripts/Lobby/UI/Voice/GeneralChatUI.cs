using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Handles the text chat UI for lobby/in-game communication.
    /// Displays chat messages and allows sending new messages.
    /// Features auto-hide with fade in/out animations.
    /// </summary>
    public class GeneralChatUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Chat Display")]
        [Tooltip("Container for chat message items.")]
        [SerializeField] private RectTransform messageContainer;
        
        [Tooltip("Prefab for individual chat message items.")]
        [SerializeField] private ChatMessageItem messagePrefab;
        
        [Tooltip("ScrollRect for the chat view.")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Input")]
        [Tooltip("Input field for typing messages.")]
        [SerializeField] private TMP_InputField messageInput;
        
        [Tooltip("Button to send the message.")]
        [SerializeField] private Button sendButton;

        [Tooltip("Manual toggle button to show/hide chat.")]
        [SerializeField] private Button toggleChatButton;

        [Header("Fade Settings")]
        [Tooltip("CanvasGroup for fading the chat panel.")]
        [SerializeField] private CanvasGroup chatCanvasGroup;
        
        [Tooltip("Time in seconds before auto-hiding after last message.")]
        [SerializeField] private float autoHideDelay = 5f;
        
        [Tooltip("Time in seconds to keep visible when opened via manual button.")]
        [SerializeField] private float manualOpenDuration = 10f;
        
        [Tooltip("Duration of fade animation in seconds.")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Settings")]
        [Tooltip("Maximum number of messages to keep in chat history.")]
        [SerializeField] private int maxMessages = 100;

        private readonly List<ChatMessageItem> messageItems = new();
        private string localPlayerId;
        
        // Fade state
        private Coroutine fadeCoroutine;
        private Coroutine autoHideCoroutine;
        private bool isVisible = false;
        private bool isHovered = false;
        private bool isInputFocused = false;
        private bool isLocked = false; // When locked, chat stays visible (no auto-hide)

        /// <summary>
        /// Initializes the chat UI and sets up event listeners.
        /// </summary>
        public void Initialize()
        {
            SetupInputListeners();
            SubscribeToEvents();
            CacheLocalPlayerId();
            SetupFadeSystem();
        }

        private void CacheLocalPlayerId()
        {
            if (PlayerProfileManager.Instance?.LocalPlayerStats != null)
                localPlayerId = PlayerProfileManager.Instance.LocalPlayerStats.PlayerId;
        }

        private void SetupFadeSystem()
        {
            // Ensure we have a CanvasGroup
            if (chatCanvasGroup == null)
                chatCanvasGroup = GetComponent<CanvasGroup>();
            
            if (chatCanvasGroup == null)
                chatCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Start hidden
            SetAlpha(0f);
            isVisible = false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Sets up input field and button listeners.
        /// </summary>
        private void SetupInputListeners()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendButtonClicked);

            if (messageInput != null)
            {
                messageInput.onSubmit.AddListener(OnMessageSubmitted);
                messageInput.onValueChanged.AddListener(OnInputValueChanged);
                messageInput.onSelect.AddListener((_) => OnInputFocused());
                messageInput.onDeselect.AddListener((_) => OnInputDefocused());
            }

            if (toggleChatButton != null)
                toggleChatButton.onClick.AddListener(OnToggleChatButtonClicked);

            UpdateSendButtonState();
        }

        /// <summary>
        /// Called when input field gains focus.
        /// </summary>
        private void OnInputFocused()
        {
            isInputFocused = true;
            CancelAutoHide();
        }

        /// <summary>
        /// Called when input field loses focus.
        /// </summary>
        private void OnInputDefocused()
        {
            isInputFocused = false;
            if (!isHovered)
                StartAutoHideTimer(autoHideDelay);
        }

        /// <summary>
        /// Manual toggle button handler - acts as lock/unlock.
        /// When locked: chat stays visible, no auto-hide.
        /// Click again to unlock and hide.
        /// </summary>
        private void OnToggleChatButtonClicked()
        {
            if (isLocked)
            {
                // Unlock and hide
                isLocked = false;
                isHovered = false;
                isInputFocused = false;
                FadeOutForced();
            }
            else
            {
                // Lock - show and keep visible
                isLocked = true;
                CancelAutoHide();
                FadeIn();
            }
        }

        /// <summary>
        /// Subscribes to VoiceManager text chat events.
        /// </summary>
        private void SubscribeToEvents()
        {
            VoiceManager.OnMessageReceived += OnMessageReceived;
            VoiceManager.ChannelJoined += OnChannelJoined;
            VoiceManager.ChannelLeft += OnChannelLeft;
        }

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            VoiceManager.OnMessageReceived -= OnMessageReceived;
            VoiceManager.ChannelJoined -= OnChannelJoined;
            VoiceManager.ChannelLeft -= OnChannelLeft;
        }

        /// <summary>
        /// Handles received chat messages.
        /// </summary>
        private void OnMessageReceived(string senderId, string message)
        {
            // Skip messages from ourselves (we already added them locally when sending)
            if (!string.IsNullOrEmpty(localPlayerId) && senderId == localPlayerId)
                return;

            string senderName = GetSenderDisplayName(senderId);
            AddMessage(senderName, message, false);
        }

        /// <summary>
        /// Gets the display name for a sender ID.
        /// </summary>
        private string GetSenderDisplayName(string senderId)
        {
            // Check if it's the local player
            if (PlayerProfileManager.Instance?.LocalPlayerStats != null)
            {
                if (PlayerProfileManager.Instance.LocalPlayerStats.PlayerId == senderId)
                    return PlayerProfileManager.Instance.LocalPlayerStats.DisplayName;
            }

            // Try to get display name from lobby players
            if (LobbyManager.Instance?.LobbyData?.Players != null)
            {
                var lobbyPlayer = LobbyManager.Instance.LobbyData.Players
                    .FirstOrDefault(p => p.PlayerId == senderId);
                if (lobbyPlayer != null && !string.IsNullOrEmpty(lobbyPlayer.PlayerName))
                    return lobbyPlayer.PlayerName;
            }

            // Try to get display name from friends list
            if (FriendsManager.Instance != null)
            {
                var friend = FriendsManager.Instance.FriendsList.Find(f => f.PlayerId == senderId);
                if (friend != null && !string.IsNullOrEmpty(friend.DisplayName))
                    return friend.DisplayName;
            }

            // Fallback to player ID (truncate if too long)
            if (senderId.Length > 12)
                return senderId.Substring(0, 12) + "...";
            return senderId;
        }

        /// <summary>
        /// Called when the send button is clicked.
        /// </summary>
        private void OnSendButtonClicked()
        {
            SendCurrentMessage();
        }

        /// <summary>
        /// Called when Enter is pressed in the input field.
        /// </summary>
        private void OnMessageSubmitted(string text)
        {
            SendCurrentMessage();
        }

        /// <summary>
        /// Called when input field text changes.
        /// </summary>
        private void OnInputValueChanged(string text)
        {
            UpdateSendButtonState();
        }

        /// <summary>
        /// Sends the current message from the input field.
        /// </summary>
        private void SendCurrentMessage()
        {
            if (messageInput == null || string.IsNullOrWhiteSpace(messageInput.text))
                return;

            string message = messageInput.text.Trim();
            
            // Send through VoiceManager
            VoiceManager.Instance?.SendTextMessage(message);
            
            // Add local message immediately (for responsiveness)
            string localName = GetLocalPlayerName();
            AddMessage(localName, message, true);
            
            // Clear input and refocus
            messageInput.text = string.Empty;
            messageInput.ActivateInputField();
        }

        /// <summary>
        /// Gets the local player's display name.
        /// </summary>
        private string GetLocalPlayerName()
        {
            if (PlayerProfileManager.Instance?.LocalPlayerStats != null)
                return PlayerProfileManager.Instance.LocalPlayerStats.DisplayName;
            return "You";
        }

        /// <summary>
        /// Adds a message to the chat display.
        /// </summary>
        /// <param name="senderName">Name of the message sender.</param>
        /// <param name="message">The message content.</param>
        /// <param name="isLocalPlayer">Whether this is from the local player.</param>
        public void AddMessage(string senderName, string message, bool isLocalPlayer)
        {
            if (messagePrefab == null || messageContainer == null)
            {
                Debug.LogWarning("GeneralChatUI: messagePrefab or messageContainer not set.");
                return;
            }

            // Remove oldest messages if at capacity
            while (messageItems.Count >= maxMessages && messageItems.Count > 0)
            {
                var oldestItem = messageItems[0];
                messageItems.RemoveAt(0);
                if (oldestItem != null)
                    Destroy(oldestItem.gameObject);
            }

            // Create new message item
            var newMessage = Instantiate(messagePrefab, messageContainer);
            newMessage.SetMessage(senderName, message, isLocalPlayer);
            messageItems.Add(newMessage);

            // Scroll to bottom
            ScrollToBottom();

            // Show chat when new message arrives
            FadeIn();
            
            // Start auto-hide timer (unless user is interacting)
            if (!isHovered && !isInputFocused)
                StartAutoHideTimer(autoHideDelay);
        }

        /// <summary>
        /// Scrolls chat to the bottom to show latest messages.
        /// </summary>
        private void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Updates the send button interactability based on input.
        /// </summary>
        private void UpdateSendButtonState()
        {
            if (sendButton != null && messageInput != null)
            {
                sendButton.interactable = !string.IsNullOrWhiteSpace(messageInput.text);
            }
        }

        /// <summary>
        /// Called when joining a voice/chat channel.
        /// </summary>
        private void OnChannelJoined(string channelId)
        {
            if (messageInput != null)
                messageInput.interactable = true;
            
            AddSystemMessage("Connected to chat.");
        }

        /// <summary>
        /// Called when leaving a voice/chat channel.
        /// </summary>
        private void OnChannelLeft()
        {
            if (messageInput != null)
                messageInput.interactable = false;
            
            AddSystemMessage("Disconnected from chat.");
        }

        /// <summary>
        /// Adds a system message to the chat.
        /// </summary>
        public void AddSystemMessage(string message)
        {
            AddMessage("System", message, false);
        }

        /// <summary>
        /// Clears all chat messages.
        /// </summary>
        public void ClearChat()
        {
            foreach (var item in messageItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            messageItems.Clear();
        }

        // ========================================
        // Fade In/Out System
        // ========================================

        /// <summary>
        /// Fades in the chat panel.
        /// </summary>
        public void FadeIn()
        {
            if (isVisible) return;
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeToAlpha(1f));
            isVisible = true;
            
            // Enable interaction
            if (chatCanvasGroup != null)
            {
                chatCanvasGroup.interactable = true;
                chatCanvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Fades out the chat panel.
        /// </summary>
        public void FadeOut()
        {
            if (!isVisible) return;
            
            // Don't fade out if locked or user is interacting
            if (isLocked || isHovered || isInputFocused) return;
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeToAlpha(0f));
            isVisible = false;
            
            // Disable interaction when hidden
            if (chatCanvasGroup != null)
            {
                chatCanvasGroup.interactable = false;
                chatCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Forces fade out regardless of hover/focus state (for manual toggle).
        /// </summary>
        public void FadeOutForced()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            CancelAutoHide();
            
            fadeCoroutine = StartCoroutine(FadeToAlpha(0f));
            isVisible = false;
            
            // Disable interaction when hidden
            if (chatCanvasGroup != null)
            {
                chatCanvasGroup.interactable = false;
                chatCanvasGroup.blocksRaycasts = false;
            }
        }

        private IEnumerator FadeToAlpha(float targetAlpha)
        {
            if (chatCanvasGroup == null) yield break;
            
            float startAlpha = chatCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                chatCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            chatCanvasGroup.alpha = targetAlpha;
        }

        private void SetAlpha(float alpha)
        {
            if (chatCanvasGroup != null)
            {
                chatCanvasGroup.alpha = alpha;
                chatCanvasGroup.interactable = alpha > 0.5f;
                chatCanvasGroup.blocksRaycasts = alpha > 0.5f;
            }
        }

        /// <summary>
        /// Starts the auto-hide timer.
        /// </summary>
        private void StartAutoHideTimer(float delay)
        {
            CancelAutoHide();
            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(delay));
        }

        /// <summary>
        /// Cancels the auto-hide timer.
        /// </summary>
        private void CancelAutoHide()
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
        }

        private IEnumerator AutoHideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!isHovered && !isInputFocused)
            {
                FadeOut();
            }
        }

        // ========================================
        // Pointer Event Handlers
        // ========================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            CancelAutoHide();
            
            // If hidden, show it
            if (!isVisible)
                FadeIn();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            
            // Start auto-hide unless input is focused
            if (!isInputFocused)
                StartAutoHideTimer(autoHideDelay);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Clicking on chat keeps it visible
            CancelAutoHide();
            
            if (!isVisible)
                FadeIn();
        }
    }
}
