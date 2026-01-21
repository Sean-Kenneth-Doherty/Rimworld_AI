using System;
using Verse;

namespace AdvancedColonistIntelligence.Actions
{
    /// <summary>
    /// Parses LLM JSON responses into AIAction objects.
    /// </summary>
    public static class ActionParser
    {
        public static AIAction Parse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                Log.Warning("[ACI] Empty response from LLM");
                return null;
            }

            try
            {
                // Find JSON in response (might have markdown code blocks)
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
                {
                    Log.Warning($"[ACI] Could not find JSON in response: {response}");
                    return null;
                }

                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                var action = new AIAction
                {
                    Thought = ExtractJsonValue(json, "thought"),
                    ActionName = ExtractJsonValue(json, "action"),
                    Target = ExtractJsonValue(json, "target"),
                    Speech = ExtractJsonValue(json, "speech")
                };

                if (!action.IsValid)
                {
                    Log.Warning($"[ACI] Parsed action is invalid: {json}");
                    return null;
                }

                return action;
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] Failed to parse LLM response: {ex.Message}");
                return null;
            }
        }

        private static string ExtractJsonValue(string json, string key)
        {
            var keyPattern = $"\"{key}\"";
            var keyIndex = json.IndexOf(keyPattern, StringComparison.OrdinalIgnoreCase);

            if (keyIndex == -1)
                return null;

            // Find the colon after the key
            var colonIndex = json.IndexOf(':', keyIndex + keyPattern.Length);
            if (colonIndex == -1)
                return null;

            // Skip whitespace after colon
            var valueStart = colonIndex + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                valueStart++;

            if (valueStart >= json.Length)
                return null;

            // Check if value is null
            if (json.Substring(valueStart).StartsWith("null", StringComparison.OrdinalIgnoreCase))
                return null;

            // Check if value is a string (starts with quote)
            if (json[valueStart] == '"')
            {
                valueStart++; // Skip opening quote

                // Find closing quote (handling escapes)
                var valueEnd = valueStart;
                var escaped = false;
                while (valueEnd < json.Length)
                {
                    if (escaped)
                    {
                        escaped = false;
                        valueEnd++;
                        continue;
                    }
                    if (json[valueEnd] == '\\')
                    {
                        escaped = true;
                        valueEnd++;
                        continue;
                    }
                    if (json[valueEnd] == '"')
                        break;
                    valueEnd++;
                }

                var value = json.Substring(valueStart, valueEnd - valueStart);
                return UnescapeJson(value);
            }

            // Value is not a string (number, boolean, etc.)
            var endIndex = valueStart;
            while (endIndex < json.Length && json[endIndex] != ',' && json[endIndex] != '}' && json[endIndex] != ']')
                endIndex++;

            return json.Substring(valueStart, endIndex - valueStart).Trim();
        }

        private static string UnescapeJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
    }
}
