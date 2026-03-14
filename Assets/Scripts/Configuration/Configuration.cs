using Runtime;
using UnityEngine;
using UnityEngine.Serialization;
namespace Configuration
{
    [CreateAssetMenu(fileName = "New Global Configuration", menuName = "Ritsuko/GlobalConfiguration")]
    public class Configuration : ScriptableObject
    {
        [Header("General")]
        public float MinutesToAssumeUserWentToSleep = 120f;
        
        [Header("Prompts")]
        public GlobalPromptConfiguration GlobalPromptConfiguration;

        public ModePromptConfiguration DefaultModePromptConfiguration;
        public ModePromptConfiguration[] ModePromptConfigurations;

        [Header("Wake Word Detection Sidecar")]
        public string WakeWordHostIpAddress = "127.0.0.1";
        public int WakeWordHostPort = 17777;
        
        [Header("Discord Sidecar")]
        public string DiscordSidecarBaseUrlAndPort = "http://127.0.0.1:5099";

        public float DiscordPollIntervalSeconds = 0.1f;
        public float HeartbeatIntervalSeconds = 5f;
        
        [Header("Speech To Text")]
        public float DelayToPromptSttSeconds = 0.5f;

        [Header("Query Handler")]
        public int MaxRetainedConversationMessages = 100;
        public int MaxJsonParseRetries = 5;
        public string ConversationHistoryFileName = "query_handler_history.json";
        
        [Header("Azure TTS")]
        public string AzureTtsRegion = "eastus";
        public string AzureTtsVoiceName = "en-US-PhoebeMultilingualNeural";

        [Header("ElevenLabs TTS")]
        public string ElevenLabsTtsVoiceId = "EXAVITQu4vr4xnSDxMaL";
        public string ElevenLabsTtsVoiceSexyId = "EXAVITQu4vr4xnSDxMaL";
        public string ElevenLabsTtsModelId = "eleven_flash_v2";
        
        [Header("OpenRouter LLM")]
        public string OpenRouterBaseUrl = "https://openrouter.ai/api/v1/chat/completions";

        public LlmConfiguration LlmModelStandard;
        public LlmConfiguration LlmModelHighReasoning;
        public LlmConfiguration LlmModelFast;
        
        [Header("OpenAI SpeechToText")]
        public string SttModelName = "gpt-4o-transcribe";
        public float SttPrerollSeconds = 0.5f;
        
        [Header("Microphone Input Controller")]
        public int MicrophoneLoopLengthSeconds = 300;
        public int RecordingSampleRate = 16000;
        public float SpeechAmplitudeThreshold = 0.06f;
        public float SpeechEndDetectionSeconds = 5.0f;
        public int AmplitudeSampleWindowSizePerChannel = 1024;
        
        [Header("CV Presence Detection")]
        public string CVStatusUrl = "http://127.0.0.1:8002/presence";
        public float CVPollIntervalSeconds = 1.0f;
        public float CVPollRequestTimeoutSeconds = 2.0f;

        public ModePromptConfiguration GetCurrentModePromptConfiguration()
        {
            foreach(var modePromptConfiguration in ModePromptConfigurations)
            {
                if (modePromptConfiguration.BehaviourMode == GlobalManager.I.State.CurrentMode)
                {
                    return modePromptConfiguration;
                }
            }
            
            return DefaultModePromptConfiguration;
        }

    }
}