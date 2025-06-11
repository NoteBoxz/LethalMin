using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMin
{
    public class CustomPlayerAnimationManager : MonoBehaviour
    {
        public Animator anim = null!;
        public AnimatorOverrideController OverrideController = null!;
        public RuntimeAnimatorController OriginalController = null!;
        public bool AllowOverlap = false;
        public bool CheckforAnimControllerChange = true;

        private float[] storedParameters = new float[0];
        private bool[] storedBools = new bool[0];
        private int[] storedInts = new int[0];
        private AnimationInfo[] storedAnimations = new AnimationInfo[0];

        private struct AnimationInfo
        {
            public string stateName;
            public float normalizedTime;
        }

        private Coroutine? revertCoroutine = null;
        private RuntimeAnimatorController previousRAC = null!;
        private string m_originalAnimationName = "";
        private AnimationClip m_replacementClip = null!;

        public void SetUpCustomAnimation(Animator Animator, string OriginalAnimationName, AnimationClip ReplacementClip)
        {
            if (!Animator.runtimeAnimatorController.animationClips.Select(Clip => Clip.name).ToList().Contains(OriginalAnimationName))
            {
                LethalMin.Logger.LogError($"Animator ({Animator.name}) does not contain the animation {OriginalAnimationName}");
                return;
            }

            anim = Animator;
            OriginalController = anim.runtimeAnimatorController;
            previousRAC = anim.runtimeAnimatorController;
            OverrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
            OverrideController.name = $"{anim.runtimeAnimatorController.name} Override Controller";
            OverrideController[OriginalAnimationName] = ReplacementClip;
            m_originalAnimationName = OriginalAnimationName;
            m_replacementClip = ReplacementClip;
        }

        public void Update()
        {
            if (CheckforAnimControllerChange
            && anim != null
            && previousRAC != null
            && OverrideController != null
            && anim.runtimeAnimatorController != previousRAC
            && anim.runtimeAnimatorController != OverrideController)
            {
                LethalMin.Logger.LogDebug($"Animator Controller changed from {previousRAC.name} to {anim.runtimeAnimatorController.name}");
                previousRAC = anim.runtimeAnimatorController;

                OriginalController = anim.runtimeAnimatorController;
                Destroy(OverrideController);
                OverrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
                OverrideController.name = $"{anim.runtimeAnimatorController.name} Override Controller";
                OverrideController[m_originalAnimationName] = m_replacementClip;
            }
        }

        public void ReplaceAnimatorWithOverride(float AutoRevertTimer = 2)
        {
            //Resets and starts the revert timer
            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }
            revertCoroutine = StartCoroutine(AutoRevert(AutoRevertTimer));

            //Checks if the animator is already the override controller
            if (anim.runtimeAnimatorController == OverrideController && !AllowOverlap)
            {
                LethalMin.Logger.LogWarning($"Animator is already the Override Controller");
                return;
            }

            //Replaces the animator
            StoreParameters();
            anim.runtimeAnimatorController = OverrideController;
            RestoreParameters();

            LethalMin.Logger.LogInfo($"Replaced {gameObject.name} Animator with Override Controller");
        }

        IEnumerator AutoRevert(float time)
        {
            yield return new WaitForSeconds(time);
            if (anim.runtimeAnimatorController == OverrideController)
            {
                LethalMin.Logger.LogWarning($"Auto Reverted Animator to Original Controller");
                RevertAnimator();
            }

            revertCoroutine = null;
        }

        public void RevertAnimator()
        {
            //Reset the revert timer
            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }

            if (anim == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name} Animator is null when reverting");
                return;
            }

            if (OriginalController == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name} Original Controller is null when reverting");
                return;
            }

            StoreParameters();
            anim.runtimeAnimatorController = OriginalController;
            RestoreParameters();

            LethalMin.Logger.LogInfo($"Reverted {gameObject.name} Animator to Original Controller");
        }

        private void StoreParameters()
        {
            var parameters = anim.parameters;
            storedParameters = new float[parameters.Length];
            storedBools = new bool[parameters.Length];
            storedInts = new int[parameters.Length];
            storedAnimations = new AnimationInfo[anim.layerCount];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        storedParameters[i] = anim.GetFloat(param.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        storedBools[i] = anim.GetBool(param.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        storedInts[i] = anim.GetInteger(param.name);
                        break;
                }
            }

            // Store current animations for each layer
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                var stateInfo = anim.GetCurrentAnimatorStateInfo(layer);
                var animInfo = new AnimationInfo
                {
                    stateName = stateInfo.fullPathHash.ToString(),
                    normalizedTime = stateInfo.normalizedTime
                };
                storedAnimations[layer] = animInfo;
            }
        }

        private void RestoreParameters()
        {
            var parameters = anim.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        LethalMin.Logger.LogDebug($"Restoring {param.name} to {storedParameters[i]}");
                        anim.SetFloat(param.name, storedParameters[i]);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        LethalMin.Logger.LogDebug($"Restoring {param.name} to {storedBools[i]}");
                        anim.SetBool(param.name, storedBools[i]);
                        break;
                    case AnimatorControllerParameterType.Int:
                        LethalMin.Logger.LogDebug($"Restoring {param.name} to {storedInts[i]}");
                        anim.SetInteger(param.name, storedInts[i]);
                        break;
                }
            }

            // Restore animations for each layer
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                var animInfo = storedAnimations[layer];
                LethalMin.Logger.LogDebug($"Restoring {animInfo.stateName} to {animInfo.normalizedTime}");
                anim.Play(int.Parse(animInfo.stateName), layer, animInfo.normalizedTime);
            }
        }
    }
}