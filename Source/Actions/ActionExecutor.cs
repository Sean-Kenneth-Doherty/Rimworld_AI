using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedColonistIntelligence.Display;
using AdvancedColonistIntelligence.Settings;
using RimWorld;
using Verse;
using Verse.AI;

namespace AdvancedColonistIntelligence.Actions
{
    /// <summary>
    /// Executes parsed AI actions by translating them to RimWorld jobs.
    /// </summary>
    public static class ActionExecutor
    {
        public static bool Execute(Pawn pawn, AIAction action)
        {
            if (pawn == null || action == null || !action.IsValid)
                return false;

            var settings = ACIMod.Settings;

            // Display thought bubble
            if (settings.showThoughtBubbles && !string.IsNullOrEmpty(action.Thought))
            {
                ThoughtBubbleManager.ShowBubble(pawn, action.Thought);
            }

            // Log to journal
            if (settings.logToJournal)
            {
                JournalManager.AddEntry(pawn, action);
            }

            // Display speech
            if (!string.IsNullOrEmpty(action.Speech))
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, action.Speech, 5f);
            }

            // Execute the action
            var actionName = action.ActionName.ToLower();
            var target = action.Target;

            try
            {
                // Parse compound actions (work:cooking, social:chat, etc.)
                if (actionName.Contains(":"))
                {
                    var parts = actionName.Split(':');
                    var category = parts[0];
                    var subAction = parts.Length > 1 ? parts[1] : "";

                    return category switch
                    {
                        "work" => ExecuteWork(pawn, subAction, target),
                        "social" => ExecuteSocial(pawn, subAction, target),
                        _ => ExecuteSimpleAction(pawn, actionName, target)
                    };
                }

                return ExecuteSimpleAction(pawn, actionName, target);
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] Failed to execute action {action}: {ex.Message}");
                return false;
            }
        }

        private static bool ExecuteSimpleAction(Pawn pawn, string actionName, string target)
        {
            switch (actionName)
            {
                case "wait":
                    // Do nothing intentionally
                    return true;

                case "eat":
                    return ExecuteEat(pawn, target);

                case "rest":
                    return ExecuteRest(pawn);

                case "recreation":
                    return ExecuteRecreation(pawn, target);

                case "draft":
                    return ExecuteDraft(pawn, true);

                case "undraft":
                    return ExecuteDraft(pawn, false);

                case "attack":
                    return ExecuteAttack(pawn, target);

                case "flee":
                    return ExecuteFlee(pawn, target);

                case "take_cover":
                    return ExecuteTakeCover(pawn);

                case "go_to":
                    return ExecuteGoTo(pawn, target);

                case "go_to_pawn":
                    return ExecuteGoToPawn(pawn, target);

                case "comply":
                    // For refusal prompts - do nothing special
                    return true;

                case "refuse":
                    return ExecuteRefuse(pawn);

                default:
                    Log.Warning($"[ACI] Unknown action: {actionName}");
                    return false;
            }
        }

        private static bool ExecuteWork(Pawn pawn, string workType, string target)
        {
            if (pawn.workSettings == null)
                return false;

            // Find the work type def
            var workDef = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .FirstOrDefault(w => w.defName.ToLower() == workType || w.labelShort.ToLower() == workType);

            if (workDef == null)
            {
                Log.Warning($"[ACI] Unknown work type: {workType}");
                return false;
            }

            // Check if pawn can do this work
            if (pawn.WorkTypeIsDisabled(workDef))
            {
                Log.Warning($"[ACI] {pawn.Name.ToStringShort} cannot do {workDef.labelShort}");
                return false;
            }

            // Try to find and start a job for this work type
            foreach (var workGiver in workDef.workGiversByPriority)
            {
                var job = workGiver.Worker.NonScanJob(pawn);
                if (job != null)
                {
                    pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    return true;
                }

                // Try scanning for work
                var scanner = workGiver.Worker as WorkGiver_Scanner;
                if (scanner != null)
                {
                    var things = scanner.PotentialWorkThingsGlobal(pawn);
                    if (things != null)
                    {
                        foreach (var thing in things.Take(10))
                        {
                            if (scanner.HasJobOnThing(pawn, thing, false))
                            {
                                job = scanner.JobOnThing(pawn, thing, false);
                                if (job != null)
                                {
                                    pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool ExecuteSocial(Pawn pawn, string interactionType, string targetName)
        {
            var targetPawn = FindPawnByName(pawn.Map, targetName);
            if (targetPawn == null)
            {
                Log.Warning($"[ACI] Could not find pawn: {targetName}");
                return false;
            }

            // Try to initiate interaction
            InteractionDef interactionDef = interactionType switch
            {
                "chat" => InteractionDefOf.Chitchat,
                "deep_talk" => InteractionDefOf.DeepTalk,
                "romance" => InteractionDefOf.RomanceAttempt,
                _ => InteractionDefOf.Chitchat
            };

            // Create a job to go interact with the target
            var job = JobMaker.MakeJob(JobDefOf.GotoWander, targetPawn.Position);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);

            // Queue the interaction
            pawn.interactions?.TryInteractWith(targetPawn, interactionDef);

            return true;
        }

        private static bool ExecuteEat(Pawn pawn, string target)
        {
            var food = pawn.Map?.listerThings?.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree)?
                .Where(t => t.def.IsNutritionGivingIngestible && pawn.CanReserve(t))
                .OrderByDescending(t => FoodUtility.FoodOptimality(pawn, t, t.def, 0f, false))
                .FirstOrDefault();

            if (food == null)
                return false;

            var job = JobMaker.MakeJob(JobDefOf.Ingest, food);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteRest(Pawn pawn)
        {
            var bed = pawn.ownership?.OwnedBed ?? RestUtility.FindBedFor(pawn);
            if (bed == null)
            {
                // Find any spot to rest
                var spot = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 10);
                var job = JobMaker.MakeJob(JobDefOf.LayDown, spot);
                job.forceSleep = true;
                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                return true;
            }

            var bedJob = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            bedJob.forceSleep = true;
            pawn.jobs.StartJob(bedJob, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteRecreation(Pawn pawn, string target)
        {
            var joyGiver = pawn.Map?.listerThings?.AllThings?
                .Where(t => t.def.building?.joyKind != null && pawn.CanReserve(t))
                .FirstOrDefault();

            if (joyGiver != null)
            {
                var job = JobMaker.MakeJob(JobDefOf.Goto, joyGiver.Position);
                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                return true;
            }

            // Wander for joy
            var wanderJob = JobMaker.MakeJob(JobDefOf.GotoWander, CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 20));
            pawn.jobs.StartJob(wanderJob, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteDraft(Pawn pawn, bool draft)
        {
            if (pawn.drafter == null)
                return false;

            pawn.drafter.Drafted = draft;
            return true;
        }

        private static bool ExecuteAttack(Pawn pawn, string target)
        {
            if (!pawn.Drafted)
                return false;

            var hostile = pawn.Map?.mapPawns?.AllPawnsSpawned?
                .Where(p => p.HostileTo(pawn) && !p.Downed && !p.Dead)
                .OrderBy(p => p.Position.DistanceTo(pawn.Position))
                .FirstOrDefault();

            if (hostile == null)
                return false;

            var job = JobMaker.MakeJob(JobDefOf.AttackMelee, hostile);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteFlee(Pawn pawn, string direction)
        {
            IntVec3 fleeTarget;

            switch (direction?.ToLower())
            {
                case "north":
                    fleeTarget = new IntVec3(pawn.Position.x, 0, pawn.Map.Size.z - 5);
                    break;
                case "south":
                    fleeTarget = new IntVec3(pawn.Position.x, 0, 5);
                    break;
                case "east":
                    fleeTarget = new IntVec3(pawn.Map.Size.x - 5, 0, pawn.Position.z);
                    break;
                case "west":
                    fleeTarget = new IntVec3(5, 0, pawn.Position.z);
                    break;
                case "indoors":
                    var room = pawn.Map.regionGrid?.allRooms?.FirstOrDefault(r => r.Role != RoomRoleDefOf.None && r.CellCount > 10);
                    fleeTarget = room?.Cells.FirstOrDefault() ?? CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 30);
                    break;
                default:
                    fleeTarget = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 30);
                    break;
            }

            var job = JobMaker.MakeJob(JobDefOf.FleeAndCower, fleeTarget);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteTakeCover(Pawn pawn)
        {
            var coverCell = CoverUtility.BestCoverFor(pawn, pawn.Position, pawn.Map);
            if (coverCell == pawn.Position)
            {
                // Already in good cover
                return true;
            }

            var job = JobMaker.MakeJob(JobDefOf.Goto, coverCell);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteGoTo(Pawn pawn, string location)
        {
            // Find room by role
            var room = pawn.Map.regionGrid?.allRooms?
                .FirstOrDefault(r => r.Role?.LabelCap.ToString().ToLower().Contains(location.ToLower()) == true);

            if (room != null)
            {
                var cell = room.Cells.FirstOrDefault(c => c.Standable(pawn.Map));
                if (cell != default)
                {
                    var job = JobMaker.MakeJob(JobDefOf.Goto, cell);
                    pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    return true;
                }
            }

            return false;
        }

        private static bool ExecuteGoToPawn(Pawn pawn, string targetName)
        {
            var targetPawn = FindPawnByName(pawn.Map, targetName);
            if (targetPawn == null)
                return false;

            var cell = CellFinder.RandomClosewalkCellNear(targetPawn.Position, pawn.Map, 3);
            var job = JobMaker.MakeJob(JobDefOf.Goto, cell);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            return true;
        }

        private static bool ExecuteRefuse(Pawn pawn)
        {
            // Clear current job queue
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);

            // Notify player
            var settings = ACIMod.Settings;
            if (settings.notifyMajorDecisions)
            {
                Messages.Message(
                    $"{pawn.Name.ToStringShort} refused your order.",
                    pawn,
                    MessageTypeDefOf.NeutralEvent
                );
            }

            return true;
        }

        private static Pawn FindPawnByName(Map map, string name)
        {
            if (string.IsNullOrEmpty(name) || map == null)
                return null;

            name = name.ToLower();

            return map.mapPawns?.AllPawnsSpawned?
                .FirstOrDefault(p =>
                    p.Name?.ToStringShort?.ToLower() == name ||
                    p.Name?.ToStringFull?.ToLower().Contains(name) == true ||
                    p.LabelShort?.ToLower() == name
                );
        }
    }

    /// <summary>
    /// Utility for finding cover positions.
    /// </summary>
    public static class CoverUtility
    {
        public static IntVec3 BestCoverFor(Pawn pawn, IntVec3 from, Map map)
        {
            var bestCell = pawn.Position;
            var bestCover = 0f;

            foreach (var cell in GenRadial.RadialCellsAround(pawn.Position, 10, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map))
                    continue;

                var cover = cell.GetCoverFor(pawn, from, map);
                if (cover > bestCover)
                {
                    bestCover = cover;
                    bestCell = cell;
                }
            }

            return bestCell;
        }

        private static float GetCoverFor(this IntVec3 cell, Pawn pawn, IntVec3 from, Map map)
        {
            var things = cell.GetThingList(map);
            var cover = 0f;

            foreach (var thing in things)
            {
                if (thing.def.fillPercent > cover)
                    cover = thing.def.fillPercent;
            }

            return cover;
        }
    }
}
