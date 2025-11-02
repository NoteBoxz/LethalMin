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
    public class P4FusionProperties : BaseOnionFusionProperties
    {
        public Mesh[] FusionMeshes = new Mesh[0];

        public override void ApplyFusionProperties(OnionType onionType = null!, List<OnionType> fusedTypes = null!)
        {
            base.ApplyFusionProperties(onionType, fusedTypes);

            int index = Mathf.Clamp(fusedTypes.Count - 1, 0, FusionMeshes.Length - 1);
            Mesh fusionMesh = FusionMeshes[index];

            SkinnedMeshRenderer? MainOnionRenderer = this.MainOnionRenderer as SkinnedMeshRenderer;
            if (MainOnionRenderer == null)
            {
                LethalMin.Logger.LogError("MainOnionRenderer is not a SkinnedMeshRenderer!");
                return;
            }

            MainOnionRenderer.sharedMesh = fusionMesh;

            if (fusedTypes.Count != 0)
            {
                MainOnionRenderer.material.SetTexture("_GraidentOverlay", GradientTextureGenerator.Generate90DegreeGradient(fusedTypes.Select(ot => ot.OnionColor).ToList(), 0.1f));
            }
            else
            {
                MainOnionRenderer.material.SetColor("_MainColor", onionType.OnionColor);
            }
        }
    }
}
