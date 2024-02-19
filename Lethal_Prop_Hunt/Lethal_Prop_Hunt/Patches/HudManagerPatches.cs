using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using LethalPropHunt.Gamemode;
using UnityEngine;

namespace LethalPropHunt.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HudManagerPatches
    {

        [HarmonyPatch("SetSpectatingTextToPlayer")]
        [HarmonyPostfix]
        static public void SetSpectatingTextToPlayerPatch(PlayerControllerB playerScript, ref HUDManager __instance)
        {
            if (playerScript == null)
            {
                __instance.spectatingPlayerText.text = "";
            }
            else
            {
                string role = LPHRoundManager.Instance.GetPlayerRole(playerScript);
                if (role != null)
                {
                    __instance.spectatingPlayerText.text = "(Spectating: " + playerScript.playerUsername + " - " + role + ")";
                }
            }
        }

        [HarmonyPatch("UpdateBoxesSpectateUI")]
        [HarmonyPostfix]
        static public void UpdateBoxesSpectateUI(HUDManager __instance)
        {
            Dictionary<Animator, PlayerControllerB> ___spectatingPlayerBoxes = Traverse.Create(__instance).Field("spectatingPlayerBoxes").GetValue() as Dictionary<Animator, PlayerControllerB>;
            PlayerControllerB playerScript;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                playerScript = StartOfRound.Instance.allPlayerScripts[i];
                if (___spectatingPlayerBoxes.Values.Contains(playerScript))
                {
                    GameObject gameObject = ___spectatingPlayerBoxes.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == playerScript).Key.gameObject;
                    string role = LPHRoundManager.Instance.GetPlayerRole(playerScript);
                    if (role != null)
                    {
                        gameObject.GetComponentInChildren<TextMeshProUGUI>().text = playerScript.playerUsername + " - " + role;
                    }
                }
            }
        }

        [HarmonyPatch("FillEndGameStats")]
        [HarmonyPrefix]
        public static void FillEndGameStatsPatch(object[] __args)
        {
            EndOfGameStats stats = (EndOfGameStats)__args[0];
            for (int i = 0; i < stats.allPlayerStats.Length; i++)
            {
                PlayerStats playerStats = stats.allPlayerStats[i];
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                string role = LPHRoundManager.Instance.GetPlayerRole(player);
                if (role != null)
                {
                    playerStats.playerNotes.Clear();
                    playerStats.playerNotes.Add("Role: " + role);
                }
            }
        }

        //From LCThirdPerson https://github.com/bakerj76/LCThirdPerson
        [HarmonyPrefix]
        [HarmonyPatch("UpdateScanNodes")]
        private static void UnderwaterPrepatch(PlayerControllerB playerScript)
        {
            if (PropHuntBase.Camera == null)
            {
                return;
            }

            playerScript.gameplayCamera.transform.position = PropHuntBase.Camera.transform.position;
            playerScript.gameplayCamera.transform.rotation = PropHuntBase.Camera.transform.rotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateScanNodes")]
        private static void UnderwaterPostpatch(PlayerControllerB playerScript)
        {
            if (PropHuntBase.OriginalTransform == null)
            {
                return;
            }

            playerScript.gameplayCamera.transform.position = PropHuntBase.OriginalTransform.transform.position;
            playerScript.gameplayCamera.transform.rotation = PropHuntBase.OriginalTransform.transform.rotation;
        }
    }
}
