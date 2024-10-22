using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace LethalMin
{
    public class PurplePikmin : NetworkBehaviour
    {
        public PikminAI BaseScript = null!;

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
            BaseScript.OnLandPikminClientRpc.AddListener(OnLandPikminClientRpcHandler);
            BaseScript.OnLatchOntoEnemyClientRpc.AddListener(OnLatchOntoEnemyClientRpcHandler);

            // End Events
            BaseScript.OnLandPikminEnd.AddListener(OnLandPikminClientRpcEndHandler);
            BaseScript.OnLatchOntoEnemyEnd.AddListener(OnLatchOntoEnemyClientRpcEndHandler);
        }

        private void UnSubscribeToEvents()
        {
            // Start Events
            BaseScript.OnLandPikminClientRpc.RemoveListener(OnLandPikminClientRpcHandler);
            BaseScript.OnLatchOntoEnemyClientRpc.RemoveListener(OnLatchOntoEnemyClientRpcHandler);

            // End Events
            BaseScript.OnLandPikminEnd.RemoveListener(OnLandPikminClientRpcEndHandler);
            BaseScript.OnLatchOntoEnemyEnd.RemoveListener(OnLatchOntoEnemyClientRpcEndHandler);
        }

        // Handler methods for Start Events
        bool wasThrown;
        private void OnLandPikminClientRpcHandler()
        {
            wasThrown = BaseScript.isHeldOrThrown;
        }
        private void OnLatchOntoEnemyClientRpcHandler()
        {
        }

        // Handler methods for End Events
        private void OnLandPikminClientRpcEndHandler()
        {
            if (wasThrown)
            {
                wasThrown = false;
                if (!BaseScript.IsDrowing)
                {
                    if (Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position) < 10f)
                    {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    }
                    else if (Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position) < 20f)
                    {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    }
                    PlaySlamSFXClientRpc(true, true);
                }
            }
        }
        private void OnLatchOntoEnemyClientRpcEndHandler()
        {
            if (BaseScript.previousLeader != null)
            {
                BaseScript.EnemyDamager.HitInAirQoutes(.15f, BaseScript.previousLeader.Controller, true, 1);
            }
            else
            {
                BaseScript.EnemyDamager.HitInAirQoutes(.15f, null, true, 1);
            }
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            PlaySlamSFXClientRpc(true, true);
        }
        [ClientRpc]
        public void PlaySlamSFXClientRpc(bool PlayOnWalkie = true, bool AudibleToEnemies = false)
        {
            BaseScript.LocalSFX.PlayOneShot(LethalMin.PurpSlam);
            if (PlayOnWalkie)
            {
                WalkieTalkie.TransmitOneShotAudio(BaseScript.LocalSFX, LethalMin.PurpSlam);
            }
            if (AudibleToEnemies && !LethalMin.LethaDogs1Value && BaseScript.IsServer)
            {
                RoundManager.Instance.PlayAudibleNoise(transform.position, 10, 1, 0, BaseScript.IsInShip && StartOfRound.Instance.hangarDoorsClosed);
            }
        }
    }
}