using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;
using LethalPropHunt.Patches;
using LethalPropHunt.Gamemode;
using LethalPropHunt.Audio;
using System;
using GameNetcodeStuff;

namespace LethalPropHunt.Input
{
    public class LPHInputManagement : LcInputActions
    {
        [InputAction("<Keyboard>/k", Name = "Lock Rotation")]
        public InputAction LockRotation { get; set; }

        [InputAction("<Keyboard>/g", Name = "Drop Possessed Prop")]
        public InputAction DropProp { get; set; }

        [InputAction("<Keyboard>/z", Name = "Taunt")]
        public InputAction Taunt { get; set; }

        [InputAction("<Mouse>/leftButton", Name = "Activate Possessed Item")]
        public InputAction ActivatePossessedItem { get; set; }

        public static int TAUNT_DELAY = 5;
        public static bool HasTauntedYet = false;
        public static DateTime LastLocalTaunt;

        public LPHInputManagement()
        {
            LockRotation.performed += LockRotationListener;
            DropProp.performed += DropPropListener;
            Taunt.performed += TauntListener;
            ActivatePossessedItem.performed += ActivatePossessedItemListener;
        }

        private void ActivatePossessedItemListener(InputAction.CallbackContext action)
        {
            if (!action.performed) { return; }
            if (LPHRoundManager.Instance.IsRunning)
            {
                PlayerControllerB player = StartOfRound.Instance.localPlayerController;
                if (LPHRoundManager.Instance.IsPlayerProp(player) && LPHRoundManager.Props.ContainsKey(player.playerClientId))
                {
                    GrabbableObject prop = LPHRoundManager.Props[player.playerClientId];
                    if (player.quickMenuManager.isMenuOpen || player.isPlayerDead || prop == null || prop.itemProperties == null || (prop.itemProperties.usableInSpecialAnimations && (player.isGrabbingObjectAnimation || player.inTerminalMenu || player.isTypingChat || (player.inSpecialInteractAnimation && !player.inShockingMinigame))))
                    {
                        return;
                    }
                    prop.UseItemOnClient();
                }
            }
        }

        public void LockRotationListener(InputAction.CallbackContext action)
        {
            if (!action.performed) { return; }

            LPHNetworkHandler.Instance.NotifyOfRotationLockChangeServerRpc(StartOfRound.Instance.localPlayerController.playerClientId);
        }

        public void DropPropListener(InputAction.CallbackContext action)
        {
            if (!action.performed) { return; }

            if (LPHRoundManager.Instance.IsRunning && LPHRoundManager.Props.ContainsKey(StartOfRound.Instance.localPlayerController.playerClientId))
            {
                LPHNetworkHandler.Instance.SyncPropOwnershipServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, LPHRoundManager.Props[StartOfRound.Instance.localPlayerController.playerClientId].NetworkObjectId, false);
            }
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        private void TauntListener(InputAction.CallbackContext action)
        {
            if (!action.performed) { return; }
            if (LPHRoundManager.Instance != null && LPHRoundManager.Instance.IsRunning)
            {
                if(LastLocalTaunt != null && HasTauntedYet)
                {
                    TimeSpan diff = DateTime.Now - LastLocalTaunt;
                    if(diff.TotalSeconds < TAUNT_DELAY) //Can't spam
                    {
                        return;
                    }
                }
                LastLocalTaunt = DateTime.Now;
                HasTauntedYet = true;
                LPHNetworkHandler.Instance.SyncPlayAudioServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, AudioManager.SelectRandomClip(LPHRoundManager.Instance.IsPlayerProp(StartOfRound.Instance.localPlayerController) ? LPHRoundManager.PROPS_ROLE : LPHRoundManager.HUNTERS_ROLE));
            }
        }
    }
}
