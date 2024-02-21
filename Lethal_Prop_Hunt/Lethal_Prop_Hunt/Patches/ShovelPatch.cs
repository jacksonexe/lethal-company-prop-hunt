using GameNetcodeStuff;
using HarmonyLib;
using LethalPropHunt.Gamemode;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Lethal_Prop_Hunt.Patches
{
    [HarmonyPatch(typeof(Shovel))]
    internal class ShovelPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Shovel), "HitShovel")]
        public static void HitShovelPatch(bool cancel, Shovel __instance, ref RaycastHit[] ___objectsHitByShovel, ref List<RaycastHit> ___objectsHitByShovelList, ref int ___shovelMask)
        {
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (localPlayerController == null || __instance.playerHeldBy != localPlayerController || !LPHRoundManager.Instance.GetPlayerRole(localPlayerController).Equals(LPHRoundManager.HUNTERS_ROLE))
            {
                return;
            }
            int num = -1;

            if (!cancel)
            {
                ___objectsHitByShovel = Physics.SphereCastAll(localPlayerController.gameplayCamera.transform.position + localPlayerController.gameplayCamera.transform.right * -0.35f, 0.8f, localPlayerController.gameplayCamera.transform.forward, 1.5f, (___shovelMask | (1 << 6)), QueryTriggerInteraction.Collide);
                ___objectsHitByShovelList = ___objectsHitByShovel.OrderBy((RaycastHit x) => x.distance).ToList();
                for (int i = 0; i < ___objectsHitByShovelList.Count; i++)
                {
                    GrabbableObject component;
                    RaycastHit hitInfo;
                    if (___objectsHitByShovelList[i].transform.gameObject.layer == 8 || ___objectsHitByShovelList[i].transform.gameObject.layer == 11)
                    {
                        string text = ___objectsHitByShovelList[i].collider.gameObject.tag;
                        for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                        {
                            if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == text)
                            {
                                num = j;
                                break;
                            }
                        }
                    }
                    else if (___objectsHitByShovelList[i].transform.TryGetComponent<GrabbableObject>(out component) && !(___objectsHitByShovelList[i].transform == localPlayerController.transform) && (___objectsHitByShovelList[i].point == Vector3.zero || !Physics.Linecast(localPlayerController.gameplayCamera.transform.position, ___objectsHitByShovelList[i].point, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault)))
                    {
                        if (component)
                        {
                            bool found = false;
                            foreach (GrabbableObject prop in LPHRoundManager.Props.Values)
                            {
                                if (prop.NetworkObjectId == component.NetworkObjectId)
                                {
                                    found = true; break;
                                }
                            }
                            if (!found)
                            {
                                __instance.playerHeldBy.DamagePlayer(10, true, true, CauseOfDeath.Bludgeoning); //Damage player if they hit non player prop
                            }
                        }
                    }
                }
            }
        }
    }
}
