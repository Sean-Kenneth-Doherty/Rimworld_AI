using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.AI
{
    /// <summary>
    /// Core AI brain that enhances colonist decision-making capabilities.
    /// </summary>
    public class ColonistBrain
    {
        private readonly Pawn pawn;
        private readonly Dictionary<WorkTypeDef, float> workPriorities;

        public ColonistBrain(Pawn pawn)
        {
            this.pawn = pawn;
            this.workPriorities = new Dictionary<WorkTypeDef, float>();
        }

        /// <summary>
        /// Evaluates the current colony state and returns prioritized work.
        /// </summary>
        public WorkTypeDef GetOptimalWork()
        {
            // TODO: Implement intelligent work selection based on:
            // - Colony needs (food, defense, construction)
            // - Pawn skills and passions
            // - Current threats
            // - Time of day
            return null;
        }

        /// <summary>
        /// Calculates priority score for a given work type.
        /// </summary>
        public float CalculateWorkPriority(WorkTypeDef workType)
        {
            float priority = 0f;

            // Factor in pawn skill level
            var relevantSkill = workType.relevantSkills?.FirstOrFallback();
            if (relevantSkill != null)
            {
                var skillRecord = pawn.skills?.GetSkill(relevantSkill);
                if (skillRecord != null)
                {
                    priority += skillRecord.Level * 0.1f;
                    if (skillRecord.passion == Passion.Minor) priority += 0.5f;
                    if (skillRecord.passion == Passion.Major) priority += 1.0f;
                }
            }

            return priority;
        }
    }
}
