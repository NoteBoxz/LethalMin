using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace LethalMin
{
    [RequireComponent(typeof(Animator))]
    public class PikminAnimatorController : MonoBehaviour
    {
        public class AnimationCondition
        {
            public bool enabled = true;
            public System.Func<bool> EnterCondition { get; set; }
            public System.Func<bool> ExitCondition { get; set; }
            public AnimationClip Animation { get; set; }
            public int Priority { get; set; }

            public AnimationCondition(System.Func<bool> condition, AnimationClip animation, int priority)
            {
                EnterCondition = condition;
                ExitCondition = condition;
                Animation = animation;
                Priority = priority;
            }

            public AnimationCondition(System.Func<bool> enterCondition, System.Func<bool> exitCondition, AnimationClip animation, int priority)
            {
                EnterCondition = enterCondition;
                ExitCondition = exitCondition;
                Animation = animation;
                Priority = priority;
            }
        }

        public Animator animator = null!;
        public (int, AnimationClip?) _currentState = new(); // Layer -> Current animation
        public PikminAnimationPack AnimPack = null!; // Animation pack reference
        public List<PikAnimationState> AnimationStates = new(); // List of animation states
        public AnimationClip? CurrentAnimation
        {
            get
            {
                var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                return clipInfo.Length > 0 ? clipInfo[0].clip : null;
            }
        }
        [HideInInspector] public AnimationClip? IdleAnim = null;
        [HideInInspector] public int RandomIdle = 0;
        [HideInInspector] public bool IsMoving;
        [HideInInspector] public bool IsCarrying;
        [HideInInspector] public bool IsAttacking;
        [HideInInspector] public bool IsLaying;
        protected Coroutine? AnimCheckCoroutine;
        protected Dictionary<string, int> _animationHashes = new Dictionary<string, int>();
        public bool IsPlayingIdleAnim => CurrentAnimation != null && AnimPack.EditorIdleAnim.Contains(CurrentAnimation);
        public bool IsPlayingWalkingAnim => CurrentAnimation != null && CurrentAnimation == AnimPack.EditorWalkingAnim;
        public AnimationCondition WalkingCondition = null!;
        public AnimationCondition CayingCondition = null!;
        public AnimationCondition AttackingCondition = null!;
        public AnimationCondition LayingCondition = null!;
        private List<AnimationCondition> _animationConditions = new List<AnimationCondition>();
        bool PlayAnimCalled = false;
        AnimationClip? lastIdleAnim = null;



        public virtual void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            // Clear current animation states
            AnimationStates.Clear();

            // Capture animation clips on main thread
            var clips = animator.runtimeAnimatorController.animationClips;

            ProcessAnimationClipsAsync(clips);

            SetupDefaultAnimationConditions();

            FillNullAnims();
        }

        private void ProcessAnimationClipsAsync(AnimationClip[] clips)
        {
            // Create a list to store the results
            var OneShotClips = new List<AnimationClip>();
            if (AnimPack.EditorNoticeAnim != null) OneShotClips.Add(AnimPack.EditorNoticeAnim);
            if (AnimPack.EditorStandingAttackAnim != null) OneShotClips.Add(AnimPack.EditorStandingAttackAnim);
            if (AnimPack.EditorGetUpAnim != null) OneShotClips.Add(AnimPack.EditorGetUpAnim);
            if (AnimPack.EditorPluckedAnim != null) OneShotClips.Add(AnimPack.EditorPluckedAnim);
            OneShotClips.AddRange(AnimPack.EditorYayAnim);
            OneShotClips.AddRange(AnimPack.EditorMiscOneshotAnim);
            IdleAnim = AnimPack.EditorIdleAnim.Count != 0 ? AnimPack.EditorIdleAnim[0] : null;

            // Create a mapping of override clips to original clips if using an override controller
            Dictionary<AnimationClip, AnimationClip> overrideMap = new Dictionary<AnimationClip, AnimationClip>();

            if (AnimPack.AnimatorOverrideController != null)
            {
                List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                AnimPack.AnimatorOverrideController.GetOverrides(overrides);

                foreach (var pair in overrides)
                {
                    if (pair.Value != null) // Only add if there's an actual override
                    {
                        overrideMap[pair.Value] = pair.Key;
                    }
                }
            }

            // foreach (var pair in overrideMap)
            // {
            //     if (pair.Value != null && pair.Key != null)
            //     {
            //         LethalMin.Logger.LogInfo($"PikminAnimatorController: {pair.Key.name} -> {pair.Value.name}");
            //     }
            // }

            foreach (AnimationClip clip in clips)
            {
                if (AnimPack.AnimatorOverrideController != null && overrideMap.TryGetValue(clip, out AnimationClip originalClip))
                {
                    // For override clips, map the override clip name to the original clip's hash
                    _animationHashes[clip.name] = Animator.StringToHash(originalClip.name);
                }
                else
                {
                    // Regular case - use the clip's own name for the hash
                    _animationHashes[clip.name] = Animator.StringToHash(clip.name);
                }

                PikAnimationState state = new PikAnimationState(clip);
                state.IsOneShot = OneShotClips.Contains(clip);
                if (state.IsOneShot)
                {
                    state.TransitionDuration = 0;
                }
                AnimationStates.Add(state);
            }
        }

        protected virtual void SetupDefaultAnimationConditions()
        {
            // Add default conditions with priorities (higher number = higher priority)
            if (AnimPack.EditorLayingAnim != null)
            {
                LayingCondition = new AnimationCondition(() => IsLaying, AnimPack.EditorLayingAnim, 40);
                AddAnimationCondition(LayingCondition);
            }

            if (AnimPack.EditorLatchedAttackAnim != null)
            {
                AttackingCondition = new AnimationCondition(() => IsAttacking, AnimPack.EditorLatchedAttackAnim, 30);
                AddAnimationCondition(AttackingCondition);
            }

            if (AnimPack.EditorCarryAnim != null)
            {
                CayingCondition = new AnimationCondition(() => IsCarrying, AnimPack.EditorCarryAnim, 20);
                AddAnimationCondition(CayingCondition);
            }

            if (AnimPack.EditorWalkingAnim != null)
            {
                WalkingCondition = new AnimationCondition(() => IsMoving && IsPlayingIdleAnim, () => IsMoving, AnimPack.EditorWalkingAnim, 10);
                AddAnimationCondition(WalkingCondition);
            }

            // Sort conditions by priority (descending)
            _animationConditions = _animationConditions.OrderByDescending(c => c.Priority).ToList();
        }

        private void FillNullAnims()
        {
            if (!AnimPack.FillEmptyFields)
            {
                return;
            }

            AnimationClip emptyClip = LethalMin.assetBundle.LoadAsset<AnimationClip>("Assets/LethalMin/EmptyClip.anim");

            FieldInfo[] fields = AnimPack.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(AnimationClip) && field.GetValue(AnimPack) == null)
                {
                    // If the field is null, assign the empty clip
                    field.SetValue(AnimPack, emptyClip);
                    LethalMin.Logger.LogInfo($"PikminAnimatorController: Assigned empty animation clip to {field.Name}");
                }
                else if (field.FieldType == typeof(List<AnimationClip>))
                {
                    List<AnimationClip> clipList = (List<AnimationClip>)field.GetValue(AnimPack);
                    if (clipList != null && clipList.Count == 0)
                    {
                        clipList.Add(emptyClip);
                        LethalMin.Logger.LogInfo($"PikminAnimatorController: Assigned empty animation clip to {field.Name}");
                    }
                }
            }
        }

        // Public method to add new animation conditions from other scripts
        public void AddAnimationCondition(AnimationCondition condition)
        {
            _animationConditions.Add(condition);
            // Keep the list sorted by priority (descending)
            _animationConditions = _animationConditions.OrderByDescending(c => c.Priority).ToList();
        }
        public void AddAnimationCondition(System.Func<bool> condition, AnimationClip animation, int priority)
        {
            _animationConditions.Add(new AnimationCondition(condition, animation, priority));
            // Keep the list sorted by priority (descending)
            _animationConditions = _animationConditions.OrderByDescending(c => c.Priority).ToList();
        }
        public void AddAnimationCondition(System.Func<bool> enterCondition, System.Func<bool> exitCondition, AnimationClip animation, int priority)
        {
            _animationConditions.Add(new AnimationCondition(enterCondition, exitCondition, animation, priority));
            // Keep the list sorted by priority (descending)
            _animationConditions = _animationConditions.OrderByDescending(c => c.Priority).ToList();
        }

        // Public method to remove a condition
        public void RemoveAnimationCondition(AnimationClip animation)
        {
            _animationConditions.RemoveAll(c => c.Animation == animation);
        }

        public virtual void LateUpdate()
        {
            // Check if the animator is null or not initialized
            if (animator == null || !animator.isInitialized) return;

            if (!AnimPack.UseStateMachineForIdleAnims && _currentState.Item2 != null && AnimPack.EditorIdleAnim.Contains(_currentState.Item2))
            {
                if (IdleAnim != null && lastIdleAnim != IdleAnim)
                {
                    lastIdleAnim = IdleAnim;
                    PlayAnimation(IdleAnim.name);
                }
            }

            if (PlayAnimCalled) return;

            // Check for animation conditions in priority order
            bool animationPlayed = false;
            foreach (var condition in _animationConditions)
            {
                if (!condition.enabled) continue;
                if (condition.EnterCondition() && _currentState.Item2 != condition.Animation)
                {
                    PlayAnimation(condition.Animation.name);
                    animationPlayed = true;
                    //LethalMin.Logger.LogInfo($"{gameObject.name}: Animation condition '{condition.Animation.name}' is true. Playing animation: {condition.Animation.name}");
                    break;
                }
            }

            if (!animationPlayed)
            {
                // Return to idle if no longer in special states
                bool shouldReturnToIdle = false;

                // Check if we're in a state but its condition is no longer true
                foreach (var condition in _animationConditions)
                {
                    if (!condition.enabled) continue;
                    if (!condition.ExitCondition() && _currentState.Item2 == condition.Animation)
                    {
                        shouldReturnToIdle = true;
                        //LethalMin.Logger.LogInfo($"{gameObject.name}: Animation condition '{condition.Animation.name}' is no longer true. Returning to idle.");
                        break;
                    }
                }

                if (shouldReturnToIdle)
                {
                    ResetToIdleAnim();
                }
            }
        }

        public void resetRandomIdle()
        {
            //LethalMin.Logger.LogInfo($"{GetComponentInParent<PikminAI>().DebugID}: Resetting random idle animation.");
            RandomIdle = 0;
            if (AnimPack.UseStateMachineForIdleAnims)
            {
                animator.CrossFade(AnimPack.IdleAnimStateMachineEntryName, 0.2f);
            }
            else if (AnimPack.EditorIdleAnim.Count != 0)
            {
                PlayAnimation(AnimPack.EditorIdleAnim[0].name);
            }
        }

        public virtual void PlayLandAnim()
        {
            if (CurrentAnimation == AnimPack.EditorKnockbackAnim)
            {
                if (AnimPack.EditorLayingAnim != null)
                    PlayAnimation(AnimPack.EditorLayingAnim.name);
            }
            else
            {
                ResetToIdleAnim();
            }
        }

        public virtual void PlayAnimation(string animationName, float transitionDuration = 0.25f, int layer = 0)
        {
            PlayAnimCalled = true;
            if (animator == null)
            {
                PlayAnimCalled = false;
                return;
            }
            
            if (animationName == "Plucked")
            {
                animationName = AnimPack.EditorPluckedAnim?.name ?? "Plucked";
            }

            PikAnimationState state = GetAnimationStateFromNameViaHash(animationName);
            if (state.clip == null)
            {
                PlayAnimCalled = false;
                Debug.LogWarning($"{gameObject.name} Pik Animation State '{animationName}' not found");
                return;
            }

            int animHash = _animationHashes.TryGetValue(animationName, out var hash) ? hash : Animator.StringToHash(animationName);
            if (state.TransitionDuration != -1)
            {
                transitionDuration = state.TransitionDuration;
            }
            if (HasAnimation(animHash, layer))
            {
                //LethalMin.Logger.LogInfo($"{gameObject.name}: Playing animation: {animationName}");
                if (CurrentAnimation != null && CurrentAnimation.name == animationName)
                {
                    animator.Play(animHash, layer, 0.0f);
                }
                else
                {
                    animator.CrossFade(animHash, transitionDuration, layer);
                }
                _currentState = (layer, state.clip);
                if (AnimCheckCoroutine != null)
                {
                    StopCoroutine(AnimCheckCoroutine);
                    AnimCheckCoroutine = null;
                }
                AnimCheckCoroutine = StartCoroutine(CheckIfAnimationHasFinished(state));
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Animation '{animationName}' not found in layer {layer}. Could the state name be different from the clip name?");
            }
            PlayAnimCalled = false;
        }

        public virtual IEnumerator CheckIfAnimationHasFinished(PikAnimationState state, float offset = 0.0f)
        {
            yield return new WaitForSeconds(state.clip.length + offset);
            if (CurrentAnimation == state.clip && state.IsOneShot)
            {
                //LethalMin.Logger.LogInfo($"{gameObject.name}: Oneshot Animation '{state.clip.name}' finished playing. Resetting to idle.");
                ResetToIdleAnim();
            }
            AnimCheckCoroutine = null;
        }

        public PikAnimationState GetAnimationStateFromNameViaHash(string animationName)
        {
            foreach (PikAnimationState state in AnimationStates)
            {
                if (Animator.StringToHash(state.name) == Animator.StringToHash(animationName))
                {
                    return state;
                }
            }
            return default;
        }

        public PikAnimationState GetAnimationStateFromName(string animationName)
        {
            foreach (PikAnimationState state in AnimationStates)
            {
                if (state.name == animationName)
                {
                    return state;
                }
            }
            return default;
        }

        public void ResetToIdleAnim()
        {
            //LethalMin.Logger.LogInfo($"{gameObject.name}: Resetting to idle animation.");
            if (AnimPack.EditorIdleAnim.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: No idle animation found in the animation pack.");
                return;
            }
            if (AnimPack.UseStateMachineForIdleAnims)
            {
                IdleAnim = AnimPack.EditorIdleAnim[0];
                animator.CrossFade(AnimPack.IdleAnimStateMachineEntryName, 0.2f);
                _currentState = (0, IdleAnim);
            }
            else
            {
                IdleAnim = AnimPack.EditorIdleAnim[0];
                int animHash = _animationHashes.TryGetValue(IdleAnim.name, out var hash) ? hash : Animator.StringToHash(IdleAnim.name);
                animator.CrossFade(animHash, 0.2f);
                _currentState = (0, IdleAnim);
            }
        }

        public bool HasAnimation(int animationHash, int layer = 0)
        {
            return animator.HasState(layer, animationHash);
        }
    }
}
