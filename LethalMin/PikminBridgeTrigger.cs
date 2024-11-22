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
            if (other.name == "PikminColision")
            {
                PikminAI pikmin = other.GetComponentInParent<PikminAI>();
                if (!pikminOnBridge.Contains(pikmin))
                {
                    pikminOnBridge.Add(pikmin);
                }
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.name == "PikminColision")
            {
                PikminAI pikmin = other.GetComponentInParent<PikminAI>();
                if (pikminOnBridge.Contains(pikmin))
                {
                    pikminOnBridge.Remove(pikmin);
                }
            }
        }
        public void KnockoffPikmin()
        {
            foreach (PikminAI pikmin in pikminOnBridge)
            {
                pikmin.ApplyKnockbackServerRpc(Vector3.up, false, false);
            }
        }
    }
}