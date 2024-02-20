using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lethal_Prop_Hunt.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void UpdatePatch(GrabbableObject __instance)
        {
            if(__instance.isBeingUsed && __instance.itemProperties != null && __instance.itemProperties.requiresBattery && __instance.insertedBattery != null)
            {
                __instance.insertedBattery.charge = 1f; //Unlimited powerrrrrrr!!!!
            }
        }
    }
}
