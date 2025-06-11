using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public abstract class BaseOnionFusionProperties : MonoBehaviour
    {
        public Renderer MainOnionRenderer = null!;

        public virtual void ApplyFusionProperties(OnionType onionType = null!, List<OnionType> fusedTypes = null!)
        {
            // This method can be overridden to apply custom properties for fusion types
            // based on the generation and type of Pikmin. By default, it does nothing.
        }

    }
}
