using AdvancedColonistIntelligence.Display;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AdvancedColonistIntelligence.Patches
{
    /// <summary>
    /// Patches for UI elements.
    /// </summary>
    [HarmonyPatch]
    public static class UIPatches
    {
        /// <summary>
        /// Patch: Draw thought bubbles on the map.
        /// </summary>
        [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs))]
        [HarmonyPostfix]
        public static void DrawThoughtBubbles()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            ThoughtBubbleManager.DrawBubbles();
        }

        /// <summary>
        /// Patch: Tick thought bubbles.
        /// </summary>
        [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
        [HarmonyPostfix]
        public static void TickThoughtBubbles()
        {
            ThoughtBubbleManager.Tick();
        }

        /// <summary>
        /// Patch: Add journal button to colonist info card.
        /// </summary>
        [HarmonyPatch(typeof(ITab_Pawn_Character), nameof(ITab_Pawn_Character.FillTab))]
        [HarmonyPostfix]
        public static void AddJournalButton(ITab_Pawn_Character __instance)
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || !pawn.IsColonist)
                return;

            // Draw journal button in the tab
            var rect = new Rect(0, 0, ITab_Pawn_Character.PawnCardSize.x, ITab_Pawn_Character.PawnCardSize.y);
            JournalButton.TryDrawJournalButton(rect, pawn);
        }
    }
}
