using AdvancedColonistIntelligence.Brain;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for job-related events.
    /// </summary>
    [HarmonyPatch]
    public static class JobPatches
    {
        /// <summary>
        /// Patch: When a job ends, trigger AI decision.
        /// </summary>
        [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
        [HarmonyPostfix]
        public static void EndCurrentJob_Postfix(Pawn_JobTracker __instance, JobCondition condition)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return;

            var curJob = __instance.curJob;
            TriggerDetector.OnJobEnded(pawn, curJob, condition);
        }

        /// <summary>
        /// Patch: When a pawn becomes idle, trigger AI decision.
        /// </summary>
        [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryFindAndStartJob))]
        [HarmonyPostfix]
        public static void TryFindAndStartJob_Postfix(Pawn_JobTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return;

            // If still no job after trying to find one, pawn is idle
            if (__instance.curJob == null)
            {
                TriggerDetector.OnBecameIdle(pawn);
            }
        }

        /// <summary>
        /// Patch: When player manually gives a job, check for refusal.
        /// </summary>
        [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
        [HarmonyPrefix]
        public static void StartJob_Prefix(Pawn_JobTracker __instance, Job newJob, JobCondition lastJobEndCondition)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return;

            // Check if this is a player-forced job
            if (lastJobEndCondition == JobCondition.InterruptForced && newJob != null)
            {
                var target = newJob.targetA.Thing?.Label ?? newJob.targetA.Cell.ToString();
                TriggerDetector.OnPlayerOrder(pawn, newJob.def.reportString, target);
            }
        }
    }
}
