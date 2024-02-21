using BepInEx.Configuration;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using LethalPropHunt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LethalPropHunt.Gamemode;

namespace Lethal_Prop_Hunt.Gamemode.Utils
{
    internal class ConfigManager
    {
        public static ConfigEntry<int> NumberOfProps { get; private set; }
        public static ConfigEntry<float> MapMultiplier { get; private set; }
        public static ConfigEntry<float> ScrapMultiplier { get; private set; }
        public static ConfigEntry<float> TimeMultiplier { get; private set; }
        public static ConfigEntry<bool> AllowKeys { get; private set; }
        public static ConfigEntry<int> ForceTauntInterval { get; private set; }
        public static ConfigEntry<bool> ForceTaunt { get; private set; }

        public static bool init = false;

        public static void InitConfigurations()
        {
            //We want to make sure the server can only edit these values
            if (init) return;
            init = true;
            NumberOfProps = PropHuntBase.Instance.Config.Bind("Props", "Number of Props", 1, new ConfigDescription("Number of props, if exceeds number of players, 1 is used."));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(NumberOfProps, new IntInputFieldOptions
            {
                Min = 1,
                Max = LPHRoundManager.MAX_PROPS,
                CanModifyCallback = CanModifyCallback
            }));

            ForceTaunt = PropHuntBase.Instance.Config.Bind("Props", "Force Taunt", true, new ConfigDescription("Whether or not to force the player to taunt after a period of time if they have not already done it manually."));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(ForceTaunt, new BoolCheckBoxOptions
            {
                CanModifyCallback = CanModifyCallback
            }));

            ForceTauntInterval = PropHuntBase.Instance.Config.Bind("Props", "Taunting Interval", 30, new ConfigDescription("Forces the player to taunt after a number of seconds."));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(ForceTauntInterval, new IntInputFieldOptions
            {
                Min = 1,
                Max = 60,
                CanModifyCallback = CanModifyCallback
            }));

            MapMultiplier = PropHuntBase.Instance.Config.Bind("Map", "Map Size Multiplier", 1f, "This tells the level generator how big you want the maps to be.");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(MapMultiplier, new FloatInputFieldOptions
            {
                Min = 1f,
                Max = 10f,
                CanModifyCallback = CanModifyCallback
            }));
            ScrapMultiplier = PropHuntBase.Instance.Config.Bind("Map", "Scrap Multiplier", 10f, "The multiplier on how much scrap is generated for the given level.");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(ScrapMultiplier, new FloatInputFieldOptions
            {
                Min = 1f,
                Max = 100f,
                CanModifyCallback = CanModifyCallback
            }));
            TimeMultiplier = PropHuntBase.Instance.Config.Bind("Map", "Time Multiplier", 2f, "The multiplier on how fast time passes, 1 being a normal round. <1 is slower rounds, >1 is faster rounds.");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(TimeMultiplier, new FloatInputFieldOptions
            {
                Min = 0.00001f,
                Max = 100f,
                CanModifyCallback = CanModifyCallback
            }));

            AllowKeys = PropHuntBase.Instance.Config.Bind("Props", "Allow Keys", true, new ConfigDescription("Whether or not to include the key as a scrap item because its kinda really small and OP."));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(AllowKeys, new BoolCheckBoxOptions
            {
                CanModifyCallback = CanModifyCallback
            }));
        }

        private static CanModifyResult CanModifyCallback()
        {
            return (RoundManager.Instance != null && (RoundManager.Instance.IsHost || RoundManager.Instance.IsServer), "Must be the host or server to modify this");
        }
    }
}
