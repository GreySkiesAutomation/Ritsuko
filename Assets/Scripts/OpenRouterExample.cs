using UnityEngine;

public class OpenRouterExample : MonoBehaviour
{
    [SerializeField] private OpenRouterChatClient _client;

    private async void Start()
    {
        var answer = await _client.SendPromptAsync("Say hi in one sentence.");
        Debug.Log(answer);
    }
}