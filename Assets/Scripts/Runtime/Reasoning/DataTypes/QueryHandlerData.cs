using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
namespace Runtime.Reasoning.DataTypes
{
    
        [Serializable]
        public class ModelInputMessageEnvelope
        {
            public string local_time_austin;
            public string current_query_source;
            public string message;
        }

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

        [JsonConverter(typeof(StringEnumConverter))]
        public enum AssistantConversationState
        {
            Continue,
            End,
            Reset
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum AssistantEmotionResponse
        {
            Neutral,
            Pissed,
            MildlyHappy,
            Ecstatic,
            Annoyed,
            Surprised
        }

        [Serializable]
        public class StructuredAssistantResponse
        {
            public string reply;

            [JsonProperty("conversationState")]
            public AssistantConversationState ConversationState = AssistantConversationState.Continue;

            [JsonProperty("emotion")]
            public AssistantEmotionResponse Emotion = AssistantEmotionResponse.Neutral;

            [JsonProperty("toolCalls")]
            public List<StructuredToolCall> ToolCalls = new List<StructuredToolCall>();
        }

        [Serializable]
        public class StructuredToolCall
        {
            [JsonProperty("toolName")]
            public string ToolName = "None";

            [JsonProperty("toolPayload")]
            public object ToolPayload;
        }

        [Serializable]
        public class ConversationHistoryFileData
        {
            public List<ConversationMessage> ConversationHistory = new List<ConversationMessage>();
        }
}