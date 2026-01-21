using System;
using System.Threading.Tasks;

namespace AdvancedColonistIntelligence.LLM
{
    /// <summary>
    /// Interface for LLM providers. All providers must implement this.
    /// </summary>
    public interface ILLMProvider
    {
        string Name { get; }
        Task<LLMResponse> SendRequestAsync(string systemPrompt, string userPrompt, int maxTokens);
        Task<bool> TestConnectionAsync();
    }

    public class LLMResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public string Error { get; set; }
        public int TokensUsed { get; set; }
        public float ResponseTimeMs { get; set; }

        public static LLMResponse Failed(string error)
        {
            return new LLMResponse
            {
                Success = false,
                Error = error,
                Content = null
            };
        }

        public static LLMResponse Succeeded(string content, int tokens = 0)
        {
            return new LLMResponse
            {
                Success = true,
                Content = content,
                TokensUsed = tokens
            };
        }
    }
}
