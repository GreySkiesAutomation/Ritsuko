// Modified by ChatGPT and Riko Balakit / Pearl Grey

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
namespace Runtime.Environment
{
    public class TimeSceneController : PearlBehaviour
    {
        [FormerlySerializedAs("testTimeScale")] [SerializeField] private TestTimeScale _testTimeScale;

        [FormerlySerializedAs("targetRenderer")] [SerializeField] private Renderer _targetRenderer;
        [FormerlySerializedAs("targetLight")] [SerializeField] private Light _targetLight;

        [FormerlySerializedAs("timeScenes")] [SerializeField] private TimeScene[] _timeScenes;
        [FormerlySerializedAs("timeText")] [SerializeField] private TMP_Text _timeText;

        private TimeScene[] _sortedTimeScenes;
        private TimeScene _currentTimeScene;
        private float _currentLocalTimeNormalized;

        private void Start()
        {
            SortTimeScenes();
            UpdateLocalTimeNormalized();
            SetMaterialAndLightColor();
            SetInitialized();
        }

        private void Update()
        {
            UpdateLocalTimeNormalized();
            SetMaterialAndLightColor();
        }

        private void SortTimeScenes()
        {
            if (_timeScenes == null || _timeScenes.Length == 0)
            {
                _sortedTimeScenes = Array.Empty<TimeScene>();
                _currentTimeScene = null;
                return;
            }

            _sortedTimeScenes = new TimeScene[_timeScenes.Length];
            Array.Copy(_timeScenes, _sortedTimeScenes, _timeScenes.Length);

            // Simple in-place sort (no LINQ)
            for (var i = 0; i < _sortedTimeScenes.Length - 1; i++)
            {
                for (var j = i + 1; j < _sortedTimeScenes.Length; j++)
                {
                    if (_sortedTimeScenes[j] == null)
                    {
                        continue;
                    }

                    if (_sortedTimeScenes[i] == null)
                    {
                        var tempNullSwap = _sortedTimeScenes[i];
                        _sortedTimeScenes[i] = _sortedTimeScenes[j];
                        _sortedTimeScenes[j] = tempNullSwap;
                        continue;
                    }

                    if (_sortedTimeScenes[j].timeToStartHours < _sortedTimeScenes[i].timeToStartHours)
                    {
                        var temp = _sortedTimeScenes[i];
                        _sortedTimeScenes[i] = _sortedTimeScenes[j];
                        _sortedTimeScenes[j] = temp;
                    }
                }
            }
        }

        private void UpdateLocalTimeNormalized()
        {
            if (_testTimeScale == TestTimeScale.Hours)
            {
                _currentLocalTimeNormalized = (float)DateTime.Now.Hour / 24f;
            }
            else if (_testTimeScale == TestTimeScale.Minutes)
            {
                _currentLocalTimeNormalized = (float)DateTime.Now.Minute / 60f;
            }
            else if (_testTimeScale == TestTimeScale.Seconds)
            {
                _currentLocalTimeNormalized = (float)DateTime.Now.Second / 60f;
            }

            _currentLocalTimeNormalized = Mathf.Repeat(_currentLocalTimeNormalized, 1f);

            if (_timeText != null)
            {
                _timeText.text = $"{DateTime.Now.DayOfWeek}, {DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour:00}:{DateTime.Now.Minute:00}:{DateTime.Now.Second:00}";
            }

            //Log(_currentLocalTimeNormalized);
        }

        private float GetStartTimeNormalized(TimeScene timeScene)
        {
            if (timeScene == null)
            {
                return 0f;
            }
        
            return Mathf.Repeat((float)timeScene.timeToStartHours / 24f, 1f);
        }

        private void SetMaterialAndLightColor()
        {
            if (_sortedTimeScenes == null || _sortedTimeScenes.Length == 0)
            {
                return;
            }

            // Pick the last scene whose start <= current time (wraps around by defaulting to last)
            TimeScene nextTimeScene = null;

            for (var i = 0; i < _sortedTimeScenes.Length; i++)
            {
                var timeScene = _sortedTimeScenes[i];
                if (timeScene == null)
                {
                    continue;
                }

                var startTimeNormalized = GetStartTimeNormalized(timeScene);

                if (_currentLocalTimeNormalized >= startTimeNormalized)
                {
                    nextTimeScene = timeScene;
                }
            }

            if (nextTimeScene == null)
            {
                // Wrap-around case: current time is before the first start time
                for (var i = _sortedTimeScenes.Length - 1; i >= 0; i--)
                {
                    if (_sortedTimeScenes[i] != null)
                    {
                        nextTimeScene = _sortedTimeScenes[i];
                        break;
                    }
                }
            }

            if (nextTimeScene == null)
            {
                return;
            }

            if (ReferenceEquals(_currentTimeScene, nextTimeScene))
            {
                return;
            }

            _currentTimeScene = nextTimeScene;

            if (_targetRenderer != null && _currentTimeScene.backgroundMaterial != null)
            {
                // Use sharedMaterial to avoid instancing/leaks for a background renderer.
                _targetRenderer.sharedMaterial = _currentTimeScene.backgroundMaterial;
            }

            if (_targetLight != null)
            {
                _targetLight.color = _currentTimeScene.lightColor;
            }

            if (_timeText != null)
            {
                _timeText.color = _currentTimeScene.timeColor;
            }
        }
    }

    public enum TestTimeScale
    {
        Hours,
        Minutes,
        Seconds
    }

    [Serializable]
    public class TimeScene
    {
        public Material backgroundMaterial;
        public Color lightColor;
        public int timeToStartHours;
        public Color timeColor;
    }

// Modified by ChatGPT and Riko Balakit / Pearl Grey
}