using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
namespace DefaultNamespace
{
    public class GlobalManager : MonoBehaviour
    {
        private static GlobalManager _instance;
        public static GlobalManager I => _instance;
        
        [SerializeField] private float _quitHoldDurationSeconds = 3f;

        [SerializeField] private OpenRouterChatClient _llmClient;
        
        public OpenRouterChatClient LlmClient => _llmClient;
        
        [SerializeField] private DiscordClient _discordClient;
        
        public DiscordClient DiscordClient => _discordClient;
        
        [SerializeField] private InteractiveAvatarController _avatarController;
        
        public InteractiveAvatarController AvatarController => _avatarController;
        
        [SerializeField] private SpeakAndEmoteController _speakAndEmoteController;
        
        public SpeakAndEmoteController SpeakAndEmoteController => _speakAndEmoteController;
        
        [SerializeField] private MicrophoneInputController _microphoneInputController;
        
        public MicrophoneInputController MicrophoneInputController => _microphoneInputController;
        
        [SerializeField] private BaseTtsUnityPlayer _ttsPlayer;
        
        public BaseTtsUnityPlayer TtsPlayer => _ttsPlayer;
        
        [SerializeField] private QueryHandler _queryHandler;
        
        public QueryHandler QueryHandler => _queryHandler;
        
        [SerializeField] private WakeWordSidecarClient _wakeWordSidecarClient;
        
        public WakeWordSidecarClient WakeWordSidecarClient => _wakeWordSidecarClient;
        
        private float _escapeKeyHeldTimeSeconds;
        private bool _escapeKeyIsHeld;

        private void Start()
        {
            if(_instance != null)
            {
                Debug.LogError("Multiple instances of GlobalManager detected. There should only be one GlobalManager in the scene.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            InitializeCursor();
        }

        private void InitializeCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _escapeKeyIsHeld = true;
                _escapeKeyHeldTimeSeconds = 0f;
            }

            if (_escapeKeyIsHeld)
            {
                _escapeKeyHeldTimeSeconds += Time.deltaTime;

                if (_escapeKeyHeldTimeSeconds >= _quitHoldDurationSeconds)
                {
                    QuitApplication();
                }
            }

            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (_escapeKeyHeldTimeSeconds < _quitHoldDurationSeconds)
                {
                    ReloadScene();
                }

                _escapeKeyIsHeld = false;
            }
        }

        private void ReloadScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}