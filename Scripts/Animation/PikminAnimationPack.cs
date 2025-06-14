using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Utils;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "PikminAnimationPack", menuName = "Pikmin/AnimationPack", order = 0)]
    public class PikminAnimationPack : ScriptableObject
    {
        public bool FillEmptyFields = false; // Fill empty fields with blank animations to prevent null references
        public bool UseStateMachineForIdleAnims = false; // Use state machine for idle animations
        public string IdleAnimStateMachineEntryName = "Entry"; // Entry name for idle animation state machine
        [Tooltip("(Not Required) If your model uses an AnimatorOverride Controller, assign it here.")]
        public AnimatorOverrideController? AnimatorOverrideController = null; // Animator override controller reference
        public List<AnimationClip> EditorIdleAnim = new(); // List of idle animations
        public List<AnimationClip> EditorOneShotIdleAnim = new(); // List of oneshot-idle animations
        public AnimationClip? EditorWalkingAnim = null; // Walking animation
        public AnimationClip? EditorCarryAnim = null; // Carry animation
        public AnimationClip? EditorNoticeAnim = null; // Notice animation
        public AnimationClip? EditorHoldAnim = null; // Held animation
        public AnimationClip? EditorThrowAnim = null; // Thrown animation
        public AnimationClip? EditorStandingAttackAnim = null; // Standing attack animation
        public AnimationClip? EditorLatchedAttackAnim = null; // Latched attack animation
        public AnimationClip? EditorKnockbackAnim = null; // Knockback animation
        public AnimationClip? EditorLayingAnim = null; // Laying animation
        public AnimationClip? EditorGetUpAnim = null; // Get Up animation
        public List<AnimationClip> EditorYayAnim = new(); // List of yay animations
        public AnimationClip? EditorPluckedAnim = null; // spawn animation
        public List<AnimationClip> EditorMiscOneshotAnim = new(); // List of AltSpawn animations
        public AnimationClip? EditorPosionFlopAnim = null; // Poisned animation
        public AnimationClip? EditorDrowingAnim = null; // Drowing animation
        public AnimationClip? EditorBurnAnim = null; // Burning animation

        static List<AnimationClip> prossedoneshotIdleAnims = new();
        public void AddEventsToOneshotIdleAnims()
        {
            foreach (AnimationClip OSclip in EditorOneShotIdleAnim)
            {
                if (prossedoneshotIdleAnims.Contains(OSclip))
                {
                    continue;
                }
                PikUtils.AddEventToFrame(OSclip.stopTime - 0.01f, "resetRandomIdle", OSclip);
                prossedoneshotIdleAnims.Add(OSclip);
            }
        }
    }
}