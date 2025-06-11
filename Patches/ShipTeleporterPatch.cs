using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Diagnostics;
using Unity.Multiplayer.Tools.MetricTypes;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(ShipTeleporter))]
    internal class ShipTeleporterPatch
    {
        [HarmonyPatch(nameof(ShipTeleporter.beamUpPlayer))]
        [HarmonyPostfix]
        static void BeamUpPlayerPostfix(ShipTeleporter __instance)
        {
            try
            {
                PlayerControllerB playerToBeamUp = StartOfRound.Instance.mapScreen.targetedPlayer;
                if (playerToBeamUp == null)
                    return;
                if (playerToBeamUp.deadBody == null)
                {
                    Leader leader = playerToBeamUp.GetComponent<Leader>();
                    __instance.StartCoroutine(WaitForBeamUp(leader));
                }
            }
            catch (System.Exception ex)
            {
                LethalMin.Logger.LogError($"Exception in ShipTeleporterPatch.BeamUpPlayerPostfix: {ex}");
            }
        }

        static IEnumerator WaitForBeamUp(Leader leaderToBeamUp)
        {
            yield return new WaitForSeconds(3.5f);
            leaderToBeamUp.SetPikminToEntrance(leaderToBeamUp.Controller.isInsideFactory);
        }
    }
}