using HarmonyLib;
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
    }
}
