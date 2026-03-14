using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
namespace Runtime
{
    public class PearlBehaviour : MonoBehaviour
    {

        #region Serialized Fields

        #region Protected Fields

        [SerializeField]
        protected LogLevel CurrentLogLevel = LogLevel.Warning;

        #endregion

        #endregion

        #region Public Properties

        public bool IsInitialized { get; private set; } = false;

        #endregion

        #region Protected Properties

        protected string StatusReadOnly
        {
            get { return m_statusReadOnly; }
        }

        #endregion

        #region Public Enums

        public enum LogLevel
        {

            None = 0,
            Error = 1,
            Warning = 2,
            All = 3

        }

        public enum LogType
        {

            Error = 0,
            Warning = 1,
            General = 2

        }

        #endregion

        #region Private Fields

        [FormerlySerializedAs("m_status")]
        [SerializeField]
        private string m_statusReadOnly = string.Empty;

        private readonly List<string> m_logOnceList = new List<string>();

        #endregion

        #region Public Methods

        public void SetLogLevel(LogLevel logLevel)
        {
            CurrentLogLevel = logLevel;
        }

        #endregion
        
        #region Protected Methods
        
        protected void SetInitialized()
        {
            SetStatus(StandardStatus.INITIALIZATION_FINISHED, LogType.General);
            IsInitialized = true;
        }

        protected void SetInitialized(string customMessage)
        {
            SetStatus(customMessage, LogType.General);
            IsInitialized = true;
        }

        protected void SetStatus(string statusText)
        {
            SetStatus(statusText, LogType.General);
        }

        protected void SetStatus(string statusText, LogType logType)
        {
            m_statusReadOnly = statusText;

            if (statusText == StandardStatus.INITIALIZATION_FINISHED)
            {
                IsInitialized = true;
            }

            Log(statusText, logType);
        }

        // TODO-RPB: Add a filter and option for repetitive statuses.

        protected void Log(string logText)
        {
            Log(logText, LogType.General);
        }
        
        protected void LogError(string logText)
        {
            Log(logText, LogType.Error);
        }

        protected void LogWarning(string logText)
        {
            Log(logText, LogType.Warning);
        }
        protected void Log(string logText, LogType logType)
        {
            var formattedLogText = $"[{gameObject.name}.{GetType().Name}] {logText}";

            if (logType == LogType.Error && (CurrentLogLevel == LogLevel.All || CurrentLogLevel == LogLevel.Warning || CurrentLogLevel == LogLevel.Error))
            {
                Debug.LogError(formattedLogText);
            }
            else if (logType == LogType.Warning && (CurrentLogLevel == LogLevel.All || CurrentLogLevel == LogLevel.Warning))
            {
                Debug.LogWarning(formattedLogText);
            }
            else if (logType == LogType.General && CurrentLogLevel == LogLevel.All)
            {
                Debug.Log(formattedLogText);
            }
        }

        protected void LogOnce(string logText)
        {
            LogOnce(logText, LogType.General);
        }

        protected void LogOnce(string logText, LogType logType)
        {
            if (m_logOnceList.Contains(logText))
            {
                // RPB: Skip since we have already logged this before.
                return;
            }

            m_logOnceList.Add(logText);

            Log(logText, logType);
        }
        
        protected void LogThreadUnsafe(string logText)
        {
            LogThreadUnsafe(logText, LogType.General);
        }
        
        protected void LogThreadUnsafeError(string logText)
        {
            LogThreadUnsafe(logText, LogType.Error);
        }

        protected void LogThreadUnsafeWarning(string logText)
        {
            LogThreadUnsafe(logText, LogType.Warning);
        }
        protected void LogThreadUnsafe(string logText, LogType logType)
        {
            var formattedLogText = $"[{GetType().Name}] {logText}";

            if (logType == LogType.Error && (CurrentLogLevel == LogLevel.All || CurrentLogLevel == LogLevel.Warning || CurrentLogLevel == LogLevel.Error))
            {
                Debug.LogError(formattedLogText);
            }
            else if (logType == LogType.Warning && (CurrentLogLevel == LogLevel.All || CurrentLogLevel == LogLevel.Warning))
            {
                Debug.LogWarning(formattedLogText);
            }
            else if (logType == LogType.General && CurrentLogLevel == LogLevel.All)
            {
                Debug.Log(formattedLogText);
            }
        }

        protected void LogThreadUnsafeOnce(string logText)
        {
            LogThreadUnsafeOnce(logText, LogType.General);
        }

        protected void LogThreadUnsafeOnce(string logText, LogType logType)
        {
            if (m_logOnceList.Contains(logText))
            {
                // RPB: Skip since we have already logged this before.
                return;
            }

            m_logOnceList.Add(logText);

            Log(logText, logType);
        }

        protected virtual void OnDestroy()
        {
            if (!IsInitialized)
            {
                if (string.IsNullOrEmpty(m_statusReadOnly))
                {
                    // RPB: Means this was never used in the first place
                    Log("Never used Initialization/Status by end of runtime", LogType.Warning);
                }
                else
                {
                    // RPB: Means initialization failed
                    Log($"Initialization incomplete by end of runtime: {m_statusReadOnly}", LogType.Error);
                }
            }
        }

        #endregion

    }
}