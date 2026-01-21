using AdvancedColonistIntelligence.Brain;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for health-related events.
    /// </summary>
    [HarmonyPatch]
    public static class HealthPatches
    {
        /// <summary>
        /// Patch: When a pawn takes damage, trigger AI reaction.
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
        [HarmonyPostfix]
        public static void PostApplyDamage_Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || !__instance.IsColonist || __instance.Dead)
                return;

            if (totalDamageDealt > 0)
            {
                TriggerDetector.OnInjured(__instance, dinfo);
            }
        }

        /// <summary>
        /// Patch: When a pawn dies, notify nearby AI colonists.
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPostfix]
        public static void Kill_Postfix(Pawn __instance)
        {
            if (__instance?.Map == null)
                return;

            // Find nearby colonists and notify them
            var nearbyColonists = __instance.Map.mapPawns?.FreeColonists;
            if (nearbyColonists == null)
                return;

            foreach (var colonist in nearbyColonists)
            {
                if (colonist == __instance || colonist.Dead)
                    continue;

                // Check if close enough to witness
                var distance = colonist.Position.DistanceTo(__instance.Position);
                if (distance < 20)
                {
                    TriggerDetector.OnWitnessedDeath(colonist, __instance);
                }
            }
        }
    }
}
