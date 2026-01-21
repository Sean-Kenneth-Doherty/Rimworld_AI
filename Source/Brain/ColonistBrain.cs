using System;
using System.Threading.Tasks;
using AdvancedColonistIntelligence.Actions;
using AdvancedColonistIntelligence.LLM;
using AdvancedColonistIntelligence.Settings;
using Verse;

namespace AdvancedColonistIntelligence.Brain
{
    /// <summary>
    /// The AI brain for a single colonist. Handles decision-making and action execution.
    /// </summary>
    public class ColonistBrain
    {
        public Pawn Pawn { get; }
        public bool IsProcessing { get; private set; }
        public DateTime LastQueryTime { get; private set; } = DateTime.MinValue;
        public AIAction LastAction { get; private set; }
        public string LastTrigger { get; private set; }

        private bool pendingQuery = false;
        private string pendingTrigger = "";

        public ColonistBrain(Pawn pawn)
        {
            Pawn = pawn;
        }

        /// <summary>
        /// Request the brain to make a decision based on a trigger.
        /// </summary>
        public void RequestDecision(string triggerReason)
        {
            if (IsProcessing)
            {
                // Queue the request
                pendingQuery = true;
                pendingTrigger = triggerReason;
                return;
            }

            if (!CanQuery())
            {
                if (ACIMod.Settings.debugMode)
                    Log.Message($"[ACI] {Pawn.Name.ToStringShort}: Query rate limited");
                return;
            }

            LastTrigger = triggerReason;
            ProcessDecisionAsync(triggerReason);
        }

        private bool CanQuery()
        {
            var settings = ACIMod.Settings;
            if (settings == null) return false;

            var timeSinceLastQuery = (DateTime.Now - LastQueryTime).TotalSeconds;
            return timeSinceLastQuery >= settings.minQueryDelaySeconds;
        }

        private async void ProcessDecisionAsync(string triggerReason)
        {
            IsProcessing = true;

            try
            {
                var systemPrompt = PromptBuilder.BuildSystemPrompt(Pawn);
                var userPrompt = PromptBuilder.BuildUserPrompt(Pawn, triggerReason);

                var response = await LLMManager.Instance.SendRequestAsync(systemPrompt, userPrompt);

                if (response.Success)
                {
                    LastQueryTime = DateTime.Now;
                    var action = ActionParser.Parse(response.Content);

                    if (action != null)
                    {
                        LastAction = action;

                        // Execute on main thread
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            if (Pawn != null && !Pawn.Dead && !Pawn.Destroyed)
                            {
                                ActionExecutor.Execute(Pawn, action);
                            }
                        });

                        if (ACIMod.Settings.debugMode)
                        {
                            Log.Message($"[ACI] {Pawn.Name.ToStringShort} decided: {action}");
                        }
                    }
                    else
                    {
                        Log.Warning($"[ACI] {Pawn.Name.ToStringShort}: Failed to parse action from response");
                    }
                }
                else
                {
                    Log.Warning($"[ACI] {Pawn.Name.ToStringShort}: LLM request failed - {response.Error}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] {Pawn.Name.ToStringShort}: Decision processing error - {ex.Message}");
            }
            finally
            {
                IsProcessing = false;

                // Process pending query if any
                if (pendingQuery)
                {
                    pendingQuery = false;
                    var trigger = pendingTrigger;
                    pendingTrigger = "";
                    RequestDecision(trigger);
                }
            }
        }

        /// <summary>
        /// Handle a player order - decide whether to comply or refuse.
        /// </summary>
        public async void HandlePlayerOrder(string orderedAction, string target)
        {
            var settings = ACIMod.Settings;
            if (!settings.canRefuseOrders)
            {
                // Always comply
                return;
            }

            if (IsProcessing)
                return;

            IsProcessing = true;

            try
            {
                var systemPrompt = PromptBuilder.BuildSystemPrompt(Pawn);
                var userPrompt = PromptBuilder.BuildRefusalPrompt(Pawn, orderedAction, target);

                var response = await LLMManager.Instance.SendRequestAsync(systemPrompt, userPrompt);

                if (response.Success)
                {
                    var action = ActionParser.Parse(response.Content);
                    if (action != null && action.ActionName?.ToLower() == "refuse")
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            ActionExecutor.Execute(Pawn, action);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] {Pawn.Name.ToStringShort}: Order handling error - {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Handle a social interaction initiated by another pawn.
        /// </summary>
        public async void HandleSocialInteraction(Pawn initiator, string interactionType)
        {
            if (IsProcessing)
                return;

            IsProcessing = true;

            try
            {
                var systemPrompt = PromptBuilder.BuildSystemPrompt(Pawn);
                var userPrompt = PromptBuilder.BuildSocialPrompt(Pawn, initiator, interactionType);

                var response = await LLMManager.Instance.SendRequestAsync(systemPrompt, userPrompt);

                if (response.Success)
                {
                    var action = ActionParser.Parse(response.Content);
                    if (action != null)
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            // Just show the thought/speech, don't execute other actions
                            if (ACIMod.Settings.showThoughtBubbles && !string.IsNullOrEmpty(action.Thought))
                            {
                                Display.ThoughtBubbleManager.ShowBubble(Pawn, action.Thought);
                            }
                            if (!string.IsNullOrEmpty(action.Speech))
                            {
                                MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, action.Speech, 5f);
                            }
                            if (ACIMod.Settings.logToJournal)
                            {
                                Display.JournalManager.AddEntry(Pawn, action);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] {Pawn.Name.ToStringShort}: Social handling error - {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
