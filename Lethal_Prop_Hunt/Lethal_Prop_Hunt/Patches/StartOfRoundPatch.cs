using GameNetcodeStuff;
using HarmonyLib;
using Lethal_Prop_Hunt.Gamemode.Utils;
using LethalPropHunt.Gamemode;
using MoreCompany.Cosmetics;
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
            if (__instance.IsHost || __instance.IsServer)
            {
                foreach (PlayerControllerB player in LPHRoundManager.Hunters)
                {
                    LPHNetworkHandler.Instance.GiveHunterWeaponsServerRpc(player.playerClientId);
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(StartOfRound __instance)
        {
            if (__instance.mapScreen != null)
            {
                __instance.mapScreen.enabled = false; //Disable map
                __instance.mapScreen.gameObject.SetActive(false);
            }
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
                if (PropHuntBase.IsMoreCompanyLoaded() && RoundManagerPatch.PlayerCosmetics.ContainsKey(player.playerClientId))
                {
                    CosmeticApplication cosmetic = RoundManagerPatch.PlayerCosmetics[player.playerClientId];
                    if (cosmetic != null)
                    {
                        foreach (CosmeticInstance spawnedCosmetic in cosmetic.spawnedCosmetics)
                        {
                            spawnedCosmetic.gameObject.SetActive(true);
                        }
                    }
                }
            }
            PlayerControllerBPatch.OnDisable();
            UnlockableSuit.SwitchSuitForAllPlayers(0);

        }

    }
}
