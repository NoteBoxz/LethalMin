using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LethalMin.Pikmin;
using System.Collections.Generic;
using PikminType = LethalMin.Pikmin.PikminType;
using OnionType = LethalMin.Pikmin.OnionType;
using PikminModelRefernces = LethalMin.Pikmin.PikminModelRefernces;
using PikminModelGeneration = LethalMin.Pikmin.PikminModelGeneration;
using PikminSoundPack = LethalMin.Pikmin.PikminSoundPack;
using OnionModelRefernces = LethalMin.Pikmin.OnionModelRefernces;
using OnionModelGeneration = LethalMin.Pikmin.OnionModelGeneration;
using PikminLinkAnimation = LethalMin.Pikmin.PikminLinkAnimation;
using OnionItemModelRefernces = LethalMin.Pikmin.OnionItemModelRefernces;
using OnionItemModelGeneration = LethalMin.Pikmin.OnionItemModelGeneration;
using SproutModelRefences = LethalMin.Pikmin.SproutModelRefences;
using SproutModelGeneration = LethalMin.Pikmin.SproutModelGeneration;
using LethalMinLibrary;

namespace LethalMin.Library
{
    public static class TypeConverter
    {
        private static readonly string[] ExcludedProperties = { "AnimPath", "PikminGlowPath", "GrowthStagePaths" };
        private static readonly Dictionary<Type, Type> EnumTypeMap = new Dictionary<Type, Type>
                {
                    { typeof(LethalMinLibrary.PikminHazard), typeof(PikminHazard) },
                    { typeof(LethalMinLibrary.Pintent), typeof(Pintent) },
                    { typeof(LethalMinLibrary.PikminHarmTriggerDeathType), typeof(PikminHarmTriggerDeathType) },
                    { typeof(LethalMinLibrary.PikminEffectType), typeof(PikminEffectType) },
                    { typeof(LethalMinLibrary.PikminEffectMode), typeof(PikminEffectMode) },
                };

        public static void CopyFields<TSource, TDestination>(TSource source, TDestination destination, string typeName)
        {
            if (source == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) Source is null for {typeName}");
                return;
            }
            if (destination == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) Destination is null for {typeName}");
                return;
            }

            FieldInfo[] sourceFields = typeof(TSource).GetFields(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] destFields = typeof(TDestination).GetFields(BindingFlags.Public | BindingFlags.Instance);

            int convertedFieldsCount = 0;

            foreach (var sourceField in sourceFields)
            {
                // Find matching destination field - for enums, we might need to match by name but different types
                var destField = destFields.FirstOrDefault(f => f.Name.ToLower() == sourceField.Name.ToLower() &&
                    (f.FieldType == sourceField.FieldType ||
                     (f.FieldType.IsEnum && sourceField.FieldType.IsEnum) ||
                     IsEnumCollection(f.FieldType, sourceField.FieldType)));

                if (destField != null)
                {
                    object value = sourceField.GetValue(source);

                    // Handle enum conversion
                    if (value != null)
                    {
                        if (sourceField.FieldType.IsEnum && destField.FieldType.IsEnum)
                        {
                            // Direct enum conversion
                            value = ConvertEnum(value, sourceField.FieldType, destField.FieldType);
                        }
                        else if (IsEnumCollection(sourceField.FieldType, destField.FieldType))
                        {
                            // Collection of enums conversion
                            value = ConvertEnumCollection(value, sourceField.FieldType, destField.FieldType);
                        }
                    }

                    destField.SetValue(destination, value);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {sourceField.Name} from {typeof(TSource).Name.ToLower()} to {typeof(TDestination).Name.ToLower()}");
                    convertedFieldsCount++;
                }
            }

            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {convertedFieldsCount} fields from Lib{typeName} with ({sourceFields.Length}) to Lm{typeName} with ({destFields.Length})");
        }

        // Check if two types are collections of enums
        private static bool IsEnumCollection(Type type1, Type type2)
        {
            Type elementType1 = GetCollectionElementType(type1);
            Type elementType2 = GetCollectionElementType(type2);

            if (elementType1 == null || elementType2 == null)
                return false;

            return elementType1.IsEnum && elementType2.IsEnum;
        }

        // Get the element type of a collection (List<T>, T[], etc.)
        private static Type GetCollectionElementType(Type collectionType)
        {
            // For arrays
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            // For generic collections like List<T>
            if (collectionType.IsGenericType)
            {
                Type[] genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length == 1)
                    return genericArgs[0];
            }

            return null!;
        }

        // Convert a single enum value from source type to destination type
        private static object ConvertEnum(object value, Type sourceType, Type destType)
        {
            if (sourceType == destType)
                return value;

            if (EnumTypeMap.TryGetValue(sourceType, out Type mappedDestType) && mappedDestType == destType)
            {
                // Convert using the underlying value
                return Enum.ToObject(destType, Convert.ToInt32(value));
            }

            // If not in map but both are enums, try direct conversion
            if (sourceType.IsEnum && destType.IsEnum)
            {
                return Enum.ToObject(destType, Convert.ToInt32(value));
            }

            // Fallback - return original value
            return value;
        }

        // Convert a collection of enum values
        private static object ConvertEnumCollection(object collection, Type sourceType, Type destType)
        {
            // Handle arrays
            if (sourceType.IsArray && destType.IsArray)
            {
                Array sourceArray = (Array)collection;
                Type sourceElementType = sourceType.GetElementType();
                Type destElementType = destType.GetElementType();

                Array destArray = Array.CreateInstance(destElementType, sourceArray.Length);

                for (int i = 0; i < sourceArray.Length; i++)
                {
                    destArray.SetValue(
                        ConvertEnum(sourceArray.GetValue(i), sourceElementType, destElementType),
                        i);
                }

                return destArray;
            }

            // Handle List<T> and other generic collections
            if (sourceType.IsGenericType && destType.IsGenericType)
            {
                Type sourceGenericTypeDef = sourceType.GetGenericTypeDefinition();
                Type destGenericTypeDef = destType.GetGenericTypeDefinition();

                // Handle List<T>
                if (sourceGenericTypeDef == typeof(List<>) && destGenericTypeDef == typeof(List<>))
                {
                    Type sourceElementType = sourceType.GetGenericArguments()[0];
                    Type destElementType = destType.GetGenericArguments()[0];

                    // Create a new List<destElementType>
                    Type genericListType = typeof(List<>).MakeGenericType(destElementType);
                    var destList = Activator.CreateInstance(genericListType);

                    // Get Add method of the list
                    MethodInfo addMethod = genericListType.GetMethod("Add");

                    // Convert each element and add to the new list
                    foreach (var item in (System.Collections.IEnumerable)collection)
                    {
                        var convertedItem = ConvertEnum(item, sourceElementType, destElementType);
                        addMethod.Invoke(destList, new[] { convertedItem });
                    }

                    return destList;
                }
            }

            // If we can't handle this type of collection, return the original
            return collection;
        }

        public static void CopyProperties<TSource, TDestination>(TSource source, TDestination destination, string typeName)
        {
            if (source == null || destination == null) return;

            PropertyInfo[] sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] destProps = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var sourceProp in sourceProps)
            {
                if (!sourceProp.CanRead) continue;

                var destProp = destProps.FirstOrDefault(p =>
                    p.Name.ToLower() == sourceProp.Name.ToLower() &&
                    p.PropertyType == sourceProp.PropertyType &&
                    p.CanWrite);

                if (destProp != null)
                {
                    object value = sourceProp.GetValue(source);
                    destProp.SetValue(destination, value);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted property {sourceProp.Name}");
                }
            }
        }

        private static TDest ConvertScriptableObject<TSource, TDest>(
        TSource source,
        Dictionary<TSource, TDest> cache,
        string typeName)
        where TDest : ScriptableObject
        where TSource : class
        {
            if (source == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) {typeName} source is null");
                return null!;
            }

            // Check cache first
            if (cache.TryGetValue(source, out TDest cached))
            {
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached {typeName} for {(source as UnityEngine.Object)?.name ?? "unknown"}");
                return cached;
            }

            // Create new instance
            TDest result = ScriptableObject.CreateInstance<TDest>();
            result.name = (source as UnityEngine.Object)?.name ?? $"Converted{typeName}";

            // Copy fields and properties
            CopyFields(source, result, typeName);
            CopyProperties(source, result, typeName);

            // Add to cache
            cache[source] = result;
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {typeName} for {(source as UnityEngine.Object)?.name ?? "unknown"}");

            return result;
        }

        private static TDest ConvertComponent<TSource, TDest>(
        TSource source,
        Dictionary<TSource, TDest> cache,
        GameObject targetGameObject,
        string typeName)
        where TDest : Component
        where TSource : Component
        {
            if (source == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) {typeName} source is null");
                return null!;
            }

            // Check cache first
            if (cache.TryGetValue(source, out TDest cached))
            {
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached {typeName} for {source.name}");
                return cached;
            }

            // Create new component
            TDest result = targetGameObject.AddComponent<TDest>();

            // Copy fields and properties
            CopyFields(source, result, typeName);
            CopyProperties(source, result, typeName);

            // Add to cache
            cache[source] = result;
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {typeName} for {source.name}");

            return result;
        }

        public static void ConvertDepPikminType<TSource, TDest>(
        TSource source,
        TDest destination,
        string typeName)
            where TSource : Component
            where TDest : Component
        {
            try
            {
                var sourceType = source.GetType();
                var destType = destination.GetType();

                // Get all fields from source type
                var sourceFields = sourceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var sourceField in sourceFields)
                {
                    // Check if this is a LibPikminType field
                    if (sourceField.FieldType.Name == "LibPikminType")
                    {
                        // Find corresponding field in destination
                        var destField = destType.GetField(sourceField.Name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        if (destField != null && destField.FieldType.Name == "PikminType")
                        {
                            // Get the LibPikminType value from source
                            var sourceValue = sourceField.GetValue(source);

                            if (sourceValue != null)
                            {
                                // Convert LibPikminType to normal Pikmin Type
                                var convertedValue = DepsManager.MatchPossibleTypeWithDep((LibPikminType)sourceValue);

                                // Set the converted value to destination
                                destField.SetValue(destination, convertedValue);

                                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted LibPikminType field {sourceField.Name} for {typeName}");
                            }
                        }
                    }
                }

                // Do the same for properties
                var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var sourceProp in sourceProperties)
                {
                    if (sourceProp.PropertyType.Name == "LibPikminType")
                    {
                        var destProp = destType.GetProperty(sourceProp.Name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        if (destProp != null && destProp.PropertyType.Name == "PikminType" && destProp.CanWrite)
                        {
                            var sourceValue = sourceProp.GetValue(source);

                            if (sourceValue != null)
                            {
                                var convertedValue = DepsManager.MatchPossibleTypeWithDep((LibPikminType)sourceValue);
                                destProp.SetValue(destination, convertedValue);

                                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted LibPikminType property {sourceProp.Name} for {typeName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) Error processing Pikmin type fields: {ex}");
            }
        }


        public static Dictionary<LethalMinLibrary.PikminModelRefernces, PikminModelRefernces> ConvertedModelRefs = new Dictionary<LethalMinLibrary.PikminModelRefernces, PikminModelRefernces>();
        public static Dictionary<LethalMinLibrary.PikminModelGeneration, PikminModelGeneration> ConvertedModelGenerations = new Dictionary<LethalMinLibrary.PikminModelGeneration, PikminModelGeneration>();
        public static Dictionary<LethalMinLibrary.PikminAnimatorController, PikminAnimatorController> ConvertedAnimatorControllers = new Dictionary<LethalMinLibrary.PikminAnimatorController, PikminAnimatorController>();
        public static Dictionary<LibPikminSoundPack, PikminSoundPack> ConvertedSoundPacks = new Dictionary<LibPikminSoundPack, PikminSoundPack>();
        public static Dictionary<LibPikminAnimationPack, PikminAnimationPack> ConvertedAnimationPacks = new Dictionary<LibPikminAnimationPack, PikminAnimationPack>();
        public static Dictionary<LethalMinLibrary.OnionModelRefernces, OnionModelRefernces> ConvertedOnionModelRefs = new Dictionary<LethalMinLibrary.OnionModelRefernces, OnionModelRefernces>();
        public static Dictionary<LethalMinLibrary.OnionModelGeneration, OnionModelGeneration> ConvertedOnionModelGenerations = new Dictionary<LethalMinLibrary.OnionModelGeneration, OnionModelGeneration>();
        public static Dictionary<LethalMinLibrary.PikminLinkAnimation, PikminLinkAnimation> ConvertedPikminLinks = new Dictionary<LethalMinLibrary.PikminLinkAnimation, PikminLinkAnimation>();
        public static Dictionary<LethalMinLibrary.OnionSoundPack, OnionSoundPack> ConvertedOnionSoundPacks = new Dictionary<LethalMinLibrary.OnionSoundPack, OnionSoundPack>();
        public static Dictionary<LethalMinLibrary.BaseOnionFusionProperties, BaseOnionFusionProperties> ConvertedOnionFusionProperties = new Dictionary<LethalMinLibrary.BaseOnionFusionProperties, BaseOnionFusionProperties>();
        public static Dictionary<LethalMinLibrary.SproutModelRefences, SproutModelRefences> ConvertedSproutRefs = new Dictionary<LethalMinLibrary.SproutModelRefences, SproutModelRefences>();
        public static Dictionary<LethalMinLibrary.SproutModelGeneration, SproutModelGeneration> ConvertedSproutGenerations = new Dictionary<LethalMinLibrary.SproutModelGeneration, SproutModelGeneration>();
        public static Dictionary<LethalMinLibrary.OnionItemModelGeneration, OnionItemModelGeneration> ConvertedOnionItemModelGenerations = new Dictionary<LethalMinLibrary.OnionItemModelGeneration, OnionItemModelGeneration>();
        public static Dictionary<LethalMinLibrary.OnionItemModelRefernces, OnionItemModelRefernces> ConvertedOnionItemModelRefs = new Dictionary<LethalMinLibrary.OnionItemModelRefernces, OnionItemModelRefernces>();
        public static Dictionary<LibPiklopediaEntry, PiklopediaEntry> ConvertedPiklopediaEntries = new Dictionary<LibPiklopediaEntry, PiklopediaEntry>();
        public static OnionType ConvertFromLibOnionType(LibOnionType libOnion)
        {
            OnionType onion = ScriptableObject.CreateInstance<OnionType>();
            CopyFields(libOnion, onion, nameof(OnionType));
            CopyProperties(libOnion, onion, nameof(OnionType));

            ConvertModelComponents(libOnion, onion);

            ConvertItemModelComponents(libOnion, onion);

            return onion;
        }

        public static void ConvertItemModelComponents(LibOnionType libType, OnionType type)
        {
            if (libType.OnionItemOverrideModelPrefab == null)
            {
                return;
            }

            LethalMinLibrary.OnionItemModelRefernces libModelRefs = libType.OnionItemOverrideModelPrefab.GetComponentInChildren<LethalMinLibrary.OnionItemModelRefernces>();

            if (libModelRefs == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) OnionModelRefernces is null for {libType.name}");
                return;
            }
            OnionItemModelRefernces modelRefs = null!;

            // Check cache for existing model references
            if (ConvertedOnionItemModelRefs.TryGetValue(libModelRefs, out OnionItemModelRefernces cachedModelRef))
            {
                modelRefs = cachedModelRef;
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached model references for {libType.name}");
            }
            else
            {
                // Create new model references
                modelRefs = ConvertComponent<LethalMinLibrary.OnionItemModelRefernces, OnionItemModelRefernces>(
                libModelRefs,
                ConvertedOnionItemModelRefs,
                libModelRefs.gameObject,
                nameof(OnionItemModelRefernces));
            }


            // Convert generations
            List<OnionItemModelGeneration> generations = new List<OnionItemModelGeneration>();
            foreach (var libGeneration in libModelRefs.Generations)
            {
                OnionItemModelGeneration generation = null!;
                if (ConvertedOnionItemModelGenerations.TryGetValue(libGeneration, out OnionItemModelGeneration existingGeneration))
                {
                    generation = existingGeneration;
                }
                else
                {
                    generation = ConvertComponent<LethalMinLibrary.OnionItemModelGeneration, OnionItemModelGeneration>(
                        libGeneration,
                        ConvertedOnionItemModelGenerations,
                        libGeneration.gameObject,
                        nameof(OnionItemModelGeneration));
                }

                generations.Add(generation);
            }

            modelRefs.Generations = generations.ToArray();
        }

        public static void ConvertModelComponents(LibOnionType libType, OnionType type)
        {
            if (libType.OnionOverrideModelPrefab == null)
            {
                return;
            }

            LethalMinLibrary.OnionModelRefernces libModelRefs = libType.OnionOverrideModelPrefab.GetComponentInChildren<LethalMinLibrary.OnionModelRefernces>();

            if (libModelRefs == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) OnionModelRefernces is null for {libType.name}");
                return;
            }
            OnionModelRefernces modelRefs = null!;

            // Check cache for existing model references
            if (ConvertedOnionModelRefs.TryGetValue(libModelRefs, out OnionModelRefernces cachedModelRef))
            {
                modelRefs = cachedModelRef;
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached model references for {libType.name}");
            }
            else
            {
                // Create new model references
                modelRefs = ConvertComponent<LethalMinLibrary.OnionModelRefernces, OnionModelRefernces>(
                libModelRefs,
                ConvertedOnionModelRefs,
                libModelRefs.gameObject,
                nameof(OnionModelRefernces));
            }


            // convert the links
            foreach (LethalMinLibrary.PikminLinkAnimation libLink in libModelRefs.ClimbLinks)
            {
                if (ConvertedPikminLinks.TryGetValue(libLink, out PikminLinkAnimation existingLink))
                {
                    modelRefs.ClimbLinks.Add(existingLink);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached link for {libType.name}");
                }
                else
                {
                    // Convert and cache the link
                    PikminLinkAnimation link = ConvertComponent<LethalMinLibrary.PikminLinkAnimation, PikminLinkAnimation>(
                            libLink,
                            ConvertedPikminLinks,
                            libLink.gameObject,
                            nameof(PikminLinkAnimation));

                    modelRefs.ClimbLinks.Add(link);
                }
            }

            // convert the sound pack
            if (libModelRefs.SoundPack != null)
            {
                if (ConvertedOnionSoundPacks.TryGetValue(libModelRefs.SoundPack, out OnionSoundPack existingSoundPack))
                {
                    modelRefs.SoundPack = existingSoundPack;
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached sound pack for {libType.name}");
                }
                else
                {
                    OnionSoundPack soundPack = ConvertComponent<LethalMinLibrary.OnionSoundPack, OnionSoundPack>(
                        libModelRefs.SoundPack,
                        ConvertedOnionSoundPacks,
                        libModelRefs.SoundPack.gameObject,
                        nameof(OnionSoundPack));
                    modelRefs.SoundPack = soundPack;
                }
            }

            // convert the fusion properties
            if (libModelRefs.FusionProperties != null)
            {
                if (ConvertedOnionFusionProperties.TryGetValue(libModelRefs.FusionProperties, out BaseOnionFusionProperties existingFusionProperties))
                {
                    modelRefs.FusionProperties = existingFusionProperties;
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached fusion properties for {libType.name}");
                }
                else
                {
                    BaseOnionFusionProperties fusionProperties = ConvertComponent<LethalMinLibrary.BaseOnionFusionProperties, BaseOnionFusionProperties>(
                        libModelRefs.FusionProperties,
                        ConvertedOnionFusionProperties,
                        libModelRefs.FusionProperties.gameObject,
                        nameof(BaseOnionFusionProperties));
                    modelRefs.FusionProperties = fusionProperties;
                }
            }

            // Convert generations
            List<OnionModelGeneration> generations = new List<OnionModelGeneration>();
            foreach (var libGeneration in libModelRefs.Generations)
            {
                OnionModelGeneration generation = null!;
                if (ConvertedOnionModelGenerations.TryGetValue(libGeneration, out OnionModelGeneration existingGeneration))
                {
                    generation = existingGeneration;
                }
                else
                {
                    generation = ConvertComponent<LethalMinLibrary.OnionModelGeneration, OnionModelGeneration>(
                        libGeneration,
                        ConvertedOnionModelGenerations,
                        libGeneration.gameObject,
                        nameof(OnionModelGeneration));
                }

                // convert the links
                foreach (LethalMinLibrary.PikminLinkAnimation libLink in libGeneration.ClimbLinks)
                {
                    if (ConvertedPikminLinks.TryGetValue(libLink, out PikminLinkAnimation existingLink))
                    {
                        generation.ClimbLinks.Add(existingLink);
                        LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached link for {libType.name}");
                    }
                    else
                    {
                        // Convert and cache the link
                        PikminLinkAnimation link = ConvertComponent<LethalMinLibrary.PikminLinkAnimation, PikminLinkAnimation>(
                                libLink,
                                ConvertedPikminLinks,
                                libLink.gameObject,
                                nameof(PikminLinkAnimation));

                        generation.ClimbLinks.Add(link);
                    }
                }

                // convert the sound pack
                if (libGeneration.SoundPack != null)
                {
                    if (ConvertedOnionSoundPacks.TryGetValue(libGeneration.SoundPack, out OnionSoundPack existingSoundPack))
                    {
                        generation.SoundPack = existingSoundPack;
                        LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached sound pack for {libType.name}");
                    }
                    else
                    {
                        OnionSoundPack soundPack = ConvertComponent<LethalMinLibrary.OnionSoundPack, OnionSoundPack>(
                            libGeneration.SoundPack,
                            ConvertedOnionSoundPacks,
                            libGeneration.SoundPack.gameObject,
                            nameof(OnionSoundPack));
                        generation.SoundPack = soundPack;
                    }
                }

                // convert the fusion properties
                if (libGeneration.FusionProperties != null)
                {
                    if (ConvertedOnionFusionProperties.TryGetValue(libGeneration.FusionProperties, out BaseOnionFusionProperties existingFusionProperties))
                    {
                        generation.FusionProperties = existingFusionProperties;
                        LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached fusion properties for {libType.name}");
                    }
                    else
                    {
                        BaseOnionFusionProperties fusionProperties = ConvertComponent<LethalMinLibrary.BaseOnionFusionProperties, BaseOnionFusionProperties>(
                            libGeneration.FusionProperties,
                            ConvertedOnionFusionProperties,
                            libGeneration.FusionProperties.gameObject,
                            nameof(BaseOnionFusionProperties));
                        generation.FusionProperties = fusionProperties;
                    }
                }

                generations.Add(generation);
            }

            modelRefs.Generations = generations.ToArray();
        }

        public static PikminType ConvertFromLibPikminType(LibPikminType libType)
        {
            PikminType type = ScriptableObject.CreateInstance<PikminType>();
            type.name = libType.name;

            // Copy fields and properties using reflection
            CopyFields(libType, type, nameof(PikminType));
            CopyProperties(libType, type, nameof(PikminType));

            // Custom conversion for GrowthStageStats
            ConvertGrowthStageStats(libType, type);

            // Custom conversion for model components
            ConvertModelComponents(libType, type);

            // Custom conversion for override sprout model
            ConvertSproutComponets(libType, type);

            // Custom SoundPack conversion
            ConvertSoundPack(libType, type);

            return type;
        }

        public static void ConvertModelComponents(LibPikminType libType, PikminType type)
        {
            if (libType.modelRefernces == null)
            {
                LethalMin.Logger.LogError($"(LETHALMIN_CONVERTER) modelRefernces is null for {libType.name}");
                return;
            }

            // Check cache for existing model references
            if (ConvertedModelRefs.TryGetValue(libType.modelRefernces, out PikminModelRefernces cachedModelRef))
            {
                type.modelRefernces = cachedModelRef;
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached model references for {libType.name}");
                return;
            }

            // Create new model references
            PikminModelRefernces modelRefs = ConvertComponent<LethalMinLibrary.PikminModelRefernces, PikminModelRefernces>(
            libType.modelRefernces,
            ConvertedModelRefs,
            libType.modelRefernces.gameObject,
            nameof(PikminModelRefernces));

            // Handle animator controller
            if (libType.modelRefernces.AnimatorController != null)
            {
                modelRefs.AnimatorController = ConvertAnimatorController(
                    libType.modelRefernces.AnimatorController,
                    libType.name);
            }

            // Convert generations
            List<PikminModelGeneration> generations = new List<PikminModelGeneration>();
            foreach (var libGeneration in libType.modelRefernces.Generations)
            {
                PikminModelGeneration generation = ConvertComponent<LethalMinLibrary.PikminModelGeneration, PikminModelGeneration>(
                    libGeneration,
                    ConvertedModelGenerations,
                    libGeneration.gameObject,
                    nameof(PikminModelGeneration));

                // Handle animator controller for generation
                if (libGeneration.AnimatorController != null)
                {
                    generation.AnimatorController = ConvertAnimatorController(
                    libGeneration.AnimatorController,
                    $"{libType.name}_Generation{libType.modelRefernces.Generations.ToList().IndexOf(libGeneration)}");
                }

                generations.Add(generation);
            }

            modelRefs.Generations = generations.ToArray();
            type.modelRefernces = modelRefs;
        }

        private static PikminAnimatorController ConvertAnimatorController(
        LethalMinLibrary.PikminAnimatorController source,
        string contextName)
        {
            if (source == null) return null!;

            // Check cache first
            if (ConvertedAnimatorControllers.TryGetValue(source, out PikminAnimatorController cached))
            {
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached animator controller for {contextName}");
                return cached;
            }

            // Create new component
            PikminAnimatorController controller = source.gameObject.AddComponent<PikminAnimatorController>();
            CopyFields(source, controller, nameof(PikminAnimatorController));
            CopyProperties(source, controller, nameof(PikminAnimatorController));

            // Handle animation pack
            if (source.AnimPack != null)
            {
                controller.AnimPack = ConvertAnimationPack(source.AnimPack);
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Assigned animation pack to animator controller for {contextName}");
            }

            // Add to cache
            ConvertedAnimatorControllers[source] = controller;
            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted animator controller for {contextName}");

            return controller;
        }

        public static PikminAnimationPack ConvertAnimationPack(LibPikminAnimationPack libAnimPack)
        {
            return ConvertScriptableObject<LibPikminAnimationPack, PikminAnimationPack>(
                libAnimPack,
                ConvertedAnimationPacks,
                nameof(PikminAnimationPack));
        }

        public static void ConvertSproutComponets(LibPikminType libType, PikminType type)
        {
            if (libType.SproutOverrideModel == null)
            {
                return;
            }

            LethalMinLibrary.SproutModelRefences sproutRefs = libType.SproutOverrideModel.GetComponentInChildren<LethalMinLibrary.SproutModelRefences>();

            SproutModelRefences sproutModelRefs = null!;
            if (ConvertedSproutRefs.TryGetValue(sproutRefs, out SproutModelRefences cachedSproutRefs))
            {
                sproutModelRefs = cachedSproutRefs;
                LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Using cached sprout model references for {libType.name}");
            }
            else
            {
                sproutModelRefs = ConvertComponent<LethalMinLibrary.SproutModelRefences, SproutModelRefences>(
                    sproutRefs,
                    ConvertedSproutRefs,
                    sproutRefs.gameObject,
                    nameof(SproutModelRefences));
            }

            // Convert generations
            List<SproutModelGeneration> generations = new List<SproutModelGeneration>();
            foreach (var libGeneration in sproutRefs.Generations)
            {
                SproutModelGeneration generation = null!;
                if (ConvertedSproutGenerations.TryGetValue(libGeneration, out SproutModelGeneration existingGeneration))
                {
                    generation = existingGeneration;
                }
                else
                {
                    generation = ConvertComponent<LethalMinLibrary.SproutModelGeneration, SproutModelGeneration>(
                        libGeneration,
                        ConvertedSproutGenerations,
                        libGeneration.gameObject,
                        nameof(SproutModelGeneration));
                }

                generations.Add(generation);
            }
            sproutModelRefs.Generations = generations.ToArray();
        }

        public static void ConvertSoundPack(LibPikminType libType, PikminType type)
        {
            LibPikminSoundPack libSoundPack = libType.SoundPack;

            if (libSoundPack == null)
            {
                LethalMin.Logger.LogError("(LETHALMIN_CONVERTER) LibPikminSoundPack is null");
                return;
            }

            PikminSoundPack soundPack = ConvertScriptableObject<LibPikminSoundPack, PikminSoundPack>(
                libSoundPack,
                ConvertedSoundPacks,
                nameof(PikminSoundPack));

            // Add to asset bundle loader
            AssetBundleLoader.instance.customSoundPacks.Add(soundPack);
            type.SoundPack = soundPack;
        }

        private static void ConvertGrowthStageStats(LibPikminType libType, PikminType type)
        {
            if (libType.GrowStages == null || libType.GrowStages.Count == 0)
            {
                LethalMin.Logger.LogDebug("(LETHALMIN_CONVERTER) No GrowthStageStats to convert");
                return;
            }

            // Initialize the lists
            type.growSpeeds = new List<float>();
            type.growAttacks = new List<float>();
            type.growCarryStrengths = new List<int>();

            // Convert each growth stage stat
            foreach (var growthStat in libType.GrowStages)
            {
                LethalMin.Logger.LogInfo($"Stat: {libType.GrowStages.IndexOf(growthStat)} | Speed: {growthStat.Speed} | Attack: {growthStat.AttackStrength} | Carry: {growthStat.CarryStrength}");
                type.growSpeeds.Add(growthStat.Speed);
                type.growAttacks.Add(growthStat.AttackStrength);
                type.growCarryStrengths.Add(growthStat.CarryStrength);
            }

            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {libType.GrowStages.Count} growth stage stats");
        }

        public static PiklopediaEntry ConvertFromLibPiklopediaEntry(LibPiklopediaEntry libEntry)
        {
            return ConvertScriptableObject<LibPiklopediaEntry, PiklopediaEntry>(
                libEntry,
                ConvertedPiklopediaEntries,
                nameof(PiklopediaEntry));
        }

        public static void ClearCache(bool ClearLibSO = false)
        {
            if (ClearLibSO)
            {
                foreach (var key in ConvertedSoundPacks.Keys.ToList())
                {
                    UnityEngine.Object.Destroy(key);
                }
                foreach (var key in ConvertedAnimationPacks.Keys.ToList())
                {
                    UnityEngine.Object.Destroy(key);
                }
                foreach (var key in ConvertedPiklopediaEntries.Keys.ToList())
                {
                    UnityEngine.Object.Destroy(key);
                }
                foreach (var obj in LibAssetBundleLoader.LibTypesToRemove)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                ConvertedSoundPacks.Clear();
                ConvertedAnimationPacks.Clear();
                ConvertedPiklopediaEntries.Clear();
                LibAssetBundleLoader.LibTypesToRemove.Clear();
            }

            foreach (var key in ConvertedModelRefs.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedModelGenerations.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedAnimatorControllers.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedOnionModelRefs.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedOnionModelGenerations.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedPikminLinks.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedOnionSoundPacks.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedOnionFusionProperties.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedSproutRefs.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedSproutGenerations.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedOnionItemModelGenerations.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            foreach (var key in ConvertedOnionItemModelRefs.Keys.ToList())
            {
                UnityEngine.Object.Destroy(key);
            }
            ConvertedModelRefs.Clear();
            ConvertedModelGenerations.Clear();
            ConvertedAnimatorControllers.Clear();
            ConvertedOnionModelRefs.Clear();
            ConvertedOnionModelGenerations.Clear();
            ConvertedPikminLinks.Clear();
            ConvertedOnionSoundPacks.Clear();
            ConvertedOnionFusionProperties.Clear();
            ConvertedSproutRefs.Clear();
            ConvertedSproutGenerations.Clear();
            ConvertedOnionItemModelGenerations.Clear();
            ConvertedOnionItemModelRefs.Clear();
        }

        #region Legacy Conversion
#pragma warning disable CS0612 // Type or member is obsolete
        /// <summary>
        /// Because the legacy variable names a different from these varible names, and the legacy pikmin type will never be changed.
        /// We need to do this manually.
        /// </summary>
        /// <param name="legacyType"></param>
        /// <returns></returns>
        public static PikminType ConvertFromLegacyPikminType(LethalMinLibrary.PikminType legacyType)
        {
            PikminType type = ScriptableObject.CreateInstance<PikminType>();
            PikminModelRefernces modelRefs = ConvertMeshRefToModelRef(legacyType.MeshRefernces);
            if (modelRefs == null || legacyType.MeshPrefab == null)
            {
                LethalMin.Logger.LogError($"Failed to convert MeshRef to Model Ref for {legacyType.PikminName}");
                return null!;
            }

            modelRefs.AnimatorController = ConvertLegecyAnimatorToOverrideController(ref legacyType.MeshRefernces.PikminAnimator);

            type.PikminName = legacyType.PikminName;
            type.PikminPrimaryColor = legacyType.PikminColor;
            type.PikminSecondaryColor = legacyType.PikminColor2;
            type.PikminIcon = legacyType.PikminIcon;


            type.ModelPrefab = legacyType.MeshPrefab;
            type.modelRefernces = modelRefs;
            type.SproutOverrideModel = legacyType.SproutMeshPrefab;

            type.SoundPack = ScriptableObject.CreateInstance<PikminSoundPack>();
            if (legacyType.soundPack != null)
            {
                type.SoundPack.NoticeVoice = legacyType.soundPack.NoticeVoiceLine;

                type.SoundPack.AttackVoice = legacyType.soundPack.AttackVoiceLine;

                type.SoundPack.BornVoice = legacyType.soundPack.BornVoiceLine;

                type.SoundPack.ExitOnionVoice = legacyType.soundPack.ExitOnionVoiceLine;

                type.SoundPack.EnterOnionVoice = legacyType.soundPack.EnterOnionVoiceLine;

                type.SoundPack.ObjectNoticeVoice = legacyType.soundPack.ItemNoticeVoiceLine;

                type.SoundPack.GhostSound = legacyType.soundPack.GhostVoiceLine;

                type.SoundPack.ItemCarryVoice = legacyType.soundPack.CarryVoiceLine;

                type.SoundPack.LostVoice = legacyType.soundPack.LostVoiceLine;

                type.SoundPack.ItemLiftVoice = legacyType.soundPack.LiftVoiceLine;

                type.SoundPack.HurtVoice = legacyType.soundPack.HurtVoiceLine;

                type.SoundPack.CrushVoice = legacyType.soundPack.CrushedVoiceLine;

                type.SoundPack.NoticeVoice = legacyType.soundPack.NoticeVoiceLine;

                type.SoundPack.ThrownVoice = legacyType.soundPack.ThrowVoiceLine;

                type.SoundPack.PrepareVoice = legacyType.soundPack.HoldVoiceLine;

                type.SoundPack.YayVoice = legacyType.soundPack.YayVoiceLine;

                type.SoundPack.CoughVoice = legacyType.soundPack.CoughVoiceLine;

                type.SoundPack.ThrownSound = legacyType.soundPack.ThrowSFX;

                type.SoundPack.HitSound = legacyType.soundPack.HitSFX;
            }
            else
            {
                type.SoundPack = LethalMin.DefaultSoundPack;
            }


            foreach (LethalMinLibrary.LibHazardType hazd in legacyType.HazardsResistantTo)
            {
                int val = (int)hazd;
                type.HazardsResistantTo.Add((PikminHazard)val);
            }

            type.CanLatchOnToObjects = legacyType.CanLatchOnToEnemies;
            type.CanJumpOntoObjects = legacyType.CanLatchOnToEnemies;
            type.CanCarryObjects = legacyType.CanCarryItems;

            //legacyType.CanAttackWithoutLatchingOn = true;

            type.growSpeeds = legacyType.Speeds.ToList();

            //legacyType.ThrowForce = 15f;

            type.GrowSpeedMultiplier = legacyType.GrowSpeedMultipler;

            type.AttackStrength = legacyType.DamageAmmount;
            type.DamageDeltUponDeath = legacyType.DamageDeltUponDeath;
            type.DeathDamageRange = legacyType.DeathDamageRange;
            type.AttackDistance = legacyType.AttackRange;
            legacyType.AttackRate = -1f;

            type.CarryStrength = legacyType.CarryStrength;
            type.ItemDetectionRange = legacyType.ItemDetectionRange;

            type.SpawnsNaturally = legacyType.SpawnsNaturally;
            type.SpawnsIndoors = legacyType.SpawnsIndoors;
            type.SpawnsOutdoors = legacyType.SpawnsOutdoors;
            type.SpawnsAsSprout = legacyType.SpawnsAsSprout;

            type.UsesPikminContainer = legacyType.UsesPikminContainer;
            type.InstaEnterOnion = legacyType.InstaEnterOnion;

            type.GenerateConfigFile = legacyType.GenerateConfigFile = true;

            //legacyType.TargetOnion;

            legacyType.ExtraIdleAnimsCount = 0;
            legacyType.YayAnimationsCount = 0;

            type.ScientificName = legacyType.ScientificName;
            type.piklopediaDescription = legacyType.beastiarySegment;

            return type;
        }

        public static PikminModelRefernces ConvertMeshRefToModelRef(LethalMinLibrary.PikminMeshRefernces? meshRef)
        {
            if (meshRef == null)
            {
                LethalMin.Logger.LogError("(LETHALMIN_CONVERTER) attempted to convert a null meshref");
                return null!;
            }
            PikminModelRefernces modelRef = meshRef.gameObject.AddComponent<PikminModelRefernces>();

            modelRef.Generations = new PikminModelGeneration[0];
            modelRef.Model = meshRef.gameObject;
            if (meshRef.PikminAnimator != null)
                modelRef.Animator = meshRef.PikminAnimator;
            modelRef.Plants = meshRef.PikminGrowthStagePlants.ToList();
            if (meshRef.PikminGlowRoot != null)
                modelRef.SproutTop = meshRef.PikminGlowRoot;

            return modelRef;
        }


        public static PikminAnimatorController ConvertLegecyAnimatorToOverrideController(ref Animator? legacyAnimator)
        {
            if (legacyAnimator == null)
            {
                LethalMin.Logger.LogError("(LETHALMIN_CONVERTER) attempted to convert a null AnimatorOverrideController");
                return null!;
            }

            LethalMin.Logger.LogMessage($"_-_-converting {legacyAnimator.gameObject.name}-_-_");

            RuntimeAnimatorController LegacyController = legacyAnimator.runtimeAnimatorController;
            RuntimeAnimatorController BaseController = LethalMin.assetBundle.LoadAsset<RuntimeAnimatorController>("Assets/LethalMin/Animations/BasePikminAnimator.controller");
            AnimatorOverrideController overrideController = new AnimatorOverrideController(BaseController);
            PikminAnimationPack animPack = ScriptableObject.CreateInstance<PikminAnimationPack>();
            PikminAnimatorController controller = legacyAnimator.gameObject.AddComponent<PikminAnimatorController>();
            controller.animator = legacyAnimator;
            controller.AnimPack = animPack;
            animPack.name = $"{legacyAnimator.gameObject.name}_AnimPack";
            animPack.AnimatorOverrideController = overrideController;

            // Dictionary mapping base animation names to potential legacy animation names/patterns
            Dictionary<string, string[]> animationNameMappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Wait", new[] { "Idle" } },
                { "Run02", new[] { "Run", "Move", "Walk" } },
                { "Attack01", new[] { "AttackStand" } },
                { "Attach", new[] { "Attack" } },
                { "Flop", new[] { "Flop", "Poisened" } },
                { "Carry", new[] { "Carry" } },
                { "Hold", new[] { "Hold" } },
                { "Drown", new[] { "Drown", "Drowning" } },
                { "Notice", new[] { "Notice" } },
                { "Throw", new[] { "Throw", "Thrown" } },
                { "Knockback", new[] { "Knockback" } },
                { "Lay", new[] { "Lay", "Laying" } },
                // Add more mappings as needed
            };

            // Dictionary mapping animation names to their corresponding fields in PikminAnimationPack
            Dictionary<string, Action<PikminAnimationPack, AnimationClip>> animPackMappings = new Dictionary<string, Action<PikminAnimationPack, AnimationClip>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Wait", (pack, clip) => pack.EditorIdleAnim.Add(clip) },
                { "Run02", (pack, clip) => pack.EditorWalkingAnim = clip },
                { "Attack01", (pack, clip) => pack.EditorStandingAttackAnim = clip },
                { "Attach", (pack, clip) => pack.EditorLatchedAttackAnim = clip },
                { "Flop", (pack, clip) => pack.EditorPosionFlopAnim = clip },
                { "Carry", (pack, clip) => pack.EditorCarryAnim = clip },
                { "Hold", (pack, clip) => pack.EditorHoldAnim = clip },
                { "Drown", (pack, clip) => pack.EditorDrowingAnim = clip },
                { "Notice", (pack, clip) => pack.EditorNoticeAnim = clip },
                { "Throw", (pack, clip) => pack.EditorThrowAnim = clip },
                { "Knockback", (pack, clip) => pack.EditorKnockbackAnim = clip },
                { "Lay", (pack, clip) => pack.EditorLayingAnim = clip },
                { "GetUp", (pack, clip) => pack.EditorGetUpAnim = clip },
                { "Plucked", (pack, clip) => pack.EditorPluckedAnim = clip },
                { "Yay", (pack, clip) => pack.EditorYayAnim.Add(clip) },
                { "Burn", (pack, clip) => pack.EditorBurnAnim = clip },
                // Map other animations as needed
            };

            // Copy the animation clips from the legacy animator to the override controller and animation pack
            foreach (var baseClip in BaseController.animationClips)
            {
                AnimationClip legacyClip = null!;

                // First try exact match with ToLower
                legacyClip = Array.Find(LegacyController.animationClips, clip =>
                    clip.name.ToLower() == baseClip.name.ToLower());

                // If not found, check if we have mappings for this animation
                if (legacyClip == null && animationNameMappings.TryGetValue(baseClip.name, out string[] possibleNames))
                {
                    // Try each possible alternative name
                    foreach (string possibleName in possibleNames)
                    {
                        // Try exact matches with ToLower
                        legacyClip = Array.Find(LegacyController.animationClips, clip =>
                            clip.name.ToLower() == possibleName.ToLower());

                        if (legacyClip != null)
                            break;

                        // Try contains with ToLower
                        legacyClip = Array.Find(LegacyController.animationClips, clip =>
                            clip.name.ToLower().Contains(possibleName.ToLower()));

                        if (legacyClip != null)
                            break;
                    }
                }

                // If still not found, try contains with the base clip name
                if (legacyClip == null)
                {
                    legacyClip = Array.Find(LegacyController.animationClips, clip =>
                        clip.name.ToLower().Contains(baseClip.name.ToLower()));
                }

                // Last resort: try the inverse - check if base name contains any legacy clip name
                if (legacyClip == null)
                {
                    legacyClip = Array.Find(LegacyController.animationClips, clip =>
                        baseClip.name.ToLower().Contains(clip.name.ToLower()));
                }

                if (legacyClip != null)
                {
                    // Set the override for this clip in the override controller
                    overrideController[baseClip.name] = legacyClip;
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Overriding {baseClip.name} with {legacyClip.name}");

                    // Also populate the animation pack if there's a mapping for this animation
                    if (animPackMappings.TryGetValue(baseClip.name, out var assignAction))
                    {
                        assignAction(animPack, legacyClip);
                        LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Assigned {legacyClip.name} to animation pack for {baseClip.name}");
                    }
                }
                else
                {
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Failed to find matching animation for {baseClip.name}");
                }
            }

            // Process any remaining legacy clips that might not have been mapped to the base controller
            foreach (var legacyClip in LegacyController.animationClips)
            {
                // Check for additional animations like Yay, OneShot idles, etc.
                if (legacyClip.name.ToLower().Contains("yay"))
                {
                    animPack.EditorYayAnim.Add(legacyClip);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Added additional Yay animation: {legacyClip.name}");
                }
                else if (legacyClip.name.ToLower().Contains("oneshot") || legacyClip.name.ToLower().Contains("idle_special"))
                {
                    animPack.EditorOneShotIdleAnim.Add(legacyClip);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Added OneShot idle animation: {legacyClip.name}");
                }
                else if (legacyClip.name.ToLower().Contains("misc") || legacyClip.name.ToLower().Contains("alt"))
                {
                    animPack.EditorMiscOneshotAnim.Add(legacyClip);
                    LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Added Misc oneshot animation: {legacyClip.name}");
                }
            }

            legacyAnimator.runtimeAnimatorController = overrideController;
            return controller;
        }
#pragma warning restore CS0612 // Type or member is obsolete
        #endregion
    }
}