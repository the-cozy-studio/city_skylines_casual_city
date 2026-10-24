using HarmonyLib;

namespace CasualCity.Patches
{
    /// <summary>
    /// F5: always sunny. After each weather step, clear the rain, fog and cloud targets; the game then
    /// fades the current values down on its own, and lightning stops once the rain is gone.
    /// </summary>
    [HarmonyPatch(typeof(WeatherManager), "SimulationStepImpl")]
    internal static class WeatherPatch
    {
        private static void Postfix(WeatherManager __instance)
        {
            if (!Settings.SunnyWeather)
            {
                return;
            }

            __instance.m_targetRain = 0f;
            __instance.m_targetFog = 0f;
            __instance.m_targetCloud = 0f;
        }
    }
}
