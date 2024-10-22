using UnityEngine;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace LethalMin
{
    public class EaterBehavior : NetworkBehaviour
    {

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        public CaveDwellerAI __instance;

        public PikminType FavoritePikminType;
        public bool IsHeldByPikmin;
        public PikminAI[] CarriedByPikmins = new PikminAI[0];
        public void IntervalledUpdate()
        {
            if (FavoritePikminType == null)
            {
                PlayerControllerB[] allPlayersInLineOfSight = __instance.GetAllPlayersInLineOfSight(120f, 30, __instance.BabyEye, 3f);
                if (allPlayersInLineOfSight != null)
                {
                    for (int j = 0; j < allPlayersInLineOfSight.Length; j++)
                    {
                        Debug.DrawLine(__instance.BabyEye.transform.position, allPlayersInLineOfSight[j].gameplayCamera.transform.position, Color.blue, 1f);
                        BabyPlayerMemory babyMemoryOfPlayer2 = __instance.GetBabyMemoryOfPlayer(allPlayersInLineOfSight[j]);
                        if (babyMemoryOfPlayer2 == null)
                        {
                            continue;
                        }
                        if (Time.realtimeSinceStartup - babyMemoryOfPlayer2.timeAtLastSighting > 4f)
                        {
                            babyMemoryOfPlayer2.timeAtLastNoticing = Time.realtimeSinceStartup;
                        }
                        babyMemoryOfPlayer2.timeAtLastSighting = Time.realtimeSinceStartup;
                        if (babyMemoryOfPlayer2.orderSeen == -1)
                        {
                            FavoritePikminType = allPlayersInLineOfSight[j].GetComponentInChildren<LeaderManager>().GetCurrentSelectedType();
                            LethalMin.Logger.LogInfo($"Favorite Pikmin Type: {FavoritePikminType.GetName()}");
                        }
                    }
                }
            }
            else if (LethalMin.CalmableManeater)
            {
                if (!LethalMin.NonRasistManEater)
                {
                    if (IsHeldByPikmin && CarriedByPikmins.Length > 0 && GetMajorityType(CarriedByPikmins) == FavoritePikminType)
                    {
                        __instance.rockingBaby = 1;
                    }
                }
                else
                {
                    if (IsHeldByPikmin && CarriedByPikmins.Length > 0)
                    {
                        __instance.rockingBaby = 1;
                    }
                }
            }
        }
        public void CarriedByPikmin(PikminAI[] pikmins)
        {
            // if (base.IsOwner)
            // {
            //     HUDManager.Instance.ClearControlTips();
            //     __instance.propScript.SetControlTipsForItem();
            // }
            __instance.propScript.EnableItemMeshes(enable: true);
            __instance.propScript.isPocketed = false;
            if (!__instance.propScript.hasBeenHeld)
            {
                __instance.propScript.hasBeenHeld = true;
                // if (!__instance.propScript.isInShipRoom && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
                // {
                //     RoundManager.Instance.valueOfFoundScrapItems += __instance.propScript.scrapValue;
                // }
            }
            PikminAI majorityPikmin = FindPikminOfType(GetMajorityType(pikmins), pikmins);
            PlayerControllerB PCB = null!;
            if (majorityPikmin != null)
            {
                if (majorityPikmin.previousLeader != null)
                {
                    PCB = majorityPikmin.previousLeader.Controller;
                }
                else if (majorityPikmin.currentLeader != null)
                {
                    PCB = majorityPikmin.currentLeader.Controller;
                }
                else if (pikmins[0].previousLeader != null)
                {
                    PCB = pikmins[0].previousLeader.Controller;
                }
                else if (pikmins[0].currentLeader != null)
                {
                    PCB = pikmins[0].currentLeader.Controller;
                }
                if (PCB == null)
                {
                    LethalMin.Logger.LogWarning("No player controller found for pikmin");
                    PCB = StartOfRound.Instance.localPlayerController;
                }
                __instance.currentOwnershipOnThisClient = (int)PCB.playerClientId;
                __instance.inSpecialAnimation = true;
                __instance.agent.enabled = false;
                __instance.holdingBaby = true;
                if (__instance.dropBabyCoroutine != null)
                {
                    StopCoroutine(__instance.dropBabyCoroutine);
                }
                if (__instance.IsServer && __instance.babyState == BabyState.RolledOver)
                {
                    __instance.babyState = BabyState.Roaming;
                }
                __instance.SetRolledOverLocalClient(setRolled: false, scared: false);
                __instance.playerHolding = PCB;
                IsHeldByPikmin = true;
            }
            __instance.currentOwnershipOnThisClient = (int)PCB.playerClientId;
            __instance.inSpecialAnimation = true;
            __instance.agent.enabled = false;
            __instance.holdingBaby = true;
            if (__instance.dropBabyCoroutine != null)
            {
                StopCoroutine(__instance.dropBabyCoroutine);
            }
            if (__instance.IsServer && __instance.babyState == BabyState.RolledOver)
            {
                __instance.babyState = BabyState.Roaming;
            }
            __instance.SetRolledOverLocalClient(setRolled: false, scared: false);
            __instance.playerHolding = PCB;
            CarriedByPikmins = pikmins;
            IsHeldByPikmin = true;
        }
        public void DroppedByPikmin(PikminAI FirstMin)
        {
            __instance.holdingBaby = false;
            __instance.rockingBaby = 0;
            __instance.playerHolding = null;
            Debug.Log("Drop baby A");
            if (__instance.currentBehaviourStateIndex != 0)
            {
                return;
            }
            if (base.IsOwner)
            {
                Debug.Log($"Set ownership of creature. Currentownershiponthisclient: {__instance.currentOwnershipOnThisClient}");
                __instance.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            }
            __instance.serverPosition = base.transform.position;
            bool flag = true;
            Vector3 itemFloorPosition = __instance.propScript.GetItemFloorPosition(FirstMin.HoldPos.transform.position + Vector3.up * 0.5f);
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(itemFloorPosition, default(NavMeshHit), 10f);
            Debug.Log($"Drop on position: {navMeshPosition}");
            Debug.DrawRay(__instance.propScript.startFallingPosition, Vector3.up * 1f, Color.white, 10f);
            Debug.DrawRay(navMeshPosition, Vector3.up * 0.75f, Color.red, 10f);
            if (!RoundManager.Instance.GotNavMeshPositionResult || __instance.DebugEnemy)
            {
                flag = false;
                itemFloorPosition = __instance.propScript.startFallingPosition;
                Debug.Log($"Drop Baby C; {__instance.propScript.startFallingPosition}");
                if (__instance.propScript.transform.parent != null)
                {
                    itemFloorPosition = __instance.propScript.transform.parent.TransformPoint(__instance.propScript.startFallingPosition);
                }
                Debug.Log($"Drop Baby C global; {__instance.propScript.startFallingPosition}");
                Transform transform = __instance.ChooseClosestNodeToPositionNoPathCheck(itemFloorPosition);
                navMeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.transform.position);
                Debug.Log($"Got nav mesh position : {RoundManager.Instance.GotNavMeshPositionResult}; {navMeshPosition}; dist: {Vector3.Distance(base.transform.position, transform.transform.position)}");
                Debug.DrawRay(navMeshPosition, Vector3.up * 1.2f, Color.magenta, 10f);
            }
            Debug.Log("Drop baby D");
            if (__instance.propScript.transform.parent == null)
            {
                __instance.propScript.targetFloorPosition = navMeshPosition;
            }
            else
            {
                __instance.propScript.targetFloorPosition = __instance.propScript.transform.parent.InverseTransformPoint(navMeshPosition);
            }
            Debug.DrawRay(__instance.propScript.targetFloorPosition, Vector3.up * 2f, Color.yellow, 5f);
            if (flag)
            {
                if (__instance.dropBabyCoroutine != null)
                {
                    StopCoroutine(__instance.dropBabyCoroutine);
                }
                __instance.dropBabyCoroutine = StartCoroutine(__instance.DropBabyAnimation(navMeshPosition));
            }
            else
            {
                Debug.Log($"Drop baby F; got no floor target; drop pos: {navMeshPosition}");
                base.transform.position = navMeshPosition;
                __instance.inSpecialAnimation = false;
            }
            CarriedByPikmins = new PikminAI[0];
            IsHeldByPikmin = false;
        }

        public PikminType GetMajorityType(PikminAI[] pikmins)
        {
            Dictionary<PikminType, int> counts = new Dictionary<PikminType, int>();
            foreach (var val in LethalMin.RegisteredPikminTypes.Values)
            {
                counts[val] = 0;
            }
            foreach (var pikmin in pikmins)
            {
                if (counts.ContainsKey(pikmin.PminType))
                {
                    counts[pikmin.PminType]++;
                }
                else
                {
                    counts[pikmin.PminType] = 1;
                }
            }

            int maxCount = 0;
            PikminType majorityType = null!;

            foreach (var count in counts)
            {
                if (count.Value > maxCount)
                {
                    maxCount = count.Value;
                    majorityType = count.Key;
                }
            }

            return majorityType;
        }
        public PikminAI FindPikminOfType(PikminType type, PikminAI[] pikmins)
        {
            foreach (var pikmin in pikmins)
            {
                if (pikmin.PminType == type)
                {
                    return pikmin;
                }
            }

            return null!;
        }
        void LateUpdate()
        {
            CheckAndDespawnIfParentDestroyed();
        }
        private void CheckAndDespawnIfParentDestroyed()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                if (transform.parent == null || transform.parent.gameObject == null)
                {
                    // Parent has been destroyed, despawn this NetworkObject
                    if (IsServer)
                        NetworkObject.Despawn();
                    Destroy(gameObject);
                    LethalMin.Logger.LogInfo($"EaterBehavior despawned due to destroyed parent");
                }
            }
        }
    }
}