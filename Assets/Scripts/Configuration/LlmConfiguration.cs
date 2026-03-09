using UnityEngine;
namespace Configuration
{
    [CreateAssetMenu(fileName = "New LLM Configuration", menuName = "Ritsuko/LLMConfiguration")]

    public class LlmConfiguration : ScriptableObject
    {
        public string Model = "openai/gpt-4o-mini";
        public float Temperature = 0.7f;
        public int MaxTokens = 512;
    }
}