using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

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
            LethalMin.Logger.LogInfo($"Loaded {bundleFiles.Length} bundles");
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
                ProcessLoadedBundle(request.assetBundle);
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to load bundle: {bundlePath}");
            }
        }

        private void ProcessLoadedBundle(AssetBundle bundle)
        {
            // Load PikminTypes
            PikminType[] pikminTypes = bundle.LoadAllAssets<PikminType>();
            foreach (PikminType pikminType in pikminTypes)
            {
                LethalMin.RegisterPikminType(pikminType);
            }

            // Load OnionTypes
            OnionType[] onionTypes = bundle.LoadAllAssets<OnionType>();
            foreach (OnionType onionType in onionTypes)
            {
                LethalMin.RegisterOnionType(onionType);
            }

            // Load OnionFuseRules
            OnionFuseRules[] fuseRules = bundle.LoadAllAssets<OnionFuseRules>();
            foreach (OnionFuseRules fuseRule in fuseRules)
            {
                LethalMin.RegisterFuseRule(fuseRule);
            }
        }
    }
}