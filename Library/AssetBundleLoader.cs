using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using LethalMin.Library;
using LethalMin.Pikmin;

namespace LethalMin
{
    public class AssetBundleLoader : MonoBehaviour
    {
        public static AssetBundleLoader instance = null!;

        public void Initialize()
        {
            LoadLethalMinBundles();
        }

        private const string BUNDLE_EXTENSION = "*.lethalmin";
        internal static DirectoryInfo lethalMinFile = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
        internal static DirectoryInfo lethalMinFolder = null!;
        internal static DirectoryInfo pluginsFolder = null!;
        private List<PikminType> customTypes = new List<PikminType>();
        private List<OnionType> customOtypes = new List<OnionType>();
        private List<OnionFuseRules> customFuseRules = new List<OnionFuseRules>();
        public List<PikminSoundPack> customSoundPacks = new List<PikminSoundPack>();
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        public static int BundlesToProcess, BundlesProcessed;

        private void LoadLethalMinBundles()
        {
            lethalMinFolder = lethalMinFile.Parent;
            pluginsFolder = lethalMinFolder.Parent;

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
            BundlesToProcess = bundleFiles.Length;
            System.Array.Sort(bundleFiles, (a, b) => string.Compare(Path.GetFileNameWithoutExtension(a), Path.GetFileNameWithoutExtension(b), System.StringComparison.Ordinal));

            foreach (string bundleFile in bundleFiles)
            {
                StartCoroutine(LoadBundleCoroutine(bundleFile));
            }
            if (BundlesToProcess > 0)
            {
                StartCoroutine(WaitToRegister());
            }
            switch (bundleFiles.Length)
            {
                case 0:
                    LethalMin.Logger.LogDebug("No LethalMin bundles found in the plugins folder");
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
                {
                    gameObject.GetComponent<LibAssetBundleLoader>().ProcessLoadedLibBundle(request.assetBundle);
                }
                ProcessLoadedBundle(request.assetBundle);

                BundlesProcessed++;

                LethalMin.Logger.LogInfo($"Ratio: {BundlesToProcess} - {BundlesProcessed}");
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to load bundle: {bundlePath}");
                BundlesToProcess--;
            }
        }

        IEnumerator WaitToRegister()
        {
            yield return new WaitUntil(() => BundlesToProcess == BundlesProcessed);
            LethalMin.InitCustomDefaultSoundPacks(customSoundPacks);
            LethalMin.RegisterCustomPikminTypes(customTypes);
            LethalMin.RegisterCustomOnionTypes(customOtypes);
            LethalMin.RegisterCustomFuseRules(customFuseRules);

            TypeConverter.ClearCache();
            LethalMin.Logger.LogMessage($"All bundles processed: {BundlesProcessed}/{BundlesToProcess}");
        }

        private void ProcessLoadedBundle(AssetBundle bundle)
        {
            bool IsValidLethalMinBundle = false;

            PikminSoundPack[] soundPacks = bundle.LoadAllAssets<PikminSoundPack>();
            foreach (PikminSoundPack soundPack in soundPacks)
            {
                if (soundPack != null)
                {
                    customSoundPacks.Add(soundPack);
                    IsValidLethalMinBundle = true;
                }
            }

            PikminType[] Ptypes = bundle.LoadAllAssets<PikminType>();
            foreach (PikminType Ptype in Ptypes)
            {
                if (Ptype != null)
                {
                    customTypes.Add(Ptype);
                    IsValidLethalMinBundle = true;
                }
            }

            OnionType[] Otypes = bundle.LoadAllAssets<OnionType>();
            foreach (OnionType Otype in Otypes)
            {
                if (Otype != null)
                {
                    customOtypes.Add(Otype);
                    IsValidLethalMinBundle = true;
                }
            }

            OnionFuseRules[] fuseRules = bundle.LoadAllAssets<OnionFuseRules>();
            foreach (OnionFuseRules fuseRule in fuseRules)
            {
                if (fuseRule != null)
                {
                    customFuseRules.Add(fuseRule);
                    IsValidLethalMinBundle = true;
                }
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