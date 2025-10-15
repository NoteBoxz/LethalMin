using System.Collections;
using System.Collections.Generic;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class CaveDwellerPikminItem : PikminItem
    {
        public CaveDwellerPikminEnemy caveDwellerPikminEnemy = null!;
        public CaveDwellerAI caveDwellerAI = null!;
        private bool lastLeaderInsideFactory = false;

        public override void Update()
        {
            settings.GrabableToPikmin = LethalMin.CaveDweller_AllowCarry;
            base.Update();
            if (PrimaryLeader == null || PrimaryPikminOnItem == null)
            {
                return;
            }
            if (PrimaryLeader.Controller.isInsideFactory != lastLeaderInsideFactory)
            {
                lastLeaderInsideFactory = PrimaryLeader.Controller.isInsideFactory;
                caveDwellerAI.isOutside = !PrimaryLeader.Controller.isInsideFactory;
                // if (PrimaryPikminOnItem.IsOwner)
                // {
                //     PrimaryPikminOnItem.agent.Warp(PrimaryLeader.transform.position);
                // }
                // PrimaryPikminOnItem.transform2.TeleportOnLocalClient(PrimaryLeader.transform.position);
                // foreach (PikminAI pikmin in PikminOnItem)
                // {
                //     pikmin.isOutside = !PrimaryLeader.Controller.isInsideFactory;
                // }
            }
        }


        public override void GrabPikminItemOnLocalClient()
        {
            PrimaryLeader = PikUtils.GetLeaderFromMultiplePikmin(PikminOnItem);
            caveDwellerAI.ChangeOwnershipOfEnemy(PrimaryLeader.Controller.playerClientId);
            caveDwellerAI.currentOwnershipOnThisClient = (int)PrimaryLeader.Controller.playerClientId;
            caveDwellerAI.inSpecialAnimation = true;
            caveDwellerAI.agent.enabled = false;
            caveDwellerAI.holdingBaby = true;
            if (caveDwellerAI.dropBabyCoroutine != null)
            {
                StopCoroutine(caveDwellerAI.dropBabyCoroutine);
            }
            if (IsServer && caveDwellerAI.babyState == BabyState.RolledOver)
            {
                caveDwellerAI.babyState = BabyState.Roaming;
            }
            caveDwellerAI.SetRolledOverLocalClient(setRolled: false, scared: false);
            caveDwellerAI.playerHolding = PrimaryLeader.Controller;
            base.GrabPikminItemOnLocalClient();
        }
        public override void DiscardPikminItem()
        {
            DropBabyServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
            DropBabyLocalClient();
        }

        [ServerRpc]
        public void DropBabyServerRpc(int playerId)
        {
            DropBabyClientRpc(playerId);
        }

        [ClientRpc]
        public void DropBabyClientRpc(int playerId)
        {
            if (playerId != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
                DropBabyLocalClient();
        }


        public void DropBabyLocalClient()
        {
            IsBeingCarried = false;
            lastPrimaryPikminOnItem = null!;
            PikminCounter.SetCounterColor(DefultColor);
            ClearCurrentRoute();
            if (soundRoutine != null)
            {
                StopCoroutine(soundRoutine);
                soundRoutine = null!;
            }
            foreach (PikminAI pikmin in PikminOnItem)
            {
                pikmin.UnsetAsCarryingItem();
            }


            caveDwellerAI.propScript.previousPlayerHeldBy = caveDwellerAI.playerHolding;
            caveDwellerAI.holdingBaby = false;
            caveDwellerAI.rockingBaby = 0;
            caveDwellerAI.playerHolding = null;
            LethalMin.Logger.LogInfo("Drop baby A");
            if (caveDwellerAI.currentBehaviourStateIndex != 0)
            {
                LethalMin.Logger.LogWarning($"Drop baby B; currentBehaviourStateIndex: {caveDwellerAI.currentBehaviourStateIndex}");
                return;
            }
            if (IsOwner)
            {
                LethalMin.Logger.LogInfo($"Set ownership of creature. Currentownershiponthisclient: {caveDwellerAI.currentOwnershipOnThisClient}");
                caveDwellerAI.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            }
            caveDwellerAI.serverPosition = transform.position;
            bool flag = true;
            Vector3 itemFloorPosition = caveDwellerAI.propScript.GetItemFloorPosition(transform.position + Vector3.up * 0.5f);
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(itemFloorPosition, default(NavMeshHit), 10f);
            LethalMin.Logger.LogInfo($"Drop on position: {navMeshPosition}");
            Debug.DrawRay(caveDwellerAI.propScript.startFallingPosition, Vector3.up * 1f, Color.white, 10f);
            Debug.DrawRay(navMeshPosition, Vector3.up * 0.75f, Color.red, 10f);
            if (!RoundManager.Instance.GotNavMeshPositionResult || caveDwellerAI.DebugEnemy)
            {
                flag = false;
                itemFloorPosition = caveDwellerAI.propScript.startFallingPosition;
                LethalMin.Logger.LogInfo($"Drop Baby C; {caveDwellerAI.propScript.startFallingPosition}");
                if (caveDwellerAI.propScript.transform.parent != null)
                {
                    itemFloorPosition = caveDwellerAI.propScript.transform.parent.TransformPoint(caveDwellerAI.propScript.startFallingPosition);
                }
                LethalMin.Logger.LogInfo($"Drop Baby C global; {caveDwellerAI.propScript.startFallingPosition}");
                Transform transform = caveDwellerAI.ChooseClosestNodeToPositionNoPathCheck(itemFloorPosition);
                navMeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.transform.position);
                LethalMin.Logger.LogInfo($"Got nav mesh position : {RoundManager.Instance.GotNavMeshPositionResult}; {navMeshPosition}; dist: {Vector3.Distance(base.transform.position, transform.transform.position)}");
                Debug.DrawRay(navMeshPosition, Vector3.up * 1.2f, Color.magenta, 10f);
            }
            LethalMin.Logger.LogInfo("Drop baby D");
            if (caveDwellerAI.propScript.transform.parent == null)
            {
                caveDwellerAI.propScript.targetFloorPosition = navMeshPosition;
            }
            else
            {
                caveDwellerAI.propScript.targetFloorPosition = caveDwellerAI.propScript.transform.parent.InverseTransformPoint(navMeshPosition);
            }
            Debug.DrawRay(caveDwellerAI.propScript.targetFloorPosition, Vector3.up * 2f, Color.yellow, 5f);
            if (flag)
            {
                if (caveDwellerAI.dropBabyCoroutine != null)
                {
                    caveDwellerAI.StopCoroutine(caveDwellerAI.dropBabyCoroutine);
                }
                caveDwellerAI.dropBabyCoroutine = caveDwellerAI.StartCoroutine(caveDwellerAI.DropBabyAnimation(navMeshPosition));
            }
            else
            {
                LethalMin.Logger.LogInfo($"Drop baby F; got no floor target; drop pos: {navMeshPosition}");
                transform.position = navMeshPosition;
                caveDwellerAI.inSpecialAnimation = false;
            }


            ItemScript.isHeld = false;
            ItemScript.isPocketed = false;
            ItemScript.parentObject = null;
            ItemScript.isHeldByEnemy = false;
            ItemScript.DiscardItemFromEnemy();
            ItemScript.EnablePhysics(enable: true);
            ItemScript.EnableItemMeshes(enable: true);

            OnItemDropped.Invoke(this);
        }
    }
}
