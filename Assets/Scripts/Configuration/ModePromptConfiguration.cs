using UnityEngine;
namespace Configuration
{
    [CreateAssetMenu(fileName = "New Mode Prompt Configuration", menuName = "Ritsuko/ModePromptConfiguration")]
    public class ModePromptConfiguration : ScriptableObject
    {
        public BehaviourMode BehaviourMode;

        [TextArea(10, 50)]
        public string ModeContext;
        
        [TextArea(10, 50)]
        public string PersonalityAdditions;

        [TextArea(10, 50)]
        public string ResponseInstructionAdditionsAllModes;
        
        public float ProactiveLoopbackTimeMinutes = 30f;
    }
}