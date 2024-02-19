﻿using GameNetcodeStuff;
using HarmonyLib;
using LethalPropHunt.Gamemode;
using UnityEngine;

namespace LethalPropHunt.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("OnShipLandedMiscEvents")]
        [HarmonyPostfix]
        static void OnShipLandedMiscEventsPatch(StartOfRound __instance)
        {
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                terminal.groupCredits = 500;
            }
            if (__instance.IsHost || __instance.IsServer) 
            {
                LPHRoundManager.Instance.StartRound(__instance);
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(StartOfRound __instance)
        {
            if (__instance.IsHost || __instance.IsServer)
            {
                if (!LPHRoundManager.Instance.IsInitializing)
                {
                    if (LPHRoundManager.Instance.IsRunning && !LPHRoundManager.Instance.IsRoundEnding)
                    {
                        StartMatchLever startMatchLever = GameObject.FindObjectOfType<StartMatchLever>();
                        startMatchLever.triggerScript.enabled = false;
                        LPHRoundManager.Instance.CheckIfRoundOver(__instance);
                    }
                    else if (LPHRoundManager.Instance.IsRoundEnding && !LPHRoundManager.Instance.IsRoundOver)
                    {
                        LPHRoundManager.Instance.SetRoundOver();
                        LPHNetworkHandler.Instance.NotifyOfRoundEndingServerRpc();
                        StartOfRound.Instance.EndGameServerRpc(0);
                    }

                    if (LPHRoundManager.Instance.IsRunning && __instance.shipIsLeaving && TimeOfDay.Instance.currentDayTime / TimeOfDay.Instance.totalTime >= TimeOfDay.Instance.shipLeaveAutomaticallyTime)
                    {
                        LPHRoundManager.Instance.HandleShipLeaveMidnight(__instance);
                    }
                }
            }
            if (__instance.inShipPhase)
            {
                StartMatchLever startMatchLever = GameObject.FindObjectOfType<StartMatchLever>();
                int numConnected = 0;
                for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
                {
                    PlayerControllerB controller = __instance.allPlayerScripts[i];
                    if (controller.isActiveAndEnabled && (controller.isPlayerControlled || controller.isPlayerDead))
                    {
                        numConnected++;
                    }
                }
                if (numConnected < 2)
                {
                    startMatchLever.triggerScript.enabled = false;
                    startMatchLever.triggerScript.hoverTip = "At least 2 players are required";
                    startMatchLever.triggerScript.disableTriggerMesh = true;
                }
                else
                {
                    startMatchLever.triggerScript.enabled = true;
                    startMatchLever.triggerScript.hoverTip = "Start Round";
                    startMatchLever.triggerScript.disableTriggerMesh = false;
                }
            }
        }

        [HarmonyPatch("PassTimeToNextDay")]
        [HarmonyPostfix]
        static void PassTimeToNextDayPatch(StartOfRound __instance)
        {
            int levelSelection = StartOfRound.Instance.currentLevelID;
            StartOfRound.Instance.ResetShip();
            __instance.ChangeLevel(levelSelection);
            __instance.ChangePlanet();
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                terminal.groupCredits = 500;
            }
            TimeOfDay.Instance.daysUntilDeadline = 1;
            StartMatchLever startMatchLever = GameObject.FindObjectOfType<StartMatchLever>();
            startMatchLever.triggerScript.enabled = true;
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                player.thisPlayerModel.enabled = true;
                player.thisPlayerModel.gameObject.SetActive(true);
                player.thisPlayerModelLOD1.gameObject.SetActive(true);
                player.thisPlayerModelLOD2.gameObject.SetActive(true);
                player.usernameCanvas.gameObject.SetActive(true);
                player.usernameBillboard.gameObject.SetActive(true);
                player.usernameBillboardText.gameObject.SetActive(true);
                player.usernameBillboardText.SetText(player.playerUsername);
            }
        }

    }
}
