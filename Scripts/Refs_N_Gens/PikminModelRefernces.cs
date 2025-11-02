using System.Collections.Generic;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin.Pikmin
{
    public class PikminModelRefernces : MonoBehaviour
    {
        [Tooltip("The different mesh refernces for other generations, if you don't have multiple generations, set this to empty")]
        public PikminModelGeneration[] Generations = new PikminModelGeneration[] { };
        [Tooltip("The main Pikmin model for the current generation")]
        public GameObject Model = null!;
        [Tooltip("The Pikmin's animator component")]
        public Animator Animator = null!;
        [Tooltip("The Pikmin's animator controller")]
        public PikminAnimatorController AnimatorController = null!;
        [Tooltip("The top part of the Pikmin's sprout")]
        public GameObject SproutTop = null!;
        [Tooltip("Starts from Least to Greatest (0 = leaf, 2 = flower)")]
        public List<GameObject> Plants = new List<GameObject>();
        [Tooltip("The Pikmin's big eyes gameobject from Hey! Pikmin")]
        public GameObject HeyPikminBigEyes = null!;
        [Tooltip("(Optional) The audio sources that will override the Pikmin's voice")]
        public AudioSource? OverrideVoice;
        [Tooltip("(Optional) The audio sources that will override the Pikmin's SFX")]
        public AudioSource? OverrideSFX;
        [HideInInspector]
        public GrowthObjectCache growthObjectsCache = null!;

        public Dictionary<int, List<GameObject>> GetGrowthObjects()
        {
            return growthObjectsCache.ConvertCacheToDictionary();
        }

        public void SetGrowthObjects(Dictionary<int, List<GameObject>> newGrowthObjects)
        {
            if (growthObjectsCache == null)
            {
                growthObjectsCache = gameObject.AddComponent<GrowthObjectCache>();
            }
            
            growthObjectsCache.ConvertDictionaryToCache(newGrowthObjects);
        }
    }
}