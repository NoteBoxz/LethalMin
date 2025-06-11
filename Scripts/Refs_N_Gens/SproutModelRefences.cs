using System.Collections.Generic;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin.Pikmin
{
    public class SproutModelRefences : MonoBehaviour
    {
        [Tooltip("The different mesh refernces for other generations, if you don't have multiple generations, set this to empty")]
        public SproutModelGeneration[] Generations = new SproutModelGeneration[] { };
        [Tooltip("The main Sprout's model for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Sprout's animator component")]
        public Animator Animator = null!;
        [Tooltip("The Sprout's main renderer component")]
        public Renderer MainRenderer = null!;
        [Tooltip("Starts from Least to Greatest (0 = leaf, 2 = flower)")]
        public List<GameObject> Plants = new List<GameObject>();
        [Tooltip("0 = Default, 1 = Red, 2 = Purple, 3 = Yellow")]
        public List<Material> AltBudMaterials = new List<Material>();
        [Tooltip("0 = Default, 1 = Red, 2 = Purple, 3 = Yellow")]
        public List<Material> AltFlowerMaterials = new List<Material>();
        [Tooltip("The Sprout's bones for VR interaction")]
        public List<Transform> SproutBones = new List<Transform>();
        [Tooltip("The Sprout's bones for VR interaction (Colider not rendered)")]
        public GameObject SproutVRInteractableObject = null!;
    }
}