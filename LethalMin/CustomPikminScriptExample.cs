using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace LethalMin
{
    public class CustomPikminScriptExample : NetworkBehaviour
    {
        //This is set but the mod
        public PikminAI BaseScript = null!;

        //Initalizeation Stuff
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            StartCoroutine(WaitToParent());
        }
        IEnumerator WaitToParent()
        {
            yield return new WaitUntil(() => transform.parent != null);
            BaseScript = GetComponentInParent<PikminAI>();
            if (BaseScript != null)
            {
                SubscribeToEvents();
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo("Custom Pikmin script attached to " + BaseScript?.uniqueDebugId);
        }

        //Uninitalizeation Stuff
        public override void OnNetworkDespawn()
        {
            base.OnNetworkSpawn();
            if (BaseScript != null)
            {
                UnSubscribeToEvents();
            }
        }

        private void SubscribeToEvents()
        {
            // Start Events
            BaseScript.OnDoAIInterval.AddListener(OnDoAIIntervalHandler);
            BaseScript.OnHandleIdleState.AddListener(OnHandleIdleStateHandler);
            BaseScript.OnHandleFollowingState.AddListener(OnHandleFollowingStateHandler);
            BaseScript.OnHandleAirbornState.AddListener(OnHandleAirbornStateHandler);
            BaseScript.OnHandleDrowningState.AddListener(OnHandleDrowningStateHandler);
            BaseScript.OnHandleWorkingState.AddListener(OnHandleWorkingStateHandler);
            BaseScript.OnHandleAttackingState.AddListener(OnHandleAttackingStateHandler);
            BaseScript.OnHandleLeavingState.AddListener(OnHandleLeavingStateHandler);
            BaseScript.OnHandleLeaderLost.AddListener(OnHandleLeaderLostHandler);
            BaseScript.OnCheckForNearbyPlayers.AddListener(OnCheckForNearbyPlayersHandler);
            BaseScript.OnAssignLeader.AddListener(OnAssignLeaderHandler);
            BaseScript.OnAssignLeaderServerRpc.AddListener(OnAssignLeaderServerRpcHandler);
            BaseScript.OnAssignLeaderResponseClientRpc.AddListener(OnAssignLeaderResponseClientRpcHandler);
            BaseScript.OnFindLeaderManagerForPlayer.AddListener(OnFindLeaderManagerForPlayerHandler);
            BaseScript.OnSetDrowningClientRpc.AddListener(OnSetDrowningClientRpcHandler);
            BaseScript.OnStopDrowningClientRpc.AddListener(OnStopDrowningClientRpcHandler);
            BaseScript.OnIsNearDestination.AddListener(OnIsNearDestinationHandler);
            BaseScript.OnRemoveFromItemServerRpc.AddListener(OnRemoveFromItemServerRpcHandler);
            BaseScript.OnSetTargetItemServerRpc.AddListener(OnSetTargetItemServerRpcHandler);
            BaseScript.OnSetTargetItemClientRpc.AddListener(OnSetTargetItemClientRpcHandler);
            BaseScript.OnDetectNearbyItems.AddListener(OnDetectNearbyItemsHandler);
            BaseScript.OnMoveTowardsItem.AddListener(OnMoveTowardsItemHandler);
            BaseScript.OnCheckLineOfSightForItem.AddListener(OnCheckLineOfSightForItemHandler);
            BaseScript.OnLandPikminClientRpc.AddListener(OnLandPikminClientRpcHandler);
            BaseScript.OnLatchOntoEnemyClientRpc.AddListener(OnLatchOntoEnemyClientRpcHandler);
            BaseScript.OnOnCollideWithEnemy.AddListener(OnCollideWithEnemyHandler);

            // End Events
            BaseScript.OnDoAIIntervalEnd.AddListener(OnDoAIIntervalEndHandler);
            BaseScript.OnHandleIdleStateEnd.AddListener(OnHandleIdleStateEndHandler);
            BaseScript.OnHandleFollowingStateEnd.AddListener(OnHandleFollowingStateEndHandler);
            BaseScript.OnHandleAirbornStateEnd.AddListener(OnHandleAirbornStateEndHandler);
            BaseScript.OnHandleDrowningStateEnd.AddListener(OnHandleDrowningStateEndHandler);
            BaseScript.OnHandleWorkingStateEnd.AddListener(OnHandleWorkingStateEndHandler);
            BaseScript.OnHandleAttackingStateEnd.AddListener(OnHandleAttackingStateEndHandler);
            BaseScript.OnHandleLeavingStateEnd.AddListener(OnHandleLeavingStateEndHandler);
            BaseScript.OnHandleLeaderLostEnd.AddListener(OnHandleLeaderLostEndHandler);
            BaseScript.OnCheckForNearbyPlayersEnd.AddListener(OnCheckForNearbyPlayersEndHandler);
            BaseScript.OnAssignLeaderEnd.AddListener(OnAssignLeaderEndHandler);
            BaseScript.OnAssignLeaderServerRpcEnd.AddListener(OnAssignLeaderServerRpcEndHandler);
            BaseScript.OnAssignLeaderResponseClientRpcEnd.AddListener(OnAssignLeaderResponseClientRpcEndHandler);
            BaseScript.OnFindLeaderManagerForPlayerEnd.AddListener(OnFindLeaderManagerForPlayerEndHandler);
            BaseScript.OnSetDrowningClientRpcEnd.AddListener(OnSetDrowningClientRpcEndHandler);
            BaseScript.OnStopDrowningClientRpcEnd.AddListener(OnStopDrowningClientRpcEndHandler);
            BaseScript.OnIsNearDestinationEnd.AddListener(OnIsNearDestinationEndHandler);
            BaseScript.OnRemoveFromItemServerRpcEnd.AddListener(OnRemoveFromItemServerRpcEndHandler);
            BaseScript.OnSetTargetItemServerRpcEnd.AddListener(OnSetTargetItemServerRpcEndHandler);
            BaseScript.OnSetTargetItemClientRpcEnd.AddListener(OnSetTargetItemClientRpcEndHandler);
            BaseScript.OnDetectNearbyItemsEnd.AddListener(OnDetectNearbyItemsEndHandler);
            BaseScript.OnMoveTowardsItemEnd.AddListener(OnMoveTowardsItemEndHandler);
            BaseScript.OnCheckLineOfSightForItemEnd.AddListener(OnCheckLineOfSightForItemEndHandler);
            BaseScript.OnLandPikminEnd.AddListener(OnLandPikminClientRpcEndHandler);
            BaseScript.OnLatchOntoEnemyEnd.AddListener(OnLatchOntoEnemyClientRpcEndHandler);
            BaseScript.OnOnCollideWithEnemyEnd.AddListener(OnCollideWithEnemyEndHandler);
        }

        private void UnSubscribeToEvents()
        {
            // Start Events
            BaseScript.OnDoAIInterval.RemoveListener(OnDoAIIntervalHandler);
            BaseScript.OnHandleIdleState.RemoveListener(OnHandleIdleStateHandler);
            BaseScript.OnHandleFollowingState.RemoveListener(OnHandleFollowingStateHandler);
            BaseScript.OnHandleAirbornState.RemoveListener(OnHandleAirbornStateHandler);
            BaseScript.OnHandleDrowningState.RemoveListener(OnHandleDrowningStateHandler);
            BaseScript.OnHandleWorkingState.RemoveListener(OnHandleWorkingStateHandler);
            BaseScript.OnHandleAttackingState.RemoveListener(OnHandleAttackingStateHandler);
            BaseScript.OnHandleLeavingState.RemoveListener(OnHandleLeavingStateHandler);
            BaseScript.OnHandleLeaderLost.RemoveListener(OnHandleLeaderLostHandler);
            BaseScript.OnCheckForNearbyPlayers.RemoveListener(OnCheckForNearbyPlayersHandler);
            BaseScript.OnAssignLeader.RemoveListener(OnAssignLeaderHandler);
            BaseScript.OnAssignLeaderServerRpc.RemoveListener(OnAssignLeaderServerRpcHandler);
            BaseScript.OnAssignLeaderResponseClientRpc.RemoveListener(OnAssignLeaderResponseClientRpcHandler);
            BaseScript.OnFindLeaderManagerForPlayer.RemoveListener(OnFindLeaderManagerForPlayerHandler);
            BaseScript.OnSetDrowningClientRpc.RemoveListener(OnSetDrowningClientRpcHandler);
            BaseScript.OnStopDrowningClientRpc.RemoveListener(OnStopDrowningClientRpcHandler);
            BaseScript.OnIsNearDestination.RemoveListener(OnIsNearDestinationHandler);
            BaseScript.OnRemoveFromItemServerRpc.RemoveListener(OnRemoveFromItemServerRpcHandler);
            BaseScript.OnSetTargetItemServerRpc.RemoveListener(OnSetTargetItemServerRpcHandler);
            BaseScript.OnSetTargetItemClientRpc.RemoveListener(OnSetTargetItemClientRpcHandler);
            BaseScript.OnDetectNearbyItems.RemoveListener(OnDetectNearbyItemsHandler);
            BaseScript.OnMoveTowardsItem.RemoveListener(OnMoveTowardsItemHandler);
            BaseScript.OnCheckLineOfSightForItem.RemoveListener(OnCheckLineOfSightForItemHandler);
            BaseScript.OnLandPikminClientRpc.RemoveListener(OnLandPikminClientRpcHandler);
            BaseScript.OnLatchOntoEnemyClientRpc.RemoveListener(OnLatchOntoEnemyClientRpcHandler);
            BaseScript.OnOnCollideWithEnemy.RemoveListener(OnCollideWithEnemyHandler);

            // End Events
            BaseScript.OnDoAIIntervalEnd.RemoveListener(OnDoAIIntervalEndHandler);
            BaseScript.OnHandleIdleStateEnd.RemoveListener(OnHandleIdleStateEndHandler);
            BaseScript.OnHandleFollowingStateEnd.RemoveListener(OnHandleFollowingStateEndHandler);
            BaseScript.OnHandleAirbornStateEnd.RemoveListener(OnHandleAirbornStateEndHandler);
            BaseScript.OnHandleDrowningStateEnd.RemoveListener(OnHandleDrowningStateEndHandler);
            BaseScript.OnHandleWorkingStateEnd.RemoveListener(OnHandleWorkingStateEndHandler);
            BaseScript.OnHandleAttackingStateEnd.RemoveListener(OnHandleAttackingStateEndHandler);
            BaseScript.OnHandleLeavingStateEnd.RemoveListener(OnHandleLeavingStateEndHandler);
            BaseScript.OnHandleLeaderLostEnd.RemoveListener(OnHandleLeaderLostEndHandler);
            BaseScript.OnCheckForNearbyPlayersEnd.RemoveListener(OnCheckForNearbyPlayersEndHandler);
            BaseScript.OnAssignLeaderEnd.RemoveListener(OnAssignLeaderEndHandler);
            BaseScript.OnAssignLeaderServerRpcEnd.RemoveListener(OnAssignLeaderServerRpcEndHandler);
            BaseScript.OnAssignLeaderResponseClientRpcEnd.RemoveListener(OnAssignLeaderResponseClientRpcEndHandler);
            BaseScript.OnFindLeaderManagerForPlayerEnd.RemoveListener(OnFindLeaderManagerForPlayerEndHandler);
            BaseScript.OnSetDrowningClientRpcEnd.RemoveListener(OnSetDrowningClientRpcEndHandler);
            BaseScript.OnStopDrowningClientRpcEnd.RemoveListener(OnStopDrowningClientRpcEndHandler);
            BaseScript.OnIsNearDestinationEnd.RemoveListener(OnIsNearDestinationEndHandler);
            BaseScript.OnRemoveFromItemServerRpcEnd.RemoveListener(OnRemoveFromItemServerRpcEndHandler);
            BaseScript.OnSetTargetItemServerRpcEnd.RemoveListener(OnSetTargetItemServerRpcEndHandler);
            BaseScript.OnSetTargetItemClientRpcEnd.RemoveListener(OnSetTargetItemClientRpcEndHandler);
            BaseScript.OnDetectNearbyItemsEnd.RemoveListener(OnDetectNearbyItemsEndHandler);
            BaseScript.OnMoveTowardsItemEnd.RemoveListener(OnMoveTowardsItemEndHandler);
            BaseScript.OnCheckLineOfSightForItemEnd.RemoveListener(OnCheckLineOfSightForItemEndHandler);
            BaseScript.OnLandPikminEnd.RemoveListener(OnLandPikminClientRpcEndHandler);
            BaseScript.OnLatchOntoEnemyEnd.RemoveListener(OnLatchOntoEnemyClientRpcEndHandler);
            BaseScript.OnOnCollideWithEnemyEnd.RemoveListener(OnCollideWithEnemyEndHandler);
        }

        // Handler methods for Start Events
        private void OnDoAIIntervalHandler()
        {
            //LethalMin.Logger.LogInfo("OnDoAIInterval event triggered");
        }
        private void OnHandleIdleStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleIdleState event triggered");
        }
        private void OnHandleFollowingStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleFollowingState event triggered");
        }
        private void OnHandleAirbornStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleAirbornState event triggered");
        }
        private void OnHandleDrowningStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleDrowningState event triggered");
        }
        private void OnHandleWorkingStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleWorkingState event triggered");
        }
        private void OnHandleAttackingStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleAttackingState event triggered");
        }
        private void OnHandleLeavingStateHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleLeavingState event triggered");
        }
        private void OnHandleLeaderLostHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleLeaderLost event triggered");
        }
        private void OnCheckForNearbyPlayersHandler()
        {
            //LethalMin.Logger.LogInfo("OnCheckForNearbyPlayers event triggered");
        }
        private void OnAssignLeaderHandler()
        {
            //LethalMin.Logger.LogInfo("OnAssignLeader event triggered");
        }
        private void OnAssignLeaderServerRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnAssignLeaderServerRpc event triggered");
        }
        private void OnAssignLeaderResponseClientRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnAssignLeaderResponseClientRpc event triggered");
        }
        private void OnFindLeaderManagerForPlayerHandler()
        {
            //LethalMin.Logger.LogInfo("OnFindLeaderManagerForPlayer event triggered");
        }
        private void OnSetDrowningClientRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnSetDrowningClientRpc event triggered");
        }
        private void OnStopDrowningClientRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnStopDrowningClientRpc event triggered");
        }
        private void OnIsNearDestinationHandler()
        {
            //LethalMin.Logger.LogInfo("OnIsNearDestination event triggered");
        }
        private void OnRemoveFromItemServerRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnRemoveFromItemServerRpc event triggered");
        }
        private void OnSetTargetItemServerRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnSetTargetItemServerRpc event triggered");
        }
        private void OnSetTargetItemClientRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnSetTargetItemClientRpc event triggered");
        }
        private void OnDetectNearbyItemsHandler()
        {
            //LethalMin.Logger.LogInfo("OnDetectNearbyItems event triggered");
        }
        private void OnMoveTowardsItemHandler()
        {
            //LethalMin.Logger.LogInfo("OnMoveTowardsItem event triggered");
        }
        private void OnCheckLineOfSightForItemHandler()
        {
            //LethalMin.Logger.LogInfo("OnCheckLineOfSightForItem event triggered");
        }
        private void OnLandPikminClientRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnLandPikminClientRpc event triggered");
        }
        private void OnLatchOntoEnemyClientRpcHandler()
        {
            //LethalMin.Logger.LogInfo("OnLatchOntoEnemyClientRpc event triggered");
        }

        // Handler methods for End Events
        private void OnDoAIIntervalEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnDoAIIntervalEnd event triggered");
        }
        private void OnHandleIdleStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleIdleStateEnd event triggered");
        }
        private void OnHandleFollowingStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleFollowingStateEnd event triggered");
        }
        private void OnHandleAirbornStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleAirbornStateEnd event triggered");
        }
        private void OnHandleDrowningStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleDrowningStateEnd event triggered");
        }
        private void OnHandleWorkingStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleWorkingStateEnd event triggered");
        }
        private void OnHandleAttackingStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleAttackingStateEnd event triggered");
        }
        private void OnHandleLeavingStateEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleLeavingStateEnd event triggered");
        }
        private void OnHandleLeaderLostEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnHandleLeaderLostEnd event triggered");
        }
        private void OnCheckForNearbyPlayersEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnCheckForNearbyPlayersEnd event triggered");
        }
        private void OnAssignLeaderEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnAssignLeaderEnd event triggered");
        }
        private void OnAssignLeaderServerRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnAssignLeaderServerRpcEnd event triggered");
        }
        private void OnAssignLeaderResponseClientRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnAssignLeaderResponseClientRpcEnd event triggered");
        }
        private void OnFindLeaderManagerForPlayerEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnFindLeaderManagerForPlayerEnd event triggered");
        }
        private void OnSetDrowningClientRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnSetDrowningClientRpcEnd event triggered");
        }
        private void OnStopDrowningClientRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnStopDrowningClientRpcEnd event triggered");
        }
        private void OnIsNearDestinationEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnIsNearDestinationEnd event triggered");
        }
        private void OnRemoveFromItemServerRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnRemoveFromItemServerRpcEnd event triggered");
        }
        private void OnSetTargetItemServerRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnSetTargetItemServerRpcEnd event triggered");
        }
        private void OnSetTargetItemClientRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnSetTargetItemClientRpcEnd event triggered");
        }
        private void OnDetectNearbyItemsEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnDetectNearbyItemsEnd event triggered");
        }
        private void OnMoveTowardsItemEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnMoveTowardsItemEnd event triggered");
        }
        private void OnCheckLineOfSightForItemEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnCheckLineOfSightForItemEnd event triggered");
        }
        private void OnLandPikminClientRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnLandPikminClientRpcEnd event triggered");
        }
        private void OnLatchOntoEnemyClientRpcEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnLatchOntoEnemyClientRpcEnd event triggered");
        }
        private void OnCollideWithEnemyEndHandler()
        {
            //LethalMin.Logger.LogInfo("OnCollideWithEnemyEnd event triggered");
        }
        private void OnCollideWithEnemyHandler()
        {
            //LethalMin.Logger.LogInfo("OnCollideWithEnemy event triggered");
        }
    }
}