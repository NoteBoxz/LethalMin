// using HarmonyLib;
// using UnityEngine;
// using System.Collections.Generic;
// using System;
// using GameNetcodeStuff;
// using System.Linq;
// using System.Reflection;
// using System.Reflection.Emit;

// namespace LethalMin.Patches
// {
//     [HarmonyPatch(typeof(HoarderBugAI))]
//     public class HoarderBugAIPatch
//     {
//         [HarmonyPatch(typeof(HoarderBugAI), "DetectAndLookAtPlayers")]
//         [HarmonyPostfix]
//         static void DetectAndLookAtPlayersPostfix(HoarderBugAI __instance)
//         {
//             if (!LethalMin.LethalBugs) { return; }
//             if (ShouldHoarderBugBeAngry(__instance))
//             {
//                 // Check for Pikmin stealing items
//                 foreach (HoarderBugItem item in HoarderBugAI.HoarderBugItems)
//                 {
//                     if(item.status != HoarderBugItemStatus.Owned || item.status != HoarderBugItemStatus.Returned){continue;}
//                     PikminItem pikminItem = item.itemGrabbableObject.GetComponentInChildren<PikminItem>();
//                     if (pikminItem != null && pikminItem.PikminOnItemList.Count > 0)
//                     {
//                         // Pikmin are stealing an item
//                         __instance.angryAtPlayer = null; // Clear any player target
//                         __instance.angryTimer = 3.25f; // Set angry timer
//                         AttackPikmin(__instance, pikminItem);
//                         return; // Exit the method after handling Pikmin
//                     }
//                 }

//                 // If no Pikmin are stealing, check for players stealing
//                 PlayerControllerB stealingPlayer = null; //FindStealingPlayer(__instance);
//                 if (__instance.angryAtPlayer != null && __instance.IsHoarderBugAngry())
//                 {
//                     return;
//                 }

//                 // If neither Pikmin nor players are stealing, exit angry state
//                 ExitAngryState(__instance);
//             }
//         }

//         static bool ShouldHoarderBugBeAngry(HoarderBugAI hoarderBug)
//         {
//             if (!LethalMin.LethalBugs) { return false; }

//             // Check for Pikmin stealing items
//             foreach (GameObject item in PikminManager.GetPikminItemsInMap())
//             {
//                 PikminItem pikminItem = item.GetComponent<PikminItem>();
//                 if (pikminItem != null && pikminItem.PikminOnItemList.Count > 0)
//                 {
//                     return true;
//                 }
//             }

//             return false;
//         }

//         static void AttackPikmin(HoarderBugAI hoarderBug, PikminItem pikminItem)
//         {
//             if (!LethalMin.LethalBugs) { return; }
//             hoarderBug.StartCoroutine(AttackPikminCoroutine(hoarderBug, pikminItem));
//         }

//         static System.Collections.IEnumerator AttackPikminCoroutine(HoarderBugAI hoarderBug, PikminItem pikminItem)
//         {
//             while (pikminItem.PikminOnItemList.Count > 0)
//             {
//                 if (!LethalMin.LethalBugs) { break; }
//                 PikminAI targetPikmin = pikminItem.PikminOnItemList[0];

//                 // Chase the Pikmin
//                 while (Vector3.Distance(hoarderBug.transform.position, targetPikmin.rb.position) > 1.5f)
//                 {
//                     if (pikminItem.PikminOnItemList.Count == 0)
//                     {
//                         break;
//                     }
//                     hoarderBug.SetDestinationToPosition(targetPikmin.transform.position);
//                     yield return new WaitForSeconds(0.1f); // Wait a short time before updating the destination
//                 }
//                 if (pikminItem != null && pikminItem.PikminOnItemList.Count > 0 && targetPikmin != null && Vector3.Distance(hoarderBug.transform.position, targetPikmin.rb.position) > 1.5f)
//                 {
//                     // When close enough, snap the Pikmin to the grab target
//                     if (!targetPikmin.IsDying && !targetPikmin.FinnaBeDed && !targetPikmin.isEnemyDead)
//                     {
//                         targetPikmin.SnapPikminToPosition(hoarderBug.grabTarget, false, true, 1, true);
//                     }

//                     if (pikminItem.PikminOnItemList.Count == 0)
//                     {
//                         break;
//                     }

//                     yield return new WaitForSeconds(0.5f); // Short delay before targeting the next Pikmin
//                     if (pikminItem.PikminOnItemList.Count == 0)
//                     {
//                         break;
//                     }
//                 }
//             }

//             // After all Pikmin are dealt with, exit the angry state
//             ExitAngryState(hoarderBug);
//         }

//         static PlayerControllerB FindStealingPlayer(HoarderBugAI hoarderBug)
//         {
//             if (hoarderBug.angryAtPlayer != null)
//             {
//                 LethalMin.Logger.LogInfo("Buffered angry player: " + hoarderBug.angryAtPlayer);
//                 return hoarderBug.angryAtPlayer;
//             }
//             foreach (HoarderBugItem item in HoarderBugAI.HoarderBugItems)
//             {
//                 if (item.status == HoarderBugItemStatus.Stolen)
//                 {
//                     LethalMin.Logger.LogInfo("Found already stolen item: " + item.itemGrabbableObject);
//                     return item.itemGrabbableObject.playerHeldBy;
//                 }
//             }
//             return null;
//         }

//         static void ExitAngryState(HoarderBugAI hoarderBug)
//         {
//             if (!LethalMin.LethalBugs || hoarderBug.angryAtPlayer == null) { return; }
//             hoarderBug.angryTimer = 0f;
//             hoarderBug.angryAtPlayer = null;
//         }
//     }
// }