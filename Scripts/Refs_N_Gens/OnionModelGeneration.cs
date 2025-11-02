using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace LethalMin.Pikmin
{
    public class OnionModelGeneration : MonoBehaviour
    {
        [Tooltip("The Onion's generation")]
        public PikminGeneration Generation = PikminGeneration.Pikmin4;
        [Tooltip("The main Onion's model for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Onion's animator component")]
        public Animator Animator = null!;
        [Tooltip("The postions where the onion spits out sprouts")]
        public Transform SproutSpawnPos = null!;
        [Tooltip("The postions where item gets hovered into the onion")]
        public Transform ItemHoverPos = null!;
        [Tooltip("The postions where Pikmin bring items to the onion")]
        public Transform ItemDropPos = null!;
        [Tooltip("The onion's Beam (Must have a trigger colider!)")]
        public GameObject SummonBeam = null!;
        [Tooltip("The climbing links the pikmin use when going in/out of the onion")]
        public List<PikminLinkAnimation> ClimbLinks = null!;
        [Tooltip("The onion's sound pack")]
        public OnionSoundPack SoundPack = null!;
        [Tooltip("The onion's triangle renderer")]
        public Renderer MainOnionRenderer = null!;
        [Tooltip("The onion's fusion properties, this will be used to determine the fusion types for the onion")]
        public BaseOnionFusionProperties FusionProperties = null!;
    }
}