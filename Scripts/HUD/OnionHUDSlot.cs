using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class OnionHUDSlot : MonoBehaviour
    {
        public PikminType type = null!;
        public Image BG = null!, BG2 = null!, Icon = null!;
        public TMP_Text InOnionCounter = null!, InSquadCounter = null!, PikminNameTitle = null!, InTotalCounter = null!;
        public BetterButton UpButton = null!, DownButton = null!;
        public int InOnionCount = -1;
        public int InFieldCount = -1;
        //public int WithdrawingAmmount;
        private float AddTimer, SubtractTimer;

        void LateUpdate()
        {
            if (type == null)
            {
                return;
            }
            Color color = new Color(type.PikminPrimaryColor.r, type.PikminPrimaryColor.g, type.PikminPrimaryColor.b, 0.5f);
            BG.color = color;
            BG2.color = color;
            Icon.sprite = type.PikminIcon;
            PikminNameTitle.text = type.PikminName;

            Onion onion = OnionHUDManager.instance.currentOnion;
            int WithdrawingAmmount = onion.TypesToWithdraw[type];

            InOnionCount = 0;
            foreach (var pdat in onion.PikminInOnion)
            {
                if (pdat.TypeID == type.PikminTypeID)
                {
                    InOnionCount++;
                }
            }

            InFieldCount = 0;
            foreach (var p in PikminManager.instance.PikminAICounter)
            {
                if (p.pikminType == type)
                {
                    InFieldCount++;
                }
            }

            if (onion.TypesToWithdraw[type] > InOnionCount)
            {
                onion.TypesToWithdraw[type]--;
            }

            InOnionCounter.text = (InOnionCount - WithdrawingAmmount).ToString();
            InTotalCounter.text = (InOnionCount + InFieldCount).ToString();

            if (PikminManager.instance.LocalLeader.PikminInSquad.Count != 0 && PikminHUDManager.instance.TypesWithCounts.ContainsKey(type))
            {
                InSquadCounter.text = (PikminHUDManager.instance.TypesWithCounts[type] + WithdrawingAmmount).ToString();
            }
            else
            {
                InSquadCounter.text = WithdrawingAmmount.ToString();
            }
        }
        void Update()
        {
            if (UpButton.IsButtonHeld())
            {
                AddTimer += Time.deltaTime;
                if (AddTimer >= 0.05f)
                {
                    UpPressed();
                    AddTimer = 0;
                }
            }

            if (DownButton.IsButtonHeld())
            {
                SubtractTimer += Time.deltaTime;
                if (SubtractTimer >= 0.05f)
                {
                    DownPressed();
                    SubtractTimer = 0;
                }
            }
        }
        public void UpPressed()
        {
            Onion onion = OnionHUDManager.instance.currentOnion;
            int inSquadCount = PikminHUDManager.instance.TypesWithCounts.ContainsKey(type)
                ? PikminHUDManager.instance.TypesWithCounts[type]
                : 0;

            // Don't allow returning more Pikmin than what's in the leader's squad
            // Also prevent going below 0 when there are no Pikmin in squad
            if (onion.TypesToWithdraw[type] > 0 || (-onion.TypesToWithdraw[type]) < inSquadCount)
            {
                OnionHUDManager.instance.audio.Stop();
                OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.DownAC);
                onion.TypesToWithdraw[type] -= 1;
            }
            else if (!OnionHUDManager.instance.audio.isPlaying)
            {
                OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.DenyDownAC);
            }

            // if (onion.TypesToWithdraw[type] < 0)
            // {
            //     onion.TypesToWithdraw[type] = 0; // Prevent negative withdraw amounts
            //     LethalMin.Logger.LogWarning($"OnionHUDSlot: {type.PikminName} withdraw amount is negative! Resetting to 0.");
            // }
        }
        public void DownPressed()
        {
            Onion onion = OnionHUDManager.instance.currentOnion;

            int totalTypesToWithdraw = onion.TypesToWithdraw.Values.Sum();

            // Don't allow withdrawing more than what's available in the onion and ensure total does not exceed MaxPikmin
            if ((InOnionCount - onion.TypesToWithdraw[type]) > 0 &&
            totalTypesToWithdraw + PikminManager.instance.PikminAICounter.Count < LethalMin.MaxPikmin)
            {
                OnionHUDManager.instance.audio.Stop();
                OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.UpAC);
                onion.TypesToWithdraw[type] += 1;
            }
            else if (!OnionHUDManager.instance.audio.isPlaying)
            {
                OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.DenyUpAC);
            }
        }
    }
}