using System;
using Configuration;
using Runtime.Inputs.Presence;
using UnityEngine;
using UnityEngine.Serialization;
namespace Runtime.UI
{
    public class StateIconController : PearlBehaviour
    {
        [SerializeField]
        private GameObject _modeProductiveWorkIcon;

        [SerializeField]
        private GameObject _modeProductiveHobbyIcon;

        [SerializeField]
        private GameObject _modeChillIcon;
        
        [SerializeField]
        private GameObject _modeDomesticIcon;

        [SerializeField]
        private GameObject _quietModeEnabledIcon;

        [SerializeField]
        private GameObject _quietModeDisabledIcon;

        [SerializeField]
        private GameObject _respondToNameEnabledIcon;

        [SerializeField]
        private GameObject _respondToNameDisabledIcon;

        [SerializeField]
        private GameObject _auditModeEnabledIcon;

        [SerializeField]
        private GameObject _auditModeDisabledIcon;

        [SerializeField]
        private GameObject _presencePresentIcon;

        [SerializeField]
        private GameObject _presenceAbsentIcon;

        [SerializeField]
        private GameObject _presenceUnknownIcon;
        
        [SerializeField]
        private GameObject _openClawConnectedIcon;

        [SerializeField]
        private GameObject _openClawDisconnectedIcon;

        private void Start()
        {
            SetInitialized();
        }
        private void Update()
        {
            _modeProductiveWorkIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.ProductiveWork);
            _modeProductiveHobbyIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.ProductiveHobby);
            _modeChillIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.Chill);
            _modeDomesticIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.ProductiveDomestic);

            _quietModeEnabledIcon.SetActive(GlobalManager.I.State.QuietModeEnabled);
            _quietModeDisabledIcon.SetActive(!GlobalManager.I.State.QuietModeEnabled);

            _respondToNameEnabledIcon.SetActive(GlobalManager.I.State.RespondToNameEnabled);
            _respondToNameDisabledIcon.SetActive(!GlobalManager.I.State.RespondToNameEnabled);

            _auditModeEnabledIcon.SetActive(GlobalManager.I.State.AuditModeEnabled);
            _auditModeDisabledIcon.SetActive(!GlobalManager.I.State.AuditModeEnabled);

            _presencePresentIcon.SetActive(GlobalManager.I.State.PresenceState == PresenceState.Present);
            _presenceAbsentIcon.SetActive(GlobalManager.I.State.PresenceState == PresenceState.Absent);
            _presenceUnknownIcon.SetActive(GlobalManager.I.State.PresenceState == PresenceState.Unknown);
            
            _openClawConnectedIcon.SetActive(GlobalManager.I.State.OpenClawConnected);
            _openClawDisconnectedIcon.SetActive(!GlobalManager.I.State.OpenClawConnected);
        }
    }
}