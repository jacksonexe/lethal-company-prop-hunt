using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lethal_Prop_Hunt.Patches
{
    [HarmonyPatch(typeof(ShotgunItem))]
    internal class ShotgunPatch
    {
        //Infinite ammo for hunters
        [HarmonyPostfix, HarmonyPatch(typeof(ShotgunItem), "Start")]
        public static void StartPatch(ShotgunItem __instance)
        {
            __instance.shellsLoaded = 100;
            __instance.isReloading = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ShotgunItem), "Update")]
        public static void UpdatePatch(ShotgunItem __instance)
        {
            __instance.shellsLoaded = 100;
            __instance.isReloading = false;
        }

    }
}
