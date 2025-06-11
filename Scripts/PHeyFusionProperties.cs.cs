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
    public class PHeyFusionProperties : BaseOnionFusionProperties
    {
        public override void ApplyFusionProperties(OnionType onionType = null!, List<OnionType> fusedTypes = null!)
        {
            base.ApplyFusionProperties(onionType, fusedTypes);

            Texture2D text = null!;
            if (fusedTypes.Count > 0)
            {
                text = GradientTextureGenerator.GenerateHorizontalGradient(fusedTypes.Select(ot => ot.OnionColor).ToList(), 1f);
            }
            else
            {
                text = GradientTextureGenerator.GenerateHorizontalGradient(
                    new List<Color> { onionType.TypesCanHold[0].PikminPrimaryColor, onionType.TypesCanHold[0].PikminSecondaryColor }, 1f);
            }
            text.filterMode = FilterMode.Point;
            text.wrapMode = TextureWrapMode.Mirror;
            MainOnionRenderer.material.SetTexture("_BaseColorMap", text);
        }
    }
}
