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
using Unity.Multiplayer.Tools.NetStats;
namespace LethalMin
{
    public enum HudPresets
    {
        Classic,
        New,
        Simplified
    }
    public enum ElementBehavior
    {
        AlwaysHide,
        AlwaysShow,
        OnlyShowWhenChanged,
    }
    public class PikminHudElement
    {
        public CanvasGroup Group;
        Coroutine Routine;
        private float ActiveTime = 0f;
        private bool isActive = false;
        public ElementBehavior behavior;
        public float AlphaWhenActive = 1f;
        public float AlphaWhenInactive = 0.2f;
        public float FadeTime = 1f;
        public Vector3 Position, Rotaion, Scale;
        public bool HasShown;

        public Vector3 ParseStringToVector3(string str = "(0,0,0)")
        {
            string[] split = str.Split(',');
            float x = float.Parse(split[0].Replace("(", ""));
            float y = float.Parse(split[1]);
            float z = float.Parse(split[2].Replace(")", ""));
            return new Vector3(x, y, z);
        }

        public PikminHudElement(CanvasGroup group, ElementBehavior behavior)
        {
            Group = group;
            this.behavior = behavior;

            switch (behavior)
            {
                case ElementBehavior.AlwaysHide:
                    Group.alpha = 0;
                    break;
                case ElementBehavior.AlwaysShow:
                    Group.alpha = 1;
                    break;
                case ElementBehavior.OnlyShowWhenChanged:
                    Group.alpha = 0;
                    break;
            }
        }

        public void UpdateElement()
        {
            switch (behavior)
            {
                case ElementBehavior.AlwaysHide:
                    Group.alpha = 0;
                    break;
                case ElementBehavior.AlwaysShow:
                    Group.alpha = 1;
                    break;
                case ElementBehavior.OnlyShowWhenChanged:
                    Group.alpha = 0;
                    break;
            }
        }

        public void Ping()
        {
            if (Routine != null)
            {
                
                ActiveTime = 0f;
            }
            else
            {
                
                ActiveTime = 0f;
                Routine = PikminHUD.pikminHUDInstance.StartCoroutine(PingRoutine());
            }
        }

        public IEnumerator PingRoutine()
        {
            isActive = true;
            Group.alpha = AlphaWhenActive;

            while (isActive)
            {
                ActiveTime += Time.deltaTime;

                if (ActiveTime >= FadeTime)
                {
                    // Start fading out after 1.5 seconds of inactivity
                    float fadeTime = 0f;
                    while (fadeTime < 1f)
                    {
                        fadeTime += Time.deltaTime;
                        Group.alpha = Mathf.Lerp(AlphaWhenActive, AlphaWhenInactive, fadeTime);

                        // If Ping is called again during fade out, reset
                        if (ActiveTime < FadeTime)
                        {
                            Group.alpha = AlphaWhenActive;
                            break;
                        }

                        yield return null;
                    }

                    // If we completed the fade out, end the coroutine
                    if (fadeTime >= 1f)
                    {
                        isActive = false;
                    }
                }

                yield return null;
            }

            Routine = null;
        }


        public void Show()
        {
            if (behavior != ElementBehavior.OnlyShowWhenChanged) { return; }
            if (HasShown) { return; }
            HasShown = true;
            PikminHUD.pikminHUDInstance.StartCoroutine(ShowRoutine());
        }
        public IEnumerator ShowRoutine()
        {
            //tween the canva's group alpha to 0
            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime;
                Group.alpha = Mathf.Lerp(0, 1, time);
                yield return null;
            }
            Group.alpha = 1;
        }

        public void Hide()
        {
            if (behavior != ElementBehavior.OnlyShowWhenChanged) { return; }
            if (!HasShown) { return; }
            HasShown = false;
            PikminHUD.pikminHUDInstance.StartCoroutine(HideRoutine());
        }
        public IEnumerator HideRoutine()
        {
            //tween the canva's group alpha to 0
            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime;
                Group.alpha = Mathf.Lerp(1, 0, time);
                yield return null;
            }
            Group.alpha = 0;
        }
    }
    public class PikminHUD : MonoBehaviour
    {
        public static PikminHUD pikminHUDInstance;
        public LeaderManager LeaderScript;
        public HUDElement element;
        private TMP_Text PikminInSquad, PikminInField, PikminInExsistance, NextPikminCount, PrevPikminCount, CurPikminCount;
        private Image CurPikminBox, NextPikminBox, PrevPikminBox, CurPort, NextPort, PrevPort;
        private RectTransform NextPortRect, CurPortRect, PrevPortRect, PikminSelectedRect;
        private RectTransform PikminCountRect, PikminInSquadRect, PikminInFieldRect, PikminInExsistanceRect;
        private TMP_Text ThrowPrompt, LeftPrompt, RightPrompt;
        public GameObject WigglePrompt;
        public HudPresets CurrentHUDPreset = HudPresets.New;
        public PikminHudElement PromptElement, CounterElement, PortElement;

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
            PromptElement = new PikminHudElement(transform.Find("PikminSelected/Prompts/Buttons").GetComponent<CanvasGroup>(), ElementBehavior.OnlyShowWhenChanged);
            CounterElement = new PikminHudElement(transform.Find("PikminCount").GetComponent<CanvasGroup>(), ElementBehavior.OnlyShowWhenChanged);
            PortElement = new PikminHudElement(transform.Find("PikminSelected").GetComponent<CanvasGroup>(), ElementBehavior.OnlyShowWhenChanged);

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

            WigglePrompt = transform.Find("PikminSelected/Prompts/Wiggle").gameObject;
            ThrowPrompt = transform.Find("PikminSelected/Prompts/Buttons/ThrowPrompt").GetComponent<TMP_Text>();
            LeftPrompt = transform.Find("PikminSelected/Prompts/Buttons/SwitchL").GetComponent<TMP_Text>();
            RightPrompt = transform.Find("PikminSelected/Prompts/Buttons/SwitchR").GetComponent<TMP_Text>();

            UpdatePelements();
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

                    LethalMin.PCPX.Value = 262.78f;
                    LethalMin.PCPY.Value = -106f;
                    LethalMin.PCPZ.Value = -59.767f;
                    LethalMin.PCRX.Value = 0f;
                    LethalMin.PCRY.Value = 12f;
                    LethalMin.PCRZ.Value = 0f;
                    LethalMin.PCScale.Value = 0.677937f;

                    LethalMin.PCPCountX.Value = 268.4f;
                    LethalMin.PCPCountY.Value = -165.4f;
                    LethalMin.PCPCountZ.Value = -45.4f;
                    LethalMin.PCRCCountX.Value = 0f;
                    LethalMin.PCRCCountY.Value = 12f;
                    LethalMin.PCRCCountZ.Value = 0f;
                    LethalMin.PCScaleCount.Value = 0.6698875f;
                    break;
                case HudPresets.New:
                    // Set up new HUD
                    NextPortRect.gameObject.SetActive(true);
                    PrevPortRect.gameObject.SetActive(true);
                    PikminInExsistanceRect.gameObject.SetActive(true);

                    LethalMin.PCPX.Value = 8.4f;
                    LethalMin.PCPY.Value = -106.6f;
                    LethalMin.PCPZ.Value = -15.9f;
                    LethalMin.PCRX.Value = 0f;
                    LethalMin.PCRY.Value = 0f;
                    LethalMin.PCRZ.Value = 0f;
                    LethalMin.PCScale.Value = 0.6f;

                    LethalMin.PCPCountX.Value = 23.5f;
                    LethalMin.PCPCountY.Value = -204.9f;
                    LethalMin.PCPCountZ.Value = -47.4f;
                    LethalMin.PCRCCountX.Value = 0f;
                    LethalMin.PCRCCountY.Value = 0f;
                    LethalMin.PCRCCountZ.Value = 0f;
                    LethalMin.PCScaleCount.Value = 0.6f;
                    break;
                case HudPresets.Simplified:
                    // Set up simplified HUD
                    NextPortRect.gameObject.SetActive(false);
                    PrevPortRect.gameObject.SetActive(false);
                    PikminInExsistanceRect.gameObject.SetActive(false);

                    LethalMin.PCPX.Value = 109f;
                    LethalMin.PCPY.Value = -85.4f;
                    LethalMin.PCPZ.Value = -47f;
                    LethalMin.PCRX.Value = 0f;
                    LethalMin.PCRY.Value = 0f;
                    LethalMin.PCRZ.Value = 0f;
                    LethalMin.PCScale.Value = 0.6f;

                    LethalMin.PCPCountX.Value = 73f;
                    LethalMin.PCPCountY.Value = -106f;
                    LethalMin.PCPCountZ.Value = -47.4f;
                    LethalMin.PCRCCountX.Value = 0f;
                    LethalMin.PCRCCountY.Value = 0f;
                    LethalMin.PCRCCountZ.Value = 0f;
                    LethalMin.PCScaleCount.Value = 0.6f;
                    break;
                default:
                    // Default to new HUD
                    NextPortRect.gameObject.SetActive(true);
                    PrevPortRect.gameObject.SetActive(true);
                    PikminInExsistanceRect.gameObject.SetActive(true);

                    LethalMin.PCPX.Value = 8.4f;
                    LethalMin.PCPY.Value = -106.6f;
                    LethalMin.PCPZ.Value = -15.9f;
                    LethalMin.PCRX.Value = 0f;
                    LethalMin.PCRY.Value = 0f;
                    LethalMin.PCRZ.Value = 0f;
                    LethalMin.PCScale.Value = 0.6f;

                    LethalMin.PCPCountX.Value = 23.5f;
                    LethalMin.PCPCountY.Value = -204.9f;
                    LethalMin.PCPCountZ.Value = -47.4f;
                    LethalMin.PCRCCountX.Value = 0f;
                    LethalMin.PCRCCountY.Value = 0f;
                    LethalMin.PCRCCountZ.Value = 0f;
                    LethalMin.PCScaleCount.Value = 0.6f;

                    LethalMin.Logger.LogWarning("Invalid HUD preset selected, defaulting to new HUD.");
                    break;
            }
        }

        public void UpdatePelements()
        {
            if (PromptElement == null || CounterElement == null || PortElement == null) { return; }

            PromptElement.behavior = LethalMin.PromptBehavior;

            CounterElement.behavior = LethalMin.CounterBehavior;
            CounterElement.AlphaWhenInactive = LethalMin.CounterDefultAlpha;
            CounterElement.AlphaWhenActive = 0.75f;

            PortElement.behavior = LethalMin.SquadHudBehavior;
            PortElement.AlphaWhenInactive = LethalMin.SelectedDefultAlpha;

            PromptElement.UpdateElement();
            CounterElement.UpdateElement();
            PortElement.UpdateElement();
        }

        public InputAction throwAction;

        public InputAction switchPikminTypeAction, switchPikminPrevTypeAction;

        bool hasHiddenPrompts = false;
        public bool HasSeenMin, HasSwaped1, HasSwaped2, HasThrown;
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

            if (HasSeenMin && HasSwaped1 && HasSwaped2 && HasThrown && !hasHiddenPrompts)
            {
                PromptElement.Hide();
                hasHiddenPrompts = true;
            }
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
        int LastSquadCount, LastExistCount, LastFieldCount;

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
                if (LeaderScript.followingPikmin.Count != LastSquadCount)
                {
                    LastSquadCount = LeaderScript.followingPikmin.Count;
                    CounterElement.Ping();
                    if (!HasSeenMin)
                    {
                        HasSeenMin = true;
                        PromptElement.Show();
                    }
                }
                if (PikminInExistenceI != LastExistCount)
                {
                    LastExistCount = PikminInExistenceI;
                    CounterElement.Ping();
                }
                if (PikminInFieldI != LastFieldCount)
                {
                    LastFieldCount = PikminInFieldI;
                    CounterElement.Ping();
                }
            }

            LeaderScript.UpdateAvailableTypes();

            PikminType currentType = LeaderScript.GetCurrentSelectedType();
            PikminType prevType = LeaderScript.GetPreviousType();
            PikminType nextType = LeaderScript.GetNextType();

            if (LeaderScript.AvailableTypes.Count > 0)
            {
                PortElement.Show();
                //CurPikminCount.color = LethalMin.GetColorFromPType(currentType);
                CurPikminBox.color = currentType.PikminColor2;
                CurPikminCount.text = LeaderScript.GetFollowingPikminByType(currentType).Count.ToString();
                CurPort.sprite = currentType.PikminIcon;
                CurPort.color = new Color(1, 1, 1, 1);
                CurPikminCount.color = Color.white;


                //PrevPikminCount.color = LethalMin.GetColorFromPType(prevType);
                PrevPikminBox.color = prevType.PikminColor2;
                PrevPikminCount.text = LeaderScript.GetFollowingPikminByType(prevType).Count.ToString();
                PrevPort.sprite = prevType.PikminIcon;
                PrevPort.color = new Color(1, 1, 1, 1);
                PrevPikminCount.color = Color.white;


                //NextPikminCount.color = LethalMin.GetColorFromPType(nextType);
                NextPikminBox.color = nextType.PikminColor2;
                NextPikminCount.text = LeaderScript.GetFollowingPikminByType(nextType).Count.ToString();
                NextPort.sprite = nextType.PikminIcon;
                NextPort.color = new Color(1, 1, 1, 1);
                NextPikminCount.color = Color.white;
            }
            else
            {
                PortElement.Hide();
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