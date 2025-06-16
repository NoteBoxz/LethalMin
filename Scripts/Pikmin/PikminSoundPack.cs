using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LethalMin.Pikmin
{
    public enum PikminSoundPackSounds
    {
        Born,
        EnterOnion,
        ExitOnion,
        ObjectNotice,
        Lost,
        Yay,
        Idle,
        Notice,
        Prepare,
        Thrown,
        ThrownSFX,
        Attack,
        HitSFX,
        Knockback,
        GetUp,
        ItemLift,
        ItemCarry,
        Jump,
        Charge,
        Drowning,
        GhostSFX,
        Hurt,
        Cough,
        Burn,
        Crush,
        CrushSFX,
    }
    [CreateAssetMenu(fileName = "PikminSoundPack", menuName = "Pikmin/SoundPack", order = 0)]
    public class PikminSoundPack : ScriptableObject
    {
        public bool ReplaceEmptySFXWithDefault = true;
        public bool ReplaceEmptyVoiceWithDefault = false;
        public Dictionary<string, AudioClip[]> LookUpDict = new Dictionary<string, AudioClip[]>();
        [HideInInspector]
        public bool IsInitalized = false;
        public bool IsDefaultSoundPack = false;
        public AudioClip PullSoundFromDict(PikminSoundPackSounds key, System.Random rng)
        {
            return PullSoundFromDict(key.ToString(), rng);
        }
        public AudioClip PullSoundFromDict(string key, System.Random rng)
        {
            if (LookUpDict.ContainsKey(key))
            {
                AudioClip[] sounds = LookUpDict[key];
                return sounds[rng.Next(0, sounds.Length)];
            }
            else
            {
                LethalMin.Logger.LogError($"No sound found for key: {key}");
                return null!;
            }
        }

        public void InitalizeDict()
        {
            if (LookUpDict.Count > 0)
            {
                LethalMin.Logger.LogWarning($"Already intalized the look up dictionary for {name}");
                return;
            }

            // Use reflection to get all AudioClip[] fields
            var fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(AudioClip[]));

            foreach (var field in fields)
            {
                // Get the actual value of the field from this instance
                AudioClip[] clips = (AudioClip[])field.GetValue(this);

                // Remove "Voice" or "Sound" suffix if present
                string key = field.Name;

                if (key.EndsWith("Voice"))
                {
                    key = key.Substring(0, key.Length - 5);
                    if (clips.Length == 0 && !IsDefaultSoundPack && ReplaceEmptyVoiceWithDefault && LethalMin.DefaultSoundPack.IsInitalized)
                    {
                        field.SetValue(this, LethalMin.DefaultSoundPack.LookUpDict[key]);
                        clips = (AudioClip[])field.GetValue(this);
                    }
                }
                else if (key.EndsWith("Sound"))
                {
                    key = key.Substring(0, key.Length - 5);
                    key += "SFX";
                    if (clips.Length == 0 && !IsDefaultSoundPack && ReplaceEmptySFXWithDefault && LethalMin.DefaultSoundPack.IsInitalized)
                    {
                        field.SetValue(this, LethalMin.DefaultSoundPack.LookUpDict[key]);
                        clips = (AudioClip[])field.GetValue(this);
                    }
                }

                // Add to dictionary
                LookUpDict.Add(key, clips);
            }
            IsInitalized = true;
        }

        [ContextMenu("Log Dictionary")]
        public void LogDictionary()
        {
            // Use reflection to get all AudioClip[] fields
            var fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(AudioClip[]));

            string log = "";

            foreach (var field in fields)
            {
                string key = field.Name;
                // Remove "Voice" or "Sound" suffix if present
                if (key.EndsWith("Voice"))
                {
                    key = key.Substring(0, key.Length - 5);
                }
                else if (key.EndsWith("Sound"))
                {
                    key = key.Substring(0, key.Length - 5);
                    key += "SFX";
                }

                // Get the actual value of the field from this instance
                AudioClip[] clips = (AudioClip[])field.GetValue(this);

                log += key += "\n";
            }
            Debug.Log(log);
        }

        [Tooltip("This is used to determine which Pikmin types can use this sound pack. If this is the defult sound pack, then keep this as 'Any'")]
        public DisableablePikminGeneration TargetGeneration = DisableablePikminGeneration.Any;

        public AudioClip[] BornVoice = new AudioClip[0];
        public AudioClip[] EnterOnionVoice = new AudioClip[0];
        public AudioClip[] ExitOnionVoice = new AudioClip[0];
        public AudioClip[] ObjectNoticeVoice = new AudioClip[0];
        public AudioClip[] LostVoice = new AudioClip[0];
        public AudioClip[] YayVoice = new AudioClip[0];
        public AudioClip[] IdleVoice = new AudioClip[0];
        public AudioClip[] NoticeVoice = new AudioClip[0];
        public AudioClip[] PrepareVoice = new AudioClip[0];
        public AudioClip[] ThrownVoice = new AudioClip[0];
        public AudioClip[] AttackVoice = new AudioClip[0];
        public AudioClip[] KnockbackVoice = new AudioClip[0];
        public AudioClip[] GetUpVoice = new AudioClip[0];
        public AudioClip[] ItemLiftVoice = new AudioClip[0];
        public AudioClip[] ItemCarryVoice = new AudioClip[0];
        public AudioClip[] DrowningVoice = new AudioClip[0];
        public AudioClip[] HurtVoice = new AudioClip[0];
        public AudioClip[] CoughVoice = new AudioClip[0];
        public AudioClip[] BurnVoice = new AudioClip[0];
        public AudioClip[] CrushVoice = new AudioClip[0];
        public AudioClip[] JumpVoice = new AudioClip[0];
        public AudioClip[] ChargeVoice = new AudioClip[0];
        public AudioClip[] ThrownSound = new AudioClip[0];
        public AudioClip[] HitSound = new AudioClip[0];
        public AudioClip[] GhostSound = new AudioClip[0];
        public AudioClip[] CrushSound = new AudioClip[0];


        public bool IsKeyValid(string key, bool logWarnings = true)
        {
            if (LookUpDict.ContainsKey(key) && LookUpDict[key].Length > 0)
            {
                return true;
            }
            if (!LookUpDict.ContainsKey(key))
            {
                if (logWarnings)
                    LethalMin.Logger.LogWarning($"Invalid key: {key}. Please ensure the key exists in the PikminSoundPack asset.");
                return false;
            }
            if (LookUpDict[key].Length == 0)
            {
                if (logWarnings)
                    LethalMin.Logger.LogWarning($"No sounds found for key: {key}. Please ensure the key has at least one sound in the PikminSoundPack asset.");
                return false;
            }
            return false;
        }
    }
}