// Modified by ChatGPT and Riko Balakit / Pearl Grey

using System.Collections.Generic;
using NaughtyAttributes;
using Runtime.Avatar.Data;
using UnityEngine;
using UnityEngine.Serialization;
namespace Runtime.Avatar
{
    public class InteractiveAvatarController : PearlBehaviour
    {
        [FormerlySerializedAs("emotionSets")]
        [SerializeField] private EmotionSet[] _emotionSets;

        [SerializeField] private SkinnedMeshRenderer _faceMesh;
        [SerializeField] private Animator _bodyAnimator;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private float _secondsUntilNeutralAfterAudioStops = 1.0f;
        [SerializeField] private float _minimumSecondsToHoldEmotionBeforeNeutralReset = 1.0f;

        private Dictionary<string, int> _blendshapeParameters = new Dictionary<string, int>();
        private float _audioStoppedTimestampSeconds = -1.0f;
        private float _lastEmotionSetTimestampSeconds = -999.0f;
        private Emotion _currentEmotion = Emotion.Neutral;
        
        public AudioSource AudioSource => _audioSource;

        [Button]
        public void SetEmotionNeutral()
        {
            ApplyEmotionSet(GetEmotionSet(Emotion.Neutral));
        }

        [Button]
        public void SetEmotionPissed()
        {
            ApplyEmotionSet(GetEmotionSet(Emotion.Pissed));
        }

        [Button]
        public void SetEmotionGlad()
        {
            ApplyEmotionSet(GetEmotionSet(Emotion.Glad));
        }

        [Button]
        public void SetEmotionEcstatic()
        {
            ApplyEmotionSet(GetEmotionSet(Emotion.EcstaticHappy));
        }

        [Button]
        public void SetEmotionAnnoyed()
        {
            ApplyEmotionSet(GetEmotionSet(Emotion.Annoyed));
        }

        [Button]
        public void SetEmotionSurprised()
        {
            ApplyEmotionSet(GetEmotionSet(Emotion.Surprised));
        }

        private void Start()
        {
            InitializeFaceBlendshapeCache();
            SetEmotionNeutral();
            SetInitialized();
        }

        private void Update()
        {
            if (_audioSource == null)
            {
                return;
            }

            if (_audioSource.isPlaying)
            {
                _audioStoppedTimestampSeconds = -1.0f;
                return;
            }

            if (_audioStoppedTimestampSeconds < 0.0f)
            {
                _audioStoppedTimestampSeconds = Time.time;
                return;
            }

            var hasHeldEmotionLongEnough =
                Time.time - _lastEmotionSetTimestampSeconds >= _minimumSecondsToHoldEmotionBeforeNeutralReset;

            var hasAudioBeenStoppedLongEnough =
                Time.time - _audioStoppedTimestampSeconds >= _secondsUntilNeutralAfterAudioStops;

            if (_currentEmotion != Emotion.Neutral &&
                hasHeldEmotionLongEnough &&
                hasAudioBeenStoppedLongEnough)
            {
                SetEmotionNeutral();
            }
        }

        private void InitializeFaceBlendshapeCache()
        {
            _blendshapeParameters.Clear();
            var numberOfBlendshapes = _faceMesh.sharedMesh.blendShapeCount;

            for (var i = 0; i < numberOfBlendshapes; i++)
            {
                var nameOfBlendshape = _faceMesh.sharedMesh.GetBlendShapeName(i);
                _blendshapeParameters.Add(nameOfBlendshape, i);
            }
        }

        private void ApplyEmotionSet(EmotionSet emotionSet)
        {
            if (emotionSet == null)
            {
                return;
            }

            _currentEmotion = emotionSet.Emotion;
            _lastEmotionSetTimestampSeconds = Time.time;

            ApplyFacePreset(emotionSet.FaceBlendshape);
            _bodyAnimator.SetTrigger(emotionSet.AnimationTriggerName);
        }

        private void ApplyFacePreset(FaceBlendshapePreset facePreset)
        {
            foreach (var blendshapeSetting in facePreset.Settings)
            {
                _faceMesh.SetBlendShapeWeight(_blendshapeParameters[blendshapeSetting.Name], blendshapeSetting.Weight);
            }
        }

        private EmotionSet GetEmotionSet(Emotion emotion)
        {
            foreach (var emotionSet in _emotionSets)
            {
                if (emotionSet.Emotion == emotion)
                {
                    return emotionSet;
                }
            }

            LogError($"Could not find emotion {emotion}");
            return null;
        }
    }
}

// Modified by ChatGPT and Riko Balakit / Pearl Grey