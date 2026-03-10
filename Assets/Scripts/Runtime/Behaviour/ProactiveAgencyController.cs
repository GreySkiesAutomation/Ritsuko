using System;
using NaughtyAttributes;
using Runtime.Inputs.Presence;
using Runtime.Reasoning.DataTypes;
namespace Runtime.Behaviour
{
    public class ProactiveAgencyController : PearlBehaviour
    {
        private DateTime _lastTimeUserWasPrompted = DateTime.MinValue;
        public void Initialize()
        {
            SetInitialized();
        }

        public void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (GlobalManager.I.State.PresenceState == PresenceState.Absent)
            {
                return;
            }
            
            if (GlobalManager.I.State.QuietModeEnabled)
            {
                return;
            }

            var userPromptCooldownMinutes = GlobalManager.I.Configuration.GetCurrentModePromptConfiguration().ProactiveLoopbackTimeMinutes;

            if (userPromptCooldownMinutes < 0f)
            {
                return;
            }

            if (GlobalManager.I.State.MinutesSinceLastUserInteraction > userPromptCooldownMinutes && DateTime.UtcNow - _lastTimeUserWasPrompted > TimeSpan.FromMinutes(userPromptCooldownMinutes))
            {
                SimpleAskUserForTaskUpdates();
            }
        }

        [Button]
        public void SimpleAskUserForTaskUpdates()
        {
            GlobalManager.I.QueryHandler.HandleNewMessage("I want to ask the user about updates on their to-do list. If there are no tasks left, ask if there are new tasks or if we should switch to a different mode or be finished with productivity for the day. Keep the message concise and to the point.", QuerySource.AssistantSelf);
            _lastTimeUserWasPrompted = DateTime.UtcNow;
        }
    }
}