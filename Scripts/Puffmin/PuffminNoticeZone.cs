using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMin
{
    public class PuffminNoticeZone : MonoBehaviour
    {
        public bool Active = true;
        public PuffminLeader LeaderScript = null!;
        public GameObject Visualizer = null!;

        public void OnTriggerEnter(Collider other)
        {
            // LethalMin.Logger.LogInfo($"{gameObject.name}: OnTriggerEnter {other.name}");
            // LethalMin.Logger.LogInfo($"Checks: {!Active}, {!UseTriggers}, {!IsOwner}, {!LeaderScript}");
            if (!Active) return;
            if (!LeaderScript.IsOwner) { return; }
            if (!LeaderScript) { return; }
            if (LeaderScript.AI.isEnemyDead) { return; }

            if (HandlePuffminInZone(other)) { return; }
            if (HandlePikminInZone(other)) { return; }
        }
        public bool HandlePuffminInZone(Collider other)
        {
            if (other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAICollisionDetect detect) && detect.mainScript != null)
            {
                if (!detect.mainScript.TryGetComponent(out PuffminAI puffmin))
                {
                    return false;
                }
                if (puffmin.IsDeadOrDying) { return false; }
                if (puffmin.currentBehaviourStateIndex == 1) { return false; }
                if (puffmin.Leader == LeaderScript) { return false; }
                if (puffmin.Leader != null) { return false; }
                if (puffmin.IsAirborn) { return false; }
                if (puffmin.WhistleBuffer > 0) { return false; }

                puffmin.AssignLeader(LeaderScript);
                puffmin.AssignLeaderServerRpc(LeaderScript.NetworkObject);

                LethalMin.Logger.LogDebug("Puffmin has entered the evil zone");
                return true;
            }
            return false;
        }

        public bool HandlePikminInZone(Collider other)
        {
            if (other.CompareTag("Enemy") && other.TryGetComponent(out PikminCollisionDetect detectB) && detectB.mainPikmin != null)
            {
                if (detectB.mainPikmin.leader != null) { return false; }
                if (detectB.mainPikmin.IsAirborn) { return false; }
                if (detectB.mainPikmin.currentBehaviourStateIndex == 4) { return false; }
                if (detectB.mainPikmin.currentBehaviourStateIndex == 5) { return false; }
                if (detectB.mainPikmin.IsDeadOrDying) { return false; }
                if (PikChecks.IsPikminResistantToHazard(detectB.mainPikmin, PikminHazard.Spore, this)) { return false; }

                detectB.mainPikmin.TransformIntoPuffminServerRpc();
                LethalMin.Logger.LogDebug("Pikmin has entered the evil zone");
                return true;
            }
            return false;
        }
    }
}
