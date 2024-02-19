using BepInEx.Logging;
using GameNetcodeStuff;
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
                }
                else
                {
                    LPHRoundManager.Instance.RegisterOtherPlayersRole(role, player);
                }

                if (role.Equals(LPHRoundManager.PROPS_ROLE))
                {
                    if (RoundManager.Instance.insideAINodes.Length != 0)
                    {
                        Vector3 position3 = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                        position3 = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(position3);
                        //teleporterAudio.PlayOneShot(teleporterPrimeSFX);
                        //player.movementAudio.PlayOneShot(teleporterPrimeSFX);
                        StartCoroutine(Utilities.TeleportPlayerCoroutine((int)player.playerClientId, position3));
                    }
                }
                else if(id == StartOfRound.Instance.localPlayerController.playerClientId)
                {
                    //Evil gets it good
                    GiveShotgunServerRpc(id);
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
            if (!StartOfRound.Instance.IsHost && !StartOfRound.Instance.IsServer) {
                LPHRoundManager.Instance.ResetRound();
                LPHRoundManager.Instance.SetRouteStarted();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void GiveShotgunServerRpc(ulong clientId)
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
            if (LPHRoundManager.IsLocalPlayerProp)
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
            if(prop == null || StartOfRound.Instance == null)
            {
                mls.LogError("Unable to locate prop");
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)playerId];
            LPHRoundManager.Instance.SetPropOwnership(player, prop, owned);
        }

        [ServerRpc(RequireOwnership = false)]
        internal void PlayTauntServerRpc(ulong playerClientId, string role)
        {
            ClientRpcParams clientRpcParams = createBroadcastConfig();
            PlayTauntClientRpc(playerClientId, role, clientRpcParams);
        }

        [ClientRpc]
        public void PlayTauntClientRpc(ulong playerId, string role, ClientRpcParams clientRpcParams = default)
        {
            PlayerControllerB tauntingPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
            AudioClip toPlay = null;
            if(role == null)
            {
                
            }
            else if (role.Equals(LPHRoundManager.PROPS_ROLE))
            {

            }
            else
            {

            }
            if (toPlay != null) {
                tauntingPlayer.currentVoiceChatAudioSource.PlayOneShot(toPlay);
            }
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
    }
}
