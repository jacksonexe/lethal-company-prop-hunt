using GameNetcodeStuff;
using HarmonyLib;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using UnityEngine;
using LethalPropHunt.Gamemode;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Lethal_Prop_Hunt.Gamemode.Utils;

namespace LethalPropHunt.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(RoundManager __instance)
        {
            ConfigManager.InitConfigurations();
            __instance.scrapAmountMultiplier = ConfigManager.ScrapMultiplier.Value;
            __instance.mapSizeMultiplier = ConfigManager.MapMultiplier.Value;
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                terminal.groupCredits = 500;
            }
            if (TimeOfDay.Instance != null)
            {
                TimeOfDay.Instance.globalTimeSpeedMultiplier = ConfigManager.TimeMultiplier.Value;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePatch()
        {
            //Despawn hazards
            EnemyAI[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            if(enemies != null && enemies.Length > 0 )
            {
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (!enemies[i].isEnemyDead)
                    {
                        enemies[i].gameObject.SetActive(false);
                    }
                }
            }

            Landmine[] landmines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (landmines != null && landmines.Length > 0)
            {
                for (int i = 0; i < landmines.Length; i++)
                {
                    landmines[i].gameObject.SetActive(false);
                }
            }

            Turret[] turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets != null && turrets.Length > 0)
            {
                for (int i = 0; i < turrets.Length; i++)
                {
                    turrets[i].gameObject.SetActive(false);
                }
            }

            //Move props
            if (LPHRoundManager.Instance.IsRunning && !LPHRoundManager.Instance.IsRoundEnding) //This should do it across all clients assuming the roles and props are properly synched.
            {
                foreach (ulong playerId in LPHRoundManager.Props.Keys)
                {
                    PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
                    GrabbableObject prop = LPHRoundManager.Props[playerId];
                    if (player.isPlayerDead)
                    {
                        InteractTrigger trigger = prop.gameObject.GetComponent<InteractTrigger>();
                        prop.EnablePhysics(true);
                        if (trigger != null)
                        {
                            trigger.interactable = true;
                        }
                        LPHNetworkHandler.Instance.SyncPropOwnershipServerRpc(playerId, prop.NetworkObjectId, false); //If killed, get our prop back
                    }
                    else
                    {
                        prop.transform.parent = null;
                        prop.EnablePhysics(false);
                        prop.targetFloorPosition = player.transform.localPosition + new Vector3(0f, 0.2f, 0f);
                        if (!LPHRoundManager.IsPlayerRotationLocked.ContainsKey(playerId) || !LPHRoundManager.IsPlayerRotationLocked[playerId])
                        {
                            prop.transform.rotation = Quaternion.Euler(player.gameplayCamera.transform.eulerAngles.x, player.gameplayCamera.transform.eulerAngles.y, player.gameplayCamera.transform.eulerAngles.z);
                        }
                        if (player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId) //Can only sync camera on client
                        {
                            Collider[] propColliders = prop.propColliders;
                            for (int i = 0; i < propColliders.Length; i++) //Disable collision since they maybe inside one another
                            {
                                if (propColliders[i] != null)
                                {
                                    Physics.IgnoreCollision(player.playerCollider, propColliders[i], true);
                                }
                            }
                            InteractTrigger trigger = prop.gameObject.GetComponent<InteractTrigger>();
                            if (trigger != null)
                            {
                                trigger.interactable = false;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (ulong playerId in LPHRoundManager.Props.Keys)
                {
                    GrabbableObject prop = LPHRoundManager.Props[playerId];
                    InteractTrigger trigger = prop.gameObject.GetComponent<InteractTrigger>();
                    prop.EnablePhysics(true);
                    if (trigger != null)
                    {
                        trigger.interactable = true;
                    }
                }
            }
        }

        [HarmonyPatch("SetLockedDoors")]
        [HarmonyPrefix]
        public static bool SetLockedDoorsPatch(Vector3 mainEntrancePosition, RoundManager __instance)
        {
            if(!ConfigManager.AllowKeys.Value) { return false; }
            //copied from source
            List<DoorLock> list = Object.FindObjectsOfType<DoorLock>().ToList();
            for (int num = list.Count - 1; num >= 0; num--)
            {
                if (list[num].transform.position.y > -160f)
                {
                    list.RemoveAt(num);
                }
            }
            list = list.OrderByDescending((DoorLock x) => (mainEntrancePosition - x.transform.position).sqrMagnitude).ToList();
            float num2 = 1.1f;
            int num3 = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (__instance.LevelRandom.NextDouble() < (double)num2)
                {
                    //Disable locked doors but include keys
                    num3++;
                }
                num2 /= 1.55f;
            }
            if (__instance.IsServer)
            {
                for (int j = 0; j < num3; j++)
                {
                    int num4 = __instance.AnomalyRandom.Next(0, __instance.insideAINodes.Length);
                    Vector3 randomNavMeshPositionInBoxPredictable = __instance.GetRandomNavMeshPositionInBoxPredictable(__instance.insideAINodes[num4].transform.position, 8f, __instance.navHit, __instance.AnomalyRandom);
                    Object.Instantiate(__instance.keyPrefab, randomNavMeshPositionInBoxPredictable, Quaternion.identity, __instance.spawnedScrapContainer).GetComponent<NetworkObject>().Spawn();
                }
            }
            return false; //Disable parent
        }
    }
}
