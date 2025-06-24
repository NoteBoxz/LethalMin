using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LethalMin.Pikmin;
using System.Collections.Generic;
using LethalMinLibrary;
using PikminType = LethalMin.Pikmin.PikminType;

namespace LethalMin.Library
{
    public static class DepsManager
    {
        public static PikminType? MatchPossibleTypeWithDep(LibPikminType Ptype)
        {
            LethalMin.Logger.LogInfo($"Matching {Ptype.name} with dependencies...");
            PikminType? type = null;
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
    }
}