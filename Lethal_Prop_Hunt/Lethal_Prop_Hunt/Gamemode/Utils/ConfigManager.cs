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
        public static ConfigEntry<int> PlayerDistribution { get; private set; }
        public static ConfigEntry<float> MapMultiplier { get; private set; }
        public static ConfigEntry<float> ScrapMultiplier { get; private set; }
        public static ConfigEntry<float> TimeMultiplier { get; private set; }
        public static ConfigEntry<bool> AllowKeys { get; private set; }
        public static ConfigEntry<int> ForceTauntInterval { get; private set; }
        public static ConfigEntry<bool> ForceTaunt { get; private set; }
        public static ConfigEntry<float> PropDamageScale { get; private set; }
        public static ConfigEntry<bool> ForcePropWeight { get; private set; }

        public static bool init = false;

        public static void InitConfigurations()
        {
            //We want to make sure the server can only edit these values
            if (init) return;
            init = true;
            PlayerDistribution = PropHuntBase.Instance.Config.Bind("Players", "Player Team Balancing", 50, new ConfigDescription("How teams are balanced as a percentage, 50% means half the players are props and half are hunters. If the number of players is an odd number, this will favor hunters"));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(PlayerDistribution, new IntInputFieldOptions
            {
                Min = 0,
                Max = 100,
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

            ForcePropWeight = PropHuntBase.Instance.Config.Bind("Props", "Force Prop Weight Restriction", false, new ConfigDescription("Forces prop players to gain weight equal to their prop which will hinder sprinting. This is a possible balancing feature but I can also see how props need this advantage so its off by default."));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(ForcePropWeight, new BoolCheckBoxOptions
            {
                CanModifyCallback = CanModifyCallback
            }));

            MapMultiplier = PropHuntBase.Instance.Config.Bind("Map", "Map Size Multiplier", 1f, "This tells the level generator how big you want the maps to be.");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(MapMultiplier, new FloatInputFieldOptions
            {
                Min = 1f,
                Max = 10f,
                CanModifyCallback = CanModifyCallback
            }));
            ScrapMultiplier = PropHuntBase.Instance.Config.Bind("Map", "Scrap Multiplier", 20f, "The multiplier on how much scrap is generated for the given level.");
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

            PropDamageScale = PropHuntBase.Instance.Config.Bind("Props", "DamageScaling", 2f, new ConfigDescription("How damage is scaled based on prop weight, 1 is disabled."));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(PropDamageScale, new FloatInputFieldOptions
            {
                Min = 1f,
                Max = 100f,
                CanModifyCallback = CanModifyCallback
            }));
        }

        private static CanModifyResult CanModifyCallback()
        {
            return (RoundManager.Instance != null && (RoundManager.Instance.IsHost || RoundManager.Instance.IsServer), "Must be the host or server to modify this");
        }
    }
}
