using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace LethalMin
{
    public class PikminProtector : MonoBehaviour
    {
        public HazardType[] HazardTypez = { };
        public List<PikminAI> PikminAffected = new List<PikminAI>();
        public float ProtectTime = -1f;
        public void ProtectPikmin(PikminAI pikmin)
        {
            if (pikmin == null) { return; }
            foreach (HazardType hazardType in HazardTypez)
            {
                if (!LethalMin.IsPikminResistantToHazard(pikmin.PminType, hazardType))
                {
                    return;
                }
            }
            LethalMin.Logger.LogInfo($"Protecting {pikmin.name} from {name}");
            PikminAffected.Add(pikmin);
            pikmin.Invincible.Value = true;
            if (ProtectTime > 0)
            {
                StartCoroutine(WaitTillUnprotect());
            }
        }

        public IEnumerator WaitTillUnprotect()
        {
            yield return new WaitForSeconds(ProtectTime);
            UnprotectPikmin();
        }

        public void UnprotectPikmin()
        {
            if (PikminAffected.Count == 0)
            {
                //LethalMin.Logger.LogWarning("No Pikmin to unprotect");
                return;
            }
            LethalMin.Logger.LogInfo($"Unprotecting Pikmin from {name}");
            foreach (var pikmin in PikminAffected)
            {
                pikmin.Invincible.Value = false;
            }
            PikminAffected.Clear();
        }
    }
}