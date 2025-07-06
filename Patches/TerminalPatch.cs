using Discord;
using HarmonyLib;
using LethalMin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    public class TerminalPatch
    {
        public static TerminalNodesList nList = null!;
        public static List<TerminalKeyword> KeywordsWaitingToBeAdded = new List<TerminalKeyword>();
        [HarmonyPatch(nameof(Terminal.Start))]
        [HarmonyPostfix]
        public static void FixKeywords(Terminal __instance)
        {
            nList = __instance.terminalNodes;

            foreach (TerminalKeyword keyword in __instance.terminalNodes.allKeywords)
            {
                if (keyword.word.ToLower() == "pikmin-signal")
                {
                    keyword.word = "pikminsignal";
                    LethalMin.Logger.LogDebug($"Changed keyword 'pikmin-signal' to 'pikminsignal' for compatibility with LethalMin");
                }
                if (keyword.word.ToLower() == "pikmin-container")
                {
                    keyword.word = "pikmincontainer";
                    LethalMin.Logger.LogDebug($"Changed keyword 'pikmin-container' to 'pikmincontainer' for compatibility with LethalMin");
                }
            }

            foreach (TerminalKeyword waitingKeyword in KeywordsWaitingToBeAdded)
            {
                PikUtils.AddKeywordToTerminal(waitingKeyword);
            }
            KeywordsWaitingToBeAdded.Clear();

            AddPiklopedia(__instance);
        }

        public static void AddPiklopedia(Terminal __instance)
        {
            TerminalNode PiklopediaNode = PikUtils.CreateTerminalNode("0_Piklopedia", "");
            TerminalKeyword PiklopediaKeyword = PikUtils.CreateTerminalKeyword("Piklopedia", "piklopedia", PiklopediaNode);

            string DisplayTxt =
            "PIKLOPEDIA"
            + "\n\n"
            + "To access a pikmin file, type \"INFO\" after its name."
            + "\n"
            + "---------------------------------"
            + "\n\n"
            + "[currentScannedPikminList]"
            + "\n\n";

            PiklopediaNode.displayText = DisplayTxt;
        }

        [HarmonyPatch(nameof(Terminal.LoadNewNode))]
        [HarmonyPrefix]
        public static void LoadNewNodePrefix(TerminalNode node)
        {
            foreach (PiklopediaEntry entry in LethalMin.RegisteredEntries.Values)
            {
                if (entry.PiklopediaNode == node)
                {
                    if (PikminManager.instance.NewlyScannedPiklopediaIDs.Contains(entry.PiklopediaID))
                    {
                        PikminManager.instance.NewlyScannedPiklopediaIDs.Remove(entry.PiklopediaID);
                    }
                    break;
                }
            }
        }

        [HarmonyPatch(nameof(Terminal.TextPostProcess))]
        [HarmonyPrefix]
        public static void TextPostProcessPrefix(ref string modifiedDisplayText)
        {
            List<int> ScannedPiklopediaIDs = PikminManager.instance.ScannedPiklopediaIDs;
            if (modifiedDisplayText.Contains("[currentScannedPikminList]"))
            {
                if (ScannedPiklopediaIDs == null || ScannedPiklopediaIDs.Count <= 0)
                {
                    modifiedDisplayText = modifiedDisplayText.Replace("[currentScannedPikminList]", "No data collected on pikmin. Scans are required.");
                }
                else
                {
                    var stringBuilder = new StringBuilder();
                    for (int j = 0; j < ScannedPiklopediaIDs.Count; j++)
                    {
                        LethalMin.Logger.LogInfo($"scanID # {j}: {ScannedPiklopediaIDs[j]}; {LethalMin.RegisteredEntries[ScannedPiklopediaIDs[j]].EntryName}");
                        LethalMin.Logger.LogInfo($"scanID # {j}: {ScannedPiklopediaIDs[j]}");
                        stringBuilder.Append("\n" + LethalMin.RegisteredEntries[ScannedPiklopediaIDs[j]].EntryName);
                        if (PikminManager.instance.NewlyScannedPiklopediaIDs.Contains(ScannedPiklopediaIDs[j]))
                        {
                            stringBuilder.Append(" (NEW)");
                        }
                    }
                    modifiedDisplayText = modifiedDisplayText.Replace("[currentScannedPikminList]", stringBuilder.ToString());
                }
            }
        }

    }
}