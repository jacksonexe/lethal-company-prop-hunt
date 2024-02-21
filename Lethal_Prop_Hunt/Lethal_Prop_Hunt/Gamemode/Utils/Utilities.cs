using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace LethalPropHunt.Gamemode
{
    internal class Utilities
    {

        public float teleporterChargeUp = 2f;
        public static void AddChatMessage(string chatMessage, string textColor = "FF0000") //Copy of the client side only message
        {
            if (!(HUDManager.Instance.lastChatMessage == chatMessage))
            {
                HUDManager.Instance.lastChatMessage = chatMessage;
                HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat, 4f);
                if (HUDManager.Instance.ChatMessageHistory.Count >= 4)
                {
                    HUDManager.Instance.chatText.text.Remove(0, HUDManager.Instance.ChatMessageHistory[0].Length);
                    HUDManager.Instance.ChatMessageHistory.Remove(HUDManager.Instance.ChatMessageHistory[0]);
                }
                StringBuilder stringBuilder = new StringBuilder(chatMessage);
                stringBuilder.Replace("[playerNum0]", StartOfRound.Instance.allPlayerScripts[0].playerUsername);
                stringBuilder.Replace("[playerNum1]", StartOfRound.Instance.allPlayerScripts[1].playerUsername);
                stringBuilder.Replace("[playerNum2]", StartOfRound.Instance.allPlayerScripts[2].playerUsername);
                stringBuilder.Replace("[playerNum3]", StartOfRound.Instance.allPlayerScripts[3].playerUsername);
                chatMessage = stringBuilder.ToString();
                string item = "<color=#" + textColor + ">'" + chatMessage + "'</color>";
                HUDManager.Instance.ChatMessageHistory.Add(item);
                HUDManager.Instance.chatText.text = "";
                for (int i = 0; i < HUDManager.Instance.ChatMessageHistory.Count; i++)
                {
                    TextMeshProUGUI textMeshProUGUI = HUDManager.Instance.chatText;
                    textMeshProUGUI.text = textMeshProUGUI.text + "\n" + HUDManager.Instance.ChatMessageHistory[i];
                }
            }
        }

        public static void DisplayTips(string header, string message, bool warn = false)
        {
            HUDManager.Instance.DisplayTip(header, message, warn);
        }

        public static void TeleportPlayer(int playerObj, Vector3 teleportPos)
        {
            PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObj];
            if ((bool)UnityEngine.Object.FindObjectOfType<AudioReverbPresets>())
            {
                UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[2].ChangeAudioReverbForPlayer(playerControllerB);
            }
            playerControllerB.isInElevator = false;
            playerControllerB.isInHangarShipRoom = false;
            playerControllerB.isInsideFactory = true;
            playerControllerB.averageVelocity = 0f;
            playerControllerB.velocityLastFrame = Vector3.zero;
            StartOfRound.Instance.allPlayerScripts[playerObj].TeleportPlayer(teleportPos);
            StartOfRound.Instance.allPlayerScripts[playerObj].beamOutParticle.Play();
            if (playerControllerB == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
        }

        public static System.Collections.IEnumerator TeleportPlayerCoroutine(int playerObj, Vector3 teleportPos)
        {
            yield return new WaitForSeconds(1f);
            Utilities.TeleportPlayer(playerObj, teleportPos);
            LPHNetworkHandler.Instance.TeleportPlayerServerRpc(playerObj, teleportPos);
        }


        public static UnlockableSuit GetSuitByName(string Name)
        {
            List<UnlockableItem> Unlockables = StartOfRound.Instance.unlockablesList.unlockables;

            foreach (UnlockableSuit unlockable in Resources.FindObjectsOfTypeAll<UnlockableSuit>())
            {
                if (unlockable.syncedSuitID.Value >= 0)
                {
                    string SuitName = Unlockables[unlockable.syncedSuitID.Value].unlockableName;

                    if (SuitName == Name)
                    {
                        return unlockable;
                    }
                }
            }

            return null;
        }
    }
}
