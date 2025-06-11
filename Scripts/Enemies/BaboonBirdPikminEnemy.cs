using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class BaboonBirdPikminEnemy : PikminEnemy
    {
        BaboonBirdAI baboonBirdAI = null!;
        //float CheckInterval = 0.25f;
        float BiteCooldown = 1.5f;
        public float BiteResetCooldown = 1.5f;

        protected override void Start()
        {
            base.Start();
            baboonBirdAI = enemyScript as BaboonBirdAI ?? throw new System.Exception("BaboonBirdPE: enemyScript is not a BaboonBirdAI");
            if (baboonBirdAI == null)
            {
                enabled = false;
            }
            if(LethalMin.UseConfigsForEnemies)
            {
                BiteCooldown = LethalMin.BaboonBird_BiteCooldown.InternalValue;
            }
        }

        void LateUpdate()
        {
            if (BiteCooldown > 0)
            {
                BiteCooldown -= Time.deltaTime;
            }
            if(LethalMin.UseConfigsForEnemies)
            {
                BiteResetCooldown = LethalMin.BaboonBird_BiteCooldown.InternalValue;
            }
        }

        public void OnColideWithPikmin(PikminAI Pikmin)
        {
            if (!enabled)
            {
                return;
            }
            if (BiteCooldown > 0)
            {
                return;
            }
            if (Pikmin.IsDeadOrDying || Pikmin.IsAirborn || (Pikmin.CurrentLatchTrigger != null && LatchTriggers.Contains(Pikmin.CurrentLatchTrigger)))
            {
                return;
            }
            BitePikminServerRpc(Pikmin.NetworkObject);
            BitePikmin(Pikmin);
            BiteCooldown = BiteResetCooldown;
        }

        [ServerRpc(RequireOwnership = false)]
        public void BitePikminServerRpc(NetworkObjectReference Pikref)
        {
            BitePikminClientRpc(Pikref);
        }
        [ClientRpc]
        public void BitePikminClientRpc(NetworkObjectReference Pikref)
        {
            if(IsOwner)
            {
                return;
            }
            if (Pikref.TryGet(out NetworkObject NetObj) && NetObj.TryGetComponent(out PikminAI ai))
            {
                BitePikmin(ai);
            }
        }

        public void BitePikmin(PikminAI pikminAI)
        {
            LethalMin.Logger.LogInfo($"LethalMin: BaboonBirdPikminEnemy.BitePikmin: {pikminAI.gameObject.name} is being bitten by {gameObject.name}");
            BiteCooldown = BiteResetCooldown;

            pikminAI.DeathSnapToPos = baboonBirdAI.deadBodyPoint;
            pikminAI.OverrideDelay = 0.5f;
            pikminAI.HitEnemy(1);

			baboonBirdAI.creatureAnimator.ResetTrigger("Hit");
			baboonBirdAI.creatureAnimator.SetTrigger("Hit");
			baboonBirdAI.creatureSFX.PlayOneShot(baboonBirdAI.enemyType.audioClips[5]);
			WalkieTalkie.TransmitOneShotAudio(baboonBirdAI.creatureSFX, baboonBirdAI.enemyType.audioClips[5]);
			RoundManager.Instance.PlayAudibleNoise(baboonBirdAI.creatureSFX.transform.position, 8f, 0.7f);
        }
    }
}
