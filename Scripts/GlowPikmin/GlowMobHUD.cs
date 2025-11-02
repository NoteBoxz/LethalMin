using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class GlowMobHUD : MonoBehaviour
    {
        public Image fillImg = null!;
        public PikminHUDElement element = null!;
        public Animator anim = null!;
        public GameObject Check = null!;
        public GameObject Clock = null!;
        public bool HasReachedFull = false;

        public void Update()
        {
            if (PikminManager.instance == null)
                return;
            Leader leader = PikminManager.instance.LocalLeader;
            if (leader == null || leader.glowmob == null)
            {
                return;
            }

            element.targetAlpha = leader.glowmob.IsDoingGlowmob || leader.glowmob.IsOnCooldown ? 1 : 0;
            Check.SetActive(leader.glowmob.IsReady && !leader.glowmob.IsOnCooldown);
            Clock.SetActive(leader.glowmob.IsOnCooldown);

            if (!HasReachedFull && leader.glowmob.IsReady)
            {
                HasReachedFull = true;
                anim.Play("GlowReady");
                leader.glowmob.OnReadyServerRpc();
                leader.glowmob.OnReady();
            }
            if (HasReachedFull && !leader.glowmob.IsReady)
            {
                HasReachedFull = false;
            }

            if (leader.glowmob.IsDoingGlowmob || leader.glowmob.IsOnCooldown)
            {
                if (leader.glowmob.IsDoingGlowmob)
                    fillImg.fillAmount = leader.glowmob.GlowMobProgress;
                if (leader.glowmob.IsOnCooldown)
                    fillImg.fillAmount = leader.glowmob.Cooldown / leader.glowmob.MaxCooldown;
            }
            else
            {
                fillImg.fillAmount = 0;
            }
        }
    }
}