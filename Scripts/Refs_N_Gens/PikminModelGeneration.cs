using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace LethalMin.Pikmin
{
    public class PikminModelGeneration : MonoBehaviour
    {
        [Tooltip("The Pikmin's generation, if you don't have multiple generations, set this to Default.")]
        public PikminGeneration Generation = PikminGeneration.Pikmin4;
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

        [ContextMenu("Autofill")]
        public void Autofill()
        {
            if (Animator == null)
            {
                Animator = GetComponent<Animator>();
            }
            if (AnimatorController == null)
            {
                AnimatorController = GetComponent<PikminAnimatorController>();
            }
            if (Model == null)
            {
                Model = gameObject;
            }

            // Clear existing Plants list to avoid duplicates
            Plants.Clear();

            // Start recursive search
            SearchChildrenRecursively(transform);
        }

        private void SearchChildrenRecursively(Transform parent)
        {
            foreach (Transform child in parent)
            {
                // Check the current child
                if (child.name == "H_j003" || child.name == "loc_point_01")
                {
                    SproutTop = child.gameObject;
                }
                if (child.name == "piki_bigeye_0_0_node")
                {
                    HeyPikminBigEyes = child.gameObject;
                }
                if (child.name.ToLower().Contains("leaf"))
                {
                    Plants.Add(child.gameObject);
                }
                if (child.name.ToLower().Contains("bud"))
                {
                    Plants.Add(child.gameObject);
                }
                if (child.name.ToLower().Contains("flower"))
                {
                    Plants.Add(child.gameObject);
                }

                // Recursively search this child's children
                SearchChildrenRecursively(child);
            }
        }
    }
}