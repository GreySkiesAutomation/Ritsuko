
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class OpenRouterChatClient : MonoBehaviour
{
    [SerializeField] private string _model = "openai/gpt-4o-mini";
    [SerializeField] private string _baseUrl = "https://openrouter.ai/api/v1/chat/completions";

    [Header("Optional (recommended by OpenRouter)")]
    [SerializeField] private string _httpReferer = "http://localhost";
    [SerializeField] private string _xTitle = "Unity OpenRouter Client";

    [Header("Generation")]
    [SerializeField] private float _temperature = 0.7f;
    [SerializeField] private int _maxTokens = 512;

    private static HttpClient s_httpClient;
    private CancellationTokenSource _activeRequestCancellationTokenSource;

    public string Model
    {
        get { return _model; }
        set { _model = value; }
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

    public async Task<string> SendPromptAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(Secrets.OPEN_ROUTER_API_KEY))
        {
            throw new Exception("OpenRouter API key is empty.");
        }

        if (string.IsNullOrWhiteSpace(_model))
        {
            throw new Exception("Model is empty.");
        }

        EnsureHttpClientCreated();

        CancelActiveRequest();
        _activeRequestCancellationTokenSource = new CancellationTokenSource();

        var requestBody = new ChatCompletionsRequest
        {
            model = _model,
            temperature = _temperature,
            max_tokens = _maxTokens,
            messages = new ChatMessage[]
            {
                new ChatMessage
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
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
        }
        catch (TaskCanceledException)
        {
            throw new Exception("OpenRouter request canceled.");
        }
        finally
        {
            if (httpResponse != null)
            {
                httpResponse.Dispose();
            }

            httpRequest.Dispose();

            if (_activeRequestCancellationTokenSource != null)
            {
                _activeRequestCancellationTokenSource.Dispose();
                _activeRequestCancellationTokenSource = null;
            }
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new Exception("OpenRouter error: " + (int)httpResponse.StatusCode + " " + httpResponse.ReasonPhrase + "\n" + responseText);
        }

        var parsed = JsonConvert.DeserializeObject<ChatCompletionsResponse>(responseText);

        if (parsed == null || parsed.choices == null || parsed.choices.Length == 0 || parsed.choices[0] == null || parsed.choices[0].message == null)
        {
            throw new Exception("OpenRouter response missing choices/message.\n" + responseText);
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
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatCompletionsResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public ChatMessage message;
    }
}

