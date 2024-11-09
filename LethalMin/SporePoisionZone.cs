using System.Collections.Generic;
using LethalMin;
using UnityEngine;

public class SporePoisonZone : MonoBehaviour
{
    public List<PikminAI> pikminList;
    public void OnTriggerEnter(Collider other)
    {
        if (other.name == "PikminColision")
        {
            PikminAI pikmin = other.GetComponentInParent<PikminAI>();
            if (!pikminList.Contains(pikmin) && pikmin.currentBehaviourStateIndex != (int)PState.Panic)
            {
                pikminList.Add(pikmin);
                pikmin.EnterPanicState(true, HazardType.Poison, true, Random.Range(5f, 12f));
            }
        }
    }
}