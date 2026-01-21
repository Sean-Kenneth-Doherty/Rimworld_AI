using AdvancedColonistIntelligence.Brain;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for mood-related events.
    /// </summary>
    [HarmonyPatch]
    public static class MoodPatches
    {
        private static System.Collections.Generic.Dictionary<Pawn, float> lastMoodLevels = new System.Collections.Generic.Dictionary<Pawn, float>();

        /// <summary>
        /// Patch: Track mood changes.
        /// </summary>
        [HarmonyPatch(typeof(Need_Mood), nameof(Need_Mood.NeedInterval))]
        [HarmonyPostfix]
        public static void NeedInterval_Postfix(Need_Mood __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return;

            var currentMood = __instance.CurLevelPercentage;

            if (lastMoodLevels.TryGetValue(pawn, out var lastMood))
            {
                var change = currentMood - lastMood;

                // Trigger on significant mood changes (>15%)
                if (System.Math.Abs(change) > 0.15f)
                {
                    TriggerDetector.OnMoodChanged(pawn, lastMood, currentMood);
                }
            }

            lastMoodLevels[pawn] = currentMood;
        }

        /// <summary>
        /// Patch: When a mental break starts.
        /// </summary>
        [HarmonyPatch(typeof(MentalBreaker), nameof(MentalBreaker.TryDoRandomMoodCausedMentalBreak))]
        [HarmonyPrefix]
        public static void TryDoRandomMoodCausedMentalBreak_Prefix(MentalBreaker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return;

            TriggerDetector.OnMentalBreakPending(pawn);
        }
    }
}
