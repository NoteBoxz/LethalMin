using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using System.Linq;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(RadMechAI))]
    internal class RadMechAIPatch
    {
        static Dictionary<RadMechAI, List<PikminAI>> instanceData = new Dictionary<RadMechAI, List<PikminAI>>();
        [HarmonyPatch("AttemptGrabIfClose")]
        [HarmonyPrefix]
        public static void AttemptGrabIfClosePrefix(RadMechAI __instance)
        {
            if (!LethalMin.LethalMech) { return; }
            if (!__instance.IsServer || __instance.inSpecialAnimation || __instance.currentBehaviourStateIndex == 2)
            {
                return;
            }
            if (!__instance.waitingToAttemptGrab && !__instance.attemptingGrab && __instance.attemptGrabTimer < 0f)
            {
                PikminAI[] pikmin = LethalMin.FindNearestPikmin(__instance.transform.position, 5.2f, LethalMin.MechBurnLimmit).ToArray();
                List<PikminAI> TargetPikmin = new List<PikminAI>();
                if (pikmin.Length == 0) { return; }
                for (int i = 0; i < pikmin.Length; i++)
                {
                    if (pikmin[i] == null || pikmin[i].isEnemyDead || pikmin[i].IsDying
                    || pikmin[i].FinnaBeDed || pikmin[i].KncockedBack) { continue; }

                    TargetPikmin.Add(pikmin[i]);
                }
                if (TargetPikmin.Count > 0)
                {
                    LethalMin.Logger.LogInfo("Attempting to grab " + TargetPikmin.Count + " Pikmin");
                    __instance.waitingToAttemptGrab = true;
                    __instance.disableWalking = true;
                }
                else
                {
                    __instance.attemptGrabTimer = 0.4f;
                }

            }
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(RadMechAI __instance)
        {
            if (!LethalMin.LethalMech) { return; }
            //LethalMin.Logger.LogInfo("Attempting to grab Pikmin = " + __instance.attemptingGrab);
            //LethalMin.Logger.LogInfo("isinanim = " + __instance.inSpecialAnimation);
            if (__instance.timeSinceGrabbingPlayer > 1f && __instance.attemptingGrab && !__instance.inSpecialAnimation)
            {
                PikminAI[] pikmin = LethalMin.FindNearestPikmin(__instance.transform.position, 5.2f, LethalMin.MechBurnLimmit).ToArray();
                List<NetworkObjectReference> pikminList = new List<NetworkObjectReference>();
                foreach (var item in pikmin)
                {
                    if (item == null || item.isEnemyDead || item.IsDying
                    || item.FinnaBeDed || item.KncockedBack) { continue; }
                    if (!Physics.Linecast(__instance.centerPosition.position, item.transform.position + Vector3.up * 0.6f,
                    StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    {
                        pikminList.Add(new NetworkObjectReference(item.NetworkObject));
                    }
                }
                if (pikminList.Count > 0)
                {
                    __instance.timeSinceGrabbingPlayer = 0f;
                    GrabPikminServerRpc(new NetworkObjectReference(__instance.NetworkObject), pikminList.ToArray());
                }
            }
        }

        [ServerRpc]
        public static void GrabPikminServerRpc(NetworkObjectReference __instanceRef, NetworkObjectReference[] pikmins)
        {
            // Get the RadMechAI instance from the network object reference
            RadMechAI radMechAI = ;
            if (__instanceRef.TryGet(out NetworkObject __instance))
            {
                radMechAI = __instance.GetComponent<RadMechAI>();
            }
            else
            {
                return;
            }
            if (!radMechAI.attemptingGrab || radMechAI.inTorchPlayerAnimation || radMechAI.inSpecialAnimationWithPlayer || radMechAI.inSpecialAnimation)
            {
                radMechAI.inSpecialAnimation = false;
                radMechAI.FinishAttemptGrab();
                return;
            }

            LethalMin.Logger.LogInfo("Grabed " + pikmins.Length + " Pikmin on server");
            radMechAI.inTorchPlayerAnimation = true;
            radMechAI.inSpecialAnimation = true;
            radMechAI.agent.enabled = false;
            radMechAI.attemptingGrab = false;
            int enemyYRot = (int)radMechAI.transform.eulerAngles.y;
            if (Physics.Raycast(radMechAI.centerPosition.position, radMechAI.centerPosition.forward, out var _, 6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                enemyYRot = (int)RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(radMechAI.centerPosition.position, 20f, 5);
            }
            GrabPikminClientRpc(__instanceRef, pikmins, __instance.transform.position, enemyYRot);
        }

        [ClientRpc]
        public static void GrabPikminClientRpc(NetworkObjectReference __instanceRef, NetworkObjectReference[] pikmins, Vector3 enemyPosition, int enemyYRot)
        {
            // Get the RadMechAI instance from the network object reference
            LethalMin.Logger.LogInfo("Grabed " + pikmins.ToArray().Length + " Pikmin on client");
            List<PikminAI> pikminAIs = new List<PikminAI>();
            RadMechAI radMechAI = ;
            if (__instanceRef.TryGet(out NetworkObject __instance))
            {
                radMechAI = __instance.GetComponent<RadMechAI>();
            }
            else
            {
                LethalMin.Logger.LogError("Failed to get RadMechAI instance from network object reference");
                foreach (var mech in GameObject.FindObjectsOfType<RadMechAI>())
                {

                    mech.inSpecialAnimation = false;
                    mech.FinishAttemptGrab();
                }
                return;
            }
            foreach (var pref in pikmins)
            {
                if (pref.TryGet(out NetworkObject pikminObject))
                {
                    if (pikminObject != null)
                    {
                        PikminAI script = pikminObject.GetComponent<PikminAI>();
                        if (script != null)
                        {
                            pikminAIs.Add(script);
                        }
                    }
                }
                else
                {
                    LethalMin.Logger.LogWarning("Failed to get PikminAI instance from network object reference");
                }
            }
            LethalMin.Logger.LogInfo("Grabed " + pikminAIs.ToArray().Length + " Pikmin on client 4 real");

            BeginTorchPikmin(radMechAI, pikminAIs.ToArray(), enemyPosition, enemyYRot);
        }

        private static void BeginTorchPikmin(RadMechAI __instance, PikminAI[] PikminsBeingTorched, Vector3 enemyPosition, int enemyYRot)
        {
            foreach (var pikmin in PikminsBeingTorched)
            {
                pikmin.SnapPikminToPosition(__instance.holdPlayerPoint, false, false, 0, true);
            }
            if (__instance.torchPlayerCoroutine != null)
            {
                __instance.StopCoroutine(__instance.torchPlayerCoroutine);
            }
            __instance.torchPlayerCoroutine = __instance.StartCoroutine(TorchPikminAnimation(__instance, PikminsBeingTorched, enemyPosition, enemyYRot));
        }

        private static IEnumerator TorchPikminAnimation(RadMechAI __instance, PikminAI[] PikminsBeingTorched, Vector3 enemyPosition, int enemyYRot)
        {
            __instance.creatureAnimator.SetBool("AttemptingGrab", value: true);
            __instance.creatureAnimator.SetBool("GrabSuccessful", value: true);
            __instance.creatureAnimator.SetBool("GrabUnsuccessful", value: false);
            float startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => __instance.blowtorchActivated || Time.realtimeSinceStartup - startTime > 6f);
            startTime = Time.realtimeSinceStartup;
            float totalDuration = 6f; // Total duration of the while loop
            float interval = totalDuration / PikminsBeingTorched.Length; // Interval for each Pikmin
            List<PikminAI> pikminList = PikminsBeingTorched.ToList();

            while (__instance.blowtorchActivated && Time.realtimeSinceStartup - startTime < 6 && pikminList.Count > 0)
            {
                for (int i = 0; i < pikminList.Count; i++)
                {
                    yield return new WaitForSeconds(0.1f);
                    LethalMin.Logger.LogInfo("Torching Pikmin " + i);
                    if (pikminList[i] == null || pikminList[i].isEnemyDead || pikminList[i].IsDying
                    || pikminList[i].FinnaBeDed || pikminList[i].SnapToPos == null)
                    {
                        pikminList.RemoveAt(i);
                        continue;
                    }
                    if (!LethalMin.IsPikminResistantToHazard(pikminList[i].PminType, HazardType.Fire))
                    {
                        pikminList[i].KillEnemyOnOwnerClient(true);
                        pikminList.RemoveAt(i);
                    }
                }
            }
            for (int i = 0; i < pikminList.Count; i++)
            {
                if (!LethalMin.IsPikminResistantToHazard(pikminList[i].PminType, HazardType.Fire))
                {
                    pikminList[i].DeathBuffer = true;
                    pikminList[i].KillEnemyOnOwnerClient();
                    pikminList.RemoveAt(i);
                }
            }
            //yield return new WaitForSeconds(1.5f);
            CancelTorchPikminAnimation(__instance, pikminList.ToArray());
            if (__instance.IsServer)
            {
                __instance.inTorchPlayerAnimation = false;
                __instance.inSpecialAnimationWithPlayer = null;
                __instance.inSpecialAnimation = false;
                __instance.agent.enabled = true;
            }
        }

        public static void CancelTorchPikminAnimation(RadMechAI __instance, PikminAI[] PikminsBeingTorched)
        {
            __instance.inTorchPlayerAnimation = false;
            __instance.inSpecialAnimation = false;
            __instance.disableWalking = false;
            __instance.attemptGrabTimer = 5f;
            if (__instance.IsServer)
            {
                __instance.enabled = true;
            }
            __instance.creatureAnimator.SetBool("GrabSuccessful", value: false);
            __instance.creatureAnimator.SetBool("AttemptingGrab", value: false);
            __instance.creatureAnimator.SetBool("GrabUnsuccessful", value: false);
            if (PikminsBeingTorched != null && PikminsBeingTorched.Length > 0)
            {
                foreach (var pikmin in PikminsBeingTorched)
                {
                    pikmin.CannotEscape = false;
                    pikmin.UnSnapPikmin(true,false);
                }
            }
            if (__instance.blowtorchActivated)
            {
                __instance.DisableBlowtorch();
            }
            if (__instance.torchPlayerCoroutine != null)
            {
                __instance.StopCoroutine(__instance.torchPlayerCoroutine);
            }
        }

        public static NetworkObject GetNetworkObjectById(ulong networkObjectId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                return networkObject;
            }
            return null;
        }

    }
}