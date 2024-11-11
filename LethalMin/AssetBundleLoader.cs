using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using LethalMin.Library;
using UnityEngine.SceneManagement;

namespace LethalMin
{
    public class AssetBundleLoader : MonoBehaviour
    {
        public static AssetBundleLoader instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LethalMin AssetBundleLoader");
                instance = go.AddComponent<AssetBundleLoader>();
                DontDestroyOnLoad(go);
                instance.Initialize();
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Initialize()
        {
            // Move your initialization logic here
            LoadLethalMinBundles();
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
            if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                gameObject.AddComponent<LibAssetBundleLoader>();
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
                if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
                    gameObject.GetComponent<LibAssetBundleLoader>().ProcessLoadedLibBundle(request.assetBundle);
                ProcessLoadedBundle(request.assetBundle);
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to load bundle: {bundlePath}");
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
            if (!IsValidLethalMinBundle && !LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                LethalMin.Logger.LogWarning($"Bundle does not contain any valid LethalMin assets: {bundle.name} This could be because you are missing the LethalMinLibrary mod");
            }
        }
    }
}