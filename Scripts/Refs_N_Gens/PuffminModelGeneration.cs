using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace LethalMin.Pikmin
{
    public class PuffminModelGeneration : MonoBehaviour
    {
        [Tooltip("The Pikmin's generation, if you don't have multiple generations, set this to Default.")]
        public PikminGeneration Generation = PikminGeneration.Pikmin4;
        [Tooltip("The main Puffmin model for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Puffmin's animator component")]
        public Animator Animator = null!;
        [Tooltip("The Puffmin's body renderer (for changing colors)")]
        public Renderer BodyRenderer = null!;
        [Tooltip("The Puffmin big eyes gameobject from Hey! Pikmin")]
        public GameObject HeyPikminBigEyes = null!;
    }
}