using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using System.Runtime.CompilerServices;
using LethalPropHunt.Patches;
using System.Reflection.Emit;
using Lethal_Prop_Hunt.Gamemode.Utils;
using LethalPropHunt.Input;

namespace LethalPropHunt.Gamemode
{
    public class LPHRoundManager
    {
        public static List<PlayerControllerB> PropPlayers = new List<PlayerControllerB>();
        public static Dictionary<ulong, bool> IsPlayerRotationLocked = new Dictionary<ulong, bool>();
        public static Dictionary<ulong, GrabbableObject> Props = new Dictionary<ulong, GrabbableObject>();
        public static List<PlayerControllerB> Hunters = new List<PlayerControllerB>();

        public static bool IsLocalPlayerProp = false;

        public static readonly string PROPS_ROLE = "Prop";
        public static readonly string HUNTERS_ROLE = "Hunter";

        internal ManualLogSource mls;
        private static LPHRoundManager _instance;
        public static LPHRoundManager Instance 
        {
            get {
                LPHRoundManager.Init();
                return _instance;
            }
            private set { _instance = value; }
        }
        public bool IsRunning { get; private set; }
        public bool IsInitializing { get; private set; }
        public bool IsRoundEnding { get; private set; }
        public bool IsRoundOver { get; private set; }

        public LPHRoundManager() {
            mls = BepInEx.Logging.Logger.CreateLogSource("LPHNetworkManager");
        }

        public static void Init()
        {
            if(_instance == null)
            {
                _instance = new LPHRoundManager();
            }
        }

        public void StartRound(StartOfRound playersManager)
        {
            if (IsRunning) return;
            this.ResetRound();
            IsInitializing = true;
            populatePlayers(playersManager);
            IsInitializing = false;
            IsRunning = true;
        }

        public void StopRound() {  IsRunning = false; }

        private List<PlayerControllerB> GetProps(StartOfRound playersManager)
        {
            List<PlayerControllerB> playersList = new List<PlayerControllerB>();
            List<PlayerControllerB> props = new List<PlayerControllerB>();
            for (int i = 0; i < playersManager.allPlayerScripts.Length; i++)
            {
                PlayerControllerB controller = playersManager.allPlayerScripts[i];
                if (controller.isActiveAndEnabled && (controller.isPlayerControlled || controller.isPlayerDead))
                {
                    mls.LogDebug("Getting player info for " + controller.playerClientId);
                    playersList.Add(controller);
                }
            }
            float dist = (float)ConfigManager.PlayerDistribution.Value / 100f;
            float numPlayers = playersList.Count;
            int numProps = (int)Math.Floor(numPlayers * dist);
            mls.LogDebug("Picked " + numProps + " based on dist " + dist + " and player count " + (playersList.Count));

            if (numProps > playersList.Count || numProps == 0)
            {
                numProps = playersList.Count - 1;
            }
            for (int i = 1; i <= numProps; i++)
            {
                System.Random rand = new System.Random();
                PlayerControllerB propController = playersList[rand.Next(0, playersList.Count)];
                mls.LogDebug("Picked " + propController.playerClientId + " as a prop from between 0 and " + (playersList.Count-1));
                playersList.Remove(propController);
                props.Add(propController);
            }
            return props;
        }

        private void populatePlayers(StartOfRound playersManager)
        {
            ulong localPlayer = GameNetworkManager.Instance.localPlayerController.playerClientId;
            List<PlayerControllerB> props = this.GetProps(playersManager);
            LPHNetworkHandler.Instance.NotifyRoundStartServerRpc();
            for (int i = 0; i < playersManager.allPlayerScripts.Length; i++)
            {
                PlayerControllerB controller = playersManager.allPlayerScripts[i];
                if (controller.isActiveAndEnabled && (controller.isPlayerControlled || controller.isPlayerDead))
                {
                    ulong id = controller.playerClientId;
                    if (controller != null)
                    {
                        string role = PROPS_ROLE;
                        if (props.Contains(controller))
                        {
                            PropPlayers.Add(controller);
                            if (id == localPlayer)
                            {
                                IsLocalPlayerProp = true;
                            }
                        }
                        else
                        {
                            role = HUNTERS_ROLE;
                            Hunters.Add(controller);
                            if (id == localPlayer)
                            {
                                IsLocalPlayerProp = false;
                            }
                        }                       
                        mls.LogDebug("Picked " + role + " for " + id);
                        LPHNetworkHandler.Instance.sendRoleServerRpc(id, role);
                    }
                }
            }
        }

        public void RegisterLocalPlayersRole(string role, PlayerControllerB player)
        {
            mls.LogDebug("Registering Role for local player " + role);
            if (role.Equals(PROPS_ROLE))
            {
                if (!PropPlayers.Contains(player))
                {
                    PropPlayers.Add(player);
                }
                IsLocalPlayerProp = true;
            }
            else
            {
                if (!Hunters.Contains(player))
                {
                    Hunters.Add(player);
                }
                IsLocalPlayerProp = false;
            }
        }

        public void RegisterOtherPlayersRole(string role, PlayerControllerB player)
        {
            if (role.Equals(PROPS_ROLE))
            {
                if (!PropPlayers.Contains(player))
                {
                    PropPlayers.Add(player);
                }
                IsLocalPlayerProp = true;
            }
            else
            {
                if (!Hunters.Contains(player))
                {
                    Hunters.Add(player);
                }
                IsLocalPlayerProp = false;
            }
        }

        public void ResetRound()
        {
            PropPlayers.Clear();
            Hunters.Clear();
            Props.Clear();
            IsPlayerRotationLocked.Clear();
            IsLocalPlayerProp = false;
            IsRoundOver = false;
            LPHInputManagement.LastLocalTaunt = DateTime.Now;
            LPHInputManagement.HasTauntedYet = false;
        }

        public void SetRouteStarted()
        {
            IsRunning = true;
        }

        public void SetRoundOver() { 
            IsRoundOver = true;
            IsRoundEnding = false;
            IsRunning = false;
            Props.Clear();
            IsPlayerRotationLocked.Clear();
        }

        public void CheckIfRoundOver(StartOfRound playersManager)
        {
            if (IsInitializing || IsRoundEnding || IsRoundOver) return;
            bool isOnlyOneFactionAlive = true;
            string startRole = null;
            int activeCrew = 0;
            for (int i = 0; i < playersManager.allPlayerScripts.Length; i++)
            {
                PlayerControllerB controller = playersManager.allPlayerScripts[i];
                if (controller != null && controller.isActiveAndEnabled && (controller.isPlayerControlled || controller.isPlayerDead))
                {
                    string foundRole = HUNTERS_ROLE;
                    activeCrew++;
                    if (PropPlayers.Contains(controller))
                    {
                        foundRole = PROPS_ROLE;
                    }
                    if (foundRole != null)
                    {
                        if (startRole == null)
                        {
                            if (controller.isPlayerDead)
                            {
                                continue;
                            }
                            startRole = foundRole;
                        }
                        if(!startRole.Equals(foundRole) && !controller.isPlayerDead)
                        {
                            isOnlyOneFactionAlive = false;
                        }
                    }
                }
            }
            if (isOnlyOneFactionAlive && startRole != null) //Make sure players are done loading
            {
                LPHNetworkHandler.Instance.NotifyRoundOverServerRpc(startRole);
                IsRoundEnding = true;
            }
        }

        public void HandleShipLeaveMidnight(StartOfRound playersManager)
        {
            bool livingPropFound = false;
            for (int i = 0; i < playersManager.allPlayerScripts.Length; i++)
            {
                string foundRole = GetPlayerRole(playersManager.allPlayerScripts[i]);
                if (foundRole != null)
                {
                    if (foundRole.Equals(PROPS_ROLE) && !playersManager.allPlayerScripts[i].isPlayerDead)
                    {
                        livingPropFound = true;
                        break;
                    }
                }
            }
            LPHNetworkHandler.Instance.NotifyRoundOverServerRpc(livingPropFound ? PROPS_ROLE : HUNTERS_ROLE);
            IsRoundEnding = true;
        
    }

        public string GetPlayerRole(PlayerControllerB player)
        {
            return PropPlayers.Contains(player) ? PROPS_ROLE : HUNTERS_ROLE;
        }

        public bool IsPlayerProp(PlayerControllerB player)
        {
            return PropPlayers.Contains(player);
        }

        internal string GetPlayerRoleById(ulong playerId)
        {
            PlayerControllerB player = null;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].playerClientId == playerId)
                {
                    player = StartOfRound.Instance.allPlayerScripts[i];
                }
            }

            return this.GetPlayerRole(player);
        }

        public void SetPropOwnership(PlayerControllerB player, GrabbableObject prop, bool owned)
        {
            if (!IsRunning || IsRoundEnding || player == null || prop == null || Props == null || StartOfRound.Instance == null) { return; }
            if(owned && !Props.ContainsKey(player.playerClientId))
            {
                Props.Add(player.playerClientId, prop);
                if(player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                {
                    PlayerControllerBPatch.OnEnable();
                }
                player.playerCollider.transform.localScale = prop.propBody.transform.localScale;
            }
            else if(!owned && Props.ContainsKey(player.playerClientId)) {
                if (player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                {
                    PlayerControllerBPatch.OnDisable();
                }
                prop.FallToGround();
                InteractTrigger trigger = prop.gameObject.GetComponent<InteractTrigger>();
                prop.EnablePhysics(true);
                if (trigger != null)
                {
                    trigger.interactable = true;
                }
                Props.Remove(player.playerClientId);
                player.playerCollider.transform.localScale = PlayerControllerBPatch.DefaultScale;
            }
        }

        internal void SwapPropOwnership(PlayerControllerB player, GrabbableObject oldProp, GrabbableObject newProp)
        {
            this.SetPropOwnership(player, oldProp, false);
            this.SetPropOwnership(player, newProp, true);
        }
    }
}
