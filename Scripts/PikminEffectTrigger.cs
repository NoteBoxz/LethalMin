using System.Collections;
using System.Collections.Generic;
using LethalMin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LethalMin
{
    public enum PikminEffectType
    {
        Paralized,
        Scatter
    }
    public enum PikminEffectMode
    {
        Persitant,
        Limited,
    }
    public class PikminEffectTrigger : MonoBehaviour
    {
        public float KillTimer = 5;
        public PikminHazard HazardType = PikminHazard.Water;
        public PikminEffectMode Mode = PikminEffectMode.Persitant;
        public PikminEffectType EffectType = PikminEffectType.Paralized;
        public string OverridePanicAnim = "";

        public bool IsPikminResistantToTrigger(PikminAI ai)
        {
            return PikChecks.IsPikminResistantToHazard(ai.pikminType, HazardType);
        }
        public string GetAnimString()
        {
            if (!string.IsNullOrEmpty(OverridePanicAnim))
            {
                return OverridePanicAnim;
            }
            switch (HazardType)
            {
                case PikminHazard.Water:
                    return "WaterPanic";
                case PikminHazard.Fire:
                    return "FirePanic";
                case PikminHazard.Poison:
                    return "PoisonPanic";
                default:
                    return "Panic";
            }
        }
    }
}