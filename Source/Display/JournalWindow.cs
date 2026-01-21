using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AdvancedColonistIntelligence.Display
{
    /// <summary>
    /// Window for viewing a colonist's AI journal.
    /// </summary>
    public class JournalWindow : Window
    {
        private readonly Pawn pawn;
        private Vector2 scrollPosition;
        private List<JournalEntry> entries;

        public override Vector2 InitialSize => new Vector2(500, 600);

        public JournalWindow(Pawn pawn)
        {
            this.pawn = pawn;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = true;

            RefreshEntries();
        }

        private void RefreshEntries()
        {
            entries = JournalManager.GetEntries(pawn, 50);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            var titleRect = new Rect(0, 0, inRect.width, 35);
            Widgets.Label(titleRect, $"{pawn.Name.ToStringShort}'s Mind");
            Text.Font = GameFont.Small;

            // Refresh button
            var refreshRect = new Rect(inRect.width - 80, 5, 70, 25);
            if (Widgets.ButtonText(refreshRect, "Refresh"))
                RefreshEntries();

            // Entries list
            var listRect = new Rect(0, 45, inRect.width, inRect.height - 90);
            var viewRect = new Rect(0, 0, listRect.width - 20, entries.Count * 100);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            var y = 0f;
            foreach (var entry in entries)
            {
                var entryRect = new Rect(0, y, viewRect.width, 95);
                DrawEntry(entryRect, entry);
                y += 100;
            }

            Widgets.EndScrollView();

            // Stats at bottom
            var statsRect = new Rect(0, inRect.height - 35, inRect.width, 30);
            Text.Font = GameFont.Tiny;
            Widgets.Label(statsRect, $"Total entries: {entries.Count}");
            Text.Font = GameFont.Small;
        }

        private void DrawEntry(Rect rect, JournalEntry entry)
        {
            // Background
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Widgets.DrawBox(rect);

            var padding = 5f;
            var innerRect = rect.ContractedBy(padding);

            // Time
            Text.Font = GameFont.Tiny;
            var timeRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 18);
            GUI.color = Color.gray;
            Widgets.Label(timeRect, $"{entry.FormattedTime} ({entry.TimeAgo} ago)");
            GUI.color = Color.white;

            // Thought
            Text.Font = GameFont.Small;
            var thoughtRect = new Rect(innerRect.x, innerRect.y + 20, innerRect.width, 40);
            GUI.color = new Color(0.9f, 0.9f, 1f);
            Widgets.Label(thoughtRect, $"\"{entry.Thought ?? "..."}\"");
            GUI.color = Color.white;

            // Action
            if (!string.IsNullOrEmpty(entry.Action))
            {
                var actionRect = new Rect(innerRect.x, innerRect.y + 55, innerRect.width, 18);
                Text.Font = GameFont.Tiny;
                var targetStr = !string.IsNullOrEmpty(entry.Target) ? $" -> {entry.Target}" : "";
                Widgets.Label(actionRect, $"Action: {entry.Action}{targetStr}");
            }

            // Speech
            if (!string.IsNullOrEmpty(entry.Speech))
            {
                var speechRect = new Rect(innerRect.x, innerRect.y + 72, innerRect.width, 18);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.yellow;
                Widgets.Label(speechRect, $"Said: \"{entry.Speech}\"");
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
        }
    }

    /// <summary>
    /// Adds a button to open the journal from the colonist's info card.
    /// </summary>
    public static class JournalButton
    {
        public static void TryDrawJournalButton(Rect rect, Pawn pawn)
        {
            if (pawn == null)
                return;

            var brain = Brain.BrainManager.Instance?.GetBrain(pawn);
            if (brain == null)
                return;

            var buttonRect = new Rect(rect.xMax - 130, rect.y + 5, 120, 25);
            if (Widgets.ButtonText(buttonRect, "AI Journal"))
            {
                Find.WindowStack.Add(new JournalWindow(pawn));
            }
        }
    }
}
