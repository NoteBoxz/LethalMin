using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LethalMin
{
    public class CustomMaskedAnimationManager : MonoBehaviour
    {
        public MaskedPlayerEnemy maskedPlayerEnemy = null!;
        public ChainIKConstraint WhistleConstraint = null!; // This will be used to control the animation for the whistle
        public ChainIKConstraint OriginalConstraint = null!;
        public Transform WhistleTarget = null!; // This will be the target for the whistle animation
        public Transform OverrideHandBone = null!;
        public bool IsOverriding = false;

        public void Start()
        {
            if (WhistleConstraint == null)
                CreateWhistleConstraint();
            if (OverrideHandBone == null)
                CreateOverrideHandBone();
        }

        public void CreateWhistleConstraint()
        {
            foreach (Rig rig in maskedPlayerEnemy.gameObject.GetComponentsInChildren<Rig>())
            {
                foreach (ChainIKConstraint constraint in rig.GetComponentsInChildren<ChainIKConstraint>())
                {
                    if (constraint.gameObject.name.ToLower() == "rightarm")
                    {
                        // We found the right arm constraint, now we can set it up for the whistle animation
                        // First, dupleicate the constrant to avoid modifying the original one
                        ChainIKConstraint newConstraint = Instantiate(constraint);
                        newConstraint.name = "WhistleConstraint"; // Rename the new constraint for clarity
                        newConstraint.transform.SetParent(constraint.transform.parent);

                        GameObject OldTarget = constraint.data.target.gameObject;
                        GameObject NewTarget = new GameObject("WhistleTarget");

                        // Set the new target's position to be the same as the old target's position
                        NewTarget.transform.position = OldTarget.transform.position;
                        NewTarget.transform.rotation = OldTarget.transform.rotation;
                        NewTarget.transform.SetParent(OldTarget.transform.parent);
                        NewTarget.transform.localPosition = new Vector3(-0.0025f, 0.2861f, 0.2635f);

                        newConstraint.m_Data.target = NewTarget.transform; // Set the new target for the constraint
                        newConstraint.weight = 0f;

                        // Now we can assign the new constraint and target to the class variables
                        WhistleConstraint = newConstraint; // This will be used to control the animation for the whistle
                        WhistleTarget = NewTarget.transform; // This will be the target for the whistle animation
                        OriginalConstraint = constraint; // This will be used to reference the original constraint if needed

                        LethalMin.Logger.LogInfo($"Created a new ChainIKConstraint from {rig.gameObject.name} - {constraint.gameObject.name} for the whistle animation on {maskedPlayerEnemy.gameObject.name}");
                        break;
                    }
                }

                if (WhistleConstraint == null)
                {
                    break;
                }
            }
        }

        public void CreateOverrideHandBone()
        {
            //     Dictionary<SkinnedMeshRenderer, int> renderToReplaceBone = new Dictionary<SkinnedMeshRenderer, int>();
            //     foreach (SkinnedMeshRenderer render in maskedPlayerEnemy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            //     {
            //         foreach (Transform bone in render.bones)
            //         {
            //             if (bone.name.ToLower() == "hand.r")
            //             {
            //                 if (OverrideHandBone == null)
            //                 {
            //                     GameObject overrideBone = new GameObject("OverrideHandBone");
            //                     overrideBone.transform.position = bone.position; // Set the position to the original bone's position
            //                     overrideBone.transform.rotation = bone.rotation; // Set the rotation to the original bone's rotation

            //                     overrideBone.transform.SetParent(bone.parent); // Set the parent to the original bone's parent
            //                                                                    // This will ensure that the new bone follows the same hierarchy as the original bone
            //                                                                    // Now we can assign the new bone to the class variable

            //                     OverrideHandBone = overrideBone.transform; // This will be used to control the hand position for the whistle animation
            //                 }
            //                 renderToReplaceBone.Add(render, render.bones.ToList().IndexOf(bone));
            //                 break;
            //             }
            //         }
            //     }

            //     foreach (SkinnedMeshRenderer smr in renderToReplaceBone.Keys)
            //     {
            //         int IndexToReplaceAt = renderToReplaceBone[smr];

            //         // Get the current bones array
            //         Transform[] bones = smr.bones;

            //         // Modify the copied array
            //         bones[IndexToReplaceAt] = OverrideHandBone!;

            //         // Apply the modified array back to the renderer
            //         smr.bones = bones;

            //         // Force mesh update
            //         smr.rootBone = smr.rootBone; // This forces a refresh

            //         LethalMin.Logger.LogInfo($"Created a new OverrideHandBone for {smr.gameObject.name} at position {OverrideHandBone?.position} and rotation {OverrideHandBone?.rotation} at index {IndexToReplaceAt} on {maskedPlayerEnemy.gameObject.name}");
            //     }
        }


        public IEnumerator SetWhistleToOverride()
        {
            IsOverriding = true; // Set the override flag to true so we know we're in override mode

            //tween the whistle contraint's weight to 1 and the original contraint's weight to 0
            float duration = 0.5f; // Duration of the tween
            float elapsedTime = 0f; // Time elapsed since the start of the tween

            float startWeight = OriginalConstraint.weight; // Get the starting weight of the original constraint
            float endWeight = WhistleConstraint.weight; // Get the starting weight of the whistle constraint

            // While the elapsed time is less than the duration, keep interpolating the weights
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime; // Increment the elapsed time
                float t = Mathf.Clamp01(elapsedTime / duration); // Calculate the interpolation factor

                // Interpolate the weights based on the elapsed time
                OriginalConstraint.weight = Mathf.Lerp(startWeight, 0f, t); // Set the weight of the original constraint to decrease to 0
                WhistleConstraint.weight = Mathf.Lerp(endWeight, 1f, t); // Set the weight of the whistle constraint to increase to 1

                yield return null; // Wait for the next frame
            }

            // Ensure the weights are set to their final values after the tween is complete
            OriginalConstraint.weight = 0f; // Set the weight of the original constraint to 0
            WhistleConstraint.weight = 1f; // Set the weight of the whistle constraint to 1
        }


        public IEnumerator DeSetWhistleToOverride()
        {
            IsOverriding = false; // Set the override flag to false so we know we're not in override mode anymore

            //tween the whistle contraint's weight to 1 and the original contraint's weight to 0
            float duration = 0.5f; // Duration of the tween
            float elapsedTime = 0f; // Time elapsed since the start of the tween

            float startWeight = WhistleConstraint.weight; // Get the starting weight of the whistle constraint
            float endWeight = OriginalConstraint.weight; // Get the starting weight of the original constraint

            // While the elapsed time is less than the duration, keep interpolating the weights
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime; // Increment the elapsed time
                float t = Mathf.Clamp01(elapsedTime / duration); // Calculate the interpolation factor

                // Interpolate the weights based on the elapsed time
                WhistleConstraint.weight = Mathf.Lerp(startWeight, 0f, t); // Set the weight of the whistle constraint to decrease to 0
                OriginalConstraint.weight = Mathf.Lerp(endWeight, 1f, t); // Set the weight of the original constraint to increase to 1

                yield return null; // Wait for the next frame
            }

            // Ensure the weights are set to their final values after the tween is complete
            WhistleConstraint.weight = 0f; // Set the weight of the whistle constraint to 0
            OriginalConstraint.weight = 1f; // Set the weight of the original constraint to 1
        }
    }
}