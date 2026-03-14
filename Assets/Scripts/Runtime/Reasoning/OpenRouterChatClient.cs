// Modified by ChatGPT and Riko Balakit/Pearl Grey

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Configuration;
using Newtonsoft.Json;
using UnityEngine;

namespace Runtime.Reasoning
{
    public class OpenRouterChatClient : PearlBehaviour
    {
        [Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;

            public ChatMessage()
            {
            }

            public ChatMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        private string _baseUrl => GlobalManager.I.Configuration.OpenRouterBaseUrl;

        [Header("Optional (recommended by OpenRouter)")]
        [SerializeField] private string _httpReferer = "http://localhost";
        [SerializeField] private string _xTitle = "Unity OpenRouter Client";
        [SerializeField] private bool _logFullInputPayload = true;
        [SerializeField] private bool _logFullResponsePayload = true;

        private static HttpClient s_httpClient;
        private CancellationTokenSource _activeRequestCancellationTokenSource;

        private void Start()
        {
            SetInitialized();
        }

        public void CancelActiveRequest()
        {
            if (_activeRequestCancellationTokenSource != null)
            {
                _activeRequestCancellationTokenSource.Cancel();
                _activeRequestCancellationTokenSource.Dispose();
                _activeRequestCancellationTokenSource = null;
            }
        }

        public Task<string> SendPromptAsync(string prompt, LlmConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt is null or whitespace.", nameof(prompt));
            }

            return SendPromptAsync(new List<ChatMessage>()
            {
                new ChatMessage("user", prompt)
            }, configuration);
        }

        public async Task<string> SendPromptAsync(IReadOnlyList<ChatMessage> messageHistory, LlmConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(Secrets.OPEN_ROUTER_API_KEY))
            {
                throw new Exception("OpenRouter API key is empty.");
            }

            var selectedModel = configuration.Model;
            var selectedTemperature = configuration.Temperature;
            var selectedMaxTokens = configuration.MaxTokens;

            if (string.IsNullOrWhiteSpace(selectedModel))
            {
                throw new Exception("Model is empty.");
            }

            if (messageHistory == null || messageHistory.Count == 0)
            {
                throw new Exception("Message history is empty.");
            }

            EnsureHttpClientCreated();

            CancelActiveRequest();
            _activeRequestCancellationTokenSource = new CancellationTokenSource();

            var copiedMessages = new ChatMessage[messageHistory.Count];

            for (var i = 0; i < messageHistory.Count; i++)
            {
                var sourceMessage = messageHistory[i];

                if (sourceMessage == null)
                {
                    throw new Exception("Message history contains a null message at index " + i + ".");
                }

                copiedMessages[i] = new ChatMessage(sourceMessage.role, sourceMessage.content);
            }

            var requestBody = new ChatCompletionsRequest
            {
                model = selectedModel,
                temperature = selectedTemperature,
                max_tokens = selectedMaxTokens,
                messages = copiedMessages
            };

            var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);

            if (_logFullInputPayload)
            {
                Debug.Log("[OpenRouterChatClient] Full input payload:\n" + json);
            }

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.OPEN_ROUTER_API_KEY);

            if (!string.IsNullOrWhiteSpace(_httpReferer))
            {
                httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", _httpReferer);
            }

            if (!string.IsNullOrWhiteSpace(_xTitle))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-Title", _xTitle);
            }

            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponse = null;
            string responseText = null;

            try
            {
                httpResponse = await s_httpClient.SendAsync(httpRequest, _activeRequestCancellationTokenSource.Token);
                responseText = await httpResponse.Content.ReadAsStringAsync();

                if (_logFullResponsePayload)
                {
                    Debug.Log("[OpenRouterChatClient] Full response payload:\n" + responseText);
                }
            }
            catch (TaskCanceledException)
            {
                throw new Exception("OpenRouter request canceled.");
            }
            finally
            {
                httpRequest.Dispose();

                if (_activeRequestCancellationTokenSource != null)
                {
                    _activeRequestCancellationTokenSource.Dispose();
                    _activeRequestCancellationTokenSource = null;
                }
            }

            if (httpResponse == null)
            {
                throw new Exception("OpenRouter response was null.");
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorMessage = "OpenRouter error: " + (int)httpResponse.StatusCode + " " + httpResponse.ReasonPhrase + "\n" + responseText;
                httpResponse.Dispose();
                throw new Exception(errorMessage);
            }

            var parsed = JsonConvert.DeserializeObject<ChatCompletionsResponse>(responseText);

            httpResponse.Dispose();

            if (parsed == null ||
                parsed.choices == null ||
                parsed.choices.Length == 0 ||
                parsed.choices[0] == null ||
                parsed.choices[0].message == null)
            {
                throw new Exception("OpenRouter response missing choices/message.\n" + responseText);
            }

            if (parsed.usage != null)
            {
                Debug.Log(
                    "[OpenRouterChatClient] Model: " + configuration.Model +
                    ", Prompt tokens: " + parsed.usage.prompt_tokens +
                    ", Completion tokens: " + parsed.usage.completion_tokens +
                    ", Total tokens: " + parsed.usage.total_tokens);
            }

            return parsed.choices[0].message.content ?? "";
        }

        private static void EnsureHttpClientCreated()
        {
            if (s_httpClient != null)
            {
                return;
            }

            s_httpClient = new HttpClient();
            s_httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        [Serializable]
        private class ChatCompletionsRequest
        {
            public string model;
            public float temperature;
            public int max_tokens;
            public ChatMessage[] messages;
        }

        [Serializable]
        private class ChatCompletionsResponse
        {
            public Choice[] choices;
            public Usage usage;
        }

        [Serializable]
        private class Choice
        {
            public ChatMessage message;
        }

        [Serializable]
        private class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }
}

// Modified by ChatGPT and Riko Balakit/Pearl Grey