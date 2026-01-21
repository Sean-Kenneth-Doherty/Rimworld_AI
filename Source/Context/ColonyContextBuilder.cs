using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AdvancedColonistIntelligence.Context
{
    /// <summary>
    /// Extracts comprehensive context about the colony state.
    /// </summary>
    public static class ColonyContextBuilder
    {
        public static string BuildFullContext(Pawn pawn)
        {
            var sb = new StringBuilder();
            var map = pawn.Map;

            if (map == null)
            {
                sb.AppendLine("## COLONY STATUS: Not on a map");
                return sb.ToString();
            }

            sb.AppendLine(BuildTimeAndWeather(map));
            sb.AppendLine(BuildPopulationSection(map, pawn));
            sb.AppendLine(BuildResourcesSection(map));
            sb.AppendLine(BuildDefenseSection(map));
            sb.AppendLine(BuildThreatsSection(map, pawn));
            sb.AppendLine(BuildProjectsSection(map));
            sb.AppendLine(BuildFacilitiesSection(map));

            return sb.ToString();
        }

        private static string BuildTimeAndWeather(Map map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## TIME & ENVIRONMENT");

            // Time
            var hour = GenLocalDate.HourOfDay(map);
            var day = GenLocalDate.DayOfQuadrum(map) + 1;
            var quadrum = GenLocalDate.Quadrum(map);
            var year = GenLocalDate.Year(map);

            sb.AppendLine($"Time: {hour:00}:00");
            sb.AppendLine($"Date: {day} {quadrum}, Year {year}");

            // Season
            var season = GenLocalDate.Season(map);
            sb.AppendLine($"Season: {season}");

            // Weather
            var weather = map.weatherManager?.curWeather;
            if (weather != null)
                sb.AppendLine($"Weather: {weather.LabelCap}");

            // Temperature
            var temp = map.mapTemperature?.OutdoorTemp ?? 0;
            sb.AppendLine($"Temperature: {temp:F1}°C ({(temp * 9 / 5 + 32):F1}°F)");

            // Daytime
            var dayPercent = GenLocalDate.DayPercent(map);
            var timeOfDay = dayPercent < 0.25f ? "Night" :
                           dayPercent < 0.5f ? "Morning" :
                           dayPercent < 0.75f ? "Afternoon" : "Evening";
            sb.AppendLine($"Time of day: {timeOfDay}");

            return sb.ToString();
        }

        private static string BuildPopulationSection(Map map, Pawn self)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## COLONY POPULATION");

            var colonists = map.mapPawns?.FreeColonists?.ToList() ?? new List<Pawn>();
            var prisoners = map.mapPawns?.PrisonersOfColony?.ToList() ?? new List<Pawn>();
            var guests = map.mapPawns?.AllPawns?.Where(p => p.guest?.IsPrisoner == false && p.HostFaction == Faction.OfPlayer).ToList() ?? new List<Pawn>();
            var animals = map.mapPawns?.SpawnedColonyAnimals?.ToList() ?? new List<Pawn>();

            sb.AppendLine($"Colonists: {colonists.Count}");
            sb.AppendLine($"Prisoners: {prisoners.Count}");
            sb.AppendLine($"Colony Animals: {animals.Count}");

            // List colonists with status
            sb.AppendLine("\n### Colonists:");
            foreach (var pawn in colonists)
            {
                var isSelf = pawn == self ? " (YOU)" : "";
                var status = GetPawnStatus(pawn);
                var mood = pawn.needs?.mood?.CurLevelPercentage * 100 ?? 0;
                sb.AppendLine($"  - {pawn.Name.ToStringShort}{isSelf}: {status}, Mood {mood:F0}%");
            }

            // Prisoners
            if (prisoners.Count > 0)
            {
                sb.AppendLine("\n### Prisoners:");
                foreach (var prisoner in prisoners)
                {
                    var resistance = prisoner.guest?.Resistance ?? 0;
                    var will = prisoner.guest?.will ?? 0;
                    sb.AppendLine($"  - {prisoner.Name.ToStringShort}: Resistance {resistance:F0}, Will {will:F0}");
                }
            }

            // Animals summary
            if (animals.Count > 0)
            {
                var animalGroups = animals.GroupBy(a => a.def.label);
                sb.AppendLine("\n### Animals:");
                foreach (var group in animalGroups.OrderByDescending(g => g.Count()))
                    sb.AppendLine($"  - {group.Key}: {group.Count()}");
            }

            return sb.ToString();
        }

        private static string BuildResourcesSection(Map map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## RESOURCES");

            // Silver
            var silver = map.resourceCounter?.GetCount(ThingDefOf.Silver) ?? 0;
            sb.AppendLine($"Silver: {silver}");

            // Food
            var meals = CountThingsOfCategory(map, ThingCategoryDefOf.Foods);
            var rawFood = CountThingsOfCategory(map, ThingCategoryDefOf.MeatRaw) +
                         CountThingsOfCategory(map, ThingCategoryDefOf.PlantFoodRaw);
            var daysOfFood = EstimateFoodDays(map);
            sb.AppendLine($"Prepared meals: {meals}");
            sb.AppendLine($"Raw food: {rawFood}");
            sb.AppendLine($"Estimated days of food: {daysOfFood:F1}");

            // Medicine
            var medicine = map.resourceCounter?.GetCount(ThingDefOf.MedicineHerbal) ?? 0;
            medicine += map.resourceCounter?.GetCount(ThingDefOf.MedicineIndustrial) ?? 0;
            medicine += map.resourceCounter?.GetCount(ThingDefOf.MedicineUltratech) ?? 0;
            sb.AppendLine($"Medicine: {medicine}");

            // Components
            var components = map.resourceCounter?.GetCount(ThingDefOf.ComponentIndustrial) ?? 0;
            var advComponents = map.resourceCounter?.GetCount(ThingDefOf.ComponentSpacer) ?? 0;
            sb.AppendLine($"Components: {components}");
            sb.AppendLine($"Advanced components: {advComponents}");

            // Materials
            sb.AppendLine("\n### Building Materials:");
            var steel = map.resourceCounter?.GetCount(ThingDefOf.Steel) ?? 0;
            var plasteel = map.resourceCounter?.GetCount(ThingDefOf.Plasteel) ?? 0;
            var wood = map.resourceCounter?.GetCount(ThingDefOf.WoodLog) ?? 0;
            var stone = CountStoneBlocks(map);

            sb.AppendLine($"  Steel: {steel}");
            sb.AppendLine($"  Plasteel: {plasteel}");
            sb.AppendLine($"  Wood: {wood}");
            sb.AppendLine($"  Stone blocks: {stone}");

            // Power
            var powerNet = map.powerNetManager?.AllNetsListForReading?.FirstOrDefault();
            if (powerNet != null)
            {
                var stored = powerNet.CurrentStoredEnergy();
                var excess = powerNet.CurrentEnergyGainRate() * 60000; // Per day
                sb.AppendLine($"\n### Power:");
                sb.AppendLine($"  Stored: {stored:F0} Wd");
                sb.AppendLine($"  Net production: {excess:F0} W/day");
            }

            return sb.ToString();
        }

        private static string BuildDefenseSection(Map map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## DEFENSE");

            // Turrets
            var turrets = map.listerBuildings?.AllBuildingsColonistOfClass<Building_Turret>()?.ToList();
            if (turrets != null && turrets.Count > 0)
            {
                sb.AppendLine($"Turrets: {turrets.Count}");
                var turretGroups = turrets.GroupBy(t => t.def.label);
                foreach (var group in turretGroups)
                    sb.AppendLine($"  - {group.Key}: {group.Count()}");
            }

            // Traps
            var traps = map.listerBuildings?.AllBuildingsColonistOfClass<Building_Trap>()?.Count() ?? 0;
            sb.AppendLine($"Traps: {traps}");

            // Walls - rough count
            var walls = map.listerBuildings?.allBuildingsColonist?
                .Count(b => b.def.building?.isPlaceOverableWall == true || b.def == ThingDefOf.Wall) ?? 0;
            sb.AppendLine($"Wall segments: {walls}");

            // Combat-capable colonists
            var fighters = map.mapPawns?.FreeColonists?
                .Where(p => !p.Downed && !p.InMentalState && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                .ToList();

            if (fighters != null)
            {
                var shooters = fighters.Count(p => p.skills?.GetSkill(SkillDefOf.Shooting)?.Level >= 8);
                var melee = fighters.Count(p => p.skills?.GetSkill(SkillDefOf.Melee)?.Level >= 8);
                sb.AppendLine($"\nCombat-ready colonists: {fighters.Count}");
                sb.AppendLine($"  Skilled shooters: {shooters}");
                sb.AppendLine($"  Skilled melee: {melee}");
            }

            return sb.ToString();
        }

        private static string BuildThreatsSection(Map map, Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## THREATS & DANGERS");

            // Hostile pawns on map
            var hostiles = map.mapPawns?.AllPawnsSpawned?
                .Where(p => p.HostileTo(Faction.OfPlayer) && !p.Downed && !p.Dead)
                .ToList();

            if (hostiles != null && hostiles.Count > 0)
            {
                sb.AppendLine($"*** ACTIVE THREAT: {hostiles.Count} hostile(s) on map! ***");
                var groups = hostiles.GroupBy(h => h.Faction?.Name ?? "Wild");
                foreach (var group in groups)
                    sb.AppendLine($"  - {group.Key}: {group.Count()}");
            }
            else
            {
                sb.AppendLine("No active threats on map.");
            }

            // Downed colonists
            var downed = map.mapPawns?.FreeColonists?.Where(p => p.Downed).ToList();
            if (downed != null && downed.Count > 0)
            {
                sb.AppendLine($"\n*** DOWNED COLONISTS: {downed.Count} ***");
                foreach (var p in downed)
                    sb.AppendLine($"  - {p.Name.ToStringShort}");
            }

            // Fire
            var fires = map.listerThings?.ThingsOfDef(ThingDefOf.Fire)?.Count ?? 0;
            if (fires > 0)
                sb.AppendLine($"\n*** FIRES: {fires} active fires! ***");

            // Toxic fallout / other conditions
            var gameConditions = map.gameConditionManager?.ActiveConditions;
            if (gameConditions != null && gameConditions.Count > 0)
            {
                sb.AppendLine("\nActive conditions:");
                foreach (var condition in gameConditions)
                    sb.AppendLine($"  - {condition.LabelCap}");
            }

            return sb.ToString();
        }

        private static string BuildProjectsSection(Map map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## ONGOING PROJECTS");

            // Construction
            var blueprints = map.listerThings?.ThingsInGroup(ThingRequestGroup.Blueprint)?.Count ?? 0;
            var frames = map.listerThings?.ThingsInGroup(ThingRequestGroup.BuildingFrame)?.Count ?? 0;
            if (blueprints > 0 || frames > 0)
                sb.AppendLine($"Construction: {blueprints} blueprints, {frames} frames in progress");

            // Research
            var currentResearch = Find.ResearchManager?.GetProject();
            if (currentResearch != null)
            {
                var progress = Find.ResearchManager.GetProgress(currentResearch) / currentResearch.baseCost * 100;
                sb.AppendLine($"Research: {currentResearch.LabelCap} ({progress:F0}% complete)");
            }

            // Growing
            var growingZones = map.zoneManager?.AllZones?.OfType<Zone_Growing>().ToList();
            if (growingZones != null && growingZones.Count > 0)
            {
                sb.AppendLine("\nGrowing zones:");
                foreach (var zone in growingZones)
                {
                    var plant = zone.GetPlantDefToGrow()?.LabelCap ?? "Nothing";
                    sb.AppendLine($"  - {zone.label}: {plant}");
                }
            }

            return sb.ToString();
        }

        private static string BuildFacilitiesSection(Map map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## FACILITIES");

            // Key buildings
            var buildings = map.listerBuildings?.allBuildingsColonist;
            if (buildings != null)
            {
                var beds = buildings.Count(b => b is Building_Bed);
                var tables = buildings.Count(b => b.def.surfaceType == SurfaceType.Eat);
                var stoves = buildings.Count(b => b.def.recipes?.Any(r => r.products?.Any(p => p.thingDef?.IsNutritionGivingIngestible == true) == true) == true);
                var researchBenches = buildings.Count(b => b.def == ThingDefOf.SimpleResearchBench || b.def == ThingDefOf.HiTechResearchBench);

                sb.AppendLine($"Beds: {beds}");
                sb.AppendLine($"Dining spots: {tables}");
                sb.AppendLine($"Cooking stations: {stoves}");
                sb.AppendLine($"Research benches: {researchBenches}");
            }

            // Stockpiles
            var stockpiles = map.zoneManager?.AllZones?.OfType<Zone_Stockpile>().ToList();
            if (stockpiles != null && stockpiles.Count > 0)
            {
                sb.AppendLine($"\nStockpile zones: {stockpiles.Count}");
                foreach (var zone in stockpiles.Take(5))
                    sb.AppendLine($"  - {zone.label}");
            }

            return sb.ToString();
        }

        private static string GetPawnStatus(Pawn pawn)
        {
            if (pawn.Downed) return "DOWNED";
            if (pawn.InMentalState) return $"Mental break: {pawn.MentalState.def.LabelCap}";
            if (pawn.Drafted) return "Combat ready";
            if (pawn.CurJob != null) return pawn.CurJob.def.reportString;
            return "Idle";
        }

        private static int CountThingsOfCategory(Map map, ThingCategoryDef category)
        {
            return map.listerThings?.ThingsInGroup(ThingRequestGroup.HaulableEver)?
                .Where(t => t.def.thingCategories?.Contains(category) == true)
                .Sum(t => t.stackCount) ?? 0;
        }

        private static int CountStoneBlocks(Map map)
        {
            return map.listerThings?.ThingsInGroup(ThingRequestGroup.HaulableEver)?
                .Where(t => t.def.IsWithinCategory(ThingCategoryDefOf.StoneBlocks))
                .Sum(t => t.stackCount) ?? 0;
        }

        private static float EstimateFoodDays(Map map)
        {
            var colonists = map.mapPawns?.FreeColonists?.Count() ?? 1;
            var nutrition = map.resourceCounter?.TotalHumanEdibleNutrition ?? 0;
            var dailyNeed = colonists * 1.6f; // Rough estimate
            return nutrition / dailyNeed;
        }
    }
}
