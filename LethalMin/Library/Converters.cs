using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LethalMinLibrary;

namespace LethalMin.Library
{
    public static class TypeConverter
    {
        private static readonly string[] ExcludedProperties = { "AnimPath", "GrowthStagePaths" };

        public static PikminType Convert_Lib_PikminTypeToLmPikminType(LethalMinLibrary.PikminType libType)
        {
            PikminType lmType = ScriptableObject.CreateInstance<PikminType>();

            PropertyInfo[] libProperties = typeof(LethalMinLibrary.PikminType).GetProperties();
            PropertyInfo[] lmProperties = typeof(PikminType).GetProperties();
            LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converting LibPikminType with ({libProperties.Length}) varibles to LmPikminType with ({lmProperties.Length}) variables");
            int ConvertedPropertiesCount = 0;

            foreach (var libProperty in libProperties)
            {
                if (ExcludedProperties.Contains(libProperty.Name))
                    continue;

                var lmProperty = lmProperties.FirstOrDefault(p => p.Name.ToLower() == libProperty.Name.ToLower() && p.PropertyType == libProperty.PropertyType);
                if (lmProperty != null && lmProperty.CanWrite)
                {
                    object value = libProperty.GetValue(libType);
                    lmProperty.SetValue(lmType, value);
                    LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converted {libProperty.Name} from {libType.GetType().Name.ToLower()} to {lmType.GetType().Name.ToLower()}");
                    ConvertedPropertiesCount++;
                }
            }
            LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converted {ConvertedPropertiesCount} properties from LibPikminType with ({libProperties.Length}) to LmPikminType with ({lmProperties.Length})");

            // Handle special cases
            lmType.HazardsResistantTo = EnumConverter.Convert_Lib_HazardToLmHazard(libType.HazardsResistantTo);

            return lmType;
        }

        public static PikminSoundPack Convert_Lib_PikminSoundPackToLmPikminSoundPack(LethalMinLibrary.PikminSoundPack libSoundPack)
        {
            PikminSoundPack lmSoundPack = ScriptableObject.CreateInstance<PikminSoundPack>();
            CopyProperties(libSoundPack, lmSoundPack, "PikminSoundPack");
            return lmSoundPack;
        }

        public static OnionType Convert_Lib_OnionTypeToLmOnionType(LethalMinLibrary.OnionType libOnionType)
        {
            OnionType lmOnionType = ScriptableObject.CreateInstance<OnionType>();
            CopyProperties(libOnionType, lmOnionType, "OnionType");

            // Handle special cases
            lmOnionType.TypesCanHold = libOnionType.TypesCanHold.Select(Convert_Lib_PikminTypeToLmPikminType).ToArray();
            lmOnionType.FuesingRules = Convert_Lib_OnionFuseRulesToLmOnionFuseRules(libOnionType.FuesingRules);

            return lmOnionType;
        }

        public static OnionFuseRules Convert_Lib_OnionFuseRulesToLmOnionFuseRules(LethalMinLibrary.OnionFuseRules libFuseRules)
        {
            OnionFuseRules lmFuseRules = ScriptableObject.CreateInstance<OnionFuseRules>();
            CopyProperties(libFuseRules, lmFuseRules, "OnionFuseRules");

            // Handle special cases
            lmFuseRules.CompatibleOnions = libFuseRules.CompatibleOnions.Select(Convert_Lib_OnionTypeToLmOnionType).ToArray();

            return lmFuseRules;
        }

        private static void CopyProperties<TSource, TDestination>(TSource source, TDestination destination, string typeName)
        {
            PropertyInfo[] sourceProperties = typeof(TSource).GetProperties();
            PropertyInfo[] destProperties = typeof(TDestination).GetProperties();

            int convertedPropertiesCount = 0;

            foreach (var sourceProperty in sourceProperties)
            {
                var destProperty = destProperties.FirstOrDefault(p => p.Name.ToLower() == sourceProperty.Name.ToLower() && p.PropertyType == sourceProperty.PropertyType);
                if (destProperty != null && destProperty.CanWrite)
                {
                    object value = sourceProperty.GetValue(source);
                    destProperty.SetValue(destination, value);
                    LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converted {sourceProperty.Name} from {typeof(TSource).Name.ToLower()} to {typeof(TDestination).Name.ToLower()}");
                    convertedPropertiesCount++;
                }
            }

            LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converted {convertedPropertiesCount} properties from Lib{typeName} with ({sourceProperties.Length}) to Lm{typeName} with ({destProperties.Length})");
        }
    }

    public static class EnumConverter
    {
        public static HazardType Convert_Lib_HazardToLmHazard(LibHazardType libHazardType)
        {
            HazardType hazardType = Enum.TryParse<HazardType>(libHazardType.ToString(), out var result) ? result : HazardType.Lethal;
            LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converted {libHazardType} to {hazardType}");
            return hazardType;
        }

        public static HazardType[] Convert_Lib_HazardToLmHazard(LibHazardType[] libHazardTypes)
        {
            return libHazardTypes.Select(Convert_Lib_HazardToLmHazard).ToArray();
        }

        public static LibHazardType Convert_LethalMin_HazardToLibHazard(HazardType hazardType)
        {
            LibHazardType libHazardType = Enum.TryParse<LibHazardType>(hazardType.ToString(), out var result) ? result : LibHazardType.Lethal;
            LethalMin.Logger.LogMessage($"(LETHALMIN_CONVERTER) Converted {hazardType} to {libHazardType}");
            return libHazardType;
        }

        public static LibHazardType[] Convert_LethalMin_HazardToLibHazard(HazardType[] hazardTypes)
        {
            return hazardTypes.Select(Convert_LethalMin_HazardToLibHazard).ToArray();
        }
    }
}