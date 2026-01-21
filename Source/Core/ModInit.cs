using HarmonyLib;
using Verse;

namespace AdvancedColonistIntelligence
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        public static readonly string Version = "1.0.0";

        static ModInit()
        {
            var harmony = new Harmony("sean.advancedcolonistintelligence");
            harmony.PatchAll();

            Log.Message($"[ACI] Advanced Colonist Intelligence v{Version} initialized");
            Log.Message("[ACI] Configure AI colonists in Mod Settings");
        }
    }
}
