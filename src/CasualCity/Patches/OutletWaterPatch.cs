using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace CasualCity.Patches
{
    /// <summary>
    /// F3: drain pipes (sewage outlets) and other water outlets don't add water to the map.
    /// WaterFacilityAI.HandleWaterSource(output: true) pours water into a facility water source every step.
    /// The source must stay alive (the game only counts sewage capacity while it exists), so instead of
    /// releasing it we keep it empty: a facility source only outputs min(outputRate, water).
    /// </summary>
    [HarmonyPatch(typeof(WaterFacilityAI), "HandleWaterSource")]
    internal static class OutletWaterPatch
    {
        private static bool Prefix(ref Building data, bool output, int rate, int max, ref int __result)
        {
            // Let the game create the source the first time; from then on keep it empty.
            if (!output || !Settings.OutletsAddNoWater || data.m_waterSource == 0)
            {
                return true;
            }

            int amount = Mathf.Min(rate, max) >> 1;
            WaterSimulation simulation = Singleton<TerrainManager>.instance.WaterSimulation;
            WaterSource source = simulation.LockWaterSource(data.m_waterSource);
            try
            {
                source.m_water = 0;
                source.m_pollution = 0;
                source.m_outputRate = 0;
            }
            finally
            {
                simulation.UnlockWaterSource(data.m_waterSource, source);
            }

            // Report the sewage as handled, exactly like the original does on success.
            __result = amount << 1;
            return false;
        }
    }
}
