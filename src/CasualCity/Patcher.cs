using CasualCity.Patches;
using HarmonyLib;
using UnityEngine;

namespace CasualCity
{
    internal static class Patcher
    {
        private const string HarmonyId = "com.github.the-cozy-studio.casualcity";

        private static bool patched;

        public static void PatchAll()
        {
            if (patched)
            {
                return;
            }

            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Patcher).Assembly);
            BuildingFloodPatch.Apply(harmony);
            patched = true;
            FloodDrainer.SaveGuardActive = true;
            Debug.Log("[CasualCity] Patches applied");
        }

        public static void UnpatchAll()
        {
            if (!patched)
            {
                return;
            }

            // Stop the drainer first: without the save guard its pumps could end up in a save.
            FloodDrainer.SaveGuardActive = false;
            if (FloodDrainer.InGame)
            {
                FloodDrainer.ReleaseAll();
            }

            new Harmony(HarmonyId).UnpatchAll(HarmonyId);
            patched = false;
            Debug.Log("[CasualCity] Patches removed");
        }
    }
}
