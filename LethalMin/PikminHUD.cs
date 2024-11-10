using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace LethalMin
{
    public enum HudPresets
    {
        Classic,
        New,
        Simplified
    }
    public class PikminHUD : MonoBehaviour
    {
        public LeaderManager LeaderScript;
        public HUDElement element;
        private TMP_Text PikminInSquad, PikminInField, PikminInExsistance, NextPikminCount, PrevPikminCount, CurPikminCount;
        private Image CurPikminBox, NextPikminBox, PrevPikminBox, CurPort, NextPort, PrevPort;
        private RectTransform NextPortRect, CurPortRect, PrevPortRect, PikminSelectedRect;
        private RectTransform PikminCountRect, PikminInSquadRect, PikminInFieldRect, PikminInExsistanceRect;
        public static PikminHUD pikminHUDInstance;
        private TMP_Text ThrowPrompt, LeftPrompt, RightPrompt;
        public GameObject WigglePrompt;
        public CanvasGroup PromptcanvasGroup, CountGroup;
        public HudPresets CurrentHUDPreset = HudPresets.New;

        void Awake()
        {
            if (pikminHUDInstance == null)
            {
                pikminHUDInstance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public void Start()
        {
            NextPikminCount = transform.Find("PikminSelected/Next/Counter").GetComponent<TMP_Text>();
            NextPort = transform.Find("PikminSelected/Next/Portrait").GetComponent<Image>();
            CurPikminCount = transform.Find("PikminSelected/Cur/Counter").GetComponent<TMP_Text>();
            CurPort = transform.Find("PikminSelected/Cur/Portrait").GetComponent<Image>();
            PrevPikminCount = transform.Find("PikminSelected/Prev/Counter").GetComponent<TMP_Text>();
            PrevPort = transform.Find("PikminSelected/Prev/Portrait").GetComponent<Image>();
            PikminInSquad = transform.Find("PikminCount/InSquad/Text (TMP)").GetComponent<TMP_Text>();
            PikminInField = transform.Find("PikminCount/In Field/Text (TMP)").GetComponent<TMP_Text>();
            PikminInExsistance = transform.Find("PikminCount/In Exisistance/Text (TMP)").GetComponent<TMP_Text>();
            CurPikminBox = transform.Find("PikminSelected/Cur/Image").GetComponent<Image>();
            NextPikminBox = transform.Find("PikminSelected/Next/Image").GetComponent<Image>();
            PrevPikminBox = transform.Find("PikminSelected/Prev/Image").GetComponent<Image>();

            NextPortRect = transform.Find("PikminSelected/Next").GetComponent<RectTransform>();
            CurPortRect = transform.Find("PikminSelected/Cur").GetComponent<RectTransform>();
            PrevPortRect = transform.Find("PikminSelected/Prev").GetComponent<RectTransform>();
            PikminSelectedRect = transform.Find("PikminSelected").GetComponent<RectTransform>();
            PikminCountRect = transform.Find("PikminCount").GetComponent<RectTransform>();
            PikminInSquadRect = transform.Find("PikminCount/InSquad").GetComponent<RectTransform>();
            PikminInFieldRect = transform.Find("PikminCount/In Field").GetComponent<RectTransform>();
            PikminInExsistanceRect = transform.Find("PikminCount/In Exisistance").GetComponent<RectTransform>();
            CountGroup = transform.Find("PikminCount").GetComponent<CanvasGroup>();

            WigglePrompt = transform.Find("PikminSelected/Prompts/Wiggle").gameObject;
            PromptcanvasGroup = transform.Find("PikminSelected/Prompts/Buttons").GetComponent<CanvasGroup>();
            ThrowPrompt = transform.Find("PikminSelected/Prompts/Buttons/ThrowPrompt").GetComponent<TMP_Text>();
            LeftPrompt = transform.Find("PikminSelected/Prompts/Buttons/SwitchL").GetComponent<TMP_Text>();
            RightPrompt = transform.Find("PikminSelected/Prompts/Buttons/SwitchR").GetComponent<TMP_Text>();

            SetHudPresets(CurrentHUDPreset);
        }
        public void SetHudPresets(HudPresets preset)
        {
            switch (preset)
            {
                case HudPresets.Classic:
                    // Set up classic HUD
                    NextPortRect.gameObject.SetActive(true);
                    PrevPortRect.gameObject.SetActive(true);
                    PikminInExsistanceRect.gameObject.SetActive(true);

                    LethalMin.PikminSelectedPosX = 262.78f;
                    LethalMin.PikminSelectedPosY = -106f;
                    LethalMin.PikminSelectedPosZ = -59.767f;
                    LethalMin.PikminSelectedRotX = 0f;
                    LethalMin.PikminSelectedRotY = 12f;
                    LethalMin.PikminSelectedRotZ = 0f;
                    LethalMin.PikminSelectedScale = 0.677937f;

                    LethalMin.PikminCountPosX = 268.4f;
                    LethalMin.PikminCountPosY = -165.4f;
                    LethalMin.PikminCountPosZ = -45.4f;
                    LethalMin.PikminCountRotX = 0f;
                    LethalMin.PikminCountRotY = 12f;
                    LethalMin.PikminCountRotZ = 0f;
                    LethalMin.PikminCountScale = 0.6698875f;
                    break;
                case HudPresets.New:
                    // Set up new HUD
                    NextPortRect.gameObject.SetActive(true);
                    PrevPortRect.gameObject.SetActive(true);
                    PikminInExsistanceRect.gameObject.SetActive(true);

                    LethalMin.PikminSelectedPosX = 8.4f;
                    LethalMin.PikminSelectedPosY = -90f;
                    LethalMin.PikminSelectedPosZ = -15.9f;
                    LethalMin.PikminSelectedRotX = 0f;
                    LethalMin.PikminSelectedRotY = 0f;
                    LethalMin.PikminSelectedRotZ = 0f;
                    LethalMin.PikminSelectedScale = 0.6f;

                    LethalMin.PikminCountPosX = 23.5f;
                    LethalMin.PikminCountPosY = -187f;
                    LethalMin.PikminCountPosZ = -47.4f;
                    LethalMin.PikminCountRotX = 0f;
                    LethalMin.PikminCountRotY = 0f;
                    LethalMin.PikminCountRotZ = 0f;
                    LethalMin.PikminCountScale = 0.6f;
                    break;
                case HudPresets.Simplified:
                    // Set up simplified HUD
                    NextPortRect.gameObject.SetActive(false);
                    PrevPortRect.gameObject.SetActive(false);
                    PikminInExsistanceRect.gameObject.SetActive(false);

                    LethalMin.PikminSelectedPosX = 109f;
                    LethalMin.PikminSelectedPosY = -85.4f;
                    LethalMin.PikminSelectedPosZ = -47f;
                    LethalMin.PikminSelectedRotX = 0f;
                    LethalMin.PikminSelectedRotY = 0f;
                    LethalMin.PikminSelectedRotZ = 0f;
                    LethalMin.PikminSelectedScale = 0.6f;

                    LethalMin.PikminCountPosX = 73f;
                    LethalMin.PikminCountPosY = -106f;
                    LethalMin.PikminCountPosZ = -47.4f;
                    LethalMin.PikminCountRotX = 0f;
                    LethalMin.PikminCountRotY = 0f;
                    LethalMin.PikminCountRotZ = 0f;
                    LethalMin.PikminCountScale = 0.6f;
                    break;
                default:
                    // Default to new HUD
                    NextPortRect.gameObject.SetActive(true);
                    PrevPortRect.gameObject.SetActive(true);
                    PikminInExsistanceRect.gameObject.SetActive(true);

                    LethalMin.PikminSelectedPosX = 8.4f;
                    LethalMin.PikminSelectedPosY = -106.6f;
                    LethalMin.PikminSelectedPosZ = -15.9f;
                    LethalMin.PikminSelectedRotX = 0f;
                    LethalMin.PikminSelectedRotY = 0f;
                    LethalMin.PikminSelectedRotZ = 0f;
                    LethalMin.PikminSelectedScale = 0.6f;

                    LethalMin.PikminCountPosX = 23.5f;
                    LethalMin.PikminCountPosY = -204.9f;
                    LethalMin.PikminCountPosZ = -47.4f;
                    LethalMin.PikminCountRotX = 0f;
                    LethalMin.PikminCountRotY = 0f;
                    LethalMin.PikminCountRotZ = 0f;
                    LethalMin.PikminCountScale = 0.6f;

                    LethalMin.Logger.LogWarning("Invalid HUD preset selected, defaulting to new HUD.");
                    break;
            }
        }


        public InputAction throwAction;

        public InputAction switchPikminTypeAction, switchPikminPrevTypeAction;

        public void LateUpdate()
        {
            PikminSelectedRect.localPosition = new Vector3(LethalMin.PikminSelectedPosX, LethalMin.PikminSelectedPosY, LethalMin.PikminSelectedPosZ);
            PikminSelectedRect.localRotation = Quaternion.Euler(LethalMin.PikminSelectedRotX, LethalMin.PikminSelectedRotY, LethalMin.PikminSelectedRotZ);
            PikminSelectedRect.localScale = new Vector3(LethalMin.PikminSelectedScale, LethalMin.PikminSelectedScale, LethalMin.PikminSelectedScale);

            PikminCountRect.localPosition = new Vector3(LethalMin.PikminCountPosX, LethalMin.PikminCountPosY, LethalMin.PikminCountPosZ);
            PikminCountRect.localRotation = Quaternion.Euler(LethalMin.PikminCountRotX, LethalMin.PikminCountRotY, LethalMin.PikminCountRotZ);
            PikminCountRect.localScale = new Vector3(LethalMin.PikminCountScale, LethalMin.PikminCountScale, LethalMin.PikminCountScale);


            if (throwAction != null && throwAction.controls.Count > 0)
            {
                string buttonName = throwAction.controls[0].displayName;
                ThrowPrompt.text = "[" + buttonName + "]";
            }
            else
            {
                ThrowPrompt.text = "???";
            }

            if (switchPikminTypeAction != null && switchPikminTypeAction.controls.Count > 0)
            {
                string buttonName = switchPikminTypeAction.controls[0].displayName;
                RightPrompt.text = "[" + buttonName + "]";
            }
            else
            {
                RightPrompt.text = "???";
            }

            if (switchPikminPrevTypeAction != null && switchPikminPrevTypeAction.controls.Count > 0)
            {
                string buttonName = switchPikminPrevTypeAction.controls[0].displayName;
                LeftPrompt.text = "[" + buttonName + "]";
            }
            else
            {
                LeftPrompt.text = "???";
            }
        }
        bool HasThrownMin = false;
        Coroutine promptRoutine;
        public void HidePrompts()
        {
            if (HasThrownMin) { return; }
            HasThrownMin = true;
            StartCoroutine(HidePromptsRoutine());
        }
        public IEnumerator HidePromptsRoutine()
        {
            //tween the canva's group alpha to 0
            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime;
                PromptcanvasGroup.alpha = Mathf.Lerp(1, 0, time);
                yield return null;
            }
            promptRoutine = null!;
        }
        public void PingPrompts()
        {
            if (!HasThrownMin || promptRoutine != null) { return; }
            promptRoutine = StartCoroutine(PingPromptsRoutine());
        }
        public IEnumerator PingPromptsRoutine()
        {
            //tween the canva's group alpha to 1
            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime;
                PromptcanvasGroup.alpha = Mathf.Lerp(0, 1, time);
                yield return null;
            }
            yield return new WaitForSeconds(2);
            StartCoroutine(HidePromptsRoutine());
        }
        Coroutine CounterRoutine;
        public void PingCounter()
        {
            if (CounterRoutine != null) { StopCoroutine(CounterRoutine); }
            CounterRoutine = StartCoroutine(PingCounterRoutine());
        }
        public IEnumerator PingCounterRoutine()
        {
            CountGroup.alpha = 0.75f;
            yield return new WaitForSeconds(1.5f);
            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime;
                CountGroup.alpha = Mathf.Lerp(0.75f, 0.2f, time);
                yield return null;
            }
            CounterRoutine = null!;
        }

        public float UpdateInterval = 1f;
        private float timer = 0f;

        void Update()
        {
            if (LeaderScript == null) return;
            timer += Time.deltaTime;

            if (timer >= UpdateInterval)
            {
                UpdateHUD();

                timer = 0f;
            }
        }
        public bool PikminExists;
        public void RefreshLeaderScript()
        {
            LeaderManager[] ers = FindObjectsOfType<LeaderManager>();
            foreach (LeaderManager leader in ers)
            {
                if (leader.Controller == StartOfRound.Instance.localPlayerController)
                {
                    LeaderScript = leader;
                    break;
                }
            }
        }
        int LastSquadCount;
        public void UpdateHUD()
        {
            if (LeaderScript == null)
            {
                RefreshLeaderScript();
            }

            int PikminInFieldI = FindObjectsOfType<PikminAI>().Length;
            int PikminInOnions = 0;
            Onion[] onions = FindObjectsOfType<Onion>();
            if (onions.Length > 0)
            {
                foreach (var item in onions)
                {
                    PikminInOnions += item.pikminInOnion.Count;
                }
            }
            int PikminInExistenceI = FindObjectsOfType<PikminAI>().Count(p => p.currentBehaviourStateIndex != (int)PState.Leaveing) + PikminInOnions;
            if (LeaderScript != null)
            {
                PikminInSquad.text = LeaderScript.followingPikmin.Count.ToString();
                PikminInField.text = PikminInFieldI.ToString();
                PikminInExsistance.text = PikminInExistenceI.ToString();
                if (PikminInExistenceI != LastSquadCount)
                {
                    LastSquadCount = PikminInExistenceI;
                    PingCounter();
                }
            }

            LeaderScript.UpdateAvailableTypes();

            PikminType currentType = LeaderScript.GetCurrentSelectedType();
            PikminType prevType = LeaderScript.GetPreviousType();
            PikminType nextType = LeaderScript.GetNextType();

            if (LeaderScript.AvailableTypes.Count > 0)
            {
                //CurPikminCount.color = LethalMin.GetColorFromPType(currentType);
                CurPikminBox.color = currentType.PikminColor2;
                CurPikminCount.text = LeaderScript.GetFollowingPikminByType(currentType).Count.ToString();
                CurPort.sprite = currentType.PikminIcon;
                CurPikminCount.color = Color.white;


                //PrevPikminCount.color = LethalMin.GetColorFromPType(prevType);
                PrevPikminBox.color = prevType.PikminColor2;
                PrevPikminCount.text = LeaderScript.GetFollowingPikminByType(prevType).Count.ToString();
                PrevPort.sprite = prevType.PikminIcon;
                PrevPikminCount.color = Color.white;


                //NextPikminCount.color = LethalMin.GetColorFromPType(nextType);
                NextPikminBox.color = nextType.PikminColor2;
                NextPikminCount.text = LeaderScript.GetFollowingPikminByType(nextType).Count.ToString();
                NextPort.sprite = nextType.PikminIcon;
                NextPikminCount.color = Color.white;
            }
            else
            {
                CurPikminCount.color = Color.black;
                CurPort.sprite = LethalMin.NoPikmin;
                CurPikminBox.color = Color.black;
                CurPikminCount.text = "0";
                PrevPikminCount.color = Color.black;
                PrevPort.sprite = LethalMin.NoPikmin;
                PrevPikminBox.color = Color.black;
                PrevPikminCount.text = "0";
                NextPikminCount.color = Color.black;
                NextPort.sprite = LethalMin.NoPikmin;
                NextPikminBox.color = Color.black;
                NextPikminCount.text = "0";
            }




            if (PikminInExistenceI > 0 && !PikminExists)
            {
                HUDManager.Instance.PingHUDElement(element, 1, 0, 1);
                PikminExists = true;
            }
            else if (PikminInExistenceI <= 0 && PikminExists && !StartOfRound.Instance.inShipPhase)
            {
                HUDManager.Instance.PingHUDElement(element, 1, 1, 0);
                PikminExists = false;
            }
            else if (StartOfRound.Instance.inShipPhase)
            {
                HUDManager.Instance.PingHUDElement(element, 1, 0, 0);
                PikminExists = false;
            }
        }
    }
}