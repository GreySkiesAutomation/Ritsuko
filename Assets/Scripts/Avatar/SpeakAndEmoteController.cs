using System.Threading.Tasks;
using DefaultNamespace;
using NaughtyAttributes;
using TMPro;
using UnityEngine;


public class SpeakAndEmoteController : MonoBehaviour
{
    [Header("Stuff she says")]
    [TextArea(6, 20)]
    [SerializeField] private string _thingToSpeak;
    
    [Header("Client")]
    [SerializeField] private bool _isWaiting;
    
    [Header("Reply")]
    [ReadOnly]
    [TextArea(20, 20)]
    [SerializeField] private string _reply;
    
    [SerializeField] private TMP_Text _dialogText;
    
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

        if (GlobalManager.I.LlmClient == null)
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
            var result = await GlobalManager.I.LlmClient.SendPromptAsync($"Given this dialogue: {_thingToSpeak}\n" +
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
                    GlobalManager.I.AvatarController.SetEmotionNeutral();
                    break;
                case "1":
                    GlobalManager.I.AvatarController.SetEmotionPissed();
                    break;
                case "2":
                    GlobalManager.I.AvatarController.SetEmotionGlad();
                    break;
                case "3":
                    GlobalManager.I.AvatarController.SetEmotionEcstatic();
                    break;
                case "4":
                    GlobalManager.I.AvatarController.SetEmotionAnnoyed();
                    break;
                case "5":
                    GlobalManager.I.AvatarController.SetEmotionSurprised();
                    break;
            }
            
            _dialogText.text = _thingToSpeak;
            
            GlobalManager.I.TtsPlayer.GenerateAndPlay(_thingToSpeak, promptAfterwards);
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
