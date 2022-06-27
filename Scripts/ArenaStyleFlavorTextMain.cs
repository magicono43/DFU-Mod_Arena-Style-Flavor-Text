// Project:         ArenaStyleFlavorText mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    5/26/2022, 12:10 AM
// Last Edit:		6/27/2022, 3:50 PM
// Version:			1.00
// Special Thanks:  Ted Peterson, Cliffworms, Kab the Bird Ranger, Jehuty, Ralzar, Kokytos, Hazelnut, and Interkarma
// Modifier:

using System;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace ArenaStyleFlavorText
{
    public partial class ArenaStyleFlavorTextMain : MonoBehaviour, IHasModSaveData
    {
        static ArenaStyleFlavorTextMain instance;

        public static ArenaStyleFlavorTextMain Instance
        {
            get { return instance ?? (instance = FindObjectOfType<ArenaStyleFlavorTextMain>()); }
        }

        static Mod mod;

        // Options
        public static int TextDisplayType { get; set; }
        public static int MinDisplayDuration { get; set; }
        public static int MaxDisplayDuration { get; set; }
        public static int ShopTextCooldown { get; set; }
        public static int TavernTextCooldown { get; set; }
        public static int TempleTextCooldown { get; set; }
        public static int MagesGuildTextCooldown { get; set; }
        public static int PalaceTextCooldown { get; set; }
        public static int CastleDFTextCooldown { get; set; }
        public static int CastleSentTextCooldown { get; set; }
        public static int CastleWayTextCooldown { get; set; }

        // Attached To SaveData
        public static ulong lastSeenShopText = 0;
        public static ulong lastSeenTavernText = 0;
        public static ulong lastSeenTempleText = 0;
        public static ulong lastSeenMagesGuildText = 0;
        public static ulong lastSeenPalaceText = 0;
        public static ulong lastSeenCastleDFText = 0;
        public static ulong lastSeenCastleSentText = 0;
        public static ulong lastSeenCastleWayText = 0;

        // Global Variables
        public static float TextDelay { get; set; }

        PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        DaggerfallDateTime.Seasons currentSeason = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            instance = new GameObject("ArenaStyleFlavorText").AddComponent<ArenaStyleFlavorTextMain>(); // Add script to the scene.
            mod.SaveDataInterface = instance;

            mod.LoadSettingsCallback = LoadSettings; // To enable use of the "live settings changes" feature in-game.

            mod.IsReady = true;
        }

        private void Start()
        {
            Debug.Log("Begin mod init: Arena-Style Flavor Text");

            mod.LoadSettings();

            PlayerEnterExit.OnTransitionInterior += ShowFlavorText_OnTransitionInterior;
            PlayerEnterExit.OnTransitionDungeonInterior += ShowFlavorText_OnTransitionDungeonInterior;

            Debug.Log("Finished mod init: Arena-Style Flavor Text");
        }

        #region Settings

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            TextDisplayType = mod.GetSettings().GetValue<int>("GeneralSettings", "DisplayType");
            MinDisplayDuration = mod.GetSettings().GetValue<int>("GeneralSettings", "MinDisplayTime");
            MaxDisplayDuration = mod.GetSettings().GetValue<int>("GeneralSettings", "MaxDisplayTime");
            ShopTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "ShopCooldown");
            TavernTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "TavernCooldown");
            TempleTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "TempleCooldown");
            MagesGuildTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "MagesGuildCooldown");
            PalaceTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "PalaceCooldown");
            CastleDFTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "CastleDaggerfallCooldown");
            CastleSentTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "CastleSentinelCooldown");
            CastleWayTextCooldown = mod.GetSettings().GetValue<int>("FlavorTextFrequency", "CastleWayrestCooldown");
        }

        #endregion

        public static TextFile.Token[] TextTokenFromRawString(string rawString)
        {
            var listOfCompLines = new List<string>();
            int partLength = 115;
            if (!DaggerfallUnity.Settings.SDFFontRendering)
                partLength = 65;
            string sentence = rawString;
            string[] words = sentence.Split(' ');
            TextDelay = 5f + (words.Length * 0.25f);
            var parts = new Dictionary<int, string>();
            string part = string.Empty;
            int partCounter = 0;
            foreach (var word in words)
            {
                if (part.Length + word.Length < partLength)
                {
                    part += string.IsNullOrEmpty(part) ? word : " " + word;
                }
                else
                {
                    parts.Add(partCounter, part);
                    part = word;
                    partCounter++;
                }
            }
            parts.Add(partCounter, part);

            foreach (var item in parts)
            {
                listOfCompLines.Add(item.Value);
            }

            return DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter, listOfCompLines.ToArray());
        }

        public void ShowFlavorText_OnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            WeatherType weatherType = GetCurrentWeatherType();
            DFLocation.BuildingTypes buildingType = playerEnterExit.BuildingDiscoveryData.buildingType;
            PlayerGPS.DiscoveredBuilding buildingData = playerEnterExit.BuildingDiscoveryData;

            ulong currentTimeSeconds = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds(); // 15 * 86400 = Number of seconds in 15 days.

            TextFile.Token[] tokens = null;

            if (playerEnterExit.IsPlayerInside)
            {
                if (playerEnterExit.IsPlayerInsideOpenShop && BuildingOpenCheck(buildingType, buildingData) && (currentTimeSeconds - lastSeenShopText) > 86400 * (uint)ShopTextCooldown)
                {
                    lastSeenShopText = currentTimeSeconds;

                    switch (playerGPS.ClimateSettings.ClimateType)
                    {
                        case DFLocation.ClimateBaseType.Desert:
                        case DFLocation.ClimateBaseType.Swamp:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotFallShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotFallShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotFallShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSpringShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSpringShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSpringShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSummerShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSummerShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSummerShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotWinterShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotWinterShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotWinterShopText(2); break;
                                        case WeatherType.Snow: tokens = HotWinterShopText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Mountain:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainFallShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainFallShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainFallShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSpringShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSpringShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSpringShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSummerShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSummerShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSummerShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainWinterShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainWinterShopText(1); break;
                                        case WeatherType.Snow: tokens = MountainWinterShopText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Temperate:
                        default:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateFallShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateFallShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateFallShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSpringShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSpringShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSpringShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSummerShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSummerShopText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSummerShopText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateWinterShopText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateWinterShopText(1); break;
                                        case WeatherType.Snow: tokens = TemperateWinterShopText(3); break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                else if (playerEnterExit.IsPlayerInsideTavern && (currentTimeSeconds - lastSeenTavernText) > 86400 * (uint)TavernTextCooldown)
                {
                    lastSeenTavernText = currentTimeSeconds;

                    switch (playerGPS.ClimateSettings.ClimateType)
                    {
                        case DFLocation.ClimateBaseType.Desert:
                        case DFLocation.ClimateBaseType.Swamp:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotFallTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotFallTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotFallTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSpringTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSpringTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSpringTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSummerTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSummerTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSummerTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotWinterTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotWinterTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotWinterTavernText(2); break;
                                        case WeatherType.Snow: tokens = HotWinterTavernText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Mountain:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainFallTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainFallTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainFallTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSpringTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSpringTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSpringTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSummerTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSummerTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSummerTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainWinterTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainWinterTavernText(1); break;
                                        case WeatherType.Snow: tokens = MountainWinterTavernText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Temperate:
                        default:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateFallTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateFallTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateFallTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSpringTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSpringTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSpringTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSummerTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSummerTavernText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSummerTavernText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateWinterTavernText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateWinterTavernText(1); break;
                                        case WeatherType.Snow: tokens = TemperateWinterTavernText(3); break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                else if (playerEnterExit.BuildingDiscoveryData.buildingType == DFLocation.BuildingTypes.Temple && (currentTimeSeconds - lastSeenTempleText) > 86400 * (uint)TempleTextCooldown)
                {
                    lastSeenTempleText = currentTimeSeconds;

                    switch (playerGPS.ClimateSettings.ClimateType)
                    {
                        case DFLocation.ClimateBaseType.Desert:
                        case DFLocation.ClimateBaseType.Swamp:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotFallTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotFallTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotFallTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSpringTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSpringTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSpringTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSummerTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSummerTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSummerTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotWinterTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotWinterTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotWinterTempleText(2); break;
                                        case WeatherType.Snow: tokens = HotWinterTempleText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Mountain:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainFallTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainFallTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainFallTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSpringTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSpringTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSpringTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSummerTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSummerTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSummerTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainWinterTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainWinterTempleText(1); break;
                                        case WeatherType.Snow: tokens = MountainWinterTempleText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Temperate:
                        default:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateFallTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateFallTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateFallTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSpringTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSpringTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSpringTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSummerTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSummerTempleText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSummerTempleText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateWinterTempleText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateWinterTempleText(1); break;
                                        case WeatherType.Snow: tokens = TemperateWinterTempleText(3); break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                else if (playerEnterExit.BuildingDiscoveryData.buildingType == DFLocation.BuildingTypes.GuildHall && playerEnterExit.BuildingDiscoveryData.factionID == (int)FactionFile.FactionIDs.The_Mages_Guild && BuildingOpenCheck(buildingType, buildingData) && (currentTimeSeconds - lastSeenMagesGuildText) > 86400 * (uint)MagesGuildTextCooldown)
                {
                    lastSeenMagesGuildText = currentTimeSeconds;

                    switch (playerGPS.ClimateSettings.ClimateType)
                    {
                        case DFLocation.ClimateBaseType.Desert:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = DesertFallMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = DesertFallMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = DesertFallMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = DesertSpringMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = DesertSpringMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = DesertSpringMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = DesertSummerMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = DesertSummerMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = DesertSummerMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = DesertWinterMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = DesertWinterMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = DesertWinterMagesText(2); break;
                                        case WeatherType.Snow: tokens = DesertWinterMagesText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Swamp:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = SwampFallMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = SwampFallMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = SwampFallMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = SwampSpringMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = SwampSpringMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = SwampSpringMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = SwampSummerMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = SwampSummerMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = SwampSummerMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = SwampWinterMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = SwampWinterMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = SwampWinterMagesText(2); break;
                                        case WeatherType.Snow: tokens = SwampWinterMagesText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Mountain:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainFallMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainFallMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainFallMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSpringMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSpringMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSpringMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSummerMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSummerMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSummerMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainWinterMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainWinterMagesText(1); break;
                                        case WeatherType.Snow: tokens = MountainWinterMagesText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Temperate:
                        default:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateFallMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateFallMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateFallMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSpringMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSpringMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSpringMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSummerMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSummerMagesText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSummerMagesText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateWinterMagesText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateWinterMagesText(1); break;
                                        case WeatherType.Snow: tokens = TemperateWinterMagesText(3); break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                else if (playerEnterExit.BuildingDiscoveryData.buildingType == DFLocation.BuildingTypes.Palace && BuildingOpenCheck(buildingType, buildingData) && (currentTimeSeconds - lastSeenPalaceText) > 86400 * (uint)PalaceTextCooldown)
                {
                    lastSeenPalaceText = currentTimeSeconds;

                    switch (playerGPS.ClimateSettings.ClimateType)
                    {
                        case DFLocation.ClimateBaseType.Desert:
                        case DFLocation.ClimateBaseType.Swamp:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotFallPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotFallPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotFallPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSpringPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSpringPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSpringPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotSummerPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotSummerPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotSummerPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = HotWinterPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = HotWinterPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = HotWinterPalaceText(2); break;
                                        case WeatherType.Snow: tokens = HotWinterPalaceText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Mountain:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainFallPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainFallPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainFallPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSpringPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSpringPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSpringPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainSummerPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainSummerPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = MountainSummerPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = MountainWinterPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = MountainWinterPalaceText(1); break;
                                        case WeatherType.Snow: tokens = MountainWinterPalaceText(3); break;
                                    }
                                    break;
                            }
                            break;
                        case DFLocation.ClimateBaseType.Temperate:
                        default:
                            switch (currentSeason)
                            {
                                case DaggerfallDateTime.Seasons.Fall:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateFallPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateFallPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateFallPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Spring:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSpringPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSpringPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSpringPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Summer:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateSummerPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateSummerPalaceText(1); break;
                                        case WeatherType.Rain:
                                        case WeatherType.Thunder: tokens = TemperateSummerPalaceText(2); break;
                                    }
                                    break;
                                case DaggerfallDateTime.Seasons.Winter:
                                    switch (weatherType)
                                    {
                                        case WeatherType.Sunny:
                                        default: tokens = TemperateWinterPalaceText(0); break;
                                        case WeatherType.Overcast:
                                        case WeatherType.Fog: tokens = TemperateWinterPalaceText(1); break;
                                        case WeatherType.Snow: tokens = TemperateWinterPalaceText(3); break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }

            if (tokens != null)
            {
                if (MinDisplayDuration != 0 && TextDelay < MinDisplayDuration) // If not set to 0, the minimum number of seconds a message can show for
                    TextDelay = MinDisplayDuration;

                if (MaxDisplayDuration != 0 && TextDelay > MaxDisplayDuration) // If not set to 0, the maximum number of seconds a message can show for
                    TextDelay = MaxDisplayDuration;

                if (TextDisplayType == 0) // For HUD display of text
                {
                    DaggerfallUI.AddHUDText(tokens, TextDelay);
                }
                else // For MessageBox display of text
                {
                    DaggerfallMessageBox textBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                    textBox.SetTextTokens(tokens);
                    textBox.ClickAnywhereToClose = true;
                    textBox.Show();
                }
            }
        }

        public void ShowFlavorText_OnTransitionDungeonInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            WeatherType weatherType = GetCurrentWeatherType();
            DFLocation locationData = GameManager.Instance.PlayerGPS.CurrentLocation;

            ulong currentTimeSeconds = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds(); // 15 * 86400 = Number of seconds in 15 days.

            TextFile.Token[] tokens = null;

            if (playerEnterExit.IsPlayerInside)
            {
                if (playerGPS.CurrentRegionIndex == 17 && locationData.Name == "Daggerfall" && (currentTimeSeconds - lastSeenCastleDFText) > 86400 * (uint)CastleDFTextCooldown) // Daggerfall Region
                {
                    lastSeenCastleDFText = currentTimeSeconds;

                    switch (currentSeason)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = FallDFPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = FallDFPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = FallDFPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Spring:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = SpringDFPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = SpringDFPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = SpringDFPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Summer:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = SummerDFPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = SummerDFPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = SummerDFPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Winter:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = WinterDFPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = WinterDFPalaceText(1); break;
                                case WeatherType.Snow: tokens = WinterDFPalaceText(3); break;
                            }
                            break;
                    }
                }
                else if (playerGPS.CurrentRegionIndex == 20 && locationData.Name == "Sentinel" && (currentTimeSeconds - lastSeenCastleSentText) > 86400 * (uint)CastleSentTextCooldown) // Sentinel Region
                {
                    lastSeenCastleSentText = currentTimeSeconds;

                    switch (currentSeason)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = FallSentPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = FallSentPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = FallSentPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Spring:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = SpringSentPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = SpringSentPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = SpringSentPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Summer:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = SummerSentPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = SummerSentPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = SummerSentPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Winter:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = WinterSentPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = WinterSentPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = WinterSentPalaceText(2); break;
                            }
                            break;
                    }
                }
                else if (playerGPS.CurrentRegionIndex == 23 && locationData.Name == "Wayrest" && (currentTimeSeconds - lastSeenCastleWayText) > 86400 * (uint)CastleWayTextCooldown) // Wayrest Region
                {
                    lastSeenCastleWayText = currentTimeSeconds;

                    switch (currentSeason)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = FallWayPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = FallWayPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = FallWayPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Spring:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = SpringWayPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = SpringWayPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = SpringWayPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Summer:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = SummerWayPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = SummerWayPalaceText(1); break;
                                case WeatherType.Rain:
                                case WeatherType.Thunder: tokens = SummerWayPalaceText(2); break;
                            }
                            break;
                        case DaggerfallDateTime.Seasons.Winter:
                            switch (weatherType)
                            {
                                case WeatherType.Sunny:
                                default: tokens = WinterWayPalaceText(0); break;
                                case WeatherType.Overcast:
                                case WeatherType.Fog: tokens = WinterWayPalaceText(1); break;
                                case WeatherType.Snow: tokens = WinterWayPalaceText(3); break;
                            }
                            break;
                    }
                }
            }

            if (tokens != null)
            {
                if (MinDisplayDuration != 0 && TextDelay < MinDisplayDuration) // If not set to 0, the minimum number of seconds a message can show for
                    TextDelay = MinDisplayDuration;

                if (MaxDisplayDuration != 0 && TextDelay > MaxDisplayDuration) // If not set to 0, the maximum number of seconds a message can show for
                    TextDelay = MaxDisplayDuration;

                if (TextDisplayType == 0) // For HUD display of text
                {
                    DaggerfallUI.AddHUDText(tokens, TextDelay);
                }
                else // For MessageBox display of text
                {
                    DaggerfallMessageBox textBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                    textBox.SetTextTokens(tokens);
                    textBox.ClickAnywhereToClose = true;
                    textBox.Show();
                }
            }
        }

        public bool BuildingOpenCheck(DFLocation.BuildingTypes buildingType, PlayerGPS.DiscoveredBuilding buildingData)
        {
            /*
             * Open Hours For Specific Places:
             * Temples, Dark Brotherhood, Thieves Guild: 24/7
             * All Other Guilds: 11:00 - 23:00
             * Fighters Guild & Mages Guild, Rank 6 = 24/7 Access
             * 
             * Alchemists: 07:00 - 22:00
             * Armorers: 09:00 - 19:00
             * Banks: 08:00 - 15:00
             * Bookstores: 	09:00 - 21:00
             * Clothing Stores: 10:00 - 19:00
             * Gem Stores: 09:00 - 18:00
             * General Stores + Furniture Stores: 06:00 - 23:00
             * Libraries: 09:00 - 23:00
             * Pawn Shops + Weapon Smiths: 09:00 - 20:00
            */

            int buildingInt = (int)buildingType;
            int hour = DaggerfallUnity.Instance.WorldTime.Now.Hour;
            IGuild guild = GameManager.Instance.GuildManager.GetGuild(buildingData.factionID);
            if (buildingType == DFLocation.BuildingTypes.GuildHall && (PlayerActivate.IsBuildingOpen(buildingType) || guild.HallAccessAnytime()))
                return true;
            if (buildingInt < 18)
                return PlayerActivate.IsBuildingOpen(buildingType);
            else if (buildingInt <= 22)
                return hour < 6 || hour > 18 ? false : true;
            else
                return true;
        }

        public WeatherType GetCurrentWeatherType()
        {
            WeatherManager weatherManager = GameManager.Instance.WeatherManager;

            if (weatherManager.IsSnowing)
                return WeatherType.Snow;
            else if (weatherManager.IsStorming)
                return WeatherType.Thunder;
            else if (weatherManager.IsRaining)
                return WeatherType.Rain;
            else if (weatherManager.IsOvercast && weatherManager.currentOutdoorFogSettings.density == weatherManager.HeavyFogSettings.density)
                return WeatherType.Fog;
            else if (weatherManager.IsOvercast)
                return WeatherType.Overcast;
            else
                return WeatherType.Sunny;
        }

        #region SaveData Junk

        public Type SaveDataType
        {
            get { return typeof(ArenaStyleFlavorTextSaveData); }
        }

        public object NewSaveData()
        {
            return new ArenaStyleFlavorTextSaveData
            {
                LastSeenShopText = 0,
                LastSeenTavernText = 0,
                LastSeenTempleText = 0,
                LastSeenMagesGuildText = 0,
                LastSeenPalaceText = 0,
                LastSeenCastleDFText = 0,
                LastSeenCastleSentText = 0,
                LastSeenCastleWayText = 0
            };
        }

        public object GetSaveData()
        {
            return new ArenaStyleFlavorTextSaveData
            {
                LastSeenShopText = lastSeenShopText,
                LastSeenTavernText = lastSeenTavernText,
                LastSeenTempleText = lastSeenTempleText,
                LastSeenMagesGuildText = lastSeenMagesGuildText,
                LastSeenPalaceText = lastSeenPalaceText,
                LastSeenCastleDFText = lastSeenCastleDFText,
                LastSeenCastleSentText = lastSeenCastleSentText,
                LastSeenCastleWayText = lastSeenCastleWayText
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var arenaStyleFlavorTextSaveData = (ArenaStyleFlavorTextSaveData)saveData;
            lastSeenShopText = arenaStyleFlavorTextSaveData.LastSeenShopText;
            lastSeenTavernText = arenaStyleFlavorTextSaveData.LastSeenTavernText;
            lastSeenTempleText = arenaStyleFlavorTextSaveData.LastSeenTempleText;
            lastSeenMagesGuildText = arenaStyleFlavorTextSaveData.LastSeenMagesGuildText;
            lastSeenPalaceText = arenaStyleFlavorTextSaveData.LastSeenPalaceText;
            lastSeenCastleDFText = arenaStyleFlavorTextSaveData.LastSeenCastleDFText;
            lastSeenCastleSentText = arenaStyleFlavorTextSaveData.LastSeenCastleSentText;
            lastSeenCastleWayText = arenaStyleFlavorTextSaveData.LastSeenCastleWayText;
        }
    }

    [FullSerializer.fsObject("v1")]
    public class ArenaStyleFlavorTextSaveData
    {
        public ulong LastSeenShopText;
        public ulong LastSeenTavernText;
        public ulong LastSeenTempleText;
        public ulong LastSeenMagesGuildText;
        public ulong LastSeenPalaceText;
        public ulong LastSeenCastleDFText;
        public ulong LastSeenCastleSentText;
        public ulong LastSeenCastleWayText;
    }

    #endregion
}