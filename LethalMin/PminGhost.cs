using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace LethalMin
{
    public class PminGhost : MonoBehaviour
    {
        public GameObject GhostOBJ;
        private Animator animator;
        public PikminType pmintype;

        void Start()
        {
            LethalMin.Logger.LogInfo(pmintype);

            animator = GetComponent<Animator>();
            GhostOBJ = transform.Find("WavyPlane").gameObject;
            GhostOBJ.AddComponent<LookAtMainCamera>();
            //like = animator.runtimeAnimatorController.animationClips[0].length;
            //AnimationClip clip = animator.runtimeAnimatorController.animationClips[0];

            // Add an event at the end of the animation
            //AnimationEvent animEvent = new AnimationEvent();
            //animEvent.time = clip.length;
            //animEvent.functionName = "DestroyObject";
            //clip.AddEvent(animEvent);
            GhostOBJ.GetComponent<Renderer>().material.color = LethalMin.GetColorFromPType(pmintype);
            if (pmintype.soundPack == null)
            {
                GetComponent<AudioSource>().PlayOneShot(LethalMin.GhostSFX[Random.Range(0, LethalMin.GhostSFX.Length)]);
            }
            else
            {
                if (pmintype.soundPack.GhostVoiceLine.Length == 0)
                {
                    return;
                }
                GetComponent<AudioSource>().PlayOneShot(pmintype.soundPack.GhostVoiceLine[Random.Range(0, pmintype.soundPack.GhostVoiceLine.Length)]);
            }
            StartCoroutine(DestroyObject());
        }

        IEnumerator DestroyObject()
        {
            yield return new WaitForSeconds(20f);
            Destroy(gameObject);
        }
    }
}