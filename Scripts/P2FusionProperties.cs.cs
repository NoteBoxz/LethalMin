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
    public class P2FusionProperties : BaseOnionFusionProperties
    {
        public override void ApplyFusionProperties(OnionType onionType = null!, List<OnionType> fusedTypes = null!)
        {
            base.ApplyFusionProperties(onionType, fusedTypes);

            if (fusedTypes.Count != 0)
            {
                MainOnionRenderer.material.mainTexture =
                GradientTextureGenerator.GenerateRadialGradient(fusedTypes.Select(ot => ot.OnionColor).ToList(), 0.1f);
            }
            else
            {
                MainOnionRenderer.material.color = onionType.OnionColor;
            }
        }
    }
}
