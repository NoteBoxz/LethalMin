using UnityEngine;
using System.Collections.Generic;
using LethalMin.Pikmin;
using LethalMinLibrary;
using System.Linq;
using PikminType = LethalMin.Pikmin.PikminType;
using OnionType = LethalMin.Pikmin.OnionType;
using OnionFuseRules = LethalMin.Pikmin.OnionFuseRules;

namespace LethalMin.Library
{
    public class LibAssetBundleLoader : MonoBehaviour
    {
        public static List<Object> LibTypesToRemove = new List<Object>();
        public static Dictionary<LibPikminType, PikminType> ConvertedTypesList = new Dictionary<LibPikminType, PikminType>();
        public static Dictionary<LibOnionType, OnionType> ConvertedOTypesList = new Dictionary<LibOnionType, OnionType>();
        public static Dictionary<LibOnionFuseRules, OnionFuseRules> ConvertedFuseRulesList = new Dictionary<LibOnionFuseRules, OnionFuseRules>();
        public static Dictionary<LibPiklopediaEntry, PiklopediaEntry> ConvertedPiklopediaEntries = new Dictionary<LibPiklopediaEntry, PiklopediaEntry>();

        public void ProcessLoadedLibBundle(AssetBundle bundle)
        {
            bool IsValidLethalMinBundle = false;
            List<PikminType> ConvertedLegacyTypeList = new List<PikminType>();
            Dictionary<LibPikminType, PikminType> ConvertedTypesList = new Dictionary<LibPikminType, PikminType>();
            Dictionary<LibOnionType, OnionType> ConvertedOTypesList = new Dictionary<LibOnionType, OnionType>();
            Dictionary<LibOnionFuseRules, OnionFuseRules> ConvertedFuseRulesList = new Dictionary<LibOnionFuseRules, OnionFuseRules>();
            Dictionary<LibPiklopediaEntry, PiklopediaEntry> ConvertedPiklopediaEntries = new Dictionary<LibPiklopediaEntry, PiklopediaEntry>();
            Dictionary<PikminType, LibOnionType> ConvertedTypeTargetOnions = new Dictionary<PikminType, LibOnionType>();
            List<LibOnionType> OTypesWithOvrRules = new List<LibOnionType>();

#pragma warning disable CS0612 // Type or member is obsolete
            LethalMinLibrary.PikminType[] LegacyTypes = bundle.LoadAllAssets<LethalMinLibrary.PikminType>();
            if (LegacyTypes.Length > 0)
            {
                LethalMin.Logger.LogInfo($"Found {LegacyTypes.Length} legacy PikminTypes");
                foreach (LethalMinLibrary.PikminType legacyType in LegacyTypes)
                {
                    try
                    {
                        PikminType ConvertedType = TypeConverter.ConvertFromLegacyPikminType(legacyType);
                        if (ConvertedType != null)
                        {
                            IsValidLethalMinBundle = true;
                            ConvertedLegacyTypeList.Add(ConvertedType);
                            LibTypesToRemove.Add(legacyType);
                        }
                        else
                        {
                            legacyType.name = "FailedConversion_" + legacyType.name;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LethalMin.Logger.LogError($"Failed to convert legacy PikminType: {ex}");
                    }
                }
            }
#pragma warning restore CS0612 // Type or member is obsolete

            LibPikminType[] LibTypes = bundle.LoadAllAssets<LibPikminType>();
            if (LibTypes.Length > 0)
            {
                LethalMin.Logger.LogInfo($"Found {LibTypes.Length} PikminTypes");
                foreach (LibPikminType LibType in LibTypes)
                {
                    try
                    {
                        if (LibType.ModInfo != null && LibType.ModInfo.DontLoad)
                        {
                            LethalMin.Logger.LogDebug($"Skipping loading of PikminType '{LibType.name}' from bundle '{bundle.name}' due to DontLoad flag.");
                            continue;
                        }
                        PikminType ConvertedType = TypeConverter.ConvertFromLibPikminType(LibType);
                        if (ConvertedType != null)
                        {
                            IsValidLethalMinBundle = true;
                            ConvertedTypesList.Add(LibType, ConvertedType);
                            LibAssetBundleLoader.ConvertedTypesList.Add(LibType, ConvertedType);
                            LibTypesToRemove.Add(LibType);
                            if (LibType.TargetOnion != null)
                            {
                                ConvertedTypeTargetOnions.Add(ConvertedType, LibType.TargetOnion);
                            }
                        }
                        else
                        {
                            LibType.name = "FailedConversion_" + LibType.name;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LethalMin.Logger.LogError($"Failed to convert LibPikminType: {ex}");
                    }
                }
            }

            LibOnionType[] LibOnionTypes = bundle.LoadAllAssets<LibOnionType>();
            if (LibOnionTypes.Length > 0)
            {
                LethalMin.Logger.LogInfo($"Found {LibOnionTypes.Length} OnionTypes");
                foreach (LibOnionType LibOnionType in LibOnionTypes)
                {
                    try
                    {
                        if (LibOnionType.ModInfo != null && LibOnionType.ModInfo.DontLoad)
                        {
                            LethalMin.Logger.LogDebug($"Skipping loading of OnionType '{LibOnionType.name}' from bundle '{bundle.name}' due to DontLoad flag.");
                            continue;
                        }
                        OnionType ConvertedOnionType = TypeConverter.ConvertFromLibOnionType(LibOnionType);
                        if (ConvertedOnionType != null)
                        {
                            IsValidLethalMinBundle = true;
                            ConvertedOTypesList.Add(LibOnionType, ConvertedOnionType);
                            LibAssetBundleLoader.ConvertedOTypesList.Add(LibOnionType, ConvertedOnionType);
                            LibTypesToRemove.Add(LibOnionType);
                            foreach (KeyValuePair<PikminType, LibOnionType> pair in ConvertedTypeTargetOnions)
                            {
                                if (pair.Value == LibOnionType)
                                {
                                    pair.Key.TargetOnion = ConvertedOnionType;
                                }
                            }
                            foreach (LibPikminType libPikminType in LibOnionType.TypesCanHold)
                            {
                                if (!LibAssetBundleLoader.ConvertedTypesList.ContainsKey(libPikminType))
                                    continue;

                                List<PikminType> typesCanHold = new List<PikminType>();
                                typesCanHold.Add(LibAssetBundleLoader.ConvertedTypesList[libPikminType]);
                                ConvertedOnionType.TypesCanHold = typesCanHold.ToArray();
                            }
                            if (LibOnionType.OverrideFuseRules != null)
                                OTypesWithOvrRules.Add(LibOnionType);
                        }
                        else
                        {
                            LibOnionType.name = "FailedConversion_" + LibOnionType.name;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LethalMin.Logger.LogError($"Failed to convert LibOnionType: {ex}");
                    }
                }
            }

            LibOnionFuseRules[] LibFuseRules = bundle.LoadAllAssets<LibOnionFuseRules>();
            if (LibFuseRules.Length > 0)
            {
                LethalMin.Logger.LogInfo($"Found {LibFuseRules.Length} OnionFuseRules");
                foreach (LibOnionFuseRules LibFuseRule in LibFuseRules)
                {
                    try
                    {
                        if (LibFuseRule.ModInfo != null && LibFuseRule.ModInfo.DontLoad)
                        {
                            LethalMin.Logger.LogDebug($"Skipping loading of OnionFuseRules '{LibFuseRule.name}' from bundle '{bundle.name}' due to DontLoad flag.");
                            foreach (LibOnionType libOnionType in OTypesWithOvrRules)
                            {
                                if (LibFuseRule.ReplacementObject is not null)
                                {
                                    OnionType ConvertedOnionType = LibAssetBundleLoader.ConvertedOTypesList[libOnionType];
                                    OnionFuseRules? replacementFuseRules = LibFuseRule.ReplacementObject as OnionFuseRules;
                                    if (ConvertedOnionType != null && replacementFuseRules != null)
                                    {
                                        List<OnionType> TempTypesCanFuse = new List<OnionType>(replacementFuseRules.OnionsToFuse);
                                        TempTypesCanFuse.Add(ConvertedOnionType);
                                        replacementFuseRules.OnionsToFuse = TempTypesCanFuse.ToArray();
                                        LethalMin.Logger.LogDebug($"Added {ConvertedOnionType.name} to replacement fuse rules {replacementFuseRules.name} for {LibFuseRule.name}");
                                    }
                                }
                            }
                            continue;
                        }
                        OnionFuseRules fuseRules = ScriptableObject.CreateInstance<OnionFuseRules>();
                        fuseRules.name = LibFuseRule.name;

                        List<OnionType> typesCanFuse = new List<OnionType>();
                        foreach (LibOnionType libOnionType in LibFuseRule.OnionsToFuse)
                        {
                            if (ConvertedOTypesList.ContainsKey(libOnionType))
                            {
                                typesCanFuse.Add(ConvertedOTypesList[libOnionType]);
                            }
                        }
                        foreach (LibOnionType libOnionType in OTypesWithOvrRules)
                        {
                            if (libOnionType.OverrideFuseRules == LibFuseRule)
                            {
                                typesCanFuse.Add(ConvertedOTypesList[libOnionType]);
                                LethalMin.Logger.LogDebug($"Added {ConvertedOTypesList[libOnionType].name} to fuse rules for {LibFuseRule.name} from OVR rules");
                            }
                        }
                        fuseRules.OnionsToFuse = typesCanFuse.ToArray();
                        ConvertedFuseRulesList.Add(LibFuseRule, fuseRules);
                        LibAssetBundleLoader.ConvertedFuseRulesList.Add(LibFuseRule, fuseRules);

                        LibTypesToRemove.Add(LibFuseRule);
                    }
                    catch (System.Exception ex)
                    {
                        LethalMin.Logger.LogError($"Failed to convert LibOnionFuseRules: {ex}");
                    }
                }
            }

            LibPiklopediaEntry[] LibPiklopediaEntries = bundle.LoadAllAssets<LibPiklopediaEntry>();
            if (LibPiklopediaEntries.Length > 0)
            {
                LethalMin.Logger.LogInfo($"Found {LibPiklopediaEntries.Length} PiklopediaEntries");
                foreach (LibPiklopediaEntry LibPiklopediaEntry in LibPiklopediaEntries)
                {
                    try
                    {
                        if (LibPiklopediaEntry.ModInfo != null && LibPiklopediaEntry.ModInfo.DontLoad)
                        {
                            LethalMin.Logger.LogDebug($"Skipping loading of PiklopediaEntry '{LibPiklopediaEntry.name}' from bundle '{bundle.name}' due to DontLoad flag.");
                            continue;
                        }
                        PiklopediaEntry ConvertedEntry = TypeConverter.ConvertFromLibPiklopediaEntry(LibPiklopediaEntry);
                        if (ConvertedEntry != null)
                        {
                            IsValidLethalMinBundle = true;
                            ConvertedPiklopediaEntries.Add(LibPiklopediaEntry, ConvertedEntry);
                            LibAssetBundleLoader.ConvertedPiklopediaEntries.Add(LibPiklopediaEntry, ConvertedEntry);
                            LibTypesToRemove.Add(LibPiklopediaEntry);
                        }
                        else
                        {
                            LibPiklopediaEntry.name = "FailedConversion_" + LibPiklopediaEntry.name;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LethalMin.Logger.LogError($"Failed to convert LibPiklopediaEntry: {ex}");
                    }
                }
            }

            if (IsValidLethalMinBundle == false)
            {
                LethalMin.Logger.LogWarning($"Bundle does not contain any valid LethalMin assets: {bundle.name}");
            }
            else
            {
                LethalMin.RegisterCustomPikminTypes(ConvertedLegacyTypeList);
                LethalMin.RegisterCustomPikminTypes(ConvertedTypesList.Values.ToList());
                LethalMin.RegisterCustomOnionTypes(ConvertedOTypesList.Values.ToList());
                LethalMin.RegisterCustomFuseRules(ConvertedFuseRulesList.Values.ToList());
                LethalMin.RegisterCustomPiklopediaEntries(ConvertedPiklopediaEntries.Values.ToList());
            }
        }
    }
}