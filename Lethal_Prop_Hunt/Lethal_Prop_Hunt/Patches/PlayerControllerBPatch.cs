using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LethalPropHunt.Gamemode;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem;
using LethalPropHunt.Input;
using LethalPropHunt.Audio;
using Lethal_Prop_Hunt.Gamemode.Utils;
using BepInEx;

namespace LethalPropHunt.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        //From LCThirdPerson https://github.com/bakerj76/LCThirdPerson
        public static PlayerControllerB Instance;
        private static bool InThirdPerson = false;
        private static bool TriggerAwake;
        private static int OriginalCullingMask;
        private static UnityEngine.Rendering.ShadowCastingMode OriginalShadowCastingMode;
        public static Vector3 DefaultScale;
        private static readonly string[] IgnoreGameObjectPrefixes = new[]{
            "VolumeMain"
        };

        public static void OnEnable()
        {
            if (InThirdPerson || Instance == null)
            {
                return;
            }
            InThirdPerson = true;
            var visor = Instance.localVisor;
            var playerModel = Instance.thisPlayerModel;

            // Hide the visor
            visor.gameObject.SetActive(false);

            // Show the player model
            playerModel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // Hide the player arms
            Instance.thisPlayerModelArms.enabled = false;

            // Increase the grab distance
            Instance.grabDistance = Math.Max(Instance.grabDistance - PropHuntBase.Offset.Value.z, 5);

            // Set culling mask to see model's layer
            Instance.gameplayCamera.cullingMask = OriginalCullingMask | (1 << 23);
        }

        public static void OnDisable()
        {
            if (!InThirdPerson || Instance == null)
            {
                return;
            }
            InThirdPerson = false;
            var visor = Instance.localVisor;
            var playerModel = Instance.thisPlayerModel;

            // Show the visor
            visor.gameObject.SetActive(true);

            // Hide the player model
            playerModel.shadowCastingMode = OriginalShadowCastingMode;

            // Show the arms
            Instance.thisPlayerModelArms.enabled = true;

            // Reset the grab distance
            Instance.grabDistance = 5f;

            // Hide the models' layer again
            Instance.gameplayCamera.cullingMask = OriginalCullingMask;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Awake()
        {
            TriggerAwake = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void PatchUpdate(ref PlayerControllerB __instance, ref bool ___isCameraDisabled, ref bool ___isPlayerControlled, ref MeshFilter ___playerBadgeMesh, ref MeshRenderer ___playerBetaBadgeMesh)
        {
            ___playerBadgeMesh.gameObject.SetActive(false);
            ((Renderer)___playerBetaBadgeMesh).enabled = false;
            ___playerBetaBadgeMesh.gameObject.SetActive(false);
            if (LPHRoundManager.Instance.IsRunning && !LPHRoundManager.Instance.IsRoundEnding)
            {
                if(__instance.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId && LPHRoundManager.Instance.IsPlayerProp(StartOfRound.Instance.localPlayerController))
                {
                    if (LPHInputManagement.LastLocalTaunt == null)
                    {
                        LPHInputManagement.LastLocalTaunt = DateTime.Now;
                    }
                    TimeSpan diff = DateTime.Now - LPHInputManagement.LastLocalTaunt;
                    if(diff.TotalSeconds > ConfigManager.ForceTauntInterval.Value && ConfigManager.ForceTaunt.Value) //Force taunting if enabled
                    {
                        LPHNetworkHandler.Instance.SyncPlayAudioServerRpc(__instance.playerClientId, AudioManager.SelectRandomClip(LPHRoundManager.PROPS_ROLE));
                        LPHInputManagement.LastLocalTaunt = DateTime.Now;
                        LPHInputManagement.HasTauntedYet = true;
                    }
                }
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (LPHRoundManager.Instance.IsPlayerProp(player) && LPHRoundManager.Props.ContainsKey(player.playerClientId) && !player.isPlayerDead)
                    {
                        player.thisPlayerModel.gameObject.SetActive(false);
                        player.thisPlayerModelLOD1.gameObject.SetActive(false);
                        player.thisPlayerModelLOD2.gameObject.SetActive(false);
                        player.thisPlayerModel.enabled = false;
                        player.usernameCanvas.gameObject.SetActive(false);
                        player.usernameBillboard.gameObject.SetActive(false);
                        player.usernameBillboardText.gameObject.SetActive(false);
                        player.usernameBillboardText.SetText("");
                        if (PropHuntBase.IsMoreCompanyLoaded())
                        {
                            MoreCompany.Cosmetics.CosmeticApplication cosmetic = RoundManagerPatch.GetPlayerCosmetics(player);
                            if (cosmetic != null)
                            {
                                foreach (MoreCompany.Cosmetics.CosmeticInstance spawnedCosmetic in cosmetic.spawnedCosmetics)
                                {
                                    ((Component)spawnedCosmetic).gameObject.SetActive(false);
                                }
                            }
                            else
                            {
                                PropHuntBase.mls.LogDebug("Could not find cosmetics for " + player.playerClientId);
                            }
                        }
                    }
                    else
                    {
                        player.thisPlayerModel.enabled = true;
                        player.thisPlayerModel.gameObject.SetActive(true);
                        player.thisPlayerModelLOD1.gameObject.SetActive(true);
                        player.thisPlayerModelLOD2.gameObject.SetActive(true);
                        player.usernameCanvas.gameObject.SetActive(true);
                        player.usernameBillboard.gameObject.SetActive(true);
                        player.usernameBillboardText.gameObject.SetActive(true);
                        player.usernameBillboardText.SetText(player.playerUsername);
                        if (PropHuntBase.IsMoreCompanyLoaded())
                        {
                            MoreCompany.Cosmetics.CosmeticApplication cosmetic = RoundManagerPatch.GetPlayerCosmetics(player);
                            if (cosmetic != null)
                            {
                                foreach (MoreCompany.Cosmetics.CosmeticInstance spawnedCosmetic in cosmetic.spawnedCosmetics)
                                {
                                    spawnedCosmetic.gameObject.SetActive(true);
                                }
                            }
                        }
                    }
                }
            }
            else if (LPHRoundManager.Instance.IsRunning && LPHRoundManager.Instance.IsRoundEnding)
            {
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
                    if (PropHuntBase.IsMoreCompanyLoaded())
                    {
                        MoreCompany.Cosmetics.CosmeticApplication cosmetic = RoundManagerPatch.GetPlayerCosmetics(player);
                        if (cosmetic != null)
                        {
                            foreach (MoreCompany.Cosmetics.CosmeticInstance spawnedCosmetic in cosmetic.spawnedCosmetics)
                            {
                                spawnedCosmetic.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
            if (!___isPlayerControlled || ___isCameraDisabled)
            {
                return;
            }
            if (__instance.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                if (TriggerAwake)
                {
                    Instance = __instance;
                    OriginalCullingMask = Instance.gameplayCamera.cullingMask;
                    OriginalShadowCastingMode = Instance.thisPlayerModel.shadowCastingMode;
                    DefaultScale = Instance.playerCollider.transform.localScale;
                    TriggerAwake = false;
                }

                if (Instance == null)
                {
                    return;
                }

                if (PropHuntBase.OriginalTransform == null)
                {
                    PropHuntBase.OriginalTransform = CopyTransform(Instance.gameplayCamera.transform, "LPHThirdPerson_Original Camera Position");
                    PropHuntBase.Camera = CopyTransform(Instance.gameplayCamera.transform, "LPHThirdPerson_Camera Position");
                }

                // Set this for any method that needs patching inbetween the start of Update and the end of LateUpdate
                PropHuntBase.Camera.position = Instance.gameplayCamera.transform.position;
                PropHuntBase.Camera.rotation = Instance.gameplayCamera.transform.rotation;

                // Reset the camera before the PlayerController update method, so nothing gets too messed up
                Instance.gameplayCamera.transform.rotation = PropHuntBase.OriginalTransform.transform.rotation;
                Instance.gameplayCamera.transform.position = PropHuntBase.OriginalTransform.transform.position;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        private static void PatchCamera(ref bool ___isCameraDisabled, ref bool ___isPlayerControlled, ref MeshFilter ___playerBadgeMesh, ref MeshRenderer ___playerBetaBadgeMesh)
        {
            ___playerBadgeMesh.gameObject.SetActive(false);
            ((Renderer)___playerBetaBadgeMesh).enabled = false;
            ___playerBetaBadgeMesh.gameObject.SetActive(false);
            if (Instance == null || Instance.playerClientId != StartOfRound.Instance.localPlayerController.playerClientId) { return; }
            var originalTransform = PropHuntBase.OriginalTransform;

            if (!___isPlayerControlled || ___isCameraDisabled || originalTransform == null)
            {
                return;
            }

            var gameplayCamera = Instance.gameplayCamera;

            // Set the placeholder rotation to match the updated gameplayCamera rotation
            originalTransform.transform.rotation = gameplayCamera.transform.rotation;

            if (!InThirdPerson || Instance.inTerminalMenu)
            {
                return;
            }

            var offset = originalTransform.transform.right * PropHuntBase.Offset.Value.x +
                originalTransform.transform.up * PropHuntBase.Offset.Value.y;
            var lineStart = originalTransform.transform.position;
            var lineEnd = originalTransform.transform.position + offset + originalTransform.transform.forward * PropHuntBase.Offset.Value.z;

            // Check for camera collisions
            if (Physics.Linecast(lineStart, lineEnd, out RaycastHit hit, StartOfRound.Instance.collidersAndRoomMask) && !IgnoreCollision(hit.transform.name))
            {
                offset += originalTransform.transform.forward * -Mathf.Max(hit.distance, 0);
            }
            else
            {
                offset += originalTransform.transform.forward * -2f;
            }

            // Set the camera offset
            gameplayCamera.transform.position = originalTransform.transform.position + offset;

            // Don't fix interact ray if on a ladder
            if (Instance.isClimbingLadder)
            {
                return;
            }

            // Fix the interact ray 
            var methodInfo = typeof(PlayerControllerB).GetMethod(
                "SetHoverTipAndCurrentInteractTrigger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            methodInfo.Invoke(Instance, new object[] { });
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetFaceUnderwaterFilters")]
        private static void UnderwaterPrepatch()
        {
            if (Instance == null || PropHuntBase.Camera == null)
            {
                return;
            }

            Instance.gameplayCamera.transform.position = PropHuntBase.Camera.transform.position;
            Instance.gameplayCamera.transform.rotation = PropHuntBase.Camera.transform.rotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetFaceUnderwaterFilters")]
        private static void UnderwaterPostpatch()
        {
            if (Instance == null || PropHuntBase.OriginalTransform == null)
            {
                return;
            }

            Instance.gameplayCamera.transform.position = PropHuntBase.OriginalTransform.transform.position;
            Instance.gameplayCamera.transform.rotation = PropHuntBase.OriginalTransform.transform.rotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.SpawnPlayerAnimation))]
        private static void SpawnPlayerPostpatch(ref bool ___isPlayerControlled)
        {
            if (Instance == null || !___isPlayerControlled)
            {
                return;
            }

            if (!InThirdPerson)
            {
                return;
            }

            OnEnable();
        }

        private static Transform CopyTransform(Transform copyTransform, string gameObjectName)
        {
            var newTransform = new GameObject(gameObjectName).transform;
            newTransform.position = copyTransform.position;
            newTransform.rotation = copyTransform.rotation;
            newTransform.parent = copyTransform.parent;

            return newTransform;
        }

        private static bool IgnoreCollision(string name)
        {
            return IgnoreGameObjectPrefixes.Any(prefix => name.StartsWith(prefix));
        }

        internal static Sprite CreateCrosshairSprite()
        {
            string filename = @"Lethal_Prop_Hunt.crosshair.png";
            var assembly = Assembly.GetExecutingAssembly();
            var crosshairData = assembly.GetManifestResourceStream(filename);
            var tex = new Texture2D(2, 2);

            using (var stream = new MemoryStream())
            {
                crosshairData.CopyTo(stream);
                tex.LoadImage(stream.ToArray());

            }
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(tex.width / 2, tex.height / 2)
            );
        }

        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        private static void PatchUpdate(
            ref PlayerControllerB __instance,
            ref bool ___isCameraDisabled
        )
        {
            if (!__instance.isPlayerControlled || ___isCameraDisabled || !InThirdPerson)
            {
                return;
            }

            if (!PropHuntBase.ShowCursor.Value || __instance.inTerminalMenu || __instance.cursorIcon.enabled)
            {
                return;
            }

            __instance.cursorIcon.enabled = true;
            __instance.cursorIcon.sprite = PropHuntBase.Instance.CrosshairSprite;
        }

        [HarmonyPrefix]
        [HarmonyPatch("BeginGrabObject")]
        public static bool BeginGrabObjectPatch(PlayerControllerB __instance, ref int ___interactableObjectsMask)
        {
            if (!LPHRoundManager.Instance.IsRunning) { return true; }
            Ray interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            RaycastHit hit;
            if (!Physics.Raycast(interactRay, out hit, __instance.grabDistance, ___interactableObjectsMask) || hit.collider.gameObject.layer == 8 || !(hit.collider.tag == "PhysicsProp"))
            {
                return true;
            }
            GrabbableObject currentlyGrabbingObject = hit.collider.transform.gameObject.GetComponent<GrabbableObject>();
            PropHuntBase.mls.LogDebug("Checking object " + currentlyGrabbingObject);
            if (currentlyGrabbingObject == null || __instance.inSpecialInteractAnimation)
            {
                return true;
            }
            NetworkObject networkObject = currentlyGrabbingObject.NetworkObject;
            if (networkObject == null || !networkObject.IsSpawned)
            {
                return true;
            }
            if (LPHRoundManager.Instance.IsPlayerProp(__instance))
            {
                PropHuntBase.mls.LogDebug("I am becoming a prop on layer " + currentlyGrabbingObject.gameObject.layer);
                if (currentlyGrabbingObject.itemProperties != null)
                {
                    if (ConfigManager.ForcePropWeight.Value)
                    {
                        __instance.carryWeight += Mathf.Clamp(currentlyGrabbingObject.itemProperties.weight - 1f, 0f, 10f);
                    }
                    PropHuntBase.mls.LogDebug("Prop is " + Mathf.RoundToInt(Mathf.Clamp(currentlyGrabbingObject.itemProperties.weight - 1f, 0f, 100f) * 105f) + "lbs");
                }
                if (LPHRoundManager.Props.ContainsKey(__instance.playerClientId))
                {
                    LPHNetworkHandler.Instance.SwapPropOwnershipServerRpc(__instance.playerClientId, LPHRoundManager.Props[__instance.playerClientId].NetworkObjectId, currentlyGrabbingObject.NetworkObjectId);
                }
                else
                {
                    LPHNetworkHandler.Instance.SyncPropOwnershipServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, currentlyGrabbingObject.NetworkObjectId, true);
                }
                return false;
            }
            else
            {
                bool found = false;
                foreach (GrabbableObject objs in LPHRoundManager.Props.Values)
                {
                    if (objs.NetworkObjectId == currentlyGrabbingObject.NetworkObjectId)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) //Cancel grab if prop is player
                {
                    return false;
                }
            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch("PlayFootstepSound")]
        public static bool PlayFootstepSoundPatch(PlayerControllerB __instance)
        {
            return !LPHRoundManager.Instance.IsPlayerProp(__instance) || !LPHRoundManager.Props.ContainsKey(__instance.playerClientId);
        }

        [HarmonyPrefix]
        [HarmonyPatch("PlayFootstepLocal")]
        public static bool PlayFootstepLocalPatch(PlayerControllerB __instance)
        {
            return !LPHRoundManager.Instance.IsPlayerProp(__instance) || !LPHRoundManager.Props.ContainsKey(__instance.playerClientId);
        }

        [HarmonyPrefix]
        [HarmonyPatch("PlayFootstepServer")]
        public static bool PlayFootstepServerPatch(PlayerControllerB __instance)
        {
            return !LPHRoundManager.Instance.IsPlayerProp(__instance) || !LPHRoundManager.Props.ContainsKey(__instance.playerClientId);
        }


        [HarmonyPrefix]
        [HarmonyPatch("Jump_performed")]
        private static bool Jump_performedPatch(InputAction.CallbackContext context, PlayerControllerB __instance, ref bool ___isJumping, ref float ___playerSlidingTimer, ref Coroutine ___jumpCoroutine)
        {
            //overridden from source to disable jump sfx
            var methodInfo = typeof(PlayerControllerB).GetMethod(
                "IsPlayerNearGround",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            bool isPlayerNearGround = (bool)methodInfo.Invoke(__instance, new object[] { });
            if (!__instance.quickMenuManager.isMenuOpen && ((__instance.IsOwner && __instance.isPlayerControlled && (!__instance.IsServer || __instance.isHostPlayerObject)) || __instance.isTestingPlayer) && !__instance.inSpecialInteractAnimation && !__instance.isTypingChat && (__instance.isMovementHindered <= 0 || __instance.isUnderwater) && !__instance.isExhausted && (__instance.thisController.isGrounded || (!___isJumping && isPlayerNearGround)) && !___isJumping && (!__instance.isPlayerSliding || ___playerSlidingTimer > 2.5f) && !__instance.isCrouching)
            {
                ___playerSlidingTimer = 0f;
                ___isJumping = true;
                __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter - 0.08f, 0f, 1f);
                if (!LPHRoundManager.Instance.IsPlayerProp(__instance) || !LPHRoundManager.Props.ContainsKey(__instance.playerClientId))
                {
                    __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);
                }
                if (___jumpCoroutine != null)
                {
                    __instance.StopCoroutine(___jumpCoroutine);
                }
                methodInfo = typeof(PlayerControllerB).GetMethod(
                    "PlayerJump",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
                ___jumpCoroutine = __instance.StartCoroutine((System.Collections.IEnumerator)methodInfo.Invoke(__instance, new object[] { }));
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("PlayerHitGroundEffects")]
        private static bool PlayerHitGroundEffectsPatch(PlayerControllerB __instance)
        {
            //Copied from source to remove land on ground audio
            __instance.GetCurrentMaterialStandingOn();
            if (__instance.fallValue < -9f)
            {
                if (__instance.fallValue < -16f)
                {
                    if (LPHRoundManager.Props.ContainsKey(__instance.playerClientId))
                    {
                        GrabbableObject prop = LPHRoundManager.Props[__instance.playerClientId];
                        if (prop.itemProperties != null && prop.itemProperties.dropSFX != null)
                        {
                            __instance.movementAudio.PlayOneShot(prop.itemProperties.dropSFX); //Play item sounds instead
                        }
                    }
                    else {
                        __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
                    }
                    WalkieTalkie.TransmitOneShotAudio(__instance.movementAudio, StartOfRound.Instance.playerHitGroundHard);
                }
                else if (__instance.fallValue < -2f)
                {
                    if (LPHRoundManager.Props.ContainsKey(__instance.playerClientId))
                    {
                        GrabbableObject prop = LPHRoundManager.Props[__instance.playerClientId];
                        if (prop.itemProperties != null && prop.itemProperties.dropSFX != null)
                        {
                            __instance.movementAudio.PlayOneShot(prop.itemProperties.dropSFX);
                        }
                    }
                    else
                    {
                        __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
                    }
                }
                __instance.LandFromJumpServerRpc(__instance.fallValue < -16f);
            }
            if (__instance.takingFallDamage && !__instance.jetpackControls && !__instance.disablingJetpackControls && !__instance.isSpeedCheating)
            {
                Debug.Log($"Fall damage: {__instance.fallValueUncapped}");
                if (__instance.fallValueUncapped < -48.5f)
                {
                    __instance.DamagePlayer(100, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
                else if (__instance.fallValueUncapped < -45f)
                {
                    __instance.DamagePlayer(80, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
                else if (__instance.fallValueUncapped < -40f)
                {
                    __instance.DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
                else
                {
                    __instance.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
            }
            if (__instance.fallValue < -16f)
            {
                RoundManager.Instance.PlayAudibleNoise(__instance.transform.position, 7f);
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DamagePlayer")]
        public static void DamagePlayerPatch(ref int damageNumber, bool hasDamageSFX, bool callRPC, CauseOfDeath causeOfDeath, int deathAnimation, bool fallDamage, Vector3 force, PlayerControllerB __instance)
        {
            if (LPHRoundManager.Instance.IsPlayerProp(__instance) && LPHRoundManager.Props.ContainsKey(__instance.playerClientId))
            {
                GrabbableObject prop = LPHRoundManager.Props[__instance.playerClientId];
                if (prop != null && prop.itemProperties != null && damageNumber > 0) //Reduce damage when in prop
                {
                    __instance.DropBlood(-__instance.transform.forward, true, false);
                    __instance.DropBlood(-__instance.transform.forward, true, false);
                    __instance.DropBlood(-__instance.transform.forward, true, false);
                    __instance.DropBlood(-__instance.transform.forward, true, false);
                    int weight = Mathf.RoundToInt(Mathf.Clamp(prop.itemProperties.weight - 1f, 0f, 100f) * 105f);
                    int factor = Mathf.CeilToInt(weight / ConfigManager.PropDamageScale.Value);
                    if (factor > 0 && weight > ConfigManager.PropDamageScale.Value && ConfigManager.PropDamageScale.Value != 1) //Scaling based on weight, higher the weight, the less health you have
                    {
                        PropHuntBase.mls.LogDebug("Prop is of size " + weight + " factor set to " + factor + " damaged reduced from " + damageNumber + " to " + (damageNumber / factor));
                        damageNumber = Mathf.RoundToInt(damageNumber / factor);
                    }
                }
            }
        }

        [HarmonyPatch("RandomizeBloodRotationAndScale")]
        [HarmonyPostfix]
        public static void RandomizeBloodScale(ref Transform blood, PlayerControllerB __instance)
        {
            Transform obj = blood;
            obj.localScale *= 4f;
            blood.position += new Vector3((float)UnityEngine.Random.Range(-1, 1) * 4f, 0.55f, (float)UnityEngine.Random.Range(-1, 1) * 4f);
        }
    }
}
