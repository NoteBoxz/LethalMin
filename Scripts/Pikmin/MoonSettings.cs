using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Utils;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "PikminMoonSettings", menuName = "Pikmin/MoonSettings", order = 0)]
    public class MoonSettings : ScriptableObject
    {
        public SelectableLevel Level = null!;

        public bool OverridePathing = false;
    }
}