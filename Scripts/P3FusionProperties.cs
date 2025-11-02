using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class P3FusionProperties : BaseOnionFusionProperties
    {
        public SkinnedMeshRenderer[] SMRs = new SkinnedMeshRenderer[0];
        public Texture[] FusionTextures = new Texture[0];
        public SkinnedMeshRenderer SymbolRenderer = null!;
        public override void ApplyFusionProperties(OnionType onionType = null!, List<OnionType> fusedTypes = null!)
        {
            base.ApplyFusionProperties(onionType, fusedTypes);

            int index = Mathf.Clamp(fusedTypes.Count - 1, 0, FusionTextures.Length - 1);
            SymbolRenderer.material.mainTexture = FusionTextures[index];

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
            text.wrapMode = TextureWrapMode.Mirror;
            MainOnionRenderer.material.SetTexture("_BaseColorMap", text);

            OnionFuseRules? PossibleFuseRules = null;
            foreach (OnionFuseRules rules in LethalMin.RegisteredFuseRules.Values)
            {
                int numb = 0;

                foreach (OnionType type in fusedTypes)
                {
                    if (rules.OnionsToFuse.ToList().Contains(type))
                    {
                        numb++;
                    }
                }

                if (numb == fusedTypes.Count)
                {
                    PossibleFuseRules = rules;
                    LethalMin.Logger.LogInfo($"Found a possible fuse rule: {rules}");
                    break;
                }
            }

            if (PossibleFuseRules == null)
            {
                LethalMin.Logger.LogError("No possible fuse rules found");
                return;
            }

            foreach (SkinnedMeshRenderer smr in SMRs)
            {
                if (smr == null)
                {
                    LethalMin.Logger.LogError("SkinnedMeshRenderer is null");
                    continue;
                }

                try
                {
                    float val = ((float)fusedTypes.Count / PossibleFuseRules.OnionsToFuse.Length) * 150;
                    smr.SetBlendShapeWeight(0, val);
                    LethalMin.Logger.LogInfo($"Set blend shape weight on {smr.gameObject.name} to {val}");
                }
                catch (System.Exception e)
                {
                    LethalMin.Logger.LogError($"Error setting blend shape weight on {smr.gameObject.name}: {e}");
                }
            }
        }
    }
}
