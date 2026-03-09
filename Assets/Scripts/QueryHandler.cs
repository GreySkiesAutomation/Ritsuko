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

        /*[TextArea(6, 14)]
        [SerializeField]*/
        private string _systemPrompt =
            "You are a personal assistant focused on the user's productivity, motivation, and attention. " +
            "You have a personality that is playful and supportive by default, condescending and punitive when the user gets off track for unprofessional reasons, but caring if the user says they are genuinely stressed." +
            "Reply in 1 short sentence. Be direct, helpful, and action-oriented. " +
            "If the task is to create a to do list, do not ask for an explicit number of items and do not suggest any items for the to-do list- the user is planning to give tasks they have already decided. " +
            "All timestamps in the conversation history are local time. " +
            "You may use time-of-day and elapsed time between messages as context for tone and urgency. " +
            "However, the user may activate TEST TIME MODE by including text like '(Override time: 5:20PM)' in their message. " +
            "When an override time is present, treat that time as the current local Austin time. " +
            "In TEST TIME MODE you should ignore message timestamps and instead assume the override time is the real current time. " +
            "This override exists only for simulation/testing of morning, afternoon, late night, and deadline scenarios. " +
            "Do not mention timestamps or override mode unless it is relevant to the conversation." +
            "Each conversation message may begin with metadata like [Austin Local Time: ...]. " +
            "That metadata is for reasoning only and must never be spoken, quoted, paraphrased, or included in the reply. " +
            "Respond only to the human message content itself unless the time context is useful implicitly. " +
            "User can have strange schedules so do not assume that a to-do list made late at night is meant for the next day. " +
            "If time context matters, naturally refer to it like 'this morning' or 'late tonight' instead of repeating raw timestamps. " +
            "Timestamps are particularly useful for understanding task urgency, especially if an urgent task has not been completed by the time of the next messages' timestamps."+
            "If the user's latest message indicates the user wants to end the conversation, do not try to push for more engagement. " +
            "If the user requests something about resetting the message history, just reply back that you are resetting the message history (as a second LLM reasoning layer handles this).";

        private string _cleanerPrompt =
            "You will be given a message that may contain a timestamp the assistant's actual spoken reply. " +
            "Return only the exact user-facing reply that should be spoken aloud. This can include instructions such as adding things to a to-do list. Just strip out the timestamp-related metadata" +
            "Do not add quotes, explanations, prefixes, or suffixes. " +
            "If the message is already clean, return it unchanged.\n\n" +
            "Message to clean:\n{message}";

        private string _conversationStateHandlerPrompt =
            "Given this message: '{message}'\n" +
            "If the message indicates the user wants to end the conversation, reply with 'END'. " +
            "This includes indicators that the to-do list is completed and the user is ready to start." +
            "Examples of messages that indicate ending the conversation: 'That's all for now', 'Goodbye', 'End of conversation', 'No more questions', 'I'm done', 'Thanks, that's it', etc. " +
            "If the message indicates the user wants to reset the conversation history, reply with 'RESET'" +
            "Examples of messages that indicate resetting conversation history: 'Reset conversation`, `Reset history`, `Clear history`, `Clear conversation history` " +
            "This must be a clear explicit request to reset or clear history, not just a vague message that could be interpreted as a reset request. " +
            "If the user's message indicates the conversaion should continue going, reply with 'CONTINUE'" +
            "Do not use any other words besides 'END', `RESET`, or 'CONTINUE' in your reply." +
            "The user has the ability to continue the conversation easily by repeating her name, so do not be afraid to end the conversation if you think it is appropriate.";


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

        public async void HandleNewMessage(string messageContent, QuerySource source)
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

                var endDetectionResponse = await _llmClient.SendPromptAsync(_conversationStateHandlerPrompt.Replace("{message}", messageContent));
                Debug.Log("[QueryHandler] Conversation state handler response: " + endDetectionResponse);

                if (string.Equals(endDetectionResponse.Trim(), "END", StringComparison.OrdinalIgnoreCase))
                {
                    if (source == QuerySource.Discord)
                    {
                        _discordClient.SendDirectMessage(cleanedResponse);
                    }
                    else if (source == QuerySource.Microphone)
                    {
                        _speakAndEmoteController.SendPhraseAndGetEmotion(cleanedResponse, false);
                    }

                    Debug.Log("[QueryHandler] Detected end of conversation.");
                }
                else
                {
                    if (string.Equals(endDetectionResponse.Trim(), "RESET", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("[QueryHandler] Detected conversation reset request. Resetting history.");
                        ResetHistory();
                    }

                    if (source == QuerySource.Discord)
                    {
                        _discordClient.SendDirectMessage(cleanedResponse);
                    }
                    else if (source == QuerySource.Microphone)
                    {
                        _speakAndEmoteController.SendPhraseAndGetEmotion(cleanedResponse, QuerySource.Microphone == source);
                    }
                }
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
                return utcDateTime.ToLocalTime();
            }
            catch (InvalidTimeZoneException)
            {
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