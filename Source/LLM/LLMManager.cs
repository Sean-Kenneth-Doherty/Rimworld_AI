using System;
using System.Threading.Tasks;
using AdvancedColonistIntelligence.Settings;
using Verse;

namespace AdvancedColonistIntelligence.LLM
{
    /// <summary>
    /// Manages LLM provider selection and request handling.
    /// </summary>
    public class LLMManager
    {
        private static LLMManager instance;
        public static LLMManager Instance => instance ??= new LLMManager();

        private ILLMProvider currentProvider;
        private DateTime lastRequest = DateTime.MinValue;

        private LLMManager() { }

        public ILLMProvider GetProvider()
        {
            var settings = ACIMod.Settings;
            if (settings == null)
            {
                Log.Error("[ACI] Settings not initialized");
                return null;
            }

            // Recreate provider if settings changed
            if (currentProvider == null || ProviderSettingsChanged())
            {
                currentProvider = CreateProvider(settings);
            }

            return currentProvider;
        }

        private ILLMProvider CreateProvider(ACISettings settings)
        {
            var endpoint = settings.GetApiEndpoint();

            return settings.provider switch
            {
                LLMProvider.Grok => new OpenAICompatibleProvider("Grok", endpoint, settings.apiKey, settings.model),
                LLMProvider.OpenAI => new OpenAICompatibleProvider("OpenAI", endpoint, settings.apiKey, settings.model),
                LLMProvider.LMStudio => new OpenAICompatibleProvider("LM Studio", endpoint, "", settings.model),
                LLMProvider.Claude => new ClaudeProvider(endpoint, settings.apiKey, settings.model),
                LLMProvider.Ollama => new OllamaProvider(endpoint, settings.model),
                _ => throw new NotSupportedException($"Provider {settings.provider} not supported")
            };
        }

        private bool ProviderSettingsChanged()
        {
            // Simple check - could be more sophisticated
            return false;
        }

        public void InvalidateProvider()
        {
            currentProvider = null;
        }

        public bool CanMakeRequest()
        {
            var settings = ACIMod.Settings;
            if (settings == null) return false;

            var timeSinceLastRequest = (DateTime.Now - lastRequest).TotalSeconds;
            return timeSinceLastRequest >= settings.minQueryDelaySeconds;
        }

        public async Task<LLMResponse> SendRequestAsync(string systemPrompt, string userPrompt)
        {
            var settings = ACIMod.Settings;
            if (settings == null)
                return LLMResponse.Failed("Settings not initialized");

            if (string.IsNullOrEmpty(settings.apiKey) && settings.provider != LLMProvider.Ollama && settings.provider != LLMProvider.LMStudio)
                return LLMResponse.Failed("API key not configured");

            if (!CanMakeRequest())
                return LLMResponse.Failed("Rate limited - too soon since last request");

            var provider = GetProvider();
            if (provider == null)
                return LLMResponse.Failed("No provider available");

            lastRequest = DateTime.Now;

            if (settings.logPrompts)
            {
                Log.Message($"[ACI] System prompt:\n{systemPrompt}");
                Log.Message($"[ACI] User prompt:\n{userPrompt}");
            }

            var response = await provider.SendRequestAsync(systemPrompt, userPrompt, settings.maxTokensPerQuery);

            if (settings.logResponses)
            {
                Log.Message($"[ACI] Response (success={response.Success}):\n{response.Content ?? response.Error}");
            }

            return response;
        }

        public void TestConnection(Action<bool, string> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var provider = GetProvider();
                    if (provider == null)
                    {
                        callback(false, "No provider configured");
                        return;
                    }

                    var success = await provider.TestConnectionAsync();
                    callback(success, success ? "Connection successful" : "Connection failed");
                }
                catch (Exception ex)
                {
                    callback(false, ex.Message);
                }
            });
        }
    }
}
