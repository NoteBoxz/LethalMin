using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace LethalMin.Pikmin
{
    public class OnionItemModelGeneration : MonoBehaviour
    {
        [Tooltip("The Onion Item's generation")]
        public PikminGeneration Generation = PikminGeneration.Pikmin4;
        [Tooltip("The main Onion's model for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Onion's animator component")]
        public Animator Animator = null!;
        [Tooltip("The Onion's main renderer")]
        public Renderer MainRenderer = null!;
    }
}