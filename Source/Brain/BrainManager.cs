using System.Collections.Generic;
using System.Linq;
using AdvancedColonistIntelligence.Settings;
using Verse;

namespace AdvancedColonistIntelligence.Brain
{
    /// <summary>
    /// Manages all AI colonist brains in the game.
    /// </summary>
    public class BrainManager : GameComponent
    {
        private static BrainManager instance;
        public static BrainManager Instance => instance;

        private Dictionary<string, ColonistBrain> brains = new Dictionary<string, ColonistBrain>();

        public BrainManager(Game game) : base()
        {
            instance = this;
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            InitializeBrains();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            InitializeBrains();
        }

        private void InitializeBrains()
        {
            brains.Clear();
            var settings = ACIMod.Settings;
            if (settings == null) return;

            foreach (var pawnId in settings.aiColonistIds)
            {
                var pawn = FindPawnById(pawnId);
                if (pawn != null)
                {
                    brains[pawnId] = new ColonistBrain(pawn);
                    Log.Message($"[ACI] Initialized brain for {pawn.Name.ToStringShort}");
                }
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            // Periodic check for new/removed AI colonists
            if (Find.TickManager.TicksGame % 250 == 0) // Every ~4 seconds
            {
                SyncBrains();
            }

            // Periodic query for continuous mode
            var settings = ACIMod.Settings;
            if (settings?.queryTiming == QueryTiming.Periodic)
            {
                var tickInterval = settings.minQueryDelaySeconds * 60; // Convert to ticks
                if (Find.TickManager.TicksGame % tickInterval == 0)
                {
                    foreach (var brain in brains.Values)
                    {
                        if (!brain.IsProcessing)
                        {
                            brain.RequestDecision("Periodic check-in");
                        }
                    }
                }
            }
        }

        private void SyncBrains()
        {
            var settings = ACIMod.Settings;
            if (settings == null) return;

            // Add new brains
            foreach (var pawnId in settings.aiColonistIds)
            {
                if (!brains.ContainsKey(pawnId))
                {
                    var pawn = FindPawnById(pawnId);
                    if (pawn != null && !pawn.Dead)
                    {
                        brains[pawnId] = new ColonistBrain(pawn);
                        Log.Message($"[ACI] Added brain for {pawn.Name.ToStringShort}");
                    }
                }
            }

            // Remove brains for deselected or dead colonists
            var toRemove = brains.Keys
                .Where(id => !settings.aiColonistIds.Contains(id) || FindPawnById(id)?.Dead == true)
                .ToList();

            foreach (var id in toRemove)
            {
                brains.Remove(id);
                Log.Message($"[ACI] Removed brain for pawn {id}");
            }
        }

        public ColonistBrain GetBrain(Pawn pawn)
        {
            if (pawn == null) return null;
            brains.TryGetValue(pawn.ThingID, out var brain);
            return brain;
        }

        public bool HasBrain(Pawn pawn)
        {
            return pawn != null && brains.ContainsKey(pawn.ThingID);
        }

        public IEnumerable<ColonistBrain> AllBrains => brains.Values;

        /// <summary>
        /// Trigger decision for a specific pawn if they have a brain.
        /// </summary>
        public void TriggerDecision(Pawn pawn, string reason)
        {
            var brain = GetBrain(pawn);
            brain?.RequestDecision(reason);
        }

        /// <summary>
        /// Trigger decision for all AI colonists.
        /// </summary>
        public void TriggerAllDecisions(string reason)
        {
            foreach (var brain in brains.Values)
            {
                brain.RequestDecision(reason);
            }
        }

        private Pawn FindPawnById(string thingId)
        {
            return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists
                .FirstOrDefault(p => p.ThingID == thingId);
        }
    }
}
