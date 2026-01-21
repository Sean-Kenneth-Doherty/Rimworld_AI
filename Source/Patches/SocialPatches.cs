using AdvancedColonistIntelligence.Brain;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for social interaction events.
    /// </summary>
    [HarmonyPatch]
    public static class SocialPatches
    {
        /// <summary>
        /// Patch: When a social interaction happens, let AI respond.
        /// </summary>
        [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
        [HarmonyPostfix]
        public static void TryInteractWith_Postfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef, bool __result)
        {
            if (!__result)
                return;

            var initiator = __instance.pawn;
            if (initiator == null || recipient == null)
                return;

            // Trigger for the recipient if they have an AI brain
            if (recipient.IsColonist && !recipient.Dead)
            {
                TriggerDetector.OnSocialInteraction(recipient, initiator, intDef);
            }
        }
    }
}
