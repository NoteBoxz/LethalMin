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

        public int InSquadCount
        {
            get
            {
                return PikminManager.instance.LocalLeader.GetPikminTypesInSquadWithCount().TryGetValue(type, out int count) ? count : 0;
            }
        }
        /// <summary>
        /// Count of Pikmin of this type in the onion, not counting the ones being exchanged.
        /// </summary>
        public int InOnionCount
        {
            get
            {
                Onion onion = OnionHUDManager.instance.currentOnion;
                int val = 0;
                foreach (var pdat in onion.PikminInOnion)
                {
                    if (pdat.TypeID == type.PikminTypeID)
                    {
                        val++;
                    }
                }
                return val;
            }
        }
        /// <summary>
        /// Count of Pikmin of this type in the field, not counting the ones being exchanged.
        /// </summary>
        public int InFieldCount
        {
            get
            {
                int val = 0;
                foreach (var p in PikminManager.instance.PikminAICounter)
                {
                    if (p.pikminType == type)
                    {
                        val++;
                    }
                }
                return val;
            }
        }
        /// <summary>
        /// Posotive for removing from onion, negative for adding to onion.
        /// </summary>
        public int ExchangingCount
        {
            get
            {
                Onion onion = OnionHUDManager.instance.currentOnion;
                if (onion.TypesToExchange.ContainsKey(type))
                {
                    return onion.TypesToExchange[type];
                }
                else
                {
                    LethalMin.Logger.LogWarning($"(get) OnionHUDSlot: SlotExchanging: {type.PikminName} not found in TypesToExchange dictionary.");
                }
                return 0;
            }
            set
            {
                Onion onion = OnionHUDManager.instance.currentOnion;
                if (onion.TypesToExchange.ContainsKey(type))
                {
                    onion.TypesToExchange[type] = value;
                }
                else
                {
                    LethalMin.Logger.LogWarning($"(set) OnionHUDSlot: SlotExchanging: {type.PikminName} not found in TypesToExchange dictionary.");
                }
            }
        }
        /// <summary>
        /// Total count of Pikmin of this type being exchanged across all slots.
        /// </summary>
        public int TotalExchangeCount
        {
            get
            {
                Onion onion = OnionHUDManager.instance.currentOnion;
                int val = 0;
                foreach (int value in onion.TypesToExchange.Values)
                {
                    val += value;
                }
                return val;
            }
        }
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
            int inOnionCount = InOnionCount;
            int inFieldCount = InFieldCount;
            int inSquadCount = InSquadCount;
            int PredictedInOnionCount = inOnionCount - ExchangingCount;
            int PredictedInTotalCount = inOnionCount + inFieldCount;
            int PredictedInSquadCount = inSquadCount + ExchangingCount;
            int PredictedInFieldCount = PikminManager.instance.PikminAICounter.Count + TotalExchangeCount;


            InOnionCounter.text = PredictedInOnionCount.ToString();
            InTotalCounter.text = PredictedInTotalCount.ToString();
            InSquadCounter.text = PredictedInSquadCount.ToString();

            if (LethalMin.GrayoutButtonsInOnionHUD)
            {
                DownButton.SetVisuallyDisabled(PredictedInOnionCount == 0 || PredictedInFieldCount >= LethalMin.MaxPikmin);
                UpButton.SetVisuallyDisabled(PredictedInSquadCount == 0);
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
        public void UpPressed(int count = 1)
        {
            if (OnionHUDManager.instance.TenWithdrawAction.IsPressed())
            {
                count = 10;
            }
            
            Dictionary<PikminType, int> Dict = PikminManager.instance.LocalLeader.GetPikminTypesInSquadWithCount();
            int inSquadCount = Dict.ContainsKey(type) ? Dict[type] : 0;
            int PredictedInSquadCount = inSquadCount - (-ExchangingCount + count);
            LethalMin.Logger.LogDebug($"OnionHUDSlot: UpPressed: (x{count}) {type.PikminName} (IS:{inSquadCount}) (EC:{ExchangingCount}) (PIC:{PredictedInSquadCount})");

            if (PredictedInSquadCount < 0 && count > 1)
            {
                count = Mathf.Max(1, count + PredictedInSquadCount);
                PredictedInSquadCount = inSquadCount - (-ExchangingCount + count);
                LethalMin.Logger.LogDebug($"OnionHUDSlot: UpPressed: Adjusted count to {count} for {type.PikminName} due to negative squad count.");
            }   
              
            if (PredictedInSquadCount < 0)
            {
                LethalMin.Logger.LogDebug($"__OnionHUDSlot: UpPressed: {type.PikminName} cannot withdraw more than you have in your squad.");
                OnionHUDManager.instance.audio.Stop();
                OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.DenyDownAC);
                return;
            }

            OnionHUDManager.instance.audio.Stop();
            OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.DownAC);
            ExchangingCount -= count;
        }
        public void DownPressed(int count = 1)
        {
            if (OnionHUDManager.instance.TenWithdrawAction.IsPressed())
            {
                count = 10;
            }

            Onion onion = OnionHUDManager.instance.currentOnion;
            int inOnionCount = onion.PikminInOnion.Count(pdat => pdat.TypeID == type.PikminTypeID);
            int PredictedInOnionCount = inOnionCount - (ExchangingCount + count);
            int PredictedInFieldCount = PikminManager.instance.PikminAICounter.Count + TotalExchangeCount + count;
            LethalMin.Logger.LogDebug($"OnionHUDSlot: DownPressed: (x{count}) {type.PikminName} (IO:{inOnionCount}) (EC:{ExchangingCount}) (PIC:{PredictedInOnionCount}) (PIF:{PredictedInFieldCount})");

            if (PredictedInOnionCount < 0 && count > 1)
            {
                count = Mathf.Max(1, count + PredictedInOnionCount);
                PredictedInOnionCount = inOnionCount - (ExchangingCount + count);
                LethalMin.Logger.LogDebug($"_OnionHUDSlot: DownPressed: Adjusted count to {count} for {type.PikminName} due to negative onion count.");
            }

            if(PredictedInFieldCount > LethalMin.MaxPikmin && count > 1)
            {
                count = Mathf.Max(1, count - (PredictedInFieldCount - LethalMin.MaxPikmin));
                PredictedInFieldCount = PikminManager.instance.PikminAICounter.Count + TotalExchangeCount + count;
                LethalMin.Logger.LogDebug($"_OnionHUDSlot: DownPressed: Adjusted count to {count} for {type.PikminName} due to exceeding max pikmin in field.");
            }

            if (PredictedInFieldCount > LethalMin.MaxPikmin || PredictedInOnionCount < 0)
            {
                string reason = PredictedInFieldCount > LethalMin.MaxPikmin
                    ? "cannot exchange more than you have in the field"
                    : "cannot exchange more than you have in your onion";

                LethalMin.Logger.LogDebug($"__OnionHUDSlot: DownPressed: {type.PikminName} {reason}.");
                OnionHUDManager.instance.audio.Stop();
                OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.DenyUpAC);
                return;
            }

            OnionHUDManager.instance.audio.Stop();
            OnionHUDManager.instance.audio.PlayOneShot(OnionHUDManager.instance.UpAC);
            ExchangingCount += count;
        }
    }
}