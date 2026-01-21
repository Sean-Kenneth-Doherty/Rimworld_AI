using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AdvancedColonistIntelligence.Settings
{
    public class ACIMod : Mod
    {
        public static ACISettings Settings { get; private set; }
        private Vector2 scrollPosition;
        private string apiKeyBuffer = "";
        private bool showApiKey = false;

        public ACIMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ACISettings>();
            apiKeyBuffer = Settings.apiKey;
        }

        public override string SettingsCategory() => "Advanced Colonist Intelligence";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            var viewRect = new Rect(0, 0, inRect.width - 20, 900);

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            listing.Begin(viewRect);

            // === API CONFIGURATION ===
            listing.Label("<b>API CONFIGURATION</b>");
            listing.GapLine();

            // Provider dropdown
            listing.Label("LLM Provider:");
            if (listing.ButtonText(Settings.provider.ToString()))
            {
                var options = new List<FloatMenuOption>();
                foreach (LLMProvider p in Enum.GetValues(typeof(LLMProvider)))
                {
                    options.Add(new FloatMenuOption(p.ToString(), () =>
                    {
                        Settings.provider = p;
                        Settings.model = Settings.GetDefaultModel();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // API Key
            listing.Label("API Key:");
            var keyRect = listing.GetRect(28);
            var keyFieldRect = new Rect(keyRect.x, keyRect.y, keyRect.width - 120, keyRect.height);
            var showRect = new Rect(keyRect.xMax - 115, keyRect.y, 50, keyRect.height);
            var testRect = new Rect(keyRect.xMax - 60, keyRect.y, 55, keyRect.height);

            if (showApiKey)
                apiKeyBuffer = Widgets.TextField(keyFieldRect, apiKeyBuffer);
            else
                Widgets.TextField(keyFieldRect, new string('•', apiKeyBuffer.Length));

            if (Widgets.ButtonText(showRect, showApiKey ? "Hide" : "Show"))
                showApiKey = !showApiKey;

            if (Widgets.ButtonText(testRect, "Test"))
                TestApiConnection();

            Settings.apiKey = apiKeyBuffer;

            // Model
            listing.Label($"Model: {Settings.model}");
            if (listing.ButtonText("Change Model"))
            {
                var models = GetModelsForProvider(Settings.provider);
                var options = models.Select(m => new FloatMenuOption(m, () => Settings.model = m)).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Custom endpoint
            listing.Label("Custom Endpoint (optional):");
            Settings.customEndpoint = listing.TextEntry(Settings.customEndpoint);

            listing.Gap();

            // === AI COLONIST SELECTION ===
            listing.Label("<b>AI COLONIST SELECTION</b>");
            listing.GapLine();

            listing.Label($"Max AI Colonists: {Settings.maxAIColonists}");
            Settings.maxAIColonists = (int)listing.Slider(Settings.maxAIColonists, 1, 10);

            if (Current.Game != null)
            {
                var colonists = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists;
                listing.Label($"Active AI Colonists: {Settings.aiColonistIds.Count}/{Settings.maxAIColonists}");

                foreach (var pawn in colonists)
                {
                    var isAI = Settings.aiColonistIds.Contains(pawn.ThingID);
                    var rect = listing.GetRect(24);
                    var checkRect = new Rect(rect.x, rect.y, 24, 24);
                    var labelRect = new Rect(rect.x + 28, rect.y, rect.width - 28, 24);

                    var newValue = isAI;
                    Widgets.Checkbox(checkRect.x, checkRect.y, ref newValue);
                    Widgets.Label(labelRect, $"{pawn.Name.ToStringShort} - {pawn.story?.TitleCap ?? "Unknown"}");

                    if (newValue != isAI)
                    {
                        if (newValue && Settings.aiColonistIds.Count < Settings.maxAIColonists)
                            Settings.aiColonistIds.Add(pawn.ThingID);
                        else if (!newValue)
                            Settings.aiColonistIds.Remove(pawn.ThingID);
                    }
                }
            }
            else
            {
                listing.Label("Load a game to select AI colonists.");
            }

            listing.Gap();

            // === AUTONOMY SETTINGS ===
            listing.Label("<b>AUTONOMY</b>");
            listing.GapLine();

            listing.CheckboxLabeled("AI can refuse player orders", ref Settings.canRefuseOrders,
                "When enabled, AI colonists may refuse to do tasks that conflict with their personality.");
            listing.CheckboxLabeled("AI can initiate actions independently", ref Settings.canInitiateActions,
                "AI colonists will proactively start tasks without being told.");
            listing.CheckboxLabeled("AI can draft/undraft self", ref Settings.canDraftSelf,
                "AI colonists can enter combat mode on their own.");
            listing.CheckboxLabeled("AI can leave the map", ref Settings.canLeaveMap,
                "AI colonists can join caravans or flee the map.");

            listing.Gap();

            // === DISPLAY SETTINGS ===
            listing.Label("<b>DISPLAY</b>");
            listing.GapLine();

            listing.CheckboxLabeled("Show thought bubbles", ref Settings.showThoughtBubbles,
                "Display speech bubbles showing AI colonist thoughts.");
            listing.CheckboxLabeled("Log thoughts to journal", ref Settings.logToJournal,
                "Record all AI decisions in a viewable journal.");
            listing.CheckboxLabeled("Notify on major decisions", ref Settings.notifyMajorDecisions,
                "Show notifications for significant AI actions.");

            listing.Gap();

            // === QUERY SETTINGS ===
            listing.Label("<b>QUERY TIMING</b>");
            listing.GapLine();

            listing.Label("Query Mode:");
            if (listing.ButtonText(Settings.queryTiming.ToString()))
            {
                var options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("EventDriven - Query on events and idle", () => Settings.queryTiming = QueryTiming.EventDriven),
                    new FloatMenuOption("Periodic - Query every X seconds", () => Settings.queryTiming = QueryTiming.Periodic),
                    new FloatMenuOption("Continuous - Always thinking (expensive)", () => Settings.queryTiming = QueryTiming.Continuous)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.Label($"Min delay between queries: {Settings.minQueryDelaySeconds} seconds");
            Settings.minQueryDelaySeconds = (int)listing.Slider(Settings.minQueryDelaySeconds, 1, 60);

            listing.Label($"Max tokens per query: {Settings.maxTokensPerQuery}");
            Settings.maxTokensPerQuery = (int)listing.Slider(Settings.maxTokensPerQuery, 500, 4000);

            listing.Label($"Max context tokens: {Settings.maxContextTokens}");
            Settings.maxContextTokens = (int)listing.Slider(Settings.maxContextTokens, 1000, 8000);

            listing.Gap();

            // === DEBUG ===
            listing.Label("<b>DEBUG</b>");
            listing.GapLine();

            listing.CheckboxLabeled("Debug mode", ref Settings.debugMode);
            listing.CheckboxLabeled("Log prompts to console", ref Settings.logPrompts);
            listing.CheckboxLabeled("Log responses to console", ref Settings.logResponses);

            listing.End();
            Widgets.EndScrollView();
        }

        private List<string> GetModelsForProvider(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.Grok => new List<string> { "grok-2", "grok-2-mini", "grok-beta" },
                LLMProvider.OpenAI => new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo" },
                LLMProvider.Claude => new List<string> { "claude-3-5-sonnet-20241022", "claude-3-haiku-20240307", "claude-3-opus-20240229" },
                LLMProvider.Ollama => new List<string> { "llama3.2", "llama3.1", "mistral", "codellama", "phi3" },
                LLMProvider.LMStudio => new List<string> { "local-model" },
                _ => new List<string> { "unknown" }
            };
        }

        private void TestApiConnection()
        {
            Messages.Message("Testing API connection...", MessageTypeDefOf.NeutralEvent);
            // TODO: Implement actual API test
            LLM.LLMManager.Instance.TestConnection((success, message) =>
            {
                if (success)
                    Messages.Message("API connection successful!", MessageTypeDefOf.PositiveEvent);
                else
                    Messages.Message($"API connection failed: {message}", MessageTypeDefOf.RejectInput);
            });
        }
    }
}
