using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace CasualCity
{
    /// <summary>
    /// F6: drains water that sits on top of ground-level buildings and roads.
    /// Uses the same mechanism as the game's pumping trucks: a temporary facility water source with an input rate.
    /// The source's m_target is the pump's floor, so it never removes water below the flooded object's ground level;
    /// lakes, rivers and the sea are only lowered where they have risen into the city.
    ///
    /// Performance: the city is scanned in 256 slices, one per simulation frame (the same cadence as the game's own
    /// flood check, which F1 turns off). Nothing runs while both "Keep the city dry" and "Drain now" are off.
    /// </summary>
    internal static class FloodDrainer
    {
        private const int FramesPerPass = 256;
        private const int MaxPumps = 32;
        private const uint PumpLifetime = 128;
        private const uint PumpRate = 10000; // large enough for the maximum pump radius (50 m)
        private const float PumpSpacing = 40f;
        private const uint DrainNowFrames = FramesPerPass * 8;

        // Marks our pumps so leftovers can be found after loading. Facility sources only use the output
        // position when they release water, which our pumps never do.
        private const float PumpMarkerY = -4242f;

        private struct Pump
        {
            public ushort Source;
            public uint Expires;
            public Vector3 Position;
        }

        private static readonly object PumpLock = new object();
        private static readonly List<Pump> Pumps = new List<Pump>(MaxPumps);

        private static volatile bool drainNowRequested;
        private static uint drainUntilFrame;

        /// <summary>Set while a city is loaded in game mode.</summary>
        public static volatile bool InGame;

        /// <summary>Set once the save guard patch is active, so pumps can never end up in a save file.</summary>
        public static volatile bool SaveGuardActive;

        public static void RequestDrainNow() => drainNowRequested = true;

        /// <summary>Called on the simulation thread after every simulation frame.</summary>
        public static void OnSimulationFrame()
        {
            if (!InGame || !SaveGuardActive)
            {
                return;
            }

            uint frame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
            if (drainNowRequested)
            {
                drainNowRequested = false;
                drainUntilFrame = frame + DrainNowFrames;
            }

            ExpirePumps(frame);

            if (!Settings.KeepCityDry && frame >= drainUntilFrame)
            {
                return;
            }

            int slice = (int)(frame & (FramesPerPass - 1));
            ScanBuildings(slice, frame);
            ScanRoads(slice, frame);
        }

        public static void ReleaseAll()
        {
            lock (PumpLock)
            {
                WaterSimulation simulation = Singleton<TerrainManager>.instance.WaterSimulation;
                foreach (Pump pump in Pumps)
                {
                    simulation.ReleaseWaterSource(pump.Source);
                }

                Pumps.Clear();
            }
        }

        /// <summary>Removes pumps saved by an older build or a crash, identified by the marker.</summary>
        public static void RemoveLeftoverPumps()
        {
            WaterSimulation simulation = Singleton<TerrainManager>.instance.WaterSimulation;
            var field = typeof(WaterSimulation).GetField("m_waterSources", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(field?.GetValue(simulation) is FastList<WaterSource> sources))
            {
                return;
            }

            int removed = 0;
            for (int i = sources.m_size - 1; i >= 0; i--)
            {
                WaterSource source = sources.m_buffer[i];
                if (source.m_type == WaterSource.TYPE_FACILITY && source.m_outputPosition.y == PumpMarkerY)
                {
                    simulation.ReleaseWaterSource((ushort)(i + 1));
                    removed++;
                }
            }

            if (removed != 0)
            {
                Debug.Log($"[CasualCity] Removed {removed} leftover pumps");
            }
        }

        private static void ScanBuildings(int slice, uint frame)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            SliceBounds(buildings.Length, slice, out int start, out int end);
            for (int i = start; i < end; i++)
            {
                ref Building building = ref buildings[i];
                if ((building.m_flags & (Building.Flags.Created | Building.Flags.Deleted)) != Building.Flags.Created)
                {
                    continue;
                }

                // Only buildings that stand on land; harbors, drain pipes, pumps and dams belong in the water.
                BuildingInfo info = building.Info;
                if (info == null
                    || (info.m_placementMode != BuildingInfo.PlacementMode.OnGround
                        && info.m_placementMode != BuildingInfo.PlacementMode.Roadside
                        && info.m_placementMode != BuildingInfo.PlacementMode.PathsideOrGround))
                {
                    continue;
                }

                TryDrain(building.m_position, frame);
            }
        }

        private static void ScanRoads(int slice, uint frame)
        {
            NetManager netManager = Singleton<NetManager>.instance;
            NetSegment[] segments = netManager.m_segments.m_buffer;
            NetNode[] nodes = netManager.m_nodes.m_buffer;
            SliceBounds(segments.Length, slice, out int start, out int end);
            for (int i = start; i < end; i++)
            {
                ref NetSegment segment = ref segments[i];
                if ((segment.m_flags & (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) != NetSegment.Flags.Created)
                {
                    continue;
                }

                // Ground-level roads only: bridges, elevated roads and tunnels use other AIs.
                NetInfo info = segment.Info;
                if (info == null || !(info.m_netAI is RoadAI) || (info.m_setVehicleFlags & Vehicle.Flags.Underground) != 0)
                {
                    continue;
                }

                Vector3 middle = (nodes[segment.m_startNode].m_position + nodes[segment.m_endNode].m_position) * 0.5f;
                TryDrain(middle, frame);
            }
        }

        // Covers the whole buffer even if a mod changes its size to something not divisible by FramesPerPass.
        private static void SliceBounds(int length, int slice, out int start, out int end)
        {
            start = (int)((long)length * slice / FramesPerPass);
            end = (int)((long)length * (slice + 1) / FramesPerPass);
        }

        private static void TryDrain(Vector3 ground, uint frame)
        {
            // Same test the game uses for flooding.
            float waterLevel = Singleton<TerrainManager>.instance.WaterLevel(new Vector2(ground.x, ground.z));
            if (waterLevel <= ground.y || waterLevel <= 0f)
            {
                return;
            }

            lock (PumpLock)
            {
                // Re-check under the lock: if the mod is being disabled, ReleaseAll may have just run, and a pump
                // created now would never be released.
                if (!InGame || !SaveGuardActive || Pumps.Count >= MaxPumps)
                {
                    return;
                }

                foreach (Pump pump in Pumps)
                {
                    float dx = pump.Position.x - ground.x;
                    float dz = pump.Position.z - ground.z;
                    if (dx * dx + dz * dz < PumpSpacing * PumpSpacing)
                    {
                        return;
                    }
                }

                var source = new WaterSource
                {
                    m_type = WaterSource.TYPE_FACILITY,
                    m_inputPosition = ground,
                    m_outputPosition = new Vector3(ground.x, PumpMarkerY, ground.z),
                    m_inputRate = PumpRate,
                    // Water heights are stored in 1/64 m; the pump never takes water below this level.
                    m_target = (ushort)Mathf.Clamp(Mathf.RoundToInt(ground.y * 64f), 0, ushort.MaxValue),
                };

                if (Singleton<TerrainManager>.instance.WaterSimulation.CreateWaterSource(out ushort id, source))
                {
                    Pumps.Add(new Pump { Source = id, Expires = frame + PumpLifetime, Position = ground });
                }
            }
        }

        private static void ExpirePumps(uint frame)
        {
            lock (PumpLock)
            {
                if (Pumps.Count == 0)
                {
                    return;
                }

                WaterSimulation simulation = Singleton<TerrainManager>.instance.WaterSimulation;
                for (int i = Pumps.Count - 1; i >= 0; i--)
                {
                    if (frame >= Pumps[i].Expires)
                    {
                        simulation.ReleaseWaterSource(Pumps[i].Source);
                        Pumps.RemoveAt(i);
                    }
                }
            }
        }
    }
}
