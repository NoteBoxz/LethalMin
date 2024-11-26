using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using LethalMin.Library;
using UnityEngine.SceneManagement;

namespace LethalMin.Library
{
    public class LibAssetBundleLoader : MonoBehaviour
    {
        public Dictionary<LethalMinLibrary.PikminType, PikminType> ConvertedPikminTypes = new Dictionary<LethalMinLibrary.PikminType, PikminType>();
        public Dictionary<LethalMinLibrary.OnionType, OnionType> ConvertedOnionTypes = new Dictionary<LethalMinLibrary.OnionType, OnionType>();
        
        public void ProcessLoadedLibBundle(AssetBundle bundle)
        {
            bool IsValidLethalMinBundle = false;
            // Load PikminTypes
            LethalMinLibrary.PikminType[] pikminTypes = bundle.LoadAllAssets<LethalMinLibrary.PikminType>();

            foreach (LethalMinLibrary.PikminType pikminType in pikminTypes)
            {
                PikminType Ptype = TypeConverter.Convert_Lib_PikminTypeToLmPikminType(pikminType);
                ConvertedPikminTypes.Add(pikminType, Ptype);
                LethalMin.RegisterPikminType(Ptype);
                IsValidLethalMinBundle = true;
            }

            // Load OnionTypes
            LethalMinLibrary.OnionType[] onionTypes = bundle.LoadAllAssets<LethalMinLibrary.OnionType>();
            foreach (LethalMinLibrary.OnionType onionType in onionTypes)
            {
                OnionType oynon = TypeConverter.Convert_Lib_OnionTypeToLmOnionType(onionType);
                ConvertedOnionTypes.Add(onionType, oynon);
                LethalMin.RegisterOnionType(oynon);
                IsValidLethalMinBundle = true;
            }

            // Load OnionFuseRules
            LethalMinLibrary.OnionFuseRules[] fuseRules = bundle.LoadAllAssets<LethalMinLibrary.OnionFuseRules>();
            foreach (LethalMinLibrary.OnionFuseRules fuseRule in fuseRules)
            {
                LethalMin.RegisterFuseRule(TypeConverter.Convert_Lib_OnionFuseRulesToLmOnionFuseRules(fuseRule));
                IsValidLethalMinBundle = true;
            }

            LethalMinLibrary.PikminItemOverrideSettings[] overrideSettings = bundle.LoadAllAssets<LethalMinLibrary.PikminItemOverrideSettings>();
            foreach (LethalMinLibrary.PikminItemOverrideSettings item in overrideSettings)
            {
                TypeConverter.Convert_Lib_PIOStoLmPIOS(item);
            }

            if (IsValidLethalMinBundle == false)
            {
                LethalMin.Logger.LogWarning($"Bundle does not contain any valid LethalMin assets: {bundle.name}");
            }
        }
        private void ProcessLoadedBundle(AssetBundle bundle)
        {
            bool IsValidLethalMinBundle = false;
            // Load PikminTypes
            PikminType[] pikminTypes = bundle.LoadAllAssets<PikminType>();
            foreach (PikminType pikminType in pikminTypes)
            {
                LethalMin.RegisterPikminType(pikminType);
                IsValidLethalMinBundle = true;
            }

            // Load OnionTypes
            OnionType[] onionTypes = bundle.LoadAllAssets<OnionType>();
            foreach (OnionType onionType in onionTypes)
            {
                LethalMin.RegisterOnionType(onionType);
                IsValidLethalMinBundle = true;
            }

            // Load OnionFuseRules
            OnionFuseRules[] fuseRules = bundle.LoadAllAssets<OnionFuseRules>();
            foreach (OnionFuseRules fuseRule in fuseRules)
            {
                LethalMin.RegisterFuseRule(fuseRule);
                IsValidLethalMinBundle = true;
            }

            if (IsValidLethalMinBundle == false)
            {
                //LethalMin.Logger.LogWarning($"Bundle does not contain any valid LethalMin assets: {bundle.name}");
            }
        }
    }
}