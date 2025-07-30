using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "OnionFuseRules", menuName = "Pikmin/OnionFusingRules", order = 0)]
    public class OnionFuseRules : ScriptableObject
    {
        public OnionType[] OnionsToFuse = new OnionType[0];

        [Tooltip("Set by mod, do not change.")]
        [HideInInspector]
        public int FuseRulesTypeID;
    }
}