using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(DeleteFileButton))]
    public class DeleteFileButtonPatch
    {
        [HarmonyPatch(nameof(DeleteFileButton.DeleteFile))]
        [HarmonyPostfix]
        private static void DeletePostfix(DeleteFileButton __instance)
        {
            if (LethalMin.UseModDataLib)
            {
                return;
            }
            
            string filePath = $"LCSaveFile{__instance.fileToDelete + 1}" + "_LethalMinSave";

            if (ES3.FileExists(filePath))
            {
                ES3.DeleteFile(filePath);
            }
        }
    }
}
