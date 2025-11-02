using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMin
{
    public class PikminNoticeZone : NetworkBehaviour
    {
        public bool IsAttachedToPlayer = true;
        public bool Active = true;
        public float Radius = 2;
        public bool CanSavePikmin = false;
        public bool CanPluckSprouts = false;
        public Leader LeaderScript = null!;
        public GameObject Visualizer = null!;
        public Pintent[] ValidStates = { Pintent.Idle };
        //InputAction? debugAction;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            LethalMin.Logger.LogDebug("PikminNoticeZone has spawned");
        }

        public void Start()
        {
            if (IsAttachedToPlayer)
            {
                LeaderScript = GetComponentInParent<Leader>(true);
                LeaderScript.NoticeZone = this;
            }
            if (CanPluckSprouts)
            {
                GetComponent<Collider>().excludeLayers = 0;
            }
        }

        public void OnTriggerStay(Collider other)
        {
            // LethalMin.Logger.LogInfo($"{gameObject.name}: OnTriggerEnter {other.name}");
            // LethalMin.Logger.LogInfo($"Checks: {!Active}, {!UseTriggers}, {!IsOwner}, {!LeaderScript}");
            if (!Active) return;
            if (!IsOwner) { return; }
            if (!LeaderScript) { return; }
            if (LeaderScript.Controller.isPlayerDead) { return; }

            PikminCollisionDetect detect = null!;
            NoticeZoneOnlyDetect detectB = null!;
            Sprout sprout = null!;


            if (CanPluckSprouts && other.CompareTag("InteractTrigger"))
            {
                sprout = other.GetComponentInParent<Sprout>();
                if (sprout != null && !sprout.IsBeingPlucked)
                {
                    sprout.SpawnPikminServerRpc(true, (long)LeaderScript.Controller.OwnerClientId);
                    other.tag = "Untagged";
                    return;
                }
            }

            if (other.CompareTag("Enemy") &&
             (other.TryGetComponent(out detect) && detect.mainPikmin || other.TryGetComponent(out detectB) && detectB.mainPikmin))
            {
                PikminAI ai = detect?.mainPikmin ?? detectB.mainPikmin;
                if (ai.LeaderAssesmentDelay > 0) { return; }
                if (ai.leader == LeaderScript) { return; }
                if (ai.leader != null) { return; }
                if (ai.IsAirborn) { return; }
                if (ai.currentBehaviourStateIndex == PikminAI.PANIC) { return; }
                if (ai.currentBehaviourStateIndex == PikminAI.LEAVING) { return; }
                if (!ai.HasBeenInitalized) { return; }
                if (!ValidStates.Contains(ai.CurrentIntention))
                {
                    //LethalMin.Logger.LogDebug($"{name}: {detect.mainPikmin.DebugID} is not in valid state: {detect.mainPikmin.CurrentIntention}");
                    return;
                }

                //ai.AssignLeader(LeaderScript);
                ai.AssignLeaderServerRpc(LeaderScript.NetworkObject);
            }
        }
    }
}
