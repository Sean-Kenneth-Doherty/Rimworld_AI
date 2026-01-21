using AdvancedColonistIntelligence.Settings;
using RimWorld;
using Verse;

namespace AdvancedColonistIntelligence.Brain
{
    /// <summary>
    /// Detects trigger events that should prompt AI decision-making.
    /// </summary>
    public static class TriggerDetector
    {
        /// <summary>
        /// Called when a pawn's job ends.
        /// </summary>
        public static void OnJobEnded(Pawn pawn, Verse.AI.Job job, Verse.AI.JobCondition condition)
        {
            if (!ShouldTrigger(pawn)) return;

            var reason = condition switch
            {
                Verse.AI.JobCondition.Succeeded => $"Finished task: {job?.def?.reportString ?? "unknown"}",
                Verse.AI.JobCondition.Incompletable => $"Could not complete: {job?.def?.reportString ?? "unknown"}",
                Verse.AI.JobCondition.InterruptForced => "Was interrupted",
                _ => "Task ended"
            };

            BrainManager.Instance?.TriggerDecision(pawn, reason);
        }

        /// <summary>
        /// Called when a pawn becomes idle (no job).
        /// </summary>
        public static void OnBecameIdle(Pawn pawn)
        {
            if (!ShouldTrigger(pawn)) return;
            BrainManager.Instance?.TriggerDecision(pawn, "Became idle - nothing to do");
        }

        /// <summary>
        /// Called when a threat is detected (raid, etc).
        /// </summary>
        public static void OnThreatDetected(Pawn pawn, Thing threat)
        {
            if (!ShouldTrigger(pawn)) return;
            var threatDesc = threat is Pawn p ? p.LabelShort : threat?.Label ?? "something dangerous";
            BrainManager.Instance?.TriggerDecision(pawn, $"Threat detected: {threatDesc}");
        }

        /// <summary>
        /// Called when the pawn's mood changes significantly.
        /// </summary>
        public static void OnMoodChanged(Pawn pawn, float oldMood, float newMood)
        {
            if (!ShouldTrigger(pawn)) return;

            var change = newMood - oldMood;
            if (System.Math.Abs(change) < 0.15f) return; // Only trigger on significant changes

            var reason = change > 0
                ? $"Feeling better (mood rose to {newMood * 100:F0}%)"
                : $"Feeling worse (mood dropped to {newMood * 100:F0}%)";

            BrainManager.Instance?.TriggerDecision(pawn, reason);
        }

        /// <summary>
        /// Called when the pawn is injured.
        /// </summary>
        public static void OnInjured(Pawn pawn, DamageInfo dinfo)
        {
            if (!ShouldTrigger(pawn)) return;

            var source = dinfo.Instigator is Pawn attacker ? attacker.LabelShort : "something";
            BrainManager.Instance?.TriggerDecision(pawn, $"Injured by {source}!");
        }

        /// <summary>
        /// Called when someone nearby dies.
        /// </summary>
        public static void OnWitnessedDeath(Pawn pawn, Pawn victim)
        {
            if (!ShouldTrigger(pawn)) return;

            var relation = pawn.relations?.OpinionOf(victim) ?? 0;
            var relationType = relation > 50 ? "friend" : relation < -20 ? "someone I dislike" : "someone";

            BrainManager.Instance?.TriggerDecision(pawn, $"Witnessed {victim.Name.ToStringShort}'s death ({relationType})");
        }

        /// <summary>
        /// Called when the pawn wakes up.
        /// </summary>
        public static void OnWokeUp(Pawn pawn)
        {
            if (!ShouldTrigger(pawn)) return;
            BrainManager.Instance?.TriggerDecision(pawn, "Just woke up - starting a new day");
        }

        /// <summary>
        /// Called when someone initiates a social interaction with this pawn.
        /// </summary>
        public static void OnSocialInteraction(Pawn pawn, Pawn initiator, InteractionDef interaction)
        {
            if (!ShouldTrigger(pawn)) return;

            var brain = BrainManager.Instance?.GetBrain(pawn);
            brain?.HandleSocialInteraction(initiator, interaction?.label ?? "interaction");
        }

        /// <summary>
        /// Called when the player orders this pawn to do something.
        /// </summary>
        public static void OnPlayerOrder(Pawn pawn, string action, string target)
        {
            if (!ShouldTrigger(pawn)) return;

            var settings = ACIMod.Settings;
            if (!settings.canRefuseOrders) return;

            var brain = BrainManager.Instance?.GetBrain(pawn);
            brain?.HandlePlayerOrder(action, target);
        }

        /// <summary>
        /// Called when a mental break is about to start.
        /// </summary>
        public static void OnMentalBreakPending(Pawn pawn)
        {
            if (!ShouldTrigger(pawn)) return;
            BrainManager.Instance?.TriggerDecision(pawn, "At breaking point - about to have a mental break");
        }

        private static bool ShouldTrigger(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return false;

            var settings = ACIMod.Settings;
            if (settings == null)
                return false;

            // Check if this pawn has an AI brain
            var hasBrain = BrainManager.Instance?.HasBrain(pawn) ?? false;
            if (!hasBrain)
                return false;

            // Check timing settings
            if (settings.queryTiming == QueryTiming.Periodic)
                return false; // Periodic mode handles its own timing

            return true;
        }
    }
}
