using CitiesHarmony.API;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace CasualCity
{
    /// <summary>
    /// Entry point the game discovers. Keep HarmonyLib out of this class: Harmony may not be loaded yet
    /// when the game first creates it, so all patching goes through <see cref="Patcher"/>.
    /// </summary>
    public class Mod : IUserMod
    {
        private UILabel sliderLabel;

        public string Name => "Casual City";

        public string Description => "No flooding, drain pipes that don't raise the water, fewer cars and sunny skies.";

        public Mod()
        {
            Settings.Load();
        }

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(Patcher.PatchAll);
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase water = helper.AddGroup("Water");
            water.AddCheckbox("Buildings never flood", Settings.NoBuildingFlood, Settings.SetNoBuildingFlood);
            water.AddCheckbox("Roads never flood (parked cars are safe too)", Settings.NoRoadFlood, Settings.SetNoRoadFlood);
            water.AddCheckbox("Drain pipes don't raise the water level", Settings.OutletsAddNoWater, Settings.SetOutletsAddNoWater);
            water.AddCheckbox("Keep the city dry (pump away water that reaches buildings or roads)", Settings.KeepCityDry, Settings.SetKeepCityDry);
            water.AddButton("Drain flood water now", FloodDrainer.RequestDrainNow);

            UIHelperBase traffic = helper.AddGroup("Traffic");
            var slider = (UISlider)traffic.AddSlider(
                CarTripLabel(Settings.CarTripPercent), 10f, 100f, 10f, Settings.CarTripPercent,
                value =>
                {
                    Settings.SetCarTripPercent(Mathf.RoundToInt(value));
                    SetSliderLabel(value);
                });
            sliderLabel = slider.parent.Find<UILabel>("Label");

            UIHelperBase weather = helper.AddGroup("Weather");
            weather.AddCheckbox("Always sunny (no rain, fog or storms)", Settings.SunnyWeather, Settings.SetSunnyWeather);
        }

        private void SetSliderLabel(float value)
        {
            if (sliderLabel != null)
            {
                sliderLabel.text = CarTripLabel(Mathf.RoundToInt(value));
            }
        }

        private static string CarTripLabel(int percent) =>
            $"Car use: {percent}% of normal (the rest walk, cycle or take transit)";
    }
}
