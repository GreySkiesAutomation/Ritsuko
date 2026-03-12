using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace Runtime.Tooling
{
    public class OpenClawTool : BaseTool
    {
        private const float CONNECTION_CHECK_INTERVAL_SECONDS = 10f;
        private const float ERROR_BOX_DURATION_SECONDS = 10f;

        [Header("Connection UI")]

        [SerializeField]
        private GameObject _openClawErrorBox;

        [Header("Connection Config")]

        [SerializeField]
        private string _openClawHealthEndpoint = "http://localhost:3000/health";

        private bool _isConnected;

        private Coroutine _connectionCheckCoroutine;
        private Coroutine _errorDisplayCoroutine;

        public bool IsConnected => _isConnected;
        
        public override void Initialize()
        {
            SetConnectionState(false);

            if (_connectionCheckCoroutine != null)
            {
                StopCoroutine(_connectionCheckCoroutine);
            }

            _connectionCheckCoroutine = StartCoroutine(ConnectionCheckCoroutine());

            SetInitialized();
        }

        public override bool TryExecute(string payloadJson, out string executionSummary)
        {
            throw new System.NotImplementedException();
        }
        
        
        private IEnumerator ConnectionCheckCoroutine()
        {
            while (true)
            {
                yield return CheckConnectionCoroutine();

                var startTime = DateTime.UtcNow;

                while ((DateTime.UtcNow - startTime).TotalSeconds < CONNECTION_CHECK_INTERVAL_SECONDS)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator CheckConnectionCoroutine()
        {
            using (var request = UnityWebRequest.Get(_openClawHealthEndpoint))
            {
                request.timeout = 3;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    SetConnectionState(true);
                }
                else
                {
                    SetConnectionState(false);
                }
            }
        }

        private void SetConnectionState(bool connected)
        {
            _isConnected = connected;

            GlobalManager.I.State.OpenClawConnected = connected;
        }

        public void ReportConnectionError()
        {
            if (_errorDisplayCoroutine != null)
            {
                StopCoroutine(_errorDisplayCoroutine);
            }

            _errorDisplayCoroutine = StartCoroutine(ShowErrorBoxCoroutine());
        }

        private IEnumerator ShowErrorBoxCoroutine()
        {
            if (_openClawErrorBox != null)
            {
                _openClawErrorBox.SetActive(true);
            }

            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalSeconds < ERROR_BOX_DURATION_SECONDS)
            {
                yield return null;
            }

            if (_openClawErrorBox != null)
            {
                _openClawErrorBox.SetActive(false);
            }
        }
    }
}