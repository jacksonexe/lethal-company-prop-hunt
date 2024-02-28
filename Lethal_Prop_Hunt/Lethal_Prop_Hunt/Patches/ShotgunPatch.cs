using GameNetcodeStuff;
using HarmonyLib;
using LethalPropHunt;
using LethalPropHunt.Gamemode;
using UnityEngine;

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

        [HarmonyPostfix, HarmonyPatch(typeof(ShotgunItem), "ShootGun")]
        public static void ShootGun(Vector3 shotgunPosition, Vector3 shotgunForward, ShotgunItem __instance, ref RaycastHit[] ___enemyColliders)
        {
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (__instance.isHeld && __instance.playerHeldBy != null && localPlayerController != null && __instance.playerHeldBy == localPlayerController && __instance.playerHeldBy.isInsideFactory && LPHRoundManager.Instance.GetPlayerRole(localPlayerController).Equals(LPHRoundManager.HUNTERS_ROLE))
            {
                Ray ray = new Ray(shotgunPosition - shotgunForward * 5f, shotgunForward);
                if (___enemyColliders == null)
                {
                    ___enemyColliders = new RaycastHit[10];
                }
                int num4 = Physics.SphereCastNonAlloc(ray, 5f, ___enemyColliders, 15f, (0 | (1 << 6)), QueryTriggerInteraction.Collide);//Add layer 6 to mask
                GrabbableObject component;
                RaycastHit hitInfo;
                for (int i = 0; i < num4; i++)
                {
                    PropHuntBase.mls.LogDebug("Collided with " + ___enemyColliders[i].collider.name);
                    if (Physics.Linecast(shotgunPosition, ___enemyColliders[i].collider.transform.position, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    {
                        Debug.DrawRay(hitInfo.point, Vector3.up, Color.red, 15f);
                        Debug.DrawLine(shotgunPosition, ___enemyColliders[i].point, Color.cyan, 15f);
                        Debug.Log("Raycast hit wall");
                    }
                    else if (___enemyColliders[i].collider != null && ___enemyColliders[i].collider.gameObject.TryGetComponent<GrabbableObject>(out component))
                    {
                        float num5 = Vector3.Distance(shotgunPosition, ___enemyColliders[i].point);
                        int num6 = ((num5 < 3.7f) ? 5 : ((!(num5 < 6f)) ? 2 : 3));
                        Debug.Log($"Hit enemy, hitDamage: {num6}");
                        bool found = false;
                        foreach(GrabbableObject prop in LPHRoundManager.Props.Values)
                        {
                            if(prop.NetworkObjectId == component.NetworkObjectId)
                            {
                                found = true; break;
                            }
                        }
                        if (!found)
                        {
                            __instance.playerHeldBy.DamagePlayer(10, true, true, CauseOfDeath.Gunshots); //Damage player if they hit non player prop
                        }
                    }
                }
            }
        }
    }
}
