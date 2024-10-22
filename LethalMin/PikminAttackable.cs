using UnityEngine;
namespace LethalMin
{
    public struct PikminAttackable
    {
        public float AttackRange;
        public int AttackBuffer;
        public int MaxPikminEatCount;
        public int[] AttackStates;
        public bool AttackInAnyState;
        public string PikminGrabPath;
        public Transform PikminGrabTransform;
        public AudioClip AttackSound;
        public string AttackAnimName;
        public string AttackAnimTrigger;
        public bool HarmfulToPikmin;
        public bool CheckAtGrabPos;

        public PikminAttackable(float attackRange, int attackBuffer, int maxPikminEatCount, int[] attackStates, bool attackInAnyState, string pikminGrabPath, Transform pikminGrabTransform, AudioClip attackSound, string attackAnimName, string attackAnimTrigger, bool harmfulToPikmin = true)
        {
            AttackRange = attackRange;
            AttackBuffer = attackBuffer;
            MaxPikminEatCount = maxPikminEatCount;
            AttackStates = attackStates;
            AttackInAnyState = attackInAnyState;
            PikminGrabPath = pikminGrabPath;
            PikminGrabTransform = pikminGrabTransform;
            AttackSound = attackSound;
            AttackAnimName = attackAnimName;
            AttackAnimTrigger = attackAnimTrigger;
            HarmfulToPikmin = harmfulToPikmin;
        }
    }
}
