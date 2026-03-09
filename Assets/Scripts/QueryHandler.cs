using System;
using System.Collections.Generic;
using System.Globalization;
using NaughtyAttributes;
using UnityEngine;

namespace DefaultNamespace
{
    public class QueryHandler : MonoBehaviour
    {
        [Serializable]
        public class ConversationMessage
        {
            public string Role;
            public string Content;
            public string LocalTimestampDisplay;

            public ConversationMessage(string role, string content, string localTimestampDisplay)
            {
                Role = role;
                Content = content;
                LocalTimestampDisplay = localTimestampDisplay;
            }
        }

        [Serializable]
        public class InspectorChatMessage
        {
            [ReadOnly]
            [SerializeField] private string _role;

            [ReadOnly]
            [SerializeField] private string _localTimestampDisplay;

            [TextArea(2, 8)]
            [ReadOnly]
            [SerializeField] private string _content;

            public string Role => _role;
            public string LocalTimestampDisplay => _localTimestampDisplay;
            public string Content => _content;

            public InspectorChatMessage(string role, string content, string localTimestampDisplay)
            {
                _role = role;
                _content = content;
                _localTimestampDisplay = localTimestampDisplay;
            }
        }

        [Header("Dependencies")]
        [SerializeField] private SpeakAndEmoteController _speakAndEmoteController;

        [SerializeField] private OpenRouterChatClient _llmClient;
        [SerializeField] private DiscordClient _discordClient;

        [Header("Behavior")]
        /*[TextArea(6, 14)]
        [SerializeField]*/ private string _systemPrompt =
            "You are a personal assistant focused on the user's productivity, motivation, and attention. " +
            "Reply in 1 short sentence. Be direct, helpful, and action-oriented. " +
            "All timestamps in the conversation history are Austin, Texas local time. " +
            "You may use time-of-day and elapsed time between messages as context for tone and urgency. " +
            "However, the user may activate TEST TIME MODE by including text like '(Override time: 5:20PM)' in their message. " +
            "When an override time is present, treat that time as the current local Austin time. " +
            "In TEST TIME MODE you should ignore message timestamps and instead assume the override time is the real current time. " +
            "This override exists only for simulation/testing of morning, afternoon, late night, and deadline scenarios. " +
            "Do not mention timestamps or override mode unless it is relevant to the conversation." +
            "Each conversation message may begin with metadata like [Austin Local Time: ...]. " +
            "That metadata is for reasoning only and must never be spoken, quoted, paraphrased, or included in the reply. " +
            "Respond only to the human message content itself unless the time context is useful implicitly. " +
            "If time context matters, naturally refer to it like 'this morning' or 'late tonight' instead of repeating raw timestamps. ";
        
        private string _cleanerPrompt =
            "You will be given a message that may contain metadata, timestamps, labels, or instructions mixed with the assistant's actual spoken reply. " +
            "Return only the exact user-facing reply that should be spoken aloud. " +
            "Do not add quotes, explanations, prefixes, or suffixes. " +
            "If the message is already clean, return it unchanged.\n\n" +
            "Message to clean:\n{message}";
        

        [SerializeField] private bool _includeSystemPrompt = true;
        [SerializeField] private int _maxRetainedConversationMessages = 100;

        [Header("Time")]
        [SerializeField] private string _timeZoneId = "Central Standard Time";

        [Header("Stats")]
        [ReadOnly]
        [SerializeField] private int _storedConversationMessageCount;

        [ReadOnly]
        [SerializeField] private int _userMessageCount;

        [ReadOnly]
        [SerializeField] private int _assistantMessageCount;

        [ReadOnly]
        [SerializeField] private string _currentLocalAustinTimeDisplay;

        [Header("Inspector Conversation History")]
        [SerializeField] private List<InspectorChatMessage> _inspectorConversationHistory = new List<InspectorChatMessage>();

        private readonly List<ConversationMessage> _conversationHistory = new List<ConversationMessage>();

        [Button("Reset")]
        public void ResetHistory()
        {
            _conversationHistory.Clear();
            _inspectorConversationHistory.Clear();
            RefreshStats();
            Debug.Log("[QueryHandler] Conversation history reset.");
            MarkDirty();
        }

        public async void HandleNewMessage(string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                Debug.LogWarning("[QueryHandler] Ignoring empty message.");
                return;
            }

            Debug.Log("[QueryHandler] Received new message: " + messageContent);

            try
            {
                var userTimestamp = GetCurrentLocalAustinTimeDisplay();
                var outboundMessageHistory = BuildOutboundMessageHistory(messageContent, userTimestamp);
                var response = await _llmClient.SendPromptAsync(outboundMessageHistory);

                Debug.Log("[QueryHandler] Received response: " + response);

                AddConversationMessage("user", messageContent, userTimestamp);
                AddConversationMessage("assistant", response, GetCurrentLocalAustinTimeDisplay());
                TrimConversationHistoryToLimit();

                var cleanedResponse = await _llmClient.SendPromptAsync(_cleanerPrompt.Replace("{message}", response));
                
                _speakAndEmoteController.SendPhraseAndGetEmotion(cleanedResponse);
                _discordClient.SendDirectMessage(cleanedResponse);
            }
            catch (Exception exception)
            {
                Debug.LogError("[QueryHandler] Error processing message: " + exception);
            }
        }

        private List<OpenRouterChatClient.ChatMessage> BuildOutboundMessageHistory(string newestUserMessageContent, string newestUserTimestamp)
        {
            var outboundMessageHistory = new List<OpenRouterChatClient.ChatMessage>();

            _currentLocalAustinTimeDisplay = GetCurrentLocalAustinTimeDisplay();

            if (_includeSystemPrompt && !string.IsNullOrWhiteSpace(_systemPrompt))
            {
                var systemPromptWithTimeContext =
                    _systemPrompt + "\n\n" +
                    "Current Austin local time: " + _currentLocalAustinTimeDisplay + "\n" +
                    "Treat the timestamps in the conversation history as meaningful context.";

                outboundMessageHistory.Add(new OpenRouterChatClient.ChatMessage("system", systemPromptWithTimeContext));
            }

            for (var i = 0; i < _conversationHistory.Count; i++)
            {
                var existingMessage = _conversationHistory[i];

                outboundMessageHistory.Add(
                    new OpenRouterChatClient.ChatMessage(
                        existingMessage.Role,
                        FormatMessageForModel(existingMessage.Content, existingMessage.LocalTimestampDisplay)));
            }

            outboundMessageHistory.Add(
                new OpenRouterChatClient.ChatMessage(
                    "user",
                    FormatMessageForModel(newestUserMessageContent, newestUserTimestamp)));

            return outboundMessageHistory;
        }

        private string FormatMessageForModel(string content, string localTimestampDisplay)
        {
            return
                "<metadata>\n" +
                "local_time_austin=" + localTimestampDisplay + "\n" +
                "</metadata>\n" +
                "<message>\n" +
                content + "\n" +
                "</message>";
        }

        private void AddConversationMessage(string role, string content, string localTimestampDisplay)
        {
            var conversationMessage = new ConversationMessage(role, content, localTimestampDisplay);
            _conversationHistory.Add(conversationMessage);

            var inspectorMessage = new InspectorChatMessage(role, content, localTimestampDisplay);
            _inspectorConversationHistory.Add(inspectorMessage);

            RefreshStats();
            MarkDirty();
        }

        private void TrimConversationHistoryToLimit()
        {
            if (_maxRetainedConversationMessages <= 0)
            {
                return;
            }

            while (_conversationHistory.Count > _maxRetainedConversationMessages)
            {
                _conversationHistory.RemoveAt(0);
            }

            while (_inspectorConversationHistory.Count > _maxRetainedConversationMessages)
            {
                _inspectorConversationHistory.RemoveAt(0);
            }

            RefreshStats();
            MarkDirty();
        }

        private void RefreshStats()
        {
            _storedConversationMessageCount = _conversationHistory.Count;
            _userMessageCount = 0;
            _assistantMessageCount = 0;
            _currentLocalAustinTimeDisplay = GetCurrentLocalAustinTimeDisplay();

            for (var i = 0; i < _conversationHistory.Count; i++)
            {
                var message = _conversationHistory[i];

                if (message == null || string.IsNullOrWhiteSpace(message.Role))
                {
                    continue;
                }

                if (string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
                {
                    _userMessageCount++;
                    continue;
                }

                if (string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                {
                    _assistantMessageCount++;
                }
            }
        }

        private string GetCurrentLocalAustinTimeDisplay()
        {
            var utcNow = DateTime.UtcNow;
            var localTime = ConvertUtcToConfiguredLocalTime(utcNow);

            return localTime.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
        }

        private DateTime ConvertUtcToConfiguredLocalTime(DateTime utcDateTime)
        {
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo);
            }
            catch (TimeZoneNotFoundException)
            {
                Debug.LogWarning("[QueryHandler] Time zone not found: " + _timeZoneId + ". Falling back to local system time.");
                return utcDateTime.ToLocalTime();
            }
            catch (InvalidTimeZoneException)
            {
                Debug.LogWarning("[QueryHandler] Invalid time zone: " + _timeZoneId + ". Falling back to local system time.");
                return utcDateTime.ToLocalTime();
            }
        }

#if UNITY_EDITOR
        private void MarkDirty()
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#else
        private void MarkDirty() {}
#endif
    }
}