using System.Collections.Generic;
using System.Linq;
using AdvancedColonistIntelligence.Actions;
using Verse;

namespace AdvancedColonistIntelligence.Display
{
    /// <summary>
    /// Manages the journal of AI colonist thoughts and actions.
    /// </summary>
    public static class JournalManager
    {
        private static Dictionary<string, List<JournalEntry>> journals = new Dictionary<string, List<JournalEntry>>();
        private const int MaxEntriesPerColonist = 100;

        public static void AddEntry(Pawn pawn, AIAction action)
        {
            if (pawn == null || action == null)
                return;

            var entry = new JournalEntry
            {
                Tick = Find.TickManager.TicksGame,
                Thought = action.Thought,
                Action = action.ActionName,
                Target = action.Target,
                Speech = action.Speech
            };

            var pawnId = pawn.ThingID;
            if (!journals.ContainsKey(pawnId))
                journals[pawnId] = new List<JournalEntry>();

            journals[pawnId].Insert(0, entry); // Newest first

            // Trim old entries
            if (journals[pawnId].Count > MaxEntriesPerColonist)
                journals[pawnId].RemoveRange(MaxEntriesPerColonist, journals[pawnId].Count - MaxEntriesPerColonist);
        }

        public static void AddEntry(Pawn pawn, string thought, string action = null, string target = null, string speech = null)
        {
            AddEntry(pawn, new AIAction
            {
                Thought = thought,
                ActionName = action,
                Target = target,
                Speech = speech
            });
        }

        public static List<JournalEntry> GetEntries(Pawn pawn, int count = 20)
        {
            if (pawn == null)
                return new List<JournalEntry>();

            var pawnId = pawn.ThingID;
            if (!journals.ContainsKey(pawnId))
                return new List<JournalEntry>();

            return journals[pawnId].Take(count).ToList();
        }

        public static void ClearJournal(Pawn pawn)
        {
            if (pawn != null)
                journals.Remove(pawn.ThingID);
        }

        public static void ClearAll()
        {
            journals.Clear();
        }

        public static void ExposeData()
        {
            // For save/load support
            var keys = journals.Keys.ToList();
            var values = journals.Values.ToList();

            Scribe_Collections.Look(ref keys, "journalKeys", LookMode.Value);
            // Note: Full save/load would need custom XML handling for JournalEntry
            // For now, journals reset on load
        }
    }

    public class JournalEntry : IExposable
    {
        public int Tick;
        public string Thought;
        public string Action;
        public string Target;
        public string Speech;

        public string TimeAgo
        {
            get
            {
                var ticksAgo = Find.TickManager.TicksGame - Tick;
                return ticksAgo.ToStringTicksToPeriod();
            }
        }

        public string FormattedTime
        {
            get
            {
                var hour = GenDate.HourOfDay(Tick, Find.WorldGrid.LongLatOf(Find.CurrentMap?.Tile ?? 0).x);
                var day = GenDate.DayOfQuadrum(Tick, Find.WorldGrid.LongLatOf(Find.CurrentMap?.Tile ?? 0).x) + 1;
                return $"Day {day}, {hour}:00";
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref Thought, "thought");
            Scribe_Values.Look(ref Action, "action");
            Scribe_Values.Look(ref Target, "target");
            Scribe_Values.Look(ref Speech, "speech");
        }
    }
}
