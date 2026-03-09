using System;
using Configuration;
using UnityEngine;
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
        private void Update()
        {
            _modeProductiveWorkIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.ProductiveWork);
            _modeProductiveHobbyIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.ProductiveHobby);
            _modeChillIcon.SetActive(GlobalManager.I.State.CurrentMode == BehaviourMode.Chill);
            
            _quietModeEnabledIcon.SetActive(GlobalManager.I.State.QuietModeEnabled);
            _quietModeDisabledIcon.SetActive(!GlobalManager.I.State.QuietModeEnabled);
            
            _respondToNameEnabledIcon.SetActive(GlobalManager.I.State.RespondToNameEnabled);
            _respondToNameDisabledIcon.SetActive(!GlobalManager.I.State.RespondToNameEnabled);
            
            _auditModeEnabledIcon.SetActive(GlobalManager.I.State.AuditModeEnabled);
            _auditModeDisabledIcon.SetActive(!GlobalManager.I.State.AuditModeEnabled);
        }
    }
}