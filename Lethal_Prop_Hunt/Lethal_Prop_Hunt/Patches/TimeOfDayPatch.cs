using HarmonyLib;
using Lethal_Prop_Hunt.Gamemode.Utils;
using LethalPropHunt.Gamemode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lethal_Prop_Hunt.Patches
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class TimeOfDayPatch
    {
        [HarmonyPatch("SetInsideLightingDimness")]
        [HarmonyPostfix]
        public static void SetInsideLightingDimnessPatch(bool doNotLerp, bool setValueTo)
        {
            if (LPHRoundManager.Instance.IsRunning)
            {
                HUDManager.Instance.SetClockVisible(true);
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        private static void startPatch(ref float ___globalTimeSpeedMultiplier)
        {
            ___globalTimeSpeedMultiplier = ConfigManager.TimeMultiplier.Value;
        }
    }
}
