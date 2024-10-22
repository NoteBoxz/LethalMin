using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

namespace LethalMin
{
    public class PikminWaterDetector : NetworkBehaviour
    {
        public PikminAI parentScript;
        public void Awake()
        {
            if (parentScript == null)
            {
                parentScript = GetComponentInParent<PikminAI>();
            }
        }
        public void OnCollisionEnter(Collider other)
        {
            LethalMin.Logger.LogInfo($"{parentScript.uniqueDebugId} Has entered water!");
        }
    }
}