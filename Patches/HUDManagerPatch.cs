using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin;
using LethalMin.HUD;
using LethalMin.Utils;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    public class HUDManagerPatch
    {

        [HarmonyPatch(nameof(HUDManager.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(HUDManager __instance)
        {
            AddPikminHUD(__instance);
            AddOnionHUD(__instance);
            AddStatSlots(__instance);
            AddInDanger(__instance);
        }

        public static void AddInDanger(HUDManager __instance)
        {
            if (__instance.shipLeavingEarlyIcon == null)
            {
                LethalMin.Logger.LogFatal("Ship leaving early icon is null, cannot add in danger icon.");
                return;
            }
            GameObject go = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/HUD/PikminInDanger.prefab");
            PikminHUDManager hud = PikminHUDManager.instance;
            hud.InstanceDanger = UnityEngine.Object.Instantiate(go, __instance.shipLeavingEarlyIcon.transform);
            hud.InstanceDanger.transform.SetParent(__instance.shipLeavingEarlyIcon.transform);
            hud.InstanceDanger.transform.localPosition = new Vector3(hud.InstanceDanger.transform.localPosition.x - 20,
            hud.InstanceDanger.transform.localPosition.y + 40, hud.InstanceDanger.transform.localPosition.z);
            hud.LeaveingEarlyIcon = __instance.shipLeavingEarlyIcon.GetComponent<Image>();
            hud.InDangerIcon = hud.InstanceDanger.GetComponentInChildren<Image>();
            hud.DangerCount = hud.InstanceDanger.GetComponentInChildren<TMP_Text>();
            hud.SafeIcon = LethalMin.assetBundle.LoadAsset<Sprite>("Assets/LethalMin/HUD/Psafe.png");
            hud.WarningIcon = LethalMin.assetBundle.LoadAsset<Sprite>("Assets/LethalMin/HUD/Pwarn.png");
        }

        public static void AddPikminHUD(HUDManager __instance)
        {
            GameObject go = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminHUD.prefab");
            GameObject Instance = UnityEngine.Object.Instantiate(go, __instance.HUDContainer.transform);
            PikminHUDManager hud = Instance.GetComponent<PikminHUDManager>();
            HUDElement Phud = hud.element;
            hud.FrameContainer.group.alpha = 0;
            hud.GetComponentInChildren<PikminCounter>().element.group.alpha = 0;

            Array.Resize(ref __instance.HUDElements, __instance.HUDElements.Length + 1);
            __instance.HUDElements[__instance.HUDElements.Length - 1] = Phud;
        }

        public static void AddOnionHUD(HUDManager __instance)
        {
            GameObject go = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/OnionAssets/Call_Return Pikmin UI.prefab");
            GameObject Instance = UnityEngine.Object.Instantiate(go, __instance.HUDContainer.transform.parent);
        }

        public static void AddStatSlots(HUDManager __instance)
        {
            __instance.gameObject.AddComponent<PikminEndOfGameStatUIElements>();

            PikminEndOfGameStatUIElements elements = __instance.gameObject.GetComponent<PikminEndOfGameStatUIElements>();

            elements.PikminRaisedTexts = new PikminRaisedTextBox[__instance.statsUIElements.playerNamesText.Length];

            for (int i = 0; i < __instance.statsUIElements.playerNamesText.Length; i++)
            {
                Transform SlotTransform = __instance.statsUIElements.playerNamesText[i].transform.parent;
                GameObject go = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/HUD/RaisedBox.prefab");
                GameObject Instance = UnityEngine.Object.Instantiate(go, SlotTransform);
                Instance.transform.localPosition = new Vector3(285, 69, 0);
                elements.PikminRaisedTexts[i] = Instance.GetComponent<PikminRaisedTextBox>();
            }

            GameObject go2 = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/HUD/KillNleft.prefab");
            GameObject Instance2 = UnityEngine.Object.Instantiate(go2, __instance.endgameStatsAnimator.transform.Find("Text"));
            Instance2.transform.localPosition = new Vector3(308.2475f, -133.2824f, 0);
            Instance2.transform.localScale = new Vector3(0.55f, 0.65f, 0.65f);
            elements.KilledText = Instance2.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            elements.LeftBehindText = Instance2.transform.Find("Text (TMP) (1)").GetComponent<TMP_Text>();

            GameObject go3 = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/HUD/PikmnLeft.prefab");
            GameObject Instance3 = UnityEngine.Object.Instantiate(go3, __instance.playerLevelBoxAnimator.transform);
            Instance3.transform.localPosition = new Vector3(-142.7688f, 0.1763f, -7.6766f);
            Instance3.transform.rotation = Quaternion.Euler(0, 351.9064f, 0);
            Instance3.transform.localScale = new Vector3(1f, 1f, 1f);
            elements.LeftText = Instance3.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            elements.Greater = Instance3.transform.Find("Greater").gameObject;
            elements.Same = Instance3.transform.Find("Same").gameObject;
            elements.Less = Instance3.transform.Find("Less").gameObject;
        }

        [HarmonyPatch(nameof(HUDManager.UpdateScanNodes))]
        [HarmonyPostfix]
        static void UpdateScanNodesPostFix(HUDManager __instance, PlayerControllerB playerScript)
        {
            try
            {
                for (int i = 0; i < __instance.scanElements.Length; i++)
                {
                    if (__instance.scanNodes.Count > 0 && __instance.scanNodes.TryGetValue(__instance.scanElements[i], out var value)
                     && value is PikminScanNodeProperties psp)
                    {
                        if (__instance.NodeIsNotVisible(value, i))
                        {
                            continue;
                        }
                        if (__instance.scanElements[i].gameObject.activeSelf)
                        {
                            __instance.scanElements[i].GetComponent<Animator>().SetInteger("colorNumber", psp.VisualNodeType);
                            if (psp.PiklopediaEntry != null)
                            {
                                PikminManager.instance.AttemptScanNewType(psp.PiklopediaEntry.PiklopediaID);
                            }
                        }
                    }
                }
            }
            catch (Exception arg)
            {
                Debug.LogError($"Error in updatescanNodes A: {arg}");
            }
        }

        [HarmonyPatch(nameof(HUDManager.ScanNewCreatureServerRpc))]
        [HarmonyPrefix]
        static bool ScanNewCreatureServerRpcPrefix(HUDManager __instance, int enemyID)
        {
            if (PikChecks.IsServerRpcNoOwnershipPrefixValid(__instance) == false)
            {
                return true;
            }

            if (LethalMin.EnemyIDsOverridenByPiklopedia.Contains(enemyID) && !__instance.terminalScript.scannedEnemyIDs.Contains(enemyID))
            {
                __instance.terminalScript.scannedEnemyIDs.Add(enemyID);
                __instance.terminalScript.newlyScannedEnemyIDs.Add(enemyID);
                __instance.ScanNewCreatureClientRpc(enemyID);
                LethalMin.Logger.LogInfo($"Skipping Global Message Call (serverrpc)");
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(HUDManager.ScanNewCreatureClientRpc))]
        [HarmonyPrefix]
        static bool ScanNewCreatureClientRpcPrefix(HUDManager __instance, int enemyID)
        {
            if (PikChecks.IsClientRpcPrefixValid(__instance) == false)
            {
                return true;
            }
            
            if (LethalMin.EnemyIDsOverridenByPiklopedia.Contains(enemyID) && !__instance.terminalScript.scannedEnemyIDs.Contains(enemyID))
            {
                __instance.terminalScript.scannedEnemyIDs.Add(enemyID);
                __instance.terminalScript.newlyScannedEnemyIDs.Add(enemyID);
                LethalMin.Logger.LogInfo($"Skipping Global Message Call (clientrpc)");
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(HUDManager.DisplayGlobalNotification))]
        [HarmonyPostfix]
        static void DisplayGlobalNotificationPrefix(HUDManager __instance)
        {
            if (LethalMin.HideSelectedWhenScanNotifcation)
            {
                if (PikminHUDManager.instance.HideSelectedCoroutine != null)
                {
                    PikminHUDManager.instance.StopCoroutine(PikminHUDManager.instance.HideSelectedCoroutine);
                    PikminHUDManager.instance.HideSelectedCoroutine = null;
                }
                PikminHUDManager.instance.HideSelectedCoroutine = PikminHUDManager.instance.StartCoroutine(PikminHUDManager.instance.TemporarlyHideSelectedPikmin(6.95f));
            }
        }

        [HarmonyPatch(nameof(HUDManager.FillEndGameStats))]
        [HarmonyPostfix]
        public static void FillEndGameStatsPostfix(HUDManager __instance)
        {
            __instance.gameObject.GetComponent<PikminEndOfGameStatUIElements>().FillEndGameStats();
        }
    }
}