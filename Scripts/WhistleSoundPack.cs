using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LethalMin
{
    [CreateAssetMenu(fileName = "WhistleSoundPack", menuName = "Pikmin/WhistlePack", order = 0)]
    public class WhistleSoundPack : ScriptableObject
    {
        public AudioClip StartSound = null!;
        public AudioClip LoopSound = null!;
        public AudioClip DismissSound = null!;
    }
}