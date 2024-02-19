using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;
using LethalPropHunt.Patches;
using LethalPropHunt.Gamemode;

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

        public LPHInputManagement()
        {
            LockRotation.performed += LockRotationListener;
            DropProp.performed += DropPropListener;
            Taunt.performed += TauntListener;
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

        private void TauntListener(InputAction.CallbackContext action)
        {
            if (!action.performed) { return; }
            LPHNetworkHandler.Instance.PlayTauntServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, LPHRoundManager.Instance.GetPlayerRole(StartOfRound.Instance.localPlayerController));
        }
    }
}
