using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

namespace LethalMin
{
    public class PikminBridgeTrigger : MonoBehaviour
    {
        public List<PikminAI> pikminOnBridge = new List<PikminAI>();

        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy") && other.gameObject.TryGetComponent(out PikminCollisionDetect detect))
            {
                PikminAI pikmin = detect.mainPikmin;
                if (!pikminOnBridge.Contains(pikmin))
                {
                    pikminOnBridge.Add(pikmin);
                }
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Enemy") && other.gameObject.TryGetComponent(out PikminCollisionDetect detect))
            {
                PikminAI pikmin = detect.mainPikmin;
                if (pikminOnBridge.Contains(pikmin))
                {
                    pikminOnBridge.Remove(pikmin);
                }
            }
        }
        public void KnockoffPikmin()
        {
            List<NetworkObjectReference> pikminNetworkObjects = new List<NetworkObjectReference>();
            foreach (PikminAI pikmin in pikminOnBridge)
            {
                if (pikmin.IsOwner && !pikmin.IsDeadOrDying)
                {
                    pikminNetworkObjects.Add(pikmin.NetworkObject);
                }
            }

            if (pikminNetworkObjects.Count > 0)
            {
                LethalMin.Logger.LogInfo($"Knockoff ({pikminNetworkObjects.Count} out of {pikminOnBridge.Count}) owned pikmin from bridge");
                PikminManager.instance.ApplyKnockbackServalPikminServerRpc(pikminNetworkObjects.ToArray(), Vector3.up * 2, 2);
            }
        }
    }
}