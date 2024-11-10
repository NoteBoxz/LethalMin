using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LethalMinLibrary;

namespace LethalMin.Library
{
    public static class TypeConverter
    {
        private static readonly string[] ExcludedProperties = { "AnimPath", "PikminGlowPath", "GrowthStagePaths" };

        public static PikminType Convert_Lib_PikminTypeToLmPikminType(LethalMinLibrary.PikminType libType)
        {
            PikminType lmType = ScriptableObject.CreateInstance<PikminType>();
            lmType.name = libType.name;

            FieldInfo[] libFields = typeof(LethalMinLibrary.PikminType).GetFields(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] lmFields = typeof(PikminType).GetFields(BindingFlags.Public | BindingFlags.Instance);
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converting LibPikminType with ({libFields.Length}) fields to LmPikminType with ({lmFields.Length}) fields");
            int ConvertedFieldsCount = 0;

            foreach (var libField in libFields)
            {
                if (ExcludedProperties.Contains(libField.Name))
                    continue;

                var lmField = lmFields.FirstOrDefault(f => f.Name.ToLower() == libField.Name.ToLower() && f.FieldType == libField.FieldType);
                if (lmField != null)
                {
                    object value = libField.GetValue(libType);
                    lmField.SetValue(lmType, value);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {libField.Name} from Library to LethalMin");
                    ConvertedFieldsCount++;
                }
            }
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {ConvertedFieldsCount} fields from LibPikminType with ({libFields.Length}) to LmPikminType with ({lmFields.Length})");

            // Handle special cases
            lmType.HazardsResistantTo = EnumConverter.Convert_Lib_HazardToLmHazard(libType.HazardsResistantTo);

            if (libType.soundPack != null)
                lmType.soundPack = Convert_Lib_PikminSoundPackToLmPikminSoundPack(libType.soundPack);

            if (lmType.MeshPrefab == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) MeshPrefab is null for {libType.name}");
                return lmType;
            }

            PikminMeshRefernces meshRef = lmType.MeshPrefab.AddComponent<PikminMeshRefernces>();

            CopyFields(libType.MeshRefernces, meshRef, "PikminMeshRefernces");
            lmType.MeshRefernces = meshRef;

            return lmType;
        }

        public static PikminSoundPack Convert_Lib_PikminSoundPackToLmPikminSoundPack(LethalMinLibrary.PikminSoundPack libSoundPack)
        {
            PikminSoundPack lmSoundPack = ScriptableObject.CreateInstance<PikminSoundPack>();
            lmSoundPack.name = libSoundPack.name;
            CopyFields(libSoundPack, lmSoundPack, "PikminSoundPack");
            return lmSoundPack;
        }

        public static OnionType Convert_Lib_OnionTypeToLmOnionType(LethalMinLibrary.OnionType libOnionType)
        {
            OnionType lmOnionType = ScriptableObject.CreateInstance<OnionType>();
            lmOnionType.name = libOnionType.name;
            CopyFields(libOnionType, lmOnionType, "OnionType");

            // Handle special cases
            lmOnionType.TypesCanHold = libOnionType.TypesCanHold.Select(Convert_Lib_PikminTypeToLmPikminType).ToArray();
            //if (libOnionType.FuesingRules != null)
            //    lmOnionType.FuesingRules = Convert_Lib_OnionFuseRulesToLmOnionFuseRules(libOnionType.FuesingRules);

            return lmOnionType;
        }

        public static OnionFuseRules Convert_Lib_OnionFuseRulesToLmOnionFuseRules(LethalMinLibrary.OnionFuseRules libFuseRules)
        {
            OnionFuseRules lmFuseRules = ScriptableObject.CreateInstance<OnionFuseRules>();
            lmFuseRules.name = libFuseRules.name;
            CopyFields(libFuseRules, lmFuseRules, "OnionFuseRules");

            // Handle special cases
            lmFuseRules.CompatibleOnions = libFuseRules.CompatibleOnions.Select(Convert_Lib_OnionTypeToLmOnionType).ToArray();

            return lmFuseRules;
        }

        public static PikminMeshRefernces Convert_Lib_PikminMeshReferncesToLmPikminMeshRefernces(LethalMinLibrary.PikminMeshRefernces libMeshRefernces)
        {
            PikminMeshRefernces lmMeshRefernces = new PikminMeshRefernces();
            CopyFields(libMeshRefernces, lmMeshRefernces, "PikminMeshRefernces");
            return lmMeshRefernces;
        }

        private static void CopyFields<TSource, TDestination>(TSource source, TDestination destination, string typeName)
        {
            FieldInfo[] sourceFields = typeof(TSource).GetFields(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] destFields = typeof(TDestination).GetFields(BindingFlags.Public | BindingFlags.Instance);

            int convertedFieldsCount = 0;

            foreach (var sourceField in sourceFields)
            {
                var destField = destFields.FirstOrDefault(f => f.Name.ToLower() == sourceField.Name.ToLower() && f.FieldType == sourceField.FieldType);
                if (destField != null)
                {
                    object value = sourceField.GetValue(source);
                    destField.SetValue(destination, value);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {sourceField.Name} from {typeof(TSource).Name.ToLower()} to {typeof(TDestination).Name.ToLower()}");
                    convertedFieldsCount++;
                }
            }

            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {convertedFieldsCount} fields from Lib{typeName} with ({sourceFields.Length}) to Lm{typeName} with ({destFields.Length})");
        }
    }

    public static class EnumConverter
    {
        public static HazardType Convert_Lib_HazardToLmHazard(LibHazardType libHazardType)
        {
            HazardType hazardType = Enum.TryParse<HazardType>(libHazardType.ToString(), out var result) ? result : HazardType.Lethal;
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {libHazardType} to {hazardType}");
            return hazardType;
        }

        public static HazardType[] Convert_Lib_HazardToLmHazard(LibHazardType[] libHazardTypes)
        {
            return libHazardTypes.Select(Convert_Lib_HazardToLmHazard).ToArray();
        }

        public static LibHazardType Convert_LethalMin_HazardToLibHazard(HazardType hazardType)
        {
            LibHazardType libHazardType = Enum.TryParse<LibHazardType>(hazardType.ToString(), out var result) ? result : LibHazardType.Lethal;
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {hazardType} to {libHazardType}");
            return libHazardType;
        }

        public static LibHazardType[] Convert_LethalMin_HazardToLibHazard(HazardType[] hazardTypes)
        {
            return hazardTypes.Select(Convert_LethalMin_HazardToLibHazard).ToArray();
        }
    }
}