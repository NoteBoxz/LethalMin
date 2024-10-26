using HarmonyLib;
using UnityEngine;
using System.IO;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(DeleteFileButton))]
    internal class DeleteFileButtonPatch
    {
        [HarmonyPatch("DeleteFile")]
        [HarmonyPostfix]
        public static void DeleteLethalMinSaveFile(DeleteFileButton __instance)
        {
            int fileToDelete = __instance.fileToDelete;
            string lethalMinSaveFileName = GetLethalMinSaveFileName(fileToDelete);
            string lethalMinSaveFilePath = Path.Combine(Application.persistentDataPath, lethalMinSaveFileName);

            if (File.Exists(lethalMinSaveFilePath))
            {
                File.Delete(lethalMinSaveFilePath);
                LethalMin.Logger.LogMessage($"Deleted LethalMin save file: {lethalMinSaveFileName}");
            }
            else
            {
                LethalMin.Logger.LogMessage($"LethalMin save file not found: {lethalMinSaveFileName}");
            }
        }
        public static void DeleteLethalMinSaveFile(int fileToDelete)
        {
            string lethalMinSaveFileName = GetLethalMinSaveFileName(fileToDelete);
            string lethalMinSaveFilePath = Path.Combine(Application.persistentDataPath, lethalMinSaveFileName);

            if (File.Exists(lethalMinSaveFilePath))
            {
                File.Delete(lethalMinSaveFilePath);
                LethalMin.Logger.LogMessage($"Deleted LethalMin save file: {lethalMinSaveFileName}");
            }
            else
            {
                LethalMin.Logger.LogMessage($"LethalMin save file not found: {lethalMinSaveFileName}");
            }
        }

        private static string GetLethalMinSaveFileName(int fileNum)
        {
            return fileNum switch
            {
                0 => "LethalMinSaveFile1.json",
                1 => "LethalMinSaveFile2.json",
                2 => "LethalMinSaveFile3.json",
                _ => "LethalMinSaveFile1.json",
            };
        }
    }
}