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
namespace LethalMin
{
    public class PikminHUD : MonoBehaviour
    {
        public LeaderManager LeaderScript;
        public HUDElement element;
        private TMP_Text PikminInSquad, PikminInField, PikminInExsistance, NextPikminCount, PrevPikminCount, CurPikminCount;
        private Image CurPikminBox, NextPikminBox, PrevPikminBox, CurPort, NextPort, PrevPort;
        private RectTransform NextPortRect, CurPortRect, PrevPortRect, PikminSelectedRect;
        private RectTransform PikminCountRect, PikminInSquadRect, PikminInFieldRect, PikminInExsistanceRect;
        public static PikminHUD pikminHUDInstance;

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
        }

        public void LateUpdate()
        {
            PikminSelectedRect.localPosition = new Vector3(LethalMin.PikminSelectedPosX, LethalMin.PikminSelectedPosY, LethalMin.PikminSelectedPosZ);
            PikminSelectedRect.localRotation = Quaternion.Euler(LethalMin.PikminSelectedRotX, LethalMin.PikminSelectedRotY, LethalMin.PikminSelectedRotZ);
            PikminSelectedRect.localScale = new Vector3(LethalMin.PikminSelectedScale, LethalMin.PikminSelectedScale, LethalMin.PikminSelectedScale);

            PikminCountRect.localPosition = new Vector3(LethalMin.PikminCountPosX, LethalMin.PikminCountPosY, LethalMin.PikminCountPosZ);
            PikminCountRect.localRotation = Quaternion.Euler(LethalMin.PikminCountRotX, LethalMin.PikminCountRotY, LethalMin.PikminCountRotZ);
            PikminCountRect.localScale = new Vector3(LethalMin.PikminCountScale, LethalMin.PikminCountScale, LethalMin.PikminCountScale);
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