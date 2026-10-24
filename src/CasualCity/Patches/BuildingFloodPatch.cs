using System.Reflection;
using HarmonyLib;

namespace CasualCity.Patches
{
    /// <summary>
    /// F1: buildings never flood.
    /// CommonBuildingAI.HandleCommonConsumption and CollapseIfFlooded only add the Flooded flag, flood icons
    /// and flood collapse when CanSufferFromFlood returns true. Several building AIs override it
    /// (WaterFacilityAI, ShelterAI, ...), so every declaration is patched.
    /// </summary>
    internal static class BuildingFloodPatch
    {
        private const string MethodName = "CanSufferFromFlood";

        public static void Apply(Harmony harmony)
        {
            var postfix = new HarmonyMethod(typeof(BuildingFloodPatch), nameof(Postfix));
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (var type in typeof(CommonBuildingAI).Assembly.GetTypes())
            {
                if (!typeof(CommonBuildingAI).IsAssignableFrom(type))
                {
                    continue;
                }

                var method = type.GetMethod(MethodName, flags);
                if (method != null)
                {
                    harmony.Patch(method, postfix: postfix);
                }
            }
        }

        // __0 is the "out bool onlyCollapse" parameter; positional so it works whatever an override names it.
        private static void Postfix(ref bool __result, ref bool __0)
        {
            if (Settings.NoBuildingFlood)
            {
                __result = false;
                __0 = false;
            }
        }
    }
}
