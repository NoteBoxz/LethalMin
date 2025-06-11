using System.Collections.Generic;
using LethalModDataLib.Attributes;
using LethalModDataLib.Base;
using LethalModDataLib.Enums;
using LethalModDataLib.Features;
using LethalModDataLib.Helpers;
using UnityEngine;

namespace LethalMin
{
    public static class SaveManager
    {
        internal static ES3Settings settings = new ES3Settings();

        static SaveManager()
        {
            settings.path = GameNetworkManager.Instance.currentSaveFileName + "_LethalMinSave";
        }

        public static void Save<T>(string key, T value)
        {
            if (LethalMin.UseModDataLib)
            {
                MODDATLIB_Save(key, value);
            }
            else
            {
                ES3.Save(key, value, settings);
            }
        }

        static void MODDATLIB_Save<T>(string key, T value)
        {
            SaveLoadHandler.SaveData(value, key, SaveLocation.CurrentSave);
        }

        public static T Load<T>(string key)
        {
            if (LethalMin.UseModDataLib)
            {
                return MODDATLIB_Load<T>(key);
            }
            else
            {
                return ES3.Load<T>(key, settings);
            }
        }
        public static T Load<T>(string key, T defaultValue)
        {
            if (LethalMin.UseModDataLib)
            {
                return MODDATLIB_Load(key, defaultValue);
            }
            else
            {
                return ES3.Load(key, defaultValue, settings);
            }
        }

        static T MODDATLIB_Load<T>(string key, T defaultValue)
        {
            T? value = SaveLoadHandler.LoadData(key, SaveLocation.CurrentSave, defaultValue);
            if (value == null)
            {
                Debug.LogWarning($"Failed to load key '{key}' from MODDATLIB. Returning default value.");
                return defaultValue;
            }
            return value;
        }
        static T MODDATLIB_Load<T>(string key)
        {
            return SaveLoadHandler.LoadData(key, SaveLocation.CurrentSave, default(T))!;
        }

        public static void DeleteFile()
        {
            if (LethalMin.UseModDataLib)
            {
                MODDATLIB_Delete();
            }
            else
            {
                if (ES3.FileExists(settings))
                {
                    ES3.DeleteFile(settings);
                }
            }
        }

        static void MODDATLIB_Delete()
        {
            string fileName = ModDataHelper.GetCurrentSaveFileName();
            fileName += ".moddata";

            // Check if the file exists before attempting operations
            if (ES3.FileExists(fileName))
            {
                // Get all keys from the file
                string[] allKeys = ES3.GetKeys(fileName);

                // Filter and delete only keys that start with our prefix
                foreach (string key in allKeys)
                {
                    if (key.StartsWith("NoteBoxz.LethalMin."))
                    {
                        ES3.DeleteKey(key, fileName);
                    }
                }

                Debug.Log($"Removed LethalMin keys from save file: {fileName}");
            }
        }
        
        public static bool KeyExists(string key)
        {
            if (LethalMin.UseModDataLib)
            {
                return MODDATLIB_KeyExists(key);
            }
            else
            {
                return ES3.KeyExists(key, settings);
            }
        }

        static bool MODDATLIB_KeyExists(string key)
        {
            string fileName = ModDataHelper.GetCurrentSaveFileName();
            fileName += ".moddata";
            key = "NoteBoxz.LethalMin." + key;

            return ES3.KeyExists(key, fileName);
        }
    }
}