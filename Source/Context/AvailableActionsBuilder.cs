using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.Context
{
    /// <summary>
    /// Builds a list of available actions the AI colonist can take.
    /// </summary>
    public static class AvailableActionsBuilder
    {
        public static string BuildAvailableActions(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## AVAILABLE ACTIONS");
            sb.AppendLine("Choose ONE action from below. Use the exact action name and provide a target if needed.");

            var actions = GetAvailableActions(pawn);
            foreach (var category in actions.GroupBy(a => a.Category))
            {
                sb.AppendLine($"\n### {category.Key}:");
                foreach (var action in category)
                {
                    var targetStr = action.Targets.Count > 0
                        ? $" [Targets: {string.Join(", ", action.Targets.Take(5))}]"
                        : "";
                    sb.AppendLine($"  - {action.Name}: {action.Description}{targetStr}");
                }
            }

            return sb.ToString();
        }

        public static List<AvailableAction> GetAvailableActions(Pawn pawn)
        {
            var actions = new List<AvailableAction>();
            var map = pawn.Map;

            if (map == null) return actions;

            // === BASIC NEEDS ===
            actions.AddRange(GetNeedActions(pawn));

            // === WORK ===
            actions.AddRange(GetWorkActions(pawn));

            // === SOCIAL ===
            actions.AddRange(GetSocialActions(pawn));

            // === COMBAT ===
            actions.AddRange(GetCombatActions(pawn));

            // === MOVEMENT ===
            actions.AddRange(GetMovementActions(pawn));

            // === MISC ===
            actions.Add(new AvailableAction
            {
                Name = "wait",
                Category = "Misc",
                Description = "Do nothing, stand idle and observe"
            });

            return actions;
        }

        private static List<AvailableAction> GetNeedActions(Pawn pawn)
        {
            var actions = new List<AvailableAction>();
            var needs = pawn.needs;

            // Eating
            if (needs?.food != null && needs.food.CurLevelPercentage < 0.9f)
            {
                var foods = pawn.Map?.listerThings?.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree)?
                    .Where(t => t.def.IsNutritionGivingIngestible && pawn.CanReserve(t))
                    .Select(t => t.LabelCapNoCount)
                    .Distinct()
                    .Take(5)
                    .ToList() ?? new List<string>();

                actions.Add(new AvailableAction
                {
                    Name = "eat",
                    Category = "Needs",
                    Description = "Find and consume food",
                    Targets = foods
                });
            }

            // Sleeping
            var bed = pawn.ownership?.OwnedBed;
            if (needs?.rest != null && needs.rest.CurLevelPercentage < 0.8f)
            {
                actions.Add(new AvailableAction
                {
                    Name = "rest",
                    Category = "Needs",
                    Description = bed != null ? $"Go to bed ({bed.Label})" : "Find a place to sleep",
                    Targets = bed != null ? new List<string> { bed.Label } : new List<string>()
                });
            }

            // Recreation
            if (needs?.joy != null && needs.joy.CurLevelPercentage < 0.8f)
            {
                var joyGivers = pawn.Map?.listerThings?.AllThings?
                    .Where(t => t.def.building?.joyKind != null && pawn.CanReserve(t))
                    .Select(t => t.LabelCapNoCount)
                    .Distinct()
                    .Take(5)
                    .ToList() ?? new List<string>();

                actions.Add(new AvailableAction
                {
                    Name = "recreation",
                    Category = "Needs",
                    Description = "Do something fun for joy",
                    Targets = joyGivers
                });
            }

            return actions;
        }

        private static List<AvailableAction> GetWorkActions(Pawn pawn)
        {
            var actions = new List<AvailableAction>();

            if (pawn.workSettings == null || !pawn.workSettings.EverWork)
                return actions;

            var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => !pawn.WorkTypeIsDisabled(w) && pawn.workSettings.GetPriority(w) > 0)
                .OrderBy(w => pawn.workSettings.GetPriority(w));

            foreach (var workType in workTypes)
            {
                var targets = GetWorkTargets(pawn, workType);
                if (targets.Count > 0 || IsAlwaysAvailableWork(workType))
                {
                    actions.Add(new AvailableAction
                    {
                        Name = $"work:{workType.defName.ToLower()}",
                        Category = "Work",
                        Description = $"Do {workType.labelShort} work",
                        Targets = targets.Take(5).ToList()
                    });
                }
            }

            return actions;
        }

        private static List<string> GetWorkTargets(Pawn pawn, WorkTypeDef workType)
        {
            var targets = new List<string>();
            var map = pawn.Map;

            // Get specific targets based on work type
            if (workType == WorkTypeDefOf.Doctor)
            {
                var patients = map.mapPawns?.AllPawnsSpawned?
                    .Where(p => p.health?.HasHediffsNeedingTend(false) == true && pawn.CanReserve(p))
                    .Select(p => p.Name.ToStringShort);
                if (patients != null) targets.AddRange(patients);
            }
            else if (workType == WorkTypeDefOf.Construction)
            {
                var frames = map.listerThings?.ThingsInGroup(ThingRequestGroup.BuildingFrame)?
                    .Where(t => pawn.CanReserve(t))
                    .Select(t => t.LabelCapNoCount)
                    .Distinct();
                if (frames != null) targets.AddRange(frames);
            }
            else if (workType == WorkTypeDefOf.Cooking)
            {
                var stoves = map.listerBuildings?.allBuildingsColonist?
                    .Where(b => b.def.recipes?.Any(r => r.products?.Any(p => p.thingDef?.IsNutritionGivingIngestible == true) == true) == true)
                    .Select(b => b.LabelCapNoCount)
                    .Distinct();
                if (stoves != null) targets.AddRange(stoves);
            }
            else if (workType == WorkTypeDefOf.Mining)
            {
                var minables = map.listerThings?.ThingsInGroup(ThingRequestGroup.Mineable)?
                    .Where(t => pawn.CanReserve(t))
                    .Select(t => t.def.LabelCap)
                    .Distinct();
                if (minables != null) targets.AddRange(minables);
            }
            else if (workType == WorkTypeDefOf.Hunting)
            {
                var huntables = map.mapPawns?.AllPawnsSpawned?
                    .Where(p => p.RaceProps?.Animal == true && map.designationManager.DesignationOn(p, DesignationDefOf.Hunt) != null)
                    .Select(p => p.LabelCapNoCount)
                    .Distinct();
                if (huntables != null) targets.AddRange(huntables);
            }
            else if (workType == WorkTypeDefOf.Hauling)
            {
                var haulables = map.listerHaulables?.ThingsPotentiallyNeedingHauling()?
                    .Where(t => pawn.CanReserve(t))
                    .Take(10)
                    .Select(t => t.LabelCapNoCount)
                    .Distinct();
                if (haulables != null) targets.AddRange(haulables);
            }
            else if (workType == WorkTypeDefOf.Cleaning)
            {
                var filth = map.listerFilthInHomeArea?.FilthInHomeArea?.Count ?? 0;
                if (filth > 0) targets.Add($"{filth} filth spots");
            }
            else if (workType == WorkTypeDefOf.Research)
            {
                var bench = map.listerBuildings?.allBuildingsColonist?
                    .FirstOrDefault(b => b.def == ThingDefOf.SimpleResearchBench || b.def == ThingDefOf.HiTechResearchBench);
                if (bench != null) targets.Add(bench.LabelCapNoCount);
            }

            return targets;
        }

        private static bool IsAlwaysAvailableWork(WorkTypeDef workType)
        {
            // Some work types don't have discrete targets
            return workType == WorkTypeDefOf.Research ||
                   workType == WorkTypeDefOf.Cleaning ||
                   workType == WorkTypeDefOf.Hauling;
        }

        private static List<AvailableAction> GetSocialActions(Pawn pawn)
        {
            var actions = new List<AvailableAction>();
            var map = pawn.Map;

            var nearbyColonists = map.mapPawns?.FreeColonists?
                .Where(p => p != pawn && !p.Downed && !p.Dead && p.Position.DistanceTo(pawn.Position) < 30)
                .ToList() ?? new List<Pawn>();

            if (nearbyColonists.Count > 0)
            {
                var colonistNames = nearbyColonists.Select(p => p.Name.ToStringShort).ToList();

                actions.Add(new AvailableAction
                {
                    Name = "social:chat",
                    Category = "Social",
                    Description = "Have a casual chat with someone",
                    Targets = colonistNames
                });

                actions.Add(new AvailableAction
                {
                    Name = "social:deep_talk",
                    Category = "Social",
                    Description = "Have a meaningful conversation",
                    Targets = colonistNames
                });

                // Romance (if applicable)
                var romanticOptions = nearbyColonists
                    .Where(p => pawn.relations?.SecondaryLovinChanceFactor(p) > 0.1f)
                    .Select(p => p.Name.ToStringShort)
                    .ToList();

                if (romanticOptions.Count > 0)
                {
                    actions.Add(new AvailableAction
                    {
                        Name = "social:romance",
                        Category = "Social",
                        Description = "Make a romantic advance",
                        Targets = romanticOptions
                    });
                }
            }

            return actions;
        }

        private static List<AvailableAction> GetCombatActions(Pawn pawn)
        {
            var actions = new List<AvailableAction>();
            var map = pawn.Map;
            var settings = Settings.ACIMod.Settings;

            // Draft/undraft
            if (settings?.canDraftSelf == true)
            {
                if (pawn.Drafted)
                {
                    actions.Add(new AvailableAction
                    {
                        Name = "undraft",
                        Category = "Combat",
                        Description = "Exit combat mode and return to normal activities"
                    });
                }
                else
                {
                    actions.Add(new AvailableAction
                    {
                        Name = "draft",
                        Category = "Combat",
                        Description = "Enter combat mode"
                    });
                }
            }

            // Combat actions when drafted
            if (pawn.Drafted)
            {
                var hostiles = map.mapPawns?.AllPawnsSpawned?
                    .Where(p => p.HostileTo(pawn) && !p.Downed && !p.Dead)
                    .OrderBy(p => p.Position.DistanceTo(pawn.Position))
                    .Take(5)
                    .ToList() ?? new List<Pawn>();

                if (hostiles.Count > 0)
                {
                    actions.Add(new AvailableAction
                    {
                        Name = "attack",
                        Category = "Combat",
                        Description = "Attack a hostile target",
                        Targets = hostiles.Select(h => $"{h.LabelCapNoCount} ({h.Position.DistanceTo(pawn.Position):F0} tiles)").ToList()
                    });
                }

                actions.Add(new AvailableAction
                {
                    Name = "flee",
                    Category = "Combat",
                    Description = "Run away from danger",
                    Targets = new List<string> { "north", "south", "east", "west", "indoors" }
                });

                actions.Add(new AvailableAction
                {
                    Name = "take_cover",
                    Category = "Combat",
                    Description = "Find and move to cover"
                });
            }

            return actions;
        }

        private static List<AvailableAction> GetMovementActions(Pawn pawn)
        {
            var actions = new List<AvailableAction>();
            var map = pawn.Map;

            // Get notable locations
            var rooms = map.regionGrid?.allRooms?
                .Where(r => r.Role != RoomRoleDefOf.None)
                .Select(r => r.Role.LabelCap)
                .Distinct()
                .Take(10)
                .ToList() ?? new List<string>();

            if (rooms.Count > 0)
            {
                actions.Add(new AvailableAction
                {
                    Name = "go_to",
                    Category = "Movement",
                    Description = "Move to a specific location",
                    Targets = rooms
                });
            }

            // Go to specific pawn
            var colonists = map.mapPawns?.FreeColonists?
                .Where(p => p != pawn)
                .Select(p => p.Name.ToStringShort)
                .ToList() ?? new List<string>();

            if (colonists.Count > 0)
            {
                actions.Add(new AvailableAction
                {
                    Name = "go_to_pawn",
                    Category = "Movement",
                    Description = "Move to be near a specific colonist",
                    Targets = colonists
                });
            }

            return actions;
        }
    }

    public class AvailableAction
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public List<string> Targets { get; set; } = new List<string>();
    }
}
