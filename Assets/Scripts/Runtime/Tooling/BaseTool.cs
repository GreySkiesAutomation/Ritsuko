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

        public void Initialize()
        {
            //If tool name, description, and payload example are not set, log an error since they are required for the tool to work properly
            if (string.IsNullOrWhiteSpace(_toolName))
            {
                LogError("Tool name is required but not set.");
            }

            if (string.IsNullOrWhiteSpace(_descriptionForPrompt))
            {
                LogError("Description for prompt is required but not set.");
            }

            if (string.IsNullOrWhiteSpace(_jsonPayloadExampleForPrompt))
            {
                LogError("JSON payload example for prompt is required but not set.");
            }
            
            InitializeInternal();
        }

        protected abstract void InitializeInternal();
        public abstract bool TryExecute(string payloadJson, out string executionSummary);
    }
}