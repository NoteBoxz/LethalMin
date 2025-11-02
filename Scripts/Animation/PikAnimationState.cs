using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public struct PikAnimationState
    {
        public string name;
        public AnimationClip clip;
        public float TransitionDuration;
        public bool IsOneShot;

        public PikAnimationState(string name, AnimationClip clip, float transitionDuration)
        {
            this.name = name;
            this.clip = clip;
            TransitionDuration = transitionDuration;
        }

        public PikAnimationState(AnimationClip clip)
        {
            this.name = clip.name;
            this.clip = clip;
            TransitionDuration = -1f;
        }
    }
}
