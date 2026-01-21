namespace AdvancedColonistIntelligence.Actions
{
    /// <summary>
    /// Represents a parsed action from the LLM response.
    /// </summary>
    public class AIAction
    {
        public string Thought { get; set; }
        public string ActionName { get; set; }
        public string Target { get; set; }
        public string Speech { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(ActionName);

        public override string ToString()
        {
            return $"Action: {ActionName}, Target: {Target ?? "none"}, Thought: {Thought}";
        }
    }
}
