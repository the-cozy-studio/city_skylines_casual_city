using HarmonyLib;
using UnityEngine;

namespace CasualCity.Patches
{
    /// <summary>
    /// F4: traffic slider. Scales the chance that a resident or tourist picks a car for a trip.
    /// TM:PE replaces GetVehicleInfo but still calls these methods, so this works with TM:PE installed.
    /// </summary>
    [HarmonyPatch(typeof(ResidentAI), "GetCarProbability", typeof(Citizen.AgeGroup))]
    internal static class ResidentCarProbabilityPatch
    {
        private static void Postfix(ref int __result) => __result = CarTrips.Scale(__result);
    }

    [HarmonyPatch(typeof(TouristAI), "GetCarProbability", typeof(Vector3))]
    internal static class TouristCarProbabilityPatch
    {
        private static void Postfix(ref int __result) => __result = CarTrips.Scale(__result);
    }

    internal static class CarTrips
    {
        public static int Scale(int probability) => probability * Settings.CarTripPercent / 100;
    }
}
