using System;
using ColossalFramework;
using UnityEngine;

namespace CasualCity
{
    /// <summary>
    /// Saved in the game's own settings folder (CasualCity.cgs), which is the right place on Windows and macOS.
    /// The plain static fields are what the patches read, so hot paths never touch the settings file.
    /// </summary>
    internal static class Settings
    {
        private const string FileName = "CasualCity";

        public static bool NoBuildingFlood = true;
        public static bool NoRoadFlood = true;
        public static bool OutletsAddNoWater = true;
        public static bool KeepCityDry = true;
        public static int CarTripPercent = 50;
        public static bool SunnyWeather = true;

        private static SavedBool savedNoBuildingFlood;
        private static SavedBool savedNoRoadFlood;
        private static SavedBool savedOutletsAddNoWater;
        private static SavedBool savedKeepCityDry;
        private static SavedInt savedCarTripPercent;
        private static SavedBool savedSunnyWeather;

        public static void Load()
        {
            try
            {
                if (GameSettings.FindSettingsFileByName(FileName) == null)
                {
                    GameSettings.AddSettingsFile(new SettingsFile { fileName = FileName });
                }

                savedNoBuildingFlood = new SavedBool(nameof(NoBuildingFlood), FileName, NoBuildingFlood, true);
                savedNoRoadFlood = new SavedBool(nameof(NoRoadFlood), FileName, NoRoadFlood, true);
                savedOutletsAddNoWater = new SavedBool(nameof(OutletsAddNoWater), FileName, OutletsAddNoWater, true);
                savedKeepCityDry = new SavedBool(nameof(KeepCityDry), FileName, KeepCityDry, true);
                savedCarTripPercent = new SavedInt(nameof(CarTripPercent), FileName, CarTripPercent, true);
                savedSunnyWeather = new SavedBool(nameof(SunnyWeather), FileName, SunnyWeather, true);

                NoBuildingFlood = savedNoBuildingFlood.value;
                NoRoadFlood = savedNoRoadFlood.value;
                OutletsAddNoWater = savedOutletsAddNoWater.value;
                KeepCityDry = savedKeepCityDry.value;
                CarTripPercent = Mathf.Clamp(savedCarTripPercent.value, 10, 100);
                SunnyWeather = savedSunnyWeather.value;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void SetNoBuildingFlood(bool value) => savedNoBuildingFlood.value = NoBuildingFlood = value;

        public static void SetNoRoadFlood(bool value) => savedNoRoadFlood.value = NoRoadFlood = value;

        public static void SetOutletsAddNoWater(bool value) => savedOutletsAddNoWater.value = OutletsAddNoWater = value;

        public static void SetKeepCityDry(bool value) => savedKeepCityDry.value = KeepCityDry = value;

        public static void SetCarTripPercent(int value) => savedCarTripPercent.value = CarTripPercent = Mathf.Clamp(value, 10, 100);

        public static void SetSunnyWeather(bool value) => savedSunnyWeather.value = SunnyWeather = value;
    }
}
