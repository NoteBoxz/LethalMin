using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public class PikminSproutFXManager : MonoBehaviour
    {
        public PikminAI minmin = null!;
        public Transform Top = null!;
        [SerializeField] Animator GlowAnim = null!;
        [SerializeField] SpriteRenderer GlowSprt = null!;
        [SerializeField] ParticleSystem FireParticles = null!;
        [SerializeField] ParticleSystem GasParticles = null!;

        void Start()
        {
            if (FireParticles == null)
            {
                GameObject FireInstance = Instantiate(LethalMin.PikminFirePrefab, Top);
                FireParticles = FireInstance.GetComponentInChildren<ParticleSystem>();
            }
            if (GasParticles == null)
            {
                GameObject GasInstance = Instantiate(LethalMin.PikminGasPrefab, Top);
                GasParticles = GasInstance.GetComponentInChildren<ParticleSystem>();
            }
            if (GlowAnim == null || GlowSprt == null)
            {
                GameObject GlowInstance = Instantiate(LethalMin.GlowPrefab, Top);
                GlowAnim = GlowInstance.GetComponentInChildren<Animator>();
                GlowSprt = GlowInstance.GetComponentInChildren<SpriteRenderer>();
            }

            FireParticles.Stop();
            GasParticles.Stop();
        }
        void Update()
        {
            if (minmin == null)
            {
                return;
            }
            GlowAnim.SetBool("Glow", minmin.currentBehaviourStateIndex == PikminAI.IDLE && minmin.CurrentIntention == Pintent.Idle && !minmin.Laying || minmin.ForceSproutGlow);
            GlowSprt.color = minmin.pikminType.UseOverrideSproutGlowColor ? minmin.pikminType.OverrideSproutGlowColor : minmin.pikminType.PikminPrimaryColor;

            switch (minmin.CurPanicAnim)
            {
                case "FirePanic":
                    if (!FireParticles.isPlaying)
                        FireParticles.Play();
                    break;
                case "PoisonPanic":
                    if (!GasParticles.isPlaying)
                        GasParticles.Play();
                    break;
                default:
                    if (FireParticles.isPlaying)
                        FireParticles.Stop();
                    if (GasParticles.isPlaying)
                        GasParticles.Stop();
                    break;
            }
        }
    }
}
