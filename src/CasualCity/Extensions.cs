using ICities;

namespace CasualCity
{
    /// <summary>Runs the flood drainer on the simulation thread.</summary>
    public class SimulationExtension : ThreadingExtensionBase
    {
        public override void OnAfterSimulationFrame()
        {
            FloodDrainer.OnSimulationFrame();
        }
    }

    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            FloodDrainer.RemoveLeftoverPumps();
            FloodDrainer.InGame = loadingManager.currentMode == AppMode.Game;
        }

        public override void OnLevelUnloading()
        {
            FloodDrainer.InGame = false;
            FloodDrainer.ReleaseAll();
        }
    }
}
