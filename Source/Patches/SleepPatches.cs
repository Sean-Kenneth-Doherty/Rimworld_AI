using AdvancedColonistIntelligence.Brain;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for sleep-related events.
    /// </summary>
    [HarmonyPatch]
    public static class SleepPatches
    {
        /// <summary>
        /// Patch: When a pawn wakes up.
        /// </summary>
        [HarmonyPatch(typeof(JobDriver_LayDown), nameof(JobDriver_LayDown.MakeNewToils))]
        [HarmonyPostfix]
        public static void MakeNewToils_Postfix(JobDriver_LayDown __instance)
        {
            // This is a bit indirect - we'll catch wake-ups through job ending
            // A more direct approach would require more complex patching
        }
    }

    /// <summary>
    /// Alternative: Detect waking up by checking rest need recovery.
    /// </summary>
    [HarmonyPatch(typeof(Need_Rest), nameof(Need_Rest.NeedInterval))]
    public static class RestNeedPatch
    {
        private static System.Collections.Generic.Dictionary<Pawn, bool> wasAsleep = new System.Collections.Generic.Dictionary<Pawn, bool>();

        public static void Postfix(Need_Rest __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return;

            var currentlyAsleep = pawn.CurJob?.def == JobDefOf.LayDown;
            wasAsleep.TryGetValue(pawn, out var wasAsleepBefore);

            // Detect waking up
            if (wasAsleepBefore && !currentlyAsleep)
            {
                TriggerDetector.OnWokeUp(pawn);
            }

            wasAsleep[pawn] = currentlyAsleep;
        }
    }
}
