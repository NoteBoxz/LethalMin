using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        // Reference to the LeaderManager prefab
        private static GameObject leaderManagerPrefab;
        private static AnimationClip danceAnimation;
        private static AnimatorOverrideController overrideController;
        private static bool isCustomAnimationPlaying = false;

        [HarmonyPatch("Start")]
        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        private static void StartOnEnablePostfix(PlayerControllerB __instance)
        {
            SetupDanceAnimation(__instance);
            if (!__instance.IsServer) return; // Only proceed for the local player

            if (leaderManagerPrefab == null)
            {
                leaderManagerPrefab = LethalMin.leaderManagerPrefab;
                if (leaderManagerPrefab == null)
                {
                    LethalMin.Logger.LogError("Failed to load LeaderManager prefab!");
                    return;
                }
            }
            if (__instance.GetComponentInChildren<LeaderManager>() != null)
            {
                LethalMin.Logger.LogInfo($"LeaderManager already exists for player: {__instance.playerUsername}");
                return;
            }
            // Request the server to spawn the LeaderManager
            __instance.StartCoroutine(SpawnLeaderManagerWhenReady(__instance));
        }

        private static void SetupDanceAnimation(PlayerControllerB player)
        {
            // Load the dance animation
            danceAnimation = LethalMin.PluckAnim;

            if (danceAnimation == null)
            {
                LethalMin.Logger.LogError("Failed to load Dance3 animation!");
                return;
            }

            Animator playerAnimator = player.GetComponentInChildren<Animator>();
            if (playerAnimator == null)
            {
                LethalMin.Logger.LogError("Player Animator not found!");
                return;
            }

            // Create the override controller
            overrideController = new AnimatorOverrideController(playerAnimator.runtimeAnimatorController);

            // Override the PullLever animation with the Dance3 animation
            overrideController["PullLever"] = danceAnimation;
            playerAnimator.gameObject.AddComponent<PikerAnimEvents>().controllerB = player;
        }
        public static RuntimeAnimatorController originalController;

        public static void PlayCustomAnimation(PlayerControllerB player, float CustomLengthOffset = 0.5f)
        {
            Animator playerAnimator = player.playerBodyAnimator;
            if (originalController == overrideController && playerAnimator.runtimeAnimatorController == overrideController)
                return;
            if (playerAnimator == null || overrideController == null)
            {
                LethalMin.Logger.LogError("Cannot play custom animation: Animator or OverrideController not set up!");
                return;
            }
            // Store the original controller
            if (originalController != overrideController && playerAnimator.runtimeAnimatorController != overrideController)
                originalController = playerAnimator.runtimeAnimatorController;

            // Apply the override controller
            playerAnimator.runtimeAnimatorController = overrideController;

            // Play the animation
            playerAnimator.Play("PullLever");
            //playerAnimator.SetTrigger("SA_PullLever");

            // Revert back to the original controller after the animation finishes
            player.StartCoroutine(RevertToOriginalController(playerAnimator, originalController, danceAnimation.length + CustomLengthOffset));

            isCustomAnimationPlaying = true;
        }

        public static void SetCustomAnimation(PlayerControllerB player, float CustomLengthOffset = 0.5f)
        {
            if (isCustomAnimationPlaying)
                return;

            Animator playerAnimator = player.playerBodyAnimator;

            if (originalController == overrideController && playerAnimator.runtimeAnimatorController == overrideController)
                return;

            if (playerAnimator == null || overrideController == null)
            {
                LethalMin.Logger.LogError("Cannot play custom animation: Animator or OverrideController not set up!");
                return;
            }

            // Store the original controller
            if (originalController != overrideController && playerAnimator.runtimeAnimatorController != overrideController)
                originalController = playerAnimator.runtimeAnimatorController;

            // Apply the override controller
            playerAnimator.runtimeAnimatorController = overrideController;

            // Revert back to the original controller after the animation finishes
            player.StartCoroutine(RevertToOriginalController(playerAnimator, originalController, danceAnimation.length + CustomLengthOffset));

            isCustomAnimationPlaying = true;
        }

        public static void RevertAnim(Animator animator, RuntimeAnimatorController originalController)
        {
            LethalMin.Logger.LogInfo("Reverted");
            animator.runtimeAnimatorController = originalController;
            isCustomAnimationPlaying = false;
        }

        private static System.Collections.IEnumerator RevertToOriginalController(Animator animator, RuntimeAnimatorController originalController, float delay)
        {
            LethalMin.Logger.LogInfo($"Reverting after {delay}");
            yield return new WaitForSeconds(delay);
            LethalMin.Logger.LogInfo("Reverted");
            animator.runtimeAnimatorController = originalController;
            isCustomAnimationPlaying = false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner) return; // Only proceed for the local player

            if (!isCustomAnimationPlaying && __instance.playerBodyAnimator.runtimeAnimatorController == overrideController)
            {
                LethalMin.Logger.LogInfo("Auto Reverted");
                __instance.playerBodyAnimator.runtimeAnimatorController = originalController;
                isCustomAnimationPlaying = false;
            }
        }

        private static System.Collections.IEnumerator SpawnLeaderManagerWhenReady(PlayerControllerB player)
        {
            // Wait until the player's NetworkObject is spawned
            while (player.NetworkObject == null || !player.NetworkObject.IsSpawned || PikminHUD.pikminHUDInstance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Request the server to spawn the LeaderManager
            if (NetworkManager.Singleton.IsServer)
            {
                GameObject leaderManagerInstance = UnityEngine.Object.Instantiate(leaderManagerPrefab, player.transform.position, Quaternion.identity);
                NetworkObject networkObject = leaderManagerInstance.GetComponent<NetworkObject>();

                if (networkObject != null)
                {
                    try
                    {
                        leaderManagerInstance.gameObject.name = $"LeaderManager_{player.playerUsername}";

                        // Spawn the NetworkObject with ownership set to the player
                        networkObject.SpawnWithOwnership(player.NetworkObject.OwnerClientId);

                        // Set up the LeaderManager component
                        LeaderManager leaderManager = leaderManagerInstance.GetComponent<LeaderManager>();
                        if (leaderManager != null)
                        {
                            leaderManager.Controller = player;
                        }

                        // Parent the LeaderManager to the player
                        if (NetworkManager.Singleton.IsServer)
                            leaderManagerInstance.transform.SetParent(player.transform);

                        leaderManager.Init();
                        PikminHUD.pikminHUDInstance.LeaderScript = leaderManager;
                        LethalMin.Logger.LogInfo($"Spawned and parented LeaderManager for player: {player.playerUsername}");
                    }
                    catch (Exception ex)
                    {
                        LethalMin.Logger.LogInfo($"Failed to spawn LeaderManager for player: {player.playerUsername} Due to : {ex}");
                    }
                }
                else
                {
                    LethalMin.Logger.LogError("LeaderManager prefab is missing NetworkObject component!");
                    UnityEngine.Object.Destroy(leaderManagerInstance);
                }
            }
            else
            {
                LethalMin.Logger.LogInfo($"Requested LeaderManager spawn for player: {player.playerUsername}");
                SpawnLeaderManagerServerRpc(player.NetworkObject.OwnerClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private static void SpawnLeaderManagerServerRpc(ulong ownerClientId)
        {
            PlayerControllerB player = NetworkManager.Singleton.ConnectedClients[ownerClientId].PlayerObject.GetComponent<PlayerControllerB>();
            if (player == null)
            {
                LethalMin.Logger.LogError($"Failed to find PlayerControllerB for client: {ownerClientId}");
                return;
            }

            GameObject leaderManagerInstance = UnityEngine.Object.Instantiate(leaderManagerPrefab, player.transform.position, Quaternion.identity);
            NetworkObject networkObject = leaderManagerInstance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                try
                {
                    // Parent the LeaderManager to the player
                    if (NetworkManager.Singleton.IsServer)
                        leaderManagerInstance.transform.SetParent(player.transform);

                    // Spawn the NetworkObject with ownership set to the player
                    networkObject.SpawnWithOwnership(ownerClientId);

                    // Set up the LeaderManager component
                    LeaderManager leaderManager = leaderManagerInstance.GetComponent<LeaderManager>();
                    if (leaderManager != null)
                    {
                        leaderManager.Controller = player;
                    }

                    leaderManager.Init();
                    PikminHUD.pikminHUDInstance.LeaderScript = leaderManager;
                    LethalMin.Logger.LogInfo($"Spawned and parented LeaderManager for client: {ownerClientId}");
                }
                catch (Exception ex)
                {
                    LethalMin.Logger.LogInfo($"Failed to spawn LeaderManager for client({ownerClientId}) Due to : {ex}");
                }
            }
            else
            {
                LethalMin.Logger.LogError("LeaderManager prefab is missing NetworkObject component!");
                UnityEngine.Object.Destroy(leaderManagerInstance);
            }
        }

        private static InputAction danceAction, actionD;
        private static void SetupCustomInput(PlayerControllerB __instance)
        {
            // Create a new input action for dancing
            danceAction = new InputAction("Dance", InputActionType.Button);

            // Bind the G key to the dance action
            danceAction.AddBinding("<Keyboard>/g");

            // Set up the callback for when the dance action is performed
            danceAction.performed += ctx => PlayCustomAnimation(__instance);

            // Enable the action
            danceAction.Enable();

            LethalMin.Logger.LogInfo("Custom dance input set up");
        }
        private static void AddCustomAnimationToPlayer(PlayerControllerB __instance)
        {

        }

        public static bool AnimationClipExists(Animator animator, string clipName)
        {
            if (animator == null || string.IsNullOrEmpty(clipName))
                return false;

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
                return false;

            return controller.animationClips.Any(clip => clip.name == clipName);
        }


        [HarmonyPatch("KillPlayerServerRpc")]
        [HarmonyPrefix]
        private static void KillAndRemovePrefix(PlayerControllerB __instance)
        {
            __instance.GetComponentInChildren<LeaderManager>()?.RemoveAllPikminServerRpc();
        }
    }
}