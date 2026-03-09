using System.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;


public class SpeakAndEmoteController : MonoBehaviour
{
    [Header("Stuff she says")]
    [TextArea(6, 20)]
    [SerializeField] private string _thingToSpeak;
    
    [Header("Client")]
    [SerializeField] private OpenRouterChatClient _client;

    [SerializeField] private bool _isWaiting;
    
    [Header("Reply")]
    [ReadOnly]
    [TextArea(20, 20)]
    [SerializeField] private string _reply;
    
    [SerializeField] private InteractiveAvatarController _interactiveAvatarController;
    
    [SerializeField] private TMP_Text _dialogText;
    
    [SerializeField] private BaseTtsUnityPlayer _ttsUnityPlayer;

    public void SendPhraseAndGetEmotion(string thingToSpeak, bool promptAfterwards = false)
    {
        _thingToSpeak = thingToSpeak;
        SendAndGetEmotion(promptAfterwards);
    }
    
    [Button("Send And Wait For Answer")]
    private void SendAndGetEmotion(bool promptAfterwards = false)
    {
        if (_isWaiting)
        {
            Debug.LogWarning("Already waiting for response.");
            return;
        }

        if (_client == null)
        {
            Debug.LogError("No OpenRouterChatClient assigned.");
            return;
        }

        _ = SendRoutine(promptAfterwards);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async Task SendRoutine(bool promptAfterwards = false)
    {
        _isWaiting = true;
        MarkDirty();

        try
        {
            var result = await _client.SendPromptAsync($"Given this dialogue: {_thingToSpeak}\n" +
                                                       "Which of these emotions fits it best? Only return the number of the emotion:\n" +
                                                       "0: Neutral\n"+
                                                       "1: Pissed\n"+
                                                       "2: Mildly Happy\n"+
                                                       "3: Infatuated, or Surprised (positive)\n"+
                                                       "4: Annoyed\n"+
                                                       "5: Surprised (negative)\n");
            _reply = result;

            switch (_reply)
            {
                case "0":
                    _interactiveAvatarController.SetEmotionNeutral();
                    break;
                case "1":
                    _interactiveAvatarController.SetEmotionPissed();
                    break;
                case "2":
                    _interactiveAvatarController.SetEmotionGlad();
                    break;
                case "3":
                    _interactiveAvatarController.SetEmotionEcstatic();
                    break;
                case "4":
                    _interactiveAvatarController.SetEmotionAnnoyed();
                    break;
                case "5":
                    _interactiveAvatarController.SetEmotionSurprised();
                    break;
            }
            
            _dialogText.text = _thingToSpeak;
            
            _ttsUnityPlayer.GenerateAndPlay(_thingToSpeak, promptAfterwards);
        }
        catch (System.Exception e)
        {
            _reply = "ERROR:\n" + e.Message;
        }

        _isWaiting = false;
        MarkDirty();
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
