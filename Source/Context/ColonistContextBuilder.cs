using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.Context
{
    /// <summary>
    /// Extracts comprehensive context about a colonist for LLM consumption.
    /// This builds the complete picture of who the colonist is.
    /// </summary>
    public static class ColonistContextBuilder
    {
        public static string BuildFullContext(Pawn pawn)
        {
            var sb = new StringBuilder();

            sb.AppendLine(BuildIdentitySection(pawn));
            sb.AppendLine(BuildBackstorySection(pawn));
            sb.AppendLine(BuildPersonalitySection(pawn));
            sb.AppendLine(BuildSkillsSection(pawn));
            sb.AppendLine(BuildHealthSection(pawn));
            sb.AppendLine(BuildMoodSection(pawn));
            sb.AppendLine(BuildNeedsSection(pawn));
            sb.AppendLine(BuildRelationshipsSection(pawn));
            sb.AppendLine(BuildMemoriesSection(pawn));
            sb.AppendLine(BuildInventorySection(pawn));
            sb.AppendLine(BuildScheduleSection(pawn));
            sb.AppendLine(BuildCurrentSituationSection(pawn));

            return sb.ToString();
        }

        private static string BuildIdentitySection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## IDENTITY");

            var name = pawn.Name?.ToStringFull ?? "Unknown";
            var nickname = pawn.Name is NameTriple triple ? triple.Nick : name;

            sb.AppendLine($"Name: {name}");
            if (nickname != name)
                sb.AppendLine($"Goes by: {nickname}");

            sb.AppendLine($"Age: {pawn.ageTracker.AgeBiologicalYears} years old (biological)");
            if (pawn.ageTracker.AgeChronologicalYears != pawn.ageTracker.AgeBiologicalYears)
                sb.AppendLine($"Chronological age: {pawn.ageTracker.AgeChronologicalYears} (cryptosleep/time dilation)");

            sb.AppendLine($"Gender: {pawn.gender}");

            if (pawn.story?.bodyType != null)
                sb.AppendLine($"Body type: {pawn.story.bodyType.defName}");

            if (pawn.story?.hairDef != null)
                sb.AppendLine($"Hair: {pawn.story.hairDef.label}");

            if (pawn.story?.SkinColor != null)
                sb.AppendLine($"Skin tone: {DescribeColor(pawn.story.SkinColor)}");

            var title = pawn.story?.TitleCap;
            if (!string.IsNullOrEmpty(title))
                sb.AppendLine($"Title/Role: {title}");

            return sb.ToString();
        }

        private static string BuildBackstorySection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## LIFE HISTORY");

            // Childhood
            var childhood = pawn.story?.Childhood;
            if (childhood != null)
            {
                sb.AppendLine($"\n### Childhood: {childhood.title}");
                sb.AppendLine(childhood.FullDescriptionFor(pawn));
            }

            // Adulthood
            var adulthood = pawn.story?.Adulthood;
            if (adulthood != null)
            {
                sb.AppendLine($"\n### Adulthood: {adulthood.title}");
                sb.AppendLine(adulthood.FullDescriptionFor(pawn));
            }

            // Work incapabilities from backstory
            var disabledWorkTags = pawn.CombinedDisabledWorkTags;
            if (disabledWorkTags != WorkTags.None)
            {
                sb.AppendLine("\n### Incapable of:");
                foreach (WorkTags tag in Enum.GetValues(typeof(WorkTags)))
                {
                    if (tag != WorkTags.None && (disabledWorkTags & tag) != 0)
                        sb.AppendLine($"- {tag}");
                }
            }

            return sb.ToString();
        }

        private static string BuildPersonalitySection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## PERSONALITY TRAITS");

            var traits = pawn.story?.traits?.allTraits;
            if (traits != null && traits.Count > 0)
            {
                foreach (var trait in traits)
                {
                    sb.AppendLine($"\n### {trait.LabelCap}");
                    sb.AppendLine(trait.TipString(pawn));

                    // Add specific trait effects
                    if (trait.def.degreeDatas != null)
                    {
                        var degree = trait.def.DataAtDegree(trait.Degree);
                        if (degree != null)
                        {
                            if (degree.statOffsets != null)
                            {
                                foreach (var offset in degree.statOffsets)
                                    sb.AppendLine($"  - {offset.stat.LabelCap}: {offset.value:+0.##;-0.##}");
                            }
                            if (degree.statFactors != null)
                            {
                                foreach (var factor in degree.statFactors)
                                    sb.AppendLine($"  - {factor.stat.LabelCap}: x{factor.value:0.##}");
                            }
                        }
                    }
                }
            }

            // Ideo if applicable
            if (pawn.Ideo != null)
            {
                sb.AppendLine($"\n### Ideology: {pawn.Ideo.name}");
                sb.AppendLine($"Culture: {pawn.Ideo.culture?.label ?? "Unknown"}");

                var precepts = pawn.Ideo.PreceptsListForReading;
                if (precepts.Count > 0)
                {
                    sb.AppendLine("Key beliefs:");
                    foreach (var precept in precepts.Take(10))
                        sb.AppendLine($"  - {precept.Label}");
                }
            }

            return sb.ToString();
        }

        private static string BuildSkillsSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## SKILLS & ABILITIES");

            var skills = pawn.skills?.skills;
            if (skills != null)
            {
                // Sort by level descending
                var sortedSkills = skills.OrderByDescending(s => s.Level);

                foreach (var skill in sortedSkills)
                {
                    var passionStr = skill.passion switch
                    {
                        Passion.None => "",
                        Passion.Minor => " [Interested]",
                        Passion.Major => " [PASSIONATE]",
                        _ => ""
                    };

                    var disabledStr = skill.TotallyDisabled ? " (DISABLED)" : "";
                    var levelBar = new string('█', skill.Level) + new string('░', 20 - skill.Level);

                    sb.AppendLine($"{skill.def.LabelCap,-15} [{levelBar}] {skill.Level,2}/20{passionStr}{disabledStr}");

                    // XP progress
                    if (!skill.TotallyDisabled && skill.Level < 20)
                    {
                        var xpProgress = skill.xpSinceLastLevel / skill.XpRequiredForLevelUp * 100;
                        sb.AppendLine($"                Progress to next level: {xpProgress:F0}%");
                    }
                }
            }

            return sb.ToString();
        }

        private static string BuildHealthSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## HEALTH STATUS");

            // Overall health
            var healthPercent = pawn.health.summaryHealth.SummaryHealthPercent * 100;
            sb.AppendLine($"Overall health: {healthPercent:F0}%");

            // Capacities
            sb.AppendLine("\n### Physical Capacities:");
            var capacities = new[]
            {
                PawnCapacityDefOf.Consciousness,
                PawnCapacityDefOf.Sight,
                PawnCapacityDefOf.Hearing,
                PawnCapacityDefOf.Moving,
                PawnCapacityDefOf.Manipulation,
                PawnCapacityDefOf.Talking,
                PawnCapacityDefOf.Eating,
                PawnCapacityDefOf.Breathing
            };

            foreach (var cap in capacities)
            {
                if (pawn.health.capacities.CapableOf(cap))
                {
                    var level = pawn.health.capacities.GetLevel(cap) * 100;
                    sb.AppendLine($"  {cap.LabelCap}: {level:F0}%");
                }
                else
                {
                    sb.AppendLine($"  {cap.LabelCap}: INCAPABLE");
                }
            }

            // Pain
            var painLevel = pawn.health.hediffSet.PainTotal * 100;
            if (painLevel > 0)
                sb.AppendLine($"\nPain level: {painLevel:F0}%");

            // Current injuries and conditions
            var hediffs = pawn.health.hediffSet.hediffs;
            if (hediffs.Count > 0)
            {
                var injuries = hediffs.Where(h => h is Hediff_Injury).ToList();
                var diseases = hediffs.Where(h => h.def.makesSickThought).ToList();
                var conditions = hediffs.Where(h => h is not Hediff_Injury && !h.def.makesSickThought && h.Visible).ToList();
                var implants = hediffs.Where(h => h.def.spawnThingOnRemoved != null).ToList();

                if (injuries.Count > 0)
                {
                    sb.AppendLine("\n### Current Injuries:");
                    foreach (var injury in injuries)
                    {
                        var part = injury.Part?.LabelCap ?? "Whole body";
                        sb.AppendLine($"  - {injury.LabelCap} ({part}) - Severity: {injury.Severity:F1}");
                    }
                }

                if (diseases.Count > 0)
                {
                    sb.AppendLine("\n### Diseases/Infections:");
                    foreach (var disease in diseases)
                    {
                        var part = disease.Part?.LabelCap ?? "Whole body";
                        var immunity = pawn.health.immunity.GetImmunity(disease.def) * 100;
                        sb.AppendLine($"  - {disease.LabelCap} ({part}) - Severity: {disease.Severity:F1}, Immunity: {immunity:F0}%");
                    }
                }

                if (conditions.Count > 0)
                {
                    sb.AppendLine("\n### Chronic Conditions:");
                    foreach (var condition in conditions)
                    {
                        var part = condition.Part?.LabelCap ?? "Whole body";
                        sb.AppendLine($"  - {condition.LabelCap} ({part})");
                    }
                }

                if (implants.Count > 0)
                {
                    sb.AppendLine("\n### Implants/Prosthetics:");
                    foreach (var implant in implants)
                    {
                        var part = implant.Part?.LabelCap ?? "Body";
                        sb.AppendLine($"  - {implant.LabelCap} ({part})");
                    }
                }
            }

            // Missing body parts
            var missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            if (missingParts.Count > 0)
            {
                sb.AppendLine("\n### Missing Body Parts:");
                foreach (var part in missingParts)
                    sb.AppendLine($"  - {part.Part.LabelCap}");
            }

            return sb.ToString();
        }

        private static string BuildMoodSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## MENTAL STATE");

            var needs = pawn.needs;
            var mood = needs?.mood;

            if (mood != null)
            {
                var moodLevel = mood.CurLevelPercentage * 100;
                var moodLabel = GetMoodLabel(moodLevel);

                sb.AppendLine($"Current mood: {moodLevel:F0}% ({moodLabel})");

                // Mental break thresholds
                sb.AppendLine($"\nMental break thresholds:");
                sb.AppendLine($"  Extreme break: <5%");
                sb.AppendLine($"  Major break: <15%");
                sb.AppendLine($"  Minor break: <25%");
                sb.AppendLine($"  Your current mood: {moodLevel:F0}%");

                // Thoughts
                var thoughts = mood.thoughts?.memories?.Memories;
                if (thoughts != null && thoughts.Count > 0)
                {
                    sb.AppendLine("\n### Active Thoughts:");
                    var grouped = thoughts
                        .Where(t => t.MoodOffset() != 0)
                        .OrderByDescending(t => Math.Abs(t.MoodOffset()))
                        .Take(15);

                    foreach (var thought in grouped)
                    {
                        var offset = thought.MoodOffset();
                        var sign = offset >= 0 ? "+" : "";
                        var timeLeft = thought.DurationTicks > 0
                            ? $" ({(thought.DurationTicks - thought.age).ToStringTicksToPeriod()} left)"
                            : " (permanent)";
                        sb.AppendLine($"  {sign}{offset:F0}: {thought.LabelCap}{timeLeft}");
                    }
                }

                // Situational thoughts
                var situational = mood.thoughts?.situational;
                if (situational != null)
                {
                    var sitThoughts = new List<Thought>();
                    situational.AppendMoodThoughts(sitThoughts);
                    if (sitThoughts.Count > 0)
                    {
                        sb.AppendLine("\n### Situational Thoughts:");
                        foreach (var thought in sitThoughts.OrderByDescending(t => Math.Abs(t.MoodOffset())).Take(10))
                        {
                            var offset = thought.MoodOffset();
                            var sign = offset >= 0 ? "+" : "";
                            sb.AppendLine($"  {sign}{offset:F0}: {thought.LabelCap}");
                        }
                    }
                }
            }

            // Mental state
            if (pawn.MentalState != null)
            {
                sb.AppendLine($"\n### CURRENT MENTAL BREAK: {pawn.MentalState.def.LabelCap}");
                sb.AppendLine(pawn.MentalState.def.description);
            }

            // Mental break risk
            if (mood != null && pawn.mindState != null)
            {
                var breakChance = pawn.mindState.mentalBreaker.BreakThresholdMinor;
                if (mood.CurLevel < breakChance)
                    sb.AppendLine("\n*** WARNING: At risk of mental break! ***");
            }

            return sb.ToString();
        }

        private static string BuildNeedsSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## NEEDS");

            var needs = pawn.needs?.AllNeeds;
            if (needs != null)
            {
                foreach (var need in needs.OrderBy(n => n.CurLevelPercentage))
                {
                    if (need.def == NeedDefOf.Mood) continue; // Covered in mood section

                    var level = need.CurLevelPercentage * 100;
                    var levelBar = new string('█', (int)(level / 5)) + new string('░', 20 - (int)(level / 5));
                    var urgency = level < 25 ? " [URGENT]" : level < 50 ? " [Low]" : "";

                    sb.AppendLine($"{need.LabelCap,-15} [{levelBar}] {level:F0}%{urgency}");
                }
            }

            return sb.ToString();
        }

        private static string BuildRelationshipsSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## RELATIONSHIPS");

            // Direct relations (family, spouse, etc)
            var relations = pawn.relations?.DirectRelations;
            if (relations != null && relations.Count > 0)
            {
                sb.AppendLine("\n### Family & Close Relations:");
                foreach (var rel in relations)
                {
                    var opinion = pawn.relations.OpinionOf(rel.otherPawn);
                    var opinionStr = opinion >= 0 ? $"+{opinion}" : $"{opinion}";
                    var alive = rel.otherPawn.Dead ? " (deceased)" : "";
                    var location = "";

                    if (!rel.otherPawn.Dead)
                    {
                        if (rel.otherPawn.Map == pawn.Map)
                            location = " - In colony";
                        else if (rel.otherPawn.Map != null)
                            location = " - Different map";
                        else
                            location = " - Location unknown";
                    }

                    sb.AppendLine($"  {rel.def.LabelCap}: {rel.otherPawn.Name.ToStringShort} (Opinion: {opinionStr}){alive}{location}");
                }
            }

            // Social relations with colonists
            var colonists = pawn.Map?.mapPawns?.FreeColonists?.Where(p => p != pawn).ToList();
            if (colonists != null && colonists.Count > 0)
            {
                sb.AppendLine("\n### Colony Social Standing:");
                foreach (var other in colonists.OrderByDescending(p => pawn.relations.OpinionOf(p)))
                {
                    var myOpinion = pawn.relations.OpinionOf(other);
                    var theirOpinion = other.relations.OpinionOf(pawn);

                    var relationLabel = GetRelationshipLabel(myOpinion);
                    var myOpinionStr = myOpinion >= 0 ? $"+{myOpinion}" : $"{myOpinion}";
                    var theirOpinionStr = theirOpinion >= 0 ? $"+{theirOpinion}" : $"{theirOpinion}";

                    sb.AppendLine($"  {other.Name.ToStringShort}: {relationLabel}");
                    sb.AppendLine($"    My opinion of them: {myOpinionStr}");
                    sb.AppendLine($"    Their opinion of me: {theirOpinionStr}");

                    // Recent interactions
                    var memories = pawn.needs?.mood?.thoughts?.memories?.Memories?
                        .Where(t => t.otherPawn == other)
                        .Take(3);

                    if (memories != null && memories.Any())
                    {
                        foreach (var mem in memories)
                            sb.AppendLine($"      - {mem.LabelCap}");
                    }
                }
            }

            // Romantic interests
            var romanticallyInterested = pawn.relations?.PotentiallyRelatedPawns?
                .Where(p => pawn.relations.SecondaryLovinChanceFactor(p) > 0.5f && !p.Dead)
                .Take(5);

            if (romanticallyInterested != null && romanticallyInterested.Any())
            {
                sb.AppendLine("\n### Potential Romantic Interests:");
                foreach (var p in romanticallyInterested)
                {
                    var chance = pawn.relations.SecondaryLovinChanceFactor(p) * 100;
                    sb.AppendLine($"  {p.Name.ToStringShort}: {chance:F0}% attraction");
                }
            }

            return sb.ToString();
        }

        private static string BuildMemoriesSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## RECENT MEMORIES & EXPERIENCES");

            var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
            if (memories != null && memories.Count > 0)
            {
                var recentMemories = memories
                    .OrderByDescending(m => m.age)
                    .Take(20);

                foreach (var memory in recentMemories)
                {
                    var timeAgo = memory.age.ToStringTicksToPeriod();
                    var effect = memory.MoodOffset();
                    var effectStr = effect >= 0 ? $"+{effect:F0}" : $"{effect:F0}";
                    var otherPawn = memory.otherPawn != null ? $" (involving {memory.otherPawn.Name.ToStringShort})" : "";

                    sb.AppendLine($"  [{effectStr}] {memory.LabelCap}{otherPawn} - {timeAgo} ago");
                }
            }

            // Records (kills, etc)
            if (pawn.records != null)
            {
                sb.AppendLine("\n### Lifetime Records:");
                sb.AppendLine($"  Kills: {pawn.records.GetValue(RecordDefOf.Kills):F0}");
                sb.AppendLine($"  Damage dealt: {pawn.records.GetValue(RecordDefOf.DamageDealt):F0}");
                sb.AppendLine($"  Damage taken: {pawn.records.GetValue(RecordDefOf.DamageTaken):F0}");
                sb.AppendLine($"  Times downed: {pawn.records.GetValue(RecordDefOf.TimesDowned):F0}");
                sb.AppendLine($"  Meals eaten: {pawn.records.GetValue(RecordDefOf.NumMealsEaten):F0}");
                sb.AppendLine($"  Surgeries performed: {pawn.records.GetValue(RecordDefOf.OperationsPerformed):F0}");
            }

            return sb.ToString();
        }

        private static string BuildInventorySection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## EQUIPMENT & INVENTORY");

            // Equipment
            var equipment = pawn.equipment?.AllEquipmentListForReading;
            if (equipment != null && equipment.Count > 0)
            {
                sb.AppendLine("\n### Weapons:");
                foreach (var item in equipment)
                {
                    var dps = item.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier, true);
                    sb.AppendLine($"  - {item.LabelCap}");
                }
            }

            // Apparel
            var apparel = pawn.apparel?.WornApparel;
            if (apparel != null && apparel.Count > 0)
            {
                sb.AppendLine("\n### Clothing & Armor:");
                foreach (var item in apparel)
                {
                    var condition = item.HitPoints / (float)item.MaxHitPoints * 100;
                    sb.AppendLine($"  - {item.LabelCap} ({condition:F0}% condition)");
                }
            }

            // Inventory
            var inventory = pawn.inventory?.innerContainer;
            if (inventory != null && inventory.Count > 0)
            {
                sb.AppendLine("\n### Carried Items:");
                foreach (var item in inventory)
                    sb.AppendLine($"  - {item.LabelCap} x{item.stackCount}");
            }

            return sb.ToString();
        }

        private static string BuildScheduleSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## SCHEDULE & WORK");

            // Current time and schedule
            var hour = GenLocalDate.HourOfDay(pawn);
            var assignment = pawn.timetable?.CurrentAssignment;
            sb.AppendLine($"Current hour: {hour}:00");
            sb.AppendLine($"Current schedule: {assignment?.LabelCap ?? "Any"}");

            // Work priorities
            if (pawn.workSettings != null && pawn.workSettings.EverWork)
            {
                sb.AppendLine("\n### Work Priorities:");
                var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(w => !pawn.WorkTypeIsDisabled(w))
                    .OrderByDescending(w => pawn.workSettings.GetPriority(w) > 0 ? 5 - pawn.workSettings.GetPriority(w) : -1);

                foreach (var work in workTypes)
                {
                    var priority = pawn.workSettings.GetPriority(work);
                    var priorityStr = priority == 0 ? "Disabled" : $"Priority {priority}";
                    sb.AppendLine($"  {work.labelShort,-15}: {priorityStr}");
                }
            }

            return sb.ToString();
        }

        private static string BuildCurrentSituationSection(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## CURRENT SITUATION");

            // Location
            var position = pawn.Position;
            var room = pawn.GetRoom();
            var roomName = room?.Role?.LabelCap ?? "Outside";

            sb.AppendLine($"Location: {roomName}");
            sb.AppendLine($"Position: ({position.x}, {position.z})");

            // Current job
            var job = pawn.CurJob;
            if (job != null)
            {
                sb.AppendLine($"\nCurrently doing: {job.def.reportString}");
                if (job.targetA.Thing != null)
                    sb.AppendLine($"  Target: {job.targetA.Thing.LabelCap}");
            }
            else
            {
                sb.AppendLine("\nCurrently: Idle (no current task)");
            }

            // Queued jobs
            var jobQueue = pawn.jobs?.jobQueue;
            if (jobQueue != null && jobQueue.Count > 0)
            {
                sb.AppendLine("\nQueued tasks:");
                foreach (var queuedJob in jobQueue)
                    sb.AppendLine($"  - {queuedJob.job.def.reportString}");
            }

            // Drafted status
            if (pawn.Drafted)
                sb.AppendLine("\n*** COMBAT MODE ACTIVE ***");

            // Nearby pawns
            var nearbyPawns = pawn.Map?.mapPawns?.AllPawnsSpawned?
                .Where(p => p != pawn && p.Position.DistanceTo(pawn.Position) < 10)
                .Take(10);

            if (nearbyPawns != null && nearbyPawns.Any())
            {
                sb.AppendLine("\n### Nearby:");
                foreach (var nearby in nearbyPawns)
                {
                    var distance = nearby.Position.DistanceTo(pawn.Position);
                    var faction = nearby.Faction?.Name ?? "Wild";
                    var hostile = nearby.HostileTo(pawn) ? " [HOSTILE]" : "";
                    sb.AppendLine($"  - {nearby.LabelCap} ({faction}) - {distance:F0} tiles away{hostile}");
                }
            }

            return sb.ToString();
        }

        private static string GetMoodLabel(float moodPercent)
        {
            if (moodPercent >= 90) return "Elated";
            if (moodPercent >= 75) return "Happy";
            if (moodPercent >= 60) return "Content";
            if (moodPercent >= 40) return "Neutral";
            if (moodPercent >= 25) return "Stressed";
            if (moodPercent >= 15) return "On Edge";
            if (moodPercent >= 5) return "Breaking";
            return "Broken";
        }

        private static string GetRelationshipLabel(int opinion)
        {
            if (opinion >= 80) return "Best Friend";
            if (opinion >= 60) return "Close Friend";
            if (opinion >= 40) return "Friend";
            if (opinion >= 20) return "Acquaintance";
            if (opinion >= 0) return "Neutral";
            if (opinion >= -20) return "Annoyed By";
            if (opinion >= -40) return "Dislikes";
            if (opinion >= -60) return "Hates";
            return "Rival/Enemy";
        }

        private static string DescribeColor(UnityEngine.Color color)
        {
            var brightness = (color.r + color.g + color.b) / 3;
            if (brightness > 0.8f) return "Light";
            if (brightness > 0.6f) return "Medium-light";
            if (brightness > 0.4f) return "Medium";
            if (brightness > 0.2f) return "Medium-dark";
            return "Dark";
        }
    }
}
