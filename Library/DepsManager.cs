using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LethalMin.Pikmin;
using System.Collections.Generic;
using LethalMinLibrary;
using PikminType = LethalMin.Pikmin.PikminType;
using OnionFuseRules = LethalMin.Pikmin.OnionFuseRules;

namespace LethalMin.Library
{
    public static class DepsManager
    {
        public static PikminType? MatchPossibleTypeWithDep(LibPikminType Ptype)
        {
            LethalMin.Logger.LogInfo($"Matching {Ptype.name} with dependencies...");
            PikminType? type = null;

            if (Ptype.ReplacementObject as PikminType != null)
            {
                LethalMin.Logger.LogInfo($"PikminType {Ptype.name} has replacement object.");
                return Ptype.ReplacementObject as PikminType;
            }
            
            if (Ptype.ModInfo == null)
            {
                LethalMin.Logger.LogWarning($"PikminType {Ptype.name} has no ModInfo, cannot match with dependencies.");
                return null;
            }

            if (LibAssetBundleLoader.ConvertedTypesList.ContainsKey(Ptype))
            {
                LethalMin.Logger.LogInfo($"PikminType {Ptype.name} is already in ConvertedTypesList, returning existing type.");
                return LibAssetBundleLoader.ConvertedTypesList[Ptype];
            }

            if (Ptype.ModInfo.GUID == "NoteBoxz.LethalMin"
            && Ptype.ModInfo.ModName == "LethalMin"
            && Ptype.ModInfo.ModAuthor == "NoteBoxz")
            {
                foreach (PikminType BaseType in LethalMin.RegisteredPikminTypes.Values)
                {
                    if (BaseType.name == Ptype.name)
                    {
                        type = BaseType;
                        break;
                    }
                }
            }

            foreach (PikminType ConvertedType in LibAssetBundleLoader.ConvertedTypesList.Values)
            {
                if (type != null)
                {
                    break;
                }
                if (ConvertedType.name == Ptype.name
                && ConvertedType.PikminName == Ptype.PikminName
                && ConvertedType.PikminPrimaryColor == Ptype.PikminPrimaryColor
                && ConvertedType.PikminSecondaryColor == Ptype.PikminSecondaryColor)
                {
                    type = ConvertedType;
                    break;
                }
            }

            if (type != null)
            {
                Ptype.ReplacementObject = type;
                LibAssetBundleLoader.ConvertedTypesList[Ptype] = type;
                Ptype.name = $"_Dependency_{type.name}";
                LethalMin.Logger.LogInfo($"Matched {Ptype.name} with dependency {type.name} from {Ptype.ModInfo.ModName}");
            }

            return type;
        }
        
        public static OnionFuseRules? MatchPossibleTypeWithDep(LibOnionFuseRules Ptype)
        {
            LethalMin.Logger.LogInfo($"Matching {Ptype.name} with dependencies...");
            OnionFuseRules? type = null;

            if (Ptype.ReplacementObject as OnionFuseRules != null)
            {
                LethalMin.Logger.LogInfo($"OnionFuseRules {Ptype.name} has replacement object.");
                return Ptype.ReplacementObject as OnionFuseRules;
            }

            if (Ptype.ModInfo == null)
            {
                LethalMin.Logger.LogWarning($"OnionFuseRules {Ptype.name} has no ModInfo, cannot match with dependencies.");
                return null;
            }

            if (LibAssetBundleLoader.ConvertedFuseRulesList.ContainsKey(Ptype))
            {
                LethalMin.Logger.LogInfo($"OnionFuseRules {Ptype.name} is already in ConvertedFuseRulesList, returning existing type.");
                return LibAssetBundleLoader.ConvertedFuseRulesList[Ptype];
            }

            if (Ptype.ModInfo.GUID == "NoteBoxz.LethalMin"
            && Ptype.ModInfo.ModName == "LethalMin"
            && Ptype.ModInfo.ModAuthor == "NoteBoxz")
            {
                foreach (OnionFuseRules BaseType in LethalMin.RegisteredFuseRules.Values)
                {
                    if (BaseType.name == Ptype.name)
                    {
                        type = BaseType;
                        break;
                    }
                }
            }

            foreach (OnionFuseRules ConvertedType in LibAssetBundleLoader.ConvertedFuseRulesList.Values)
            {
                if (type != null)
                {
                    break;
                }
                if (ConvertedType.name == Ptype.name)
                {
                    type = ConvertedType;
                    break;
                }
            }

            if (type != null)
            {
                Ptype.ReplacementObject = type;
                LibAssetBundleLoader.ConvertedFuseRulesList[Ptype] = type;
                Ptype.name = $"_Dependency_{type.name}";
                LethalMin.Logger.LogInfo($"Matched {Ptype.name} with dependency {type.name} from {Ptype.ModInfo.ModName}");
            }

            return type;
        }
    }
}