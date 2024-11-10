using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using LethalMin.Library;

namespace LethalMin
{
    public class AssetBundleLoader : MonoBehaviour
    {
        public static AssetBundleLoader instance;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadLethalMinBundles();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private const string BUNDLE_EXTENSION = "*.lethalmin";
        internal static DirectoryInfo lethalMinFile = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
        internal static DirectoryInfo? lethalMinFolder;
        internal static DirectoryInfo? pluginsFolder;
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

        private void LoadLethalMinBundles()
        {
            lethalMinFolder = lethalMinFile.Parent;
            pluginsFolder = lethalMinFolder?.Parent;

            if (pluginsFolder == null || !pluginsFolder.Exists)
            {
                LethalMin.Logger.LogError($"Plugins folder not found: {pluginsFolder?.FullName}");
                return;
            }

            string[] bundleFiles = Directory.GetFiles(pluginsFolder.FullName, BUNDLE_EXTENSION, SearchOption.AllDirectories);

            foreach (string bundleFile in bundleFiles)
            {
                StartCoroutine(LoadBundleCoroutine(bundleFile));
            }
            switch (bundleFiles.Length)
            {
                case 0:
                    if (LethalMin.DebugMode)
                        LethalMin.Logger.LogInfo("No LethalMin bundles found in the plugins folder");
                    break;
                case 1:
                    LethalMin.Logger.LogInfo("Loaded 1 LethalMin bundle");
                    break;
                default:
                    LethalMin.Logger.LogInfo($"Loaded {bundleFiles.Length} LethalMin bundles");
                    break;
            }
        }

        private IEnumerator LoadBundleCoroutine(string bundlePath)
        {
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            if (request.assetBundle != null)
            {
                string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
                loadedBundles[bundleName] = request.assetBundle;
                LethalMin.Logger.LogInfo($"Loaded bundle: {bundleName}");

                // Process the loaded bundle
                ProcessLoadedLibBundle(request.assetBundle);
                ProcessLoadedBundle(request.assetBundle);
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to load bundle: {bundlePath}");
            }
        }

        private void ProcessLoadedLibBundle(AssetBundle bundle)
        {
            bool IsValidLethalMinBundle = false;
            // Load PikminTypes
            LethalMinLibrary.PikminType[] pikminTypes = bundle.LoadAllAssets<LethalMinLibrary.PikminType>();
            foreach (LethalMinLibrary.PikminType pikminType in pikminTypes)
            {
                LethalMin.RegisterPikminType(TypeConverter.Convert_Lib_PikminTypeToLmPikminType(pikminType));
                IsValidLethalMinBundle = true;
            }

            // Load OnionTypes
            LethalMinLibrary.OnionType[] onionTypes = bundle.LoadAllAssets<LethalMinLibrary.OnionType>();
            foreach (LethalMinLibrary.OnionType onionType in onionTypes)
            {
                LethalMin.RegisterOnionType(TypeConverter.Convert_Lib_OnionTypeToLmOnionType(onionType));
                IsValidLethalMinBundle = true;
            }

            // Load OnionFuseRules
            LethalMinLibrary.OnionFuseRules[] fuseRules = bundle.LoadAllAssets<LethalMinLibrary.OnionFuseRules>();
            foreach (LethalMinLibrary.OnionFuseRules fuseRule in fuseRules)
            {
                LethalMin.RegisterFuseRule(TypeConverter.Convert_Lib_OnionFuseRulesToLmOnionFuseRules(fuseRule));
                IsValidLethalMinBundle = true;
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