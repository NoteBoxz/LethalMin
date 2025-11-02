using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;
using LethalMinLibrary;
using System;
using System.Reflection;
using System.Linq;

namespace LethalMin.Library
{
    [HarmonyPatch(typeof(ReplaceableComponet))]
    internal class ReplaceableComponetPatch
    {
        // Cache for component mappings
        private static Dictionary<string, Type> _componentMappings = null!;

        private static void InitializeComponentMappings()
        {
            _componentMappings = new Dictionary<string, Type>();

            // Get all types in this assembly
            var allTypes = LethalMin.GetTypesWithErrorHandling();

            // Look for types with ReplacementComponent attribute
            foreach (var type in allTypes)
            {
                if (!_componentMappings.ContainsKey(type.Name))
                    _componentMappings.Add(type.Name, type);
            }
        }

        [HarmonyPatch(nameof(ReplaceableComponet.Initialize))]
        [HarmonyPrefix]
        private static void Init4Real(ReplaceableComponet __instance)
        {
            if (_componentMappings == null)
            {
                InitializeComponentMappings();
            }

            if (_componentMappings == null)
            {
                LethalMin.Logger.LogError("Component mappings not initialized. This should never happen.");
                return;
            }

            string componentName = __instance.componentName;

            if (_componentMappings.TryGetValue(componentName, out var typeMapping))
            {
                // Get the actual runtime type of the instance (the derived class)
                Type sourceType = __instance.GetType();

                // Use reflection to call the generic method with the right types
                var method = typeof(ReplaceableComponetPatch).GetMethod(nameof(ConvertComponent),
                    BindingFlags.Public | BindingFlags.Static);

                // Use the actual runtime type as source, not just ReplaceableComponet
                var genericMethod = method.MakeGenericMethod(sourceType, typeMapping);

                genericMethod.Invoke(null, new object[] {
                    __instance,                 // Source is the actual derived instance
                    __instance.gameObject,      // Target game object remains the same
                    componentName
                });
            }
            else
            {
                LethalMin.Logger.LogWarning($"No replacement found for component: {componentName}");
            }

            GameObject.Destroy(__instance);
        }

        public static TDest ConvertComponent<TSource, TDest>(
          TSource source,
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

            // Create new component
            TDest result = targetGameObject.AddComponent<TDest>();

            // Copy fields and properties
            TypeConverter.CopyFields(source, result, typeName);
            TypeConverter.CopyProperties(source, result, typeName);
            TypeConverter.ConvertDepPikminType(source, result, typeName);

            LethalMin.Logger.LogDebug($"(LETHALMIN_CONVERTER) Converted {typeName} for {source.name}");

            return result;
        }
    }
}