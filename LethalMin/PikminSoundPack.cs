using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    [CreateAssetMenu(menuName = "LethalMin/PikminSoundPack", order = 1)]
    public class PikminSoundPack : ScriptableObject
    {
        [Tooltip("The sound the pikmin plays before attacking")]
        public AudioClip[] AttackVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when spawned in")]
        public AudioClip[] BornVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays for exiting the onion")]
        public AudioClip[] ExitOnionVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays for entering the onion")]
        public AudioClip[] EnterOnionVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when it spots an item")]
        public AudioClip[] ItemNoticeVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when it dies")]
        public AudioClip[] GhostVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays while carrying an item")]
        public AudioClip[] CarryVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when losing their leader")]
        public AudioClip[] LostVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when they start carrying an item")]
        public AudioClip[] LiftVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when it gets hurt")]
        public AudioClip[] HurtVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when it gets Crushed")]
        public AudioClip[] CrushedVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when spotting a player")]
        public AudioClip[] NoticeVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when thrown")]
        public AudioClip[] ThrowVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays when being held by a player")]
        public AudioClip[] HoldVoiceLine = new AudioClip[0];

        [Tooltip("The sound the pikmin plays for compleating action")]
        public AudioClip[] YayVoiceLine = new AudioClip[0];

        [Tooltip("the 'woosh' sound that plays when a pikmin is thrown, overridden by mod if left empty")]
        public AudioClip[] ThrowSFX = new AudioClip[0];

        [Tooltip("the hit sound that plays when a pikmin lands a hit on an enemy, overridden by mod if left empty")]
        public AudioClip[] HitSFX = new AudioClip[0];

        public bool FillEmptyWithDefault = false;
    }
}