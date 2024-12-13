using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalMin;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    public class HUDManagerPatch
    {
        static GameObject InstanceDanger, InstanceB, PminPlayerSlot1, PminPlayerSlot2, PminPlayerSlot3, PminPlayerSlot4, LeftBox;
        static TMP_Text DangerCount, Killed, LeftB, Left, Raised1, Raised2, Raised3, Raised4;
        static GameObject Greater, Less, Equal;
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPostfix(HUDManager __instance)
        {
            GameObject Insrt = UnityEngine.Object.Instantiate(LethalMin.CallminUI, __instance.HUDContainer.transform.parent);
            Insrt.transform.SetParent(__instance.HUDContainer.transform.parent);

            if (Insrt.GetComponent<OnionMenuManager>() == null)
            {
                Insrt.AddComponent<OnionMenuManager>();
            }


            HUDElement Phud = new HUDElement();
            GameObject Instance = UnityEngine.Object.Instantiate(LethalMin.StatsUI, __instance.HUDContainer.transform);

            Phud.canvasGroup = Instance.GetComponent<CanvasGroup>();
            Phud.targetAlpha = 0;
            Array.Resize(ref __instance.HUDElements, __instance.HUDElements.Length + 1);
            __instance.HUDElements[__instance.HUDElements.Length - 1] = Phud;

            Instance.transform.SetParent(__instance.HUDContainer.transform);


            if (Instance.GetComponent<PikminHUD>() == null)
            {
                Instance.AddComponent<PikminHUD>().element = Phud;
            }


            InstanceDanger = UnityEngine.Object.Instantiate(LethalMin.InDangerUIelement, __instance.shipLeavingEarlyIcon.transform);
            InstanceDanger.transform.SetParent(__instance.shipLeavingEarlyIcon.transform);
            InstanceDanger.transform.localPosition = new Vector3(InstanceDanger.transform.localPosition.x - 20,
            InstanceDanger.transform.localPosition.y + 40, InstanceDanger.transform.localPosition.z);
            img = __instance.shipLeavingEarlyIcon.GetComponent<Image>();
            DangerCount = InstanceDanger.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            imger = InstanceDanger.GetComponent<Image>();

            LeftBox = UnityEngine.Object.Instantiate(LethalMin.LeftElement, __instance.endgameStatsAnimator.transform.Find("LevelUp"));
            LeftBox.transform.SetParent(__instance.endgameStatsAnimator.transform.Find("LevelUp"));
            Left = LeftBox.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            Greater = LeftBox.transform.Find("Greater").gameObject;
            Less = LeftBox.transform.Find("Less").gameObject;
            Equal = LeftBox.transform.Find("Same").gameObject;

            InstanceB = UnityEngine.Object.Instantiate(LethalMin.KilledUIelement, __instance.endgameStatsAnimator.transform.Find("Lines"));
            InstanceB.transform.SetParent(__instance.endgameStatsAnimator.transform.Find("Lines"));
            Killed = InstanceB.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            LeftB = InstanceB.transform.Find("Text (TMP) (1)").GetComponent<TMP_Text>();

            PminPlayerSlot1 = UnityEngine.Object.Instantiate(LethalMin.RasiedUIelement, __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot1"));
            PminPlayerSlot1.transform.SetParent(__instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot1"));
            PminPlayerSlot1.transform.position = __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot1/Notes").position;
            PminPlayerSlot1.transform.localPosition = new Vector3(PminPlayerSlot1.transform.localPosition.x + 180, PminPlayerSlot1.transform.localPosition.y - 0, PminPlayerSlot1.transform.localPosition.z);
            Raised1 = PminPlayerSlot1.transform.Find("Text (TMP)").GetComponent<TMP_Text>();

            PminPlayerSlot2 = UnityEngine.Object.Instantiate(LethalMin.RasiedUIelement, __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot2"));
            PminPlayerSlot2.transform.SetParent(__instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot2"));
            PminPlayerSlot2.transform.position = __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot2/Notes").position;
            PminPlayerSlot2.transform.localPosition = new Vector3(PminPlayerSlot2.transform.localPosition.x + 180, PminPlayerSlot2.transform.localPosition.y - 0, PminPlayerSlot2.transform.localPosition.z);
            Raised2 = PminPlayerSlot2.transform.Find("Text (TMP)").GetComponent<TMP_Text>();

            PminPlayerSlot3 = UnityEngine.Object.Instantiate(LethalMin.RasiedUIelement, __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot3"));
            PminPlayerSlot3.transform.SetParent(__instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot3"));
            PminPlayerSlot3.transform.position = __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot3/Notes").position;
            PminPlayerSlot3.transform.localPosition = new Vector3(PminPlayerSlot3.transform.localPosition.x + 180, PminPlayerSlot3.transform.localPosition.y - 0, PminPlayerSlot3.transform.localPosition.z);
            Raised3 = PminPlayerSlot3.transform.Find("Text (TMP)").GetComponent<TMP_Text>();

            PminPlayerSlot4 = UnityEngine.Object.Instantiate(LethalMin.RasiedUIelement, __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot4"));
            PminPlayerSlot4.transform.SetParent(__instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot4"));
            PminPlayerSlot4.transform.position = __instance.endgameStatsAnimator.transform.Find("Text/PlayerSlot4/Notes").position;
            PminPlayerSlot4.transform.localPosition = new Vector3(PminPlayerSlot4.transform.localPosition.x + 180, PminPlayerSlot4.transform.localPosition.y - 0, PminPlayerSlot4.transform.localPosition.z);
            Raised4 = PminPlayerSlot4.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
        }

        static Image img, imger;
        static float IntervalDanger;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(HUDManager __instance)
        {
            if (InstanceDanger == null) { return; }
            if (img == null) { return; }

            InstanceDanger.SetActive(img.enabled);

            if (!img.enabled) { return; }

            if (!PikminManager.Instance.CreatedSafetyRings)
                PikminManager.Instance.CreateSafetyRings();

            if (IntervalDanger >= 0)
            {
                IntervalDanger -= Time.deltaTime;
            }
            else
            {
                // Update the count on the server
                if (NetworkManager.Singleton.IsServer)
                {
                    PikminManager.Instance.UpdatePikminInDangerCountServerRpc();
                }

                // Use the synchronized count
                int DANGERRRR = PikminManager.Instance.GetPikminInDangerCount();
                DangerCount.text = DANGERRRR.ToString();
                if (DANGERRRR > 0)
                {
                    imger.sprite = LethalMin.DangerRanger;
                }
                else
                {
                    imger.sprite = LethalMin.SaferWafer;
                }
                IntervalDanger = 0.2f;
            }
        }

        // In HUDManagerPatch.cs

        // In HUDManagerPatch.cs

        [HarmonyPatch("FillEndGameStats")]
        [HarmonyPostfix]
        public static void PikminEndgame(HUDManager __instance, EndOfGameStats stats)
        {
            PikminManager pikminManager = PikminManager.Instance;
            if (pikminManager == null) { LethalMin.Logger.LogWarning("How tf is the PikminManger NULL?!?!?!"); return; }

            // Update killed and left behind counts
            Killed.text = $"{pikminManager.TotalPikminKilled}";
            LeftB.text = $"{pikminManager.TotalPikminLeftBehind}";

            // Calculate total Pikmin count for this round
            int currentTotalPikminCount = 0;
            Onion[] onions = UnityEngine.Object.FindObjectsOfType<Onion>();
            foreach (Onion onion in onions)
            {
                currentTotalPikminCount += onion.GetPikminCount();
            }

            // Compare current total Pikmin count with previous round
            int previousTotalPikminCount = LethalMin.PreviousRoundPikminCounts.Values.Sum();
            if (previousTotalPikminCount == 0)
            {
                pikminManager.SetPikminCountFromSaveServerRpc();
                previousTotalPikminCount = pikminManager.PLLR.Value;
            }
            int difference = currentTotalPikminCount - previousTotalPikminCount;

            // Update UI based on the comparison
            Left.text = currentTotalPikminCount.ToString();
            Greater.SetActive(difference > 0);
            Less.SetActive(difference < 0);
            Equal.SetActive(difference == 0);

            // If it's the first round or no previous data

            // Update raised counts for each player
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                ulong playerId = StartOfRound.Instance.allPlayerScripts[i].playerClientId;
                int raisedCount = pikminManager.PikminRaisedPerPlayer.ContainsKey(playerId) ? pikminManager.PikminRaisedPerPlayer[playerId] : 0;
                switch (i)
                {
                    case 0:
                        Raised1.text = $"{raisedCount}";
                        break;
                    case 1:
                        Raised2.text = $"{raisedCount}";
                        PminPlayerSlot2.SetActive(StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled ||
                         !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && StartOfRound.Instance.allPlayerScripts[i].isPlayerDead);
                        break;
                    case 2:
                        Raised3.text = $"{raisedCount}";
                        PminPlayerSlot3.SetActive(StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled ||
                         !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && StartOfRound.Instance.allPlayerScripts[i].isPlayerDead);
                        break;
                    case 3:
                        Raised4.text = $"{raisedCount}";
                        PminPlayerSlot4.SetActive(StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled ||
                         !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && StartOfRound.Instance.allPlayerScripts[i].isPlayerDead);
                        break;
                }
            }

            // Store current counts for next round comparison
            LethalMin.PreviousRoundPikminCounts.Clear();
            foreach (Onion onion in onions)
            {
                LethalMin.PreviousRoundPikminCounts[onion.type] = onion.GetPikminCount();
            }
        }
    }
}