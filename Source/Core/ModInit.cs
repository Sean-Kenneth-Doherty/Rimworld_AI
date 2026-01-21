using HarmonyLib;
using Verse;

namespace AdvancedColonistIntelligence
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            var harmony = new Harmony("sean.advancedcolonistintelligence");
            harmony.PatchAll();
            Log.Message("[AdvancedColonistIntelligence] Initialized - Colonists now have advanced intelligence!");
        }
    }
}
