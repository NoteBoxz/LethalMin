using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using LethalMin;
using GameNetcodeStuff;
namespace LethalMin.Patches
{
[HarmonyPatch(typeof(ShipTeleporter))]
public class ShipTeleporterPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("TeleportPlayerOutServerRpc")]
    static void AfterTeleportPlayerOut(int playerObj, Vector3 teleportPos)
    {
        TeleportPikminToPlayer(playerObj, teleportPos);
    }

    [HarmonyPostfix]
    [HarmonyPatch("TeleportPlayerOutServerRpc")]
    static void AfterTeleportPlayerBodyOut(int playerObj, Vector3 teleportPos)
    {
        //TeleportPikminToPlayer(playerObj, teleportPos);
    }

    static void TeleportPikminToPlayer(int playerObj, Vector3 teleportPos)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObj];
        if (player == null) return;

        LeaderManager leaderManager = player.GetComponent<LeaderManager>();

        LeaderManager[] allLeaderManagers = Object.FindObjectsOfType<LeaderManager>();
        foreach (LeaderManager lm in allLeaderManagers)
        {
            if (lm.Controller == player)
            {
                leaderManager = lm;
                break;
            }
        }
        if (leaderManager == null) return;

        List<PikminAI> pikminToTeleport = new List<PikminAI>(leaderManager.followingPikmin);

        foreach (PikminAI pikmin in pikminToTeleport)
        {
            if (pikmin != null)
            {
                // Calculate a slight offset to avoid all pikmin teleporting to the exact same spot
                Vector3 offset = Random.insideUnitSphere * 0.5f;
                offset.y = 0; // Keep them on the same vertical level as the player

                Vector3 pikminTeleportPos = teleportPos + offset;

                // Teleport the pikmin
                pikmin.agent.Warp(pikminTeleportPos);

                // Update pikmin state if necessary
                pikmin.isOutside = !player.isInsideFactory;
            }
        }

        // Log the teleportation for debugging
        LethalMin.Logger.LogInfo($"Teleported {pikminToTeleport.Count} Pikmin to player {playerObj} at position {teleportPos}");
    }
}
}