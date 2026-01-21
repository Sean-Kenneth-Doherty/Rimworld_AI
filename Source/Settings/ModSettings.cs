using System.Collections.Generic;
using Verse;

namespace AdvancedColonistIntelligence.Settings
{
    public class ACISettings : ModSettings
    {
        // API Configuration
        public LLMProvider provider = LLMProvider.Grok;
        public string apiKey = "";
        public string model = "grok-2";
        public string customEndpoint = "";

        // AI Colonist Selection
        public List<string> aiColonistIds = new List<string>();
        public int maxAIColonists = 3;

        // Autonomy Settings
        public bool canRefuseOrders = true;
        public bool canInitiateActions = true;
        public bool canDraftSelf = false;
        public bool canLeaveMap = false;

        // Display Settings
        public bool showThoughtBubbles = true;
        public bool logToJournal = true;
        public bool notifyMajorDecisions = true;

        // Query Settings
        public QueryTiming queryTiming = QueryTiming.EventDriven;
        public int minQueryDelaySeconds = 5;
        public int maxTokensPerQuery = 2000;
        public int maxContextTokens = 4000;

        // Debug
        public bool debugMode = false;
        public bool logPrompts = false;
        public bool logResponses = false;

        public override void ExposeData()
        {
            base.ExposeData();

            // API
            Scribe_Values.Look(ref provider, "provider", LLMProvider.Grok);
            Scribe_Values.Look(ref apiKey, "apiKey", "");
            Scribe_Values.Look(ref model, "model", "grok-2");
            Scribe_Values.Look(ref customEndpoint, "customEndpoint", "");

            // Colonists
            Scribe_Collections.Look(ref aiColonistIds, "aiColonistIds", LookMode.Value);
            Scribe_Values.Look(ref maxAIColonists, "maxAIColonists", 3);

            // Autonomy
            Scribe_Values.Look(ref canRefuseOrders, "canRefuseOrders", true);
            Scribe_Values.Look(ref canInitiateActions, "canInitiateActions", true);
            Scribe_Values.Look(ref canDraftSelf, "canDraftSelf", false);
            Scribe_Values.Look(ref canLeaveMap, "canLeaveMap", false);

            // Display
            Scribe_Values.Look(ref showThoughtBubbles, "showThoughtBubbles", true);
            Scribe_Values.Look(ref logToJournal, "logToJournal", true);
            Scribe_Values.Look(ref notifyMajorDecisions, "notifyMajorDecisions", true);

            // Query
            Scribe_Values.Look(ref queryTiming, "queryTiming", QueryTiming.EventDriven);
            Scribe_Values.Look(ref minQueryDelaySeconds, "minQueryDelaySeconds", 5);
            Scribe_Values.Look(ref maxTokensPerQuery, "maxTokensPerQuery", 2000);
            Scribe_Values.Look(ref maxContextTokens, "maxContextTokens", 4000);

            // Debug
            Scribe_Values.Look(ref debugMode, "debugMode", false);
            Scribe_Values.Look(ref logPrompts, "logPrompts", false);
            Scribe_Values.Look(ref logResponses, "logResponses", false);

            if (aiColonistIds == null)
                aiColonistIds = new List<string>();
        }

        public string GetApiEndpoint()
        {
            if (!string.IsNullOrEmpty(customEndpoint))
                return customEndpoint;

            return provider switch
            {
                LLMProvider.Grok => "https://api.x.ai/v1/chat/completions",
                LLMProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
                LLMProvider.Claude => "https://api.anthropic.com/v1/messages",
                LLMProvider.Ollama => "http://localhost:11434/api/chat",
                LLMProvider.LMStudio => "http://localhost:1234/v1/chat/completions",
                _ => ""
            };
        }

        public string GetDefaultModel()
        {
            return provider switch
            {
                LLMProvider.Grok => "grok-2",
                LLMProvider.OpenAI => "gpt-4o-mini",
                LLMProvider.Claude => "claude-3-haiku-20240307",
                LLMProvider.Ollama => "llama3.2",
                LLMProvider.LMStudio => "local-model",
                _ => "grok-2"
            };
        }
    }

    public enum LLMProvider
    {
        Grok,
        OpenAI,
        Claude,
        Ollama,
        LMStudio
    }

    public enum QueryTiming
    {
        EventDriven,
        Periodic,
        Continuous
    }
}
