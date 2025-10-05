using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Utils;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBPatch
    {
        [HarmonyPatch(nameof(PlayerControllerB.Awake))]
        [HarmonyPrefix]
        private static void Awake(PlayerControllerB __instance)
        {
            try
            {
                if (__instance.GetComponent<Leader>() == null) // Only add leader if it doesn't already exist
                    __instance.gameObject.AddComponent<Leader>();
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogFatal($"FAILED TO ADD LEADER SCRIPT FOR {__instance.playerUsername} in PlayerControllerBPatch.Awake: {e}");
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.KillPlayerClientRpc))]
        [HarmonyPrefix]
        private static void KillPlayerClientRpcPrefix(PlayerControllerB __instance, int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation, Vector3 positionOffset)
        {
            if (PikChecks.IsClientRpcPrefixValid(__instance) == false)
            {
                return;
            }

            try
            {
                Leader leaderScript = __instance.GetComponent<Leader>();

                if (leaderScript.IsLeafling)
                {
                    leaderScript.wasLeaflingBeforeDeath = true;
                    leaderScript.IsLeafling = false;

                    GameObject ghost = null!;
                    if (deathAnimation != 4 && deathAnimation != 5)
                    {
                        ghost = GameObject.Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PlayerGhostPrefab.prefab"),
                        __instance.transform.position, Quaternion.identity);
                        __instance.overrideGameOverSpectatePivot = ghost.GetComponent<PlayerGhost>().LookAt;
                        leaderScript.LeaflingGhostInstance = ghost;
                    }
                    // if (leaderScript.LeaflingType != null)
                    // {
                    //     PlayerGhost pg = ghost.GetComponent<PlayerGhost>();
                    //     Renderer renderer = pg.GetComponentInChildren<Renderer>();
                    //     renderer.material.color = leaderScript.LeaflingType.PikminPrimaryColor;
                    //     AudioSource ghostAudio = pg.GetComponentInChildren<AudioSource>();
                    // }
                    leaderScript.LeaflingType = null;

                    if (spawnBody && ghost != null)
                        __instance.StartCoroutine(RemoveBodyAfterDeath(__instance, ghost));
                }
                else
                {
                    leaderScript.wasLeaflingBeforeDeath = false;
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in PlayerControllerBPatch.KillPlayeClientRpcPrefix: {e}");
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPrefix]
        private static void KillPlayerPrefix(PlayerControllerB __instance, Vector3 bodyVelocity, bool spawnBody = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, Vector3 positionOffset = default(Vector3))
        {
            try
            {
                Leader leaderScript = __instance.GetComponent<Leader>();
                if (__instance.AllowPlayerDeath() && __instance.IsOwner)
                {
                    leaderScript.RemoveAllPikminFromSquadOnLocalClient();
                    leaderScript.RemoveAllPikminFromSquadServerRpc();
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in PlayerControllerBPatch.KillPlayerPrefix: {e}");
            }
        }

        private static IEnumerator DeactivateBodyAfterDeath(PlayerControllerB player, GameObject GhostObj)
        {
            yield return new WaitUntil(() => player.deadBody != null && player.deadBody.grabBodyObject != null);

            Vector3 pos2 = StartOfRound.Instance.planetContainer.position;
            Vector3 oob = new Vector3(pos2.x - 500, pos2.y - 500, pos2.z - 500);
            RagdollGrabbableObject ragdollGrabbableObject = player.deadBody.grabBodyObject.GetComponent<RagdollGrabbableObject>();
            ragdollGrabbableObject.targetFloorPosition = oob;
            ragdollGrabbableObject.ragdoll.SetBodyPartsKinematic(true);
            player.deadBody.transform.position = oob;
            foreach (Rigidbody rb in ragdollGrabbableObject.ragdoll.bodyParts)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.position = oob;
                    rb.transform.position = oob;
                }
            }
            player.deadBody.transform.position = oob;
            ragdollGrabbableObject.targetFloorPosition = oob;


            player.deadBody.DeactivateBody(false);
        }

        private static IEnumerator RemoveBodyAfterDeath(PlayerControllerB player, GameObject GhostObj)
        {
            yield return new WaitUntil(() => player.deadBody != null && player.deadBody.grabBodyObject != null);

            LethalMin.Logger.LogDebug($"Removing body for {player.playerUsername} after death.");
            Vector3 pos2 = StartOfRound.Instance.planetContainer.position;
            Vector3 oob = new Vector3(pos2.x - 500, pos2.y - 500, pos2.z - 500);
            RagdollGrabbableObject ragdollGrabbableObject = player.deadBody.grabBodyObject.GetComponent<RagdollGrabbableObject>();
            ragdollGrabbableObject.targetFloorPosition = oob;
            ragdollGrabbableObject.ragdoll.SetBodyPartsKinematic(true);
            player.deadBody.transform.position = oob;
            foreach (Rigidbody rb in ragdollGrabbableObject.ragdoll.bodyParts)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.position = oob;
                    rb.transform.position = oob;
                }
            }

            WaitForSeconds wait = new WaitForSeconds(0.1f);

            while (GhostObj != null)
            {
                if (player.deadBody == null)
                {
                    yield break; // body was removed by another process
                }
                player.deadBody.transform.position = oob;
                ragdollGrabbableObject.targetFloorPosition = oob;
                yield return wait;
            }

            LethalMin.Logger.LogDebug($"Ghost for {player.playerUsername} has been removed, despawning ragdoll.");
            if (ragdollGrabbableObject != null && NetworkManager.Singleton.IsServer)
            {
                ragdollGrabbableObject.NetworkObject.Despawn(true);
            }
            if (player.deadBody != null)
            {
                if (NetworkManager.Singleton.IsServer)
                    GameObject.Destroy(player.deadBody.gameObject);
                player.deadBody = null;
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        private static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
        {
            try
            {
                if (StartOfRound.Instance.localPlayerController == __instance)
                {
                    PikminManager.instance.LocalLeader = __instance.GetComponent<Leader>();
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Error in PlayerControllerBPatch.ConnectClientToPlayerObjectPostfix: {e}");
            }
        }


        // Was used to see if awake was called before the network spawn
        // [HarmonyPatch(typeof(NetworkBehaviour))]
        // [HarmonyPatch(nameof(NetworkBehaviour.OnNetworkSpawn))]
        // [HarmonyPrefix]
        // private static void OnNetworkSpawnPatch(NetworkBehaviour __instance)
        // {
        //     LethalMin.Logger.LogInfo($"{__instance.name}: NetworkSpawnCalled!");
        // }
    }
}
