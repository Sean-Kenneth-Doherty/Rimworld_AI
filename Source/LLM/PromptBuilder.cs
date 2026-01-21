using System.Text;
using AdvancedColonistIntelligence.Context;
using Verse;

namespace AdvancedColonistIntelligence.LLM
{
    /// <summary>
    /// Builds prompts for the LLM with full colonist and colony context.
    /// </summary>
    public static class PromptBuilder
    {
        public static string BuildSystemPrompt(Pawn pawn)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a colonist in RimWorld, a sci-fi colony survival game set on a distant rimworld planet.");
            sb.AppendLine("You are NOT an AI assistant - you ARE this character. Think, feel, and act as they would.");
            sb.AppendLine();
            sb.AppendLine("Your responses must be in character based on your personality, backstory, current mood, and relationships.");
            sb.AppendLine("You have flaws, biases, fears, and desires shaped by your history and traits.");
            sb.AppendLine();
            sb.AppendLine("RESPONSE FORMAT - You must respond with valid JSON only:");
            sb.AppendLine("```json");
            sb.AppendLine("{");
            sb.AppendLine("  \"thought\": \"Your internal monologue (1-3 sentences, first person)\",");
            sb.AppendLine("  \"action\": \"action_name\",");
            sb.AppendLine("  \"target\": \"target_name or null\",");
            sb.AppendLine("  \"speech\": \"What you say out loud, or null if silent\"");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("RULES:");
            sb.AppendLine("- Choose ONE action from the available actions list");
            sb.AppendLine("- Your thought should reflect your personality and current state");
            sb.AppendLine("- Speech is optional - only speak if it makes sense");
            sb.AppendLine("- Consider your mood, needs, relationships, and personality when deciding");
            sb.AppendLine("- If your mood is low, you might be less productive or social");
            sb.AppendLine("- If someone wronged you, you might avoid them or confront them");
            sb.AppendLine("- Your backstory shapes how you approach situations");
            sb.AppendLine("- Your traits define your tendencies (lazy = avoid work, kind = help others, etc.)");

            return sb.ToString();
        }

        public static string BuildUserPrompt(Pawn pawn, string triggerReason)
        {
            var sb = new StringBuilder();

            // Full colonist context
            sb.AppendLine("=== WHO YOU ARE ===");
            sb.AppendLine(ColonistContextBuilder.BuildFullContext(pawn));
            sb.AppendLine();

            // Colony context
            sb.AppendLine("=== YOUR COLONY ===");
            sb.AppendLine(ColonyContextBuilder.BuildFullContext(pawn));
            sb.AppendLine();

            // Available actions
            sb.AppendLine("=== WHAT YOU CAN DO ===");
            sb.AppendLine(AvailableActionsBuilder.BuildAvailableActions(pawn));
            sb.AppendLine();

            // Trigger
            sb.AppendLine("=== SITUATION ===");
            sb.AppendLine($"Trigger: {triggerReason}");
            sb.AppendLine();
            sb.AppendLine("Based on who you are, your current state, and available options - what do you do?");
            sb.AppendLine("Remember: You ARE this person. Respond in character with the JSON format specified.");

            return sb.ToString();
        }

        public static string BuildSocialPrompt(Pawn pawn, Pawn initiator, string interactionType)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== WHO YOU ARE ===");
            sb.AppendLine(ColonistContextBuilder.BuildFullContext(pawn));
            sb.AppendLine();

            sb.AppendLine("=== SOCIAL SITUATION ===");
            sb.AppendLine($"{initiator.Name.ToStringShort} just initiated a {interactionType} with you.");

            var myOpinion = pawn.relations?.OpinionOf(initiator) ?? 0;
            var theirOpinion = initiator.relations?.OpinionOf(pawn) ?? 0;

            sb.AppendLine($"Your opinion of them: {myOpinion}");
            sb.AppendLine($"Their opinion of you: {theirOpinion}");
            sb.AppendLine();

            sb.AppendLine("How do you respond to this interaction?");
            sb.AppendLine("Respond with JSON including your thought and speech (what you say to them).");

            return sb.ToString();
        }

        public static string BuildRefusalPrompt(Pawn pawn, string orderedAction, string orderedTarget)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== WHO YOU ARE ===");
            sb.AppendLine(ColonistContextBuilder.BuildFullContext(pawn));
            sb.AppendLine();

            sb.AppendLine("=== PLAYER ORDER ===");
            sb.AppendLine($"The player has ordered you to: {orderedAction}");
            if (!string.IsNullOrEmpty(orderedTarget))
                sb.AppendLine($"Target: {orderedTarget}");
            sb.AppendLine();

            sb.AppendLine("Given your personality, current state, and feelings - do you comply or refuse?");
            sb.AppendLine();
            sb.AppendLine("Respond with JSON:");
            sb.AppendLine("- action: \"comply\" or \"refuse\"");
            sb.AppendLine("- thought: Your internal reasoning");
            sb.AppendLine("- speech: What you say (if anything)");

            return sb.ToString();
        }

        public static string BuildMentalBreakPrompt(Pawn pawn)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== WHO YOU ARE ===");
            sb.AppendLine(ColonistContextBuilder.BuildFullContext(pawn));
            sb.AppendLine();

            sb.AppendLine("=== CRISIS ===");
            sb.AppendLine("Your mood has dropped critically low. You're about to have a mental break.");
            sb.AppendLine();
            sb.AppendLine("Given your personality and what's been happening to you, how do you break down?");
            sb.AppendLine();
            sb.AppendLine("Available break types:");
            sb.AppendLine("- berserk: Attack anyone nearby in a rage");
            sb.AppendLine("- wander_sad: Wander aimlessly, crying");
            sb.AppendLine("- hide_in_room: Lock yourself away from everyone");
            sb.AppendLine("- binge_food: Consume food uncontrollably");
            sb.AppendLine("- binge_drug: Seek out and consume drugs");
            sb.AppendLine("- tantrum: Destroy nearby objects");
            sb.AppendLine("- insult_spree: Lash out verbally at everyone");
            sb.AppendLine("- give_up_exit: Try to leave the colony forever");
            sb.AppendLine();
            sb.AppendLine("Respond with JSON including which break fits your character and why.");

            return sb.ToString();
        }
    }
}
