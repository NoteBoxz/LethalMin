using System.Collections.Generic;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin.Pikmin
{
    public class PuffminModelRefernces : MonoBehaviour
    {
        [Tooltip("The different mesh refernces for other generations, if you don't have multiple generations, set this to empty")]
        public PuffminModelGeneration[] Generations = new PuffminModelGeneration[] { };
        [Tooltip("The main Puffminmodel for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Puffmin animator component")]
        public Animator Animator = null!;
        [Tooltip("The Puffmin's body renderer (for changing colors)")]
        public Renderer BodyRenderer = null!;
        [Tooltip("The Puffmin big eyes gameobject from Hey! Pikmin")]
        public GameObject HeyPikminBigEyes = null!;
    }
}