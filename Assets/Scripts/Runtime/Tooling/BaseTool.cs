using UnityEngine;

namespace Runtime.Tooling
{
    public abstract class BaseTool : PearlBehaviour
    {
        [SerializeField]
        private string _toolName;
        
        [SerializeField]
        private int _toolPriority; // Lowest means runs first. This is important if multiple tools can handle the same request, in which case the one with the highest priority (lowest number) will be chosen.
        
        [TextArea(10, 50)]
        [SerializeField]
        private string _descriptionForPrompt;

        [TextArea(10, 50)]
        [SerializeField]
        private string _jsonPayloadExampleForPrompt;
    
        public string ToolName => _toolName;
        public int ToolPriority => _toolPriority;
        public string DescriptionForPrompt => _descriptionForPrompt;
        public string JsonPayloadExampleForPrompt => _jsonPayloadExampleForPrompt;

        public abstract void Initialize();
        public abstract bool TryExecute(string payloadJson, out string executionSummary);
    }
}