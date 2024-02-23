using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPropHunt.Audio;
using LethalPropHunt.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LethalPropHunt.Gamemode
{
    internal class LPHNetworkHandler : NetworkBehaviour
    {
        public static LPHNetworkHandler Instance { get; private set; }
        internal ManualLogSource mls;
        public AudioSource teleporterAudio;
        public AudioClip teleporterBeamUpSFX;
        public AudioClip startTeleportingSFX;
        public AudioClip teleporterPrimeSFX;

        public override void OnNetworkSpawn()
        {
            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && Instance != null)
                Instance.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;
            if (mls == null)
            {
                mls = BepInEx.Logging.Logger.CreateLogSource("LPHNetworkManager");
            }

            base.OnNetworkSpawn();
        }

        private ClientRpcParams createBroadcastConfig()
        {
            List<ulong> clients = new List<ulong>();
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                clients.Add(StartOfRound.Instance.allPlayerScripts[i].playerClientId);
            }
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clients.ToArray(),
                }
            };
            return clientRpcParams;
        }

        [ServerRpc(RequireOwnership = false)]
        public void sendRoleServerRpc(ulong id, string role)
        {
            if (mls == null)
            {
                mls = BepInEx.Logging.Logger.CreateLogSource("LPHNetworkManager");
            }
            mls.LogDebug("Sending role " + role + " to " + id);
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            sendRoleClientRpc(id, role, clientRpcParams);
        }

        [ClientRpc]
        public void sendRoleClientRpc(ulong id, string role, ClientRpcParams clientRpcParams = default)
        {
            if (mls == null)
            {
                mls = BepInEx.Logging.Logger.CreateLogSource("LPHNetworkManager");
            }
            mls.LogDebug("recieved role " + role + " to " + id);
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[id];
            if (player != null)
            {
                if (id == StartOfRound.Instance.localPlayerController.playerClientId)
                {
                    Utilities.DisplayTips("You are a ", role, role.Equals(LPHRoundManager.HUNTERS_ROLE));
                    Utilities.AddChatMessage("You are a " + role, role.Equals(LPHRoundManager.HUNTERS_ROLE) ? "FF0000" : "008000");
                    LPHRoundManager.Instance.RegisterLocalPlayersRole(role, player);
                    if (role.Equals(LPHRoundManager.PROPS_ROLE))
                    {
                        EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
                        if (array != null && array.Length != 0)
                        {
                            for (int j = 0; j < array.Length; j++)
                            {
                                array[j].enabled = false;
                                InteractTrigger interact = array[j].gameObject.GetComponent<InteractTrigger>();
                                if(interact != null)
                                {
                                    interact.interactable = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    LPHRoundManager.Instance.RegisterOtherPlayersRole(role, player);
                }

                if (role.Equals(LPHRoundManager.PROPS_ROLE))
                {
                    UnlockableSuit.SwitchSuitForPlayer(player, 24, true);
                    if (RoundManager.Instance.insideAINodes.Length != 0)
                    {
                        Vector3 position3 = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                        position3 = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(position3);
                        //teleporterAudio.PlayOneShot(teleporterPrimeSFX);
                        //player.movementAudio.PlayOneShot(teleporterPrimeSFX);
                        StartCoroutine(Utilities.TeleportPlayerCoroutine((int)player.playerClientId, position3));
                    }
                }
                else if (id == StartOfRound.Instance.localPlayerController.playerClientId)
                {
                    //Evil gets it good
                    UnlockableSuit.SwitchSuitForPlayer(player, 0, false);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void NotifyRoundOverServerRpc(string winner)
        {
            if (mls == null)
            {
                mls = BepInEx.Logging.Logger.CreateLogSource("LPHNetworkManager");
            }
            mls.LogDebug("Round ended, winner " + winner);

            HUDManager.Instance.AddTextToChatOnServer("Round over winner is: " + winner);
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            NotifyRoundOverClientRpc(winner, winner.Equals(LPHRoundManager.HUNTERS_ROLE), clientRpcParams);
        }

        [ClientRpc]
        public void NotifyRoundOverClientRpc(string winner, bool warn, ClientRpcParams clientRpcParams = default)
        {
            if (mls == null)
            {
                mls = BepInEx.Logging.Logger.CreateLogSource("LPHNetworkManager");
            }
            mls.LogDebug("Round ended received, winner " + winner);
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            Utilities.DisplayTips("Round Over ", "Winner: " + winner, warn);
            PlayerControllerBPatch.OnDisable();
            if (!player.isPlayerDead)
            {
                StartOfRound.Instance.ForcePlayerIntoShip();
                string crew = LPHRoundManager.Instance.GetPlayerRole(StartOfRound.Instance.localPlayerController);
                if (crew != null && !winner.Equals(crew))
                {
                    StartOfRound.Instance.localPlayerController.KillPlayer(new UnityEngine.Vector3(0, 0, 0), false);
                }
            }

            if (!StartOfRound.Instance.IsHost && !StartOfRound.Instance.IsServer)
            {
                LPHRoundManager.Instance.SetRoundOver();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void NotifyRoundStartServerRpc()
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            NotifyRoundStartClientRpc(clientRpcParams);
        }

        [ClientRpc]
        public void NotifyRoundStartClientRpc(ClientRpcParams clientRpcParans = default)
        {
            if (!StartOfRound.Instance.IsHost && !StartOfRound.Instance.IsServer)
            {
                LPHRoundManager.Instance.ResetRound();
                LPHRoundManager.Instance.SetRouteStarted();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void GiveHunterWeaponsServerRpc(ulong clientId)
        {
            PlayerControllerB player = null;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].playerClientId == clientId)
                {
                    player = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            Vector3 spawnPos = player.transform.position + new Vector3(0, 1f, 0);

            if (!player.isPlayerDead)
            {
                GameObject obj = UnityEngine.Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList[59].spawnPrefab, spawnPos, Quaternion.identity);
                obj.GetComponent<GrabbableObject>().fallTime = 0f;
                
                obj.AddComponent<ScanNodeProperties>().scrapValue = 0;
                obj.GetComponent<GrabbableObject>().SetScrapValue(0);
                obj.GetComponent<NetworkObject>().Spawn();
                ulong shotgunId = obj.GetComponent<GrabbableObject>().NetworkObjectId;

                obj = UnityEngine.Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList[10].spawnPrefab, spawnPos, Quaternion.identity);
                obj.GetComponent<GrabbableObject>().fallTime = 0f;

                obj.AddComponent<ScanNodeProperties>().scrapValue = 0;
                obj.GetComponent<GrabbableObject>().SetScrapValue(0);
                obj.GetComponent<NetworkObject>().Spawn();
                ulong shovelId = obj.GetComponent<GrabbableObject>().NetworkObjectId;

                obj = UnityEngine.Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList[9].spawnPrefab, spawnPos, Quaternion.identity);
                obj.GetComponent<GrabbableObject>().fallTime = 0f;

                obj.AddComponent<ScanNodeProperties>().scrapValue = 0;
                obj.GetComponent<GrabbableObject>().SetScrapValue(0);
                obj.GetComponent<NetworkObject>().Spawn();
                ulong radioId = obj.GetComponent<GrabbableObject>().NetworkObjectId;

                obj = UnityEngine.Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList[14].spawnPrefab, spawnPos, Quaternion.identity);
                obj.GetComponent<GrabbableObject>().fallTime = 0f;

                obj.AddComponent<ScanNodeProperties>().scrapValue = 0;
                obj.GetComponent<GrabbableObject>().SetScrapValue(0);
                obj.GetComponent<NetworkObject>().Spawn();
                ulong flashlightId = obj.GetComponent<GrabbableObject>().NetworkObjectId;
                /*GiveHunterWeaponsClientRpc(clientId, shotgunId, shovelId, radioId, flashlightId, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId },
                    }
                });*/
            }
        }

        private int FirstEmptyItemSlot(PlayerControllerB player)
        {
            int result = -1;
            if (player.ItemSlots[player.currentItemSlot] == null)
            {
                result = player.currentItemSlot;
            }
            else
            {
                for (int i = 0; i < player.ItemSlots.Length; i++)
                {
                    if (player.ItemSlots[i] == null)
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        private IEnumerator GrabObject(PlayerControllerB player, GrabbableObject prop)
        {
            yield return new WaitForSeconds(0.1f);
            prop.parentObject = player.localItemHolder;
            if (prop.itemProperties.grabSFX != null)
            {
                player.itemAudio.PlayOneShot(prop.itemProperties.grabSFX, 1f);
            }
            if (prop.playerHeldBy != null)
            {
                Debug.Log($"playerHeldBy on currentlyGrabbingObject 1: {prop.playerHeldBy}");
            }
            prop.GrabItemOnClient();
            player.isHoldingObject = true;
            yield return new WaitForSeconds(player.grabObjectAnimationTime - 0.2f);
            player.playerBodyAnimator.SetBool("GrabValidated", value: true);
            player.isGrabbingObjectAnimation = false;
        }

        [ClientRpc]
        internal void GiveHunterWeaponsClientRpc(ulong playerId, ulong shotgunId, ulong shovelId, ulong radioId, ulong flashlightId, ClientRpcParams clientRpcParans = default)
        {
            PlayerControllerB player = null;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].playerClientId == playerId)
                {
                    player = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null && (array[i].NetworkObjectId == shotgunId || array[i].NetworkObjectId == shovelId || array[i].NetworkObjectId == radioId || array[i].NetworkObjectId == flashlightId))
                {
                    try
                    {
                        array[i].InteractItem();
                        if (array[i].grabbable && FirstEmptyItemSlot(player) != -1)
                        {
                            player.playerBodyAnimator.SetBool("GrabInvalidated", value: false);
                            player.playerBodyAnimator.SetBool("GrabValidated", value: false);
                            player.playerBodyAnimator.SetBool("cancelHolding", value: false);
                            player.playerBodyAnimator.ResetTrigger("Throw");
                            player.isGrabbingObjectAnimation = true;
                            player.cursorIcon.enabled = false;
                            player.cursorTip.text = "";
                            player.twoHanded = array[i].itemProperties.twoHanded;
                            player.carryWeight += Mathf.Clamp(array[i].itemProperties.weight - 1f, 0f, 10f);
                            if (!player.isTestingPlayer)
                            {
                                NetworkObject networkObject = array[i].NetworkObject;
                                var GrabObjectServerRpcInfo = typeof(PlayerControllerB).GetMethod(
                                    "GrabObjectServerRpc",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                                );
                                GrabObjectServerRpcInfo.Invoke(player, new object[] { networkObject });
                            }
                            IEnumerator grabObjectCoroutine = (IEnumerator)Traverse.Create(player).Field("grabObjectCoroutine");
                            if (grabObjectCoroutine != null)
                            {
                                player.StopCoroutine(grabObjectCoroutine);
                            }
                            Traverse.Create(player).Field("grabObjectCoroutine").SetValue(player.StartCoroutine(GrabObject(player, array[i])));
                        }
                    }
                    catch (Exception e) { }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        internal void NotifyOfRoundEndingServerRpc()
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            NotifyOfRoundEndingClientRpc(clientRpcParams);
        }

        [ClientRpc]
        internal void NotifyOfRoundEndingClientRpc(ClientRpcParams clientRpcParans = default)
        {
            StartMatchLever startMatchLever = GameObject.FindObjectOfType<StartMatchLever>();
            startMatchLever.triggerScript.animationString = "SA_PushLeverBack";
            startMatchLever.leverHasBeenPulled = false;
            startMatchLever.triggerScript.interactable = false;
            startMatchLever.leverAnimatorObject.SetBool("pullLever", false);
            //Take them out of third person and remove props
            if (LPHRoundManager.Instance.IsPlayerProp(StartOfRound.Instance.localPlayerController))
            {
                PlayerControllerBPatch.OnDisable();
            }
            LPHRoundManager.Props.Clear();
            StartOfRound.Instance.SetShipDoorsClosed(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerServerRpc(int playerObj, Vector3 teleportPos)
        {
            TeleportPlayerClientRpc(playerObj, teleportPos);
        }

        [ClientRpc]
        public void TeleportPlayerClientRpc(int playerObj, Vector3 teleportPos)
        {
            //teleporterAudio.PlayOneShot(teleporterBeamUpSFX);
            //StartOfRound.Instance.allPlayerScripts[playerObj].movementAudio.PlayOneShot(teleporterBeamUpSFX);
            Utilities.TeleportPlayer(playerObj, teleportPos);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncPropOwnershipServerRpc(ulong playerClientId, ulong propObjectId, bool owned)
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            SyncPropOwnershipClientRpc(playerClientId, propObjectId, owned, clientRpcParams);
        }

        [ClientRpc]
        public void SyncPropOwnershipClientRpc(ulong playerId, ulong objectId, bool owned, ClientRpcParams clientRpcParams = default)
        {
            mls.LogDebug("recieved prop ownership request for " + playerId + " for " + objectId + " status: " + owned);
            GrabbableObject prop = null;
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null && array[i].NetworkObjectId == objectId)
                {
                    prop = (GrabbableObject)array[i];
                }
            }
            if (prop == null || StartOfRound.Instance == null)
            {
                mls.LogError("Unable to locate prop");
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)playerId];
            LPHRoundManager.Instance.SetPropOwnership(player, prop, owned);
        }

        [ServerRpc(RequireOwnership = false)]
        internal void NotifyOfRotationLockChangeServerRpc(ulong playerId)
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            NotifyOfRotationLockChangeClientRpc(playerId, clientRpcParams);
        }

        [ClientRpc]
        internal void NotifyOfRotationLockChangeClientRpc(ulong playerId, ClientRpcParams clientRpcParams = default)
        {
            if (LPHRoundManager.IsPlayerRotationLocked.ContainsKey(playerId))
            {
                LPHRoundManager.IsPlayerRotationLocked[playerId] = !LPHRoundManager.IsPlayerRotationLocked[playerId];
            }
            else
            {
                LPHRoundManager.IsPlayerRotationLocked.Add(playerId, true);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        internal void SwapPropOwnershipServerRpc(ulong playerClientId, ulong oldProp, ulong newProp)
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            SwapPropOwnershipClientRpc(playerClientId, oldProp, newProp, clientRpcParams);
        }

        [ClientRpc]
        public void SwapPropOwnershipClientRpc(ulong playerId, ulong oldPropId, ulong newPropId, ClientRpcParams clientRpcParams = default)
        {
            mls.LogDebug("recieved prop ownership swap request for " + playerId + " from " + oldPropId + " to " + newPropId);
            GrabbableObject oldProp = null;
            GrabbableObject newProp = null;
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null && array[i].NetworkObjectId == oldPropId)
                {
                    oldProp = (GrabbableObject)array[i];
                }

                if (array[i] != null && array[i].NetworkObjectId == newPropId)
                {
                    newProp = (GrabbableObject)array[i];
                }
            }
            if (oldProp == null || newProp == null || StartOfRound.Instance == null)
            {
                mls.LogError("Unable to locate prop");
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)playerId];
            LPHRoundManager.Instance.SwapPropOwnership(player, oldProp, newProp);
        }

        [ServerRpc(RequireOwnership = false)]
        internal void SyncPlayAudioServerRpc(ulong playerClientId, string assetName)
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            SyncPlayAudioClientRpc(playerClientId, assetName, clientRpcParams);
        }

        [ClientRpc]
        public void SyncPlayAudioClientRpc(ulong playerClientId, string assetName, ClientRpcParams clientRpcParams = default)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)playerClientId];
            player.movementAudio.PlayOneShot(AudioManager.LoadAudioClip(assetName), AudioManager.TauntVolume.Value);
        }
    }
}
