using System.Collections.Generic;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin.Pikmin
{
    public class OnionItemModelRefernces : MonoBehaviour
    {
        [Tooltip("The different mesh refernces for other generations, if you don't have multiple generations, set this to empty")]
        public OnionItemModelGeneration[] Generations = new OnionItemModelGeneration[] { };
        [Tooltip("The main Onion's model for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Onion's animator component")]
        public Animator Animator = null!;
        [Tooltip("The Onion's main renderer")]
        public Renderer MainRenderer = null!;
    }
}