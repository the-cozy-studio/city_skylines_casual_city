using HarmonyLib;

namespace CasualCity.Patches
{
    /// <summary>
    /// F2: roads never flood.
    /// RoadBaseAI.SimulationStep(segment) sets the Flooded flag and flood icons when the water is above the road.
    /// TM:PE also patches this method (a non-skipping prefix), so we only add a postfix that undoes the flood state.
    /// </summary>
    [HarmonyPatch(typeof(RoadBaseAI), nameof(RoadBaseAI.SimulationStep),
        new[] { typeof(ushort), typeof(NetSegment) }, new[] { ArgumentType.Normal, ArgumentType.Ref })]
    internal static class RoadSegmentFloodPatch
    {
        private static void Postfix(ref NetSegment data)
        {
            if (!Settings.NoRoadFlood)
            {
                return;
            }

            data.m_flags &= ~NetSegment.Flags.Flooded;
            data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem1.Flood);
        }
    }

    /// <summary>
    /// F2: parked cars on flooded roads are not destroyed.
    /// </summary>
    [HarmonyPatch(typeof(RoadBaseAI), nameof(RoadBaseAI.FloodParkedCars))]
    internal static class FloodParkedCarsPatch
    {
        private static bool Prefix() => !Settings.NoRoadFlood;
    }
}
