using HarmonyLib;

namespace CasualCity.Patches
{
    /// <summary>
    /// F6 safety: release all drain pumps right before the water simulation is written to a save file, so a
    /// save never contains a pump (which would keep draining that spot forever if the mod were removed).
    /// The drainer creates new pumps on the following frames if the city is still flooded.
    /// </summary>
    [HarmonyPatch(typeof(WaterSimulation.Data), nameof(WaterSimulation.Data.Serialize))]
    internal static class SaveGuardPatch
    {
        private static void Prefix() => FloodDrainer.ReleaseAll();
    }
}
