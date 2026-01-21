using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AdvancedColonistIntelligence.Display
{
    /// <summary>
    /// Manages thought bubbles displayed over AI colonists.
    /// </summary>
    public static class ThoughtBubbleManager
    {
        private static Dictionary<Pawn, ThoughtBubble> activeBubbles = new Dictionary<Pawn, ThoughtBubble>();

        public static void ShowBubble(Pawn pawn, string thought)
        {
            if (pawn == null || string.IsNullOrEmpty(thought))
                return;

            // Remove existing bubble for this pawn
            if (activeBubbles.ContainsKey(pawn))
            {
                activeBubbles[pawn].Expire();
                activeBubbles.Remove(pawn);
            }

            var bubble = new ThoughtBubble(pawn, thought);
            activeBubbles[pawn] = bubble;
        }

        public static void Tick()
        {
            var expired = new List<Pawn>();

            foreach (var kvp in activeBubbles)
            {
                kvp.Value.Tick();
                if (kvp.Value.IsExpired)
                    expired.Add(kvp.Key);
            }

            foreach (var pawn in expired)
                activeBubbles.Remove(pawn);
        }

        public static void DrawBubbles()
        {
            foreach (var kvp in activeBubbles)
            {
                if (!kvp.Value.IsExpired)
                    kvp.Value.Draw();
            }
        }

        public static void Clear()
        {
            activeBubbles.Clear();
        }
    }

    public class ThoughtBubble
    {
        private readonly Pawn pawn;
        private readonly string thought;
        private int ticksRemaining;
        private const int DefaultDuration = 300; // ~5 seconds
        private const float FadeStartTicks = 60;

        public bool IsExpired => ticksRemaining <= 0 || pawn == null || pawn.Dead || pawn.Destroyed;

        public ThoughtBubble(Pawn pawn, string thought, int duration = DefaultDuration)
        {
            this.pawn = pawn;
            this.thought = TruncateThought(thought);
            this.ticksRemaining = duration;
        }

        private string TruncateThought(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Limit length for display
            if (text.Length > 100)
                return text.Substring(0, 97) + "...";

            return text;
        }

        public void Tick()
        {
            ticksRemaining--;
        }

        public void Expire()
        {
            ticksRemaining = 0;
        }

        public void Draw()
        {
            if (IsExpired || pawn.Map != Find.CurrentMap)
                return;

            // Calculate position above pawn's head
            var drawPos = pawn.DrawPos;
            drawPos.z += 1.5f; // Above head

            var screenPos = Find.Camera.WorldToScreenPoint(drawPos);
            screenPos.y = Screen.height - screenPos.y; // Flip Y

            // Calculate alpha for fade out
            var alpha = ticksRemaining < FadeStartTicks
                ? ticksRemaining / FadeStartTicks
                : 1f;

            // Draw bubble background
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 11,
                normal = { textColor = new Color(1f, 1f, 1f, alpha) }
            };

            var content = new GUIContent(thought);
            var size = style.CalcSize(content);
            size.x = Mathf.Min(size.x + 20, 250);
            size.y = Mathf.Max(size.y + 10, 30);

            var rect = new Rect(
                screenPos.x - size.x / 2,
                screenPos.y - size.y - 10,
                size.x,
                size.y
            );

            // Background with alpha
            var bgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f * alpha);
            var borderColor = new Color(0.3f, 0.6f, 0.9f, alpha);

            // Draw on GUI layer
            GUI.color = bgColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // Border
            GUI.color = borderColor;
            Widgets.DrawBox(rect, 2);

            // Text
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, thought, style);

            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// Component to handle drawing thought bubbles.
    /// </summary>
    [StaticConstructorOnStartup]
    public class ThoughtBubbleDrawer
    {
        static ThoughtBubbleDrawer()
        {
            // Register for GUI drawing
            // This is handled via Harmony patch on MapInterface.MapInterfaceOnGUI_BeforeMainTabs
        }
    }
}
