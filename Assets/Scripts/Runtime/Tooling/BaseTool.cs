using UnityEngine;

namespace Runtime.Tooling
{
    public abstract class BaseTool : PearlBehaviour
    {
        [SerializeField]
        private string _toolName;
        [TextArea(10, 50)]
        [SerializeField]
        private string _descriptionForPrompt;
        [TextArea(10, 50)]
        [SerializeField]
        private string _jsonPayloadExampleForPrompt;
    
        public string ToolName => _toolName;
        public string DescriptionForPrompt => _descriptionForPrompt;
        public string JsonPayloadExampleForPrompt => _jsonPayloadExampleForPrompt;

        public abstract void Initialize();
        public abstract bool TryExecute(string payloadJson, out string executionSummary);
    }
}