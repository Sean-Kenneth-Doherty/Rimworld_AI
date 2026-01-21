using System.Linq;
using AdvancedColonistIntelligence.Brain;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for threat detection.
    /// </summary>
    [HarmonyPatch]
    public static class ThreatPatches
    {
        /// <summary>
        /// Patch: When a raid starts, notify all AI colonists.
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.TryExecuteWorker))]
        [HarmonyPostfix]
        public static void RaidStarted_Postfix(IncidentParms parms, bool __result)
        {
            if (!__result || parms?.target == null)
                return;

            var map = parms.target as Map;
            if (map == null)
                return;

            // Notify all AI colonists on this map
            var colonists = map.mapPawns?.FreeColonists;
            if (colonists == null)
                return;

            foreach (var colonist in colonists)
            {
                if (colonist.Dead)
                    continue;

                TriggerDetector.OnThreatDetected(colonist, null);
            }
        }

        /// <summary>
        /// Patch: When manhunters appear.
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_ManhunterPack), nameof(IncidentWorker_ManhunterPack.TryExecuteWorker))]
        [HarmonyPostfix]
        public static void ManhunterPack_Postfix(IncidentParms parms, bool __result)
        {
            if (!__result || parms?.target == null)
                return;

            var map = parms.target as Map;
            if (map == null)
                return;

            var colonists = map.mapPawns?.FreeColonists;
            if (colonists == null)
                return;

            foreach (var colonist in colonists)
            {
                if (colonist.Dead)
                    continue;

                var nearbyThreat = map.mapPawns.AllPawnsSpawned
                    .FirstOrDefault(p => p.MentalStateDef == MentalStateDefOf.Manhunter);

                TriggerDetector.OnThreatDetected(colonist, nearbyThreat);
            }
        }
    }
}
