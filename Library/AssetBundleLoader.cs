using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using LethalMin.Library;
using LethalMin.Pikmin;
using System.Linq;
using System.Diagnostics;
using LethalConfig.Mods;

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

        // Bundle info class to track dependencies
        private class BundleInfo
        {
            public string Path = "";
            public string BundleName = "";
            public AssetBundle Bundle = null!;
            public Object? ModInfo;
            public bool IsLibraryBundle; // true if from LethalMinLibrary, false if from LethalMin
            public bool Processed = false;
            public List<string> Dependencies = new List<string>();

            public BundleInfo(string path)
            {
                Path = path;
                BundleName = System.IO.Path.GetFileNameWithoutExtension(path);
            }
        }

        private void LoadLethalMinBundles()
        {
            lethalMinFolder = lethalMinFile.Parent;
            pluginsFolder = lethalMinFolder.Parent;

            if (pluginsFolder == null || !pluginsFolder.Exists)
            {
                LethalMin.Logger.LogError($"Plugins folder not found: {pluginsFolder?.FullName}");
                return;
            }

            // Add the LibAssetBundleLoader if dependency is loaded
            if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                gameObject.AddComponent<LibAssetBundleLoader>();
            }

            // Get all bundle files and sort them alphabetically first
            string[] bundleFiles = Directory.GetFiles(pluginsFolder.FullName, BUNDLE_EXTENSION, SearchOption.AllDirectories);
            System.Array.Sort(bundleFiles, (a, b) => string.Compare(Path.GetFileNameWithoutExtension(a), Path.GetFileNameWithoutExtension(b), System.StringComparison.Ordinal));

            if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                StartCoroutine(LoadBundlesWithDependencies(bundleFiles));
            }
            else
            {
                // If LethalMinLibrary isn't loaded, just load bundles in alphabetical order
                BundlesToProcess = bundleFiles.Length;
                foreach (string bundleFile in bundleFiles)
                {
                    StartCoroutine(LoadBundleCoroutine(bundleFile));
                }
                if (BundlesToProcess > 0)
                {
                    StartCoroutine(WaitToRegister());
                }
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

        private IEnumerator LoadBundlesWithDependencies(string[] bundleFiles)
        {
            List<BundleInfo> bundleInfos = new List<BundleInfo>();

            // Step 1: Preload all bundles to extract their mod info
            foreach (string bundlePath in bundleFiles)
            {
                BundleInfo bundleInfo = new BundleInfo(bundlePath);
                bundleInfos.Add(bundleInfo);

                // Load the bundle asynchronously
                LethalMin.Logger.LogDebug($"Loading bundle: {Path.GetFileNameWithoutExtension(bundlePath)}");
                Stopwatch watch = Stopwatch.StartNew();
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return request;
                watch.Stop();
                LethalMin.Logger.LogDebug($"Loaded bundle: {Path.GetFileNameWithoutExtension(bundlePath)} in ({watch.Elapsed})s");

                if (request.assetBundle != null)
                {
                    bundleInfo.Bundle = request.assetBundle;
                    loadedBundles[bundleInfo.BundleName] = request.assetBundle;

                    // Check if this is a LethalMinLibrary bundle by looking for mod info
                    LethalMinLibrary.LMLmodInfo[] modInfos = request.assetBundle.LoadAllAssets<LethalMinLibrary.LMLmodInfo>();
                    if (modInfos.Length > 1)
                    {
                        int count = 0;
                        foreach (LethalMinLibrary.LMLmodInfo info in modInfos)
                        {
                            if (info.DontLoad)
                            {
                                count++;
                            }
                        }
                        if (modInfos.Length - count > 1)
                        {
                            LethalMin.Logger.LogWarning($"Bundle {bundleInfo.BundleName} has multiple modInfos. This may cause issues.");
                        }
                    }
                    if (modInfos.Length > 0)
                    {
                        bundleInfo.ModInfo = modInfos[0];
                        LethalMinLibrary.LMLmodInfo modInfo = modInfos[0];
                        bundleInfo.IsLibraryBundle = true;
                        if (modInfo.Dependencies != null)
                        {
                            foreach (var dependency in modInfo.Dependencies)
                            {
                                if (dependency != null)
                                {
                                    bundleInfo.Dependencies.Add(dependency.name);
                                }
                            }
                        }
                        LethalMin.Logger.LogDebug($"Found Library bundle: {bundleInfo.BundleName} with {bundleInfo.Dependencies.Count} dependencies");
                    }
                    else
                    {
                        // Check if it's a regular LethalMin bundle by looking for PikminType assets
                        PikminType[] types = request.assetBundle.LoadAllAssets<PikminType>();
                        bundleInfo.IsLibraryBundle = (types.Length == 0); // If it has no PikminType, assume it might be library bundle without modInfo
                    }
                }
                else
                {
                    LethalMin.Logger.LogError($"Failed to load bundle: {bundlePath}");
                    bundleInfos.Remove(bundleInfo);
                }
            }

            // Separate bundles into LethalMin and LethalMinLibrary bundles
            List<BundleInfo> libraryBundles = bundleInfos.Where(b => b.IsLibraryBundle).ToList();
            List<BundleInfo> regularBundles = bundleInfos.Where(b => !b.IsLibraryBundle).ToList();

            // Step 2: Sort Library bundles based on dependencies
            List<BundleInfo> sortedLibraryBundles = SortBundlesByDependencies(libraryBundles);

            // Set counters for tracking progress
            BundlesToProcess = bundleInfos.Count;
            BundlesProcessed = 0;

            // Step 3: Process bundles in the correct order - first library bundles, then regular ones
            foreach (BundleInfo bundleInfo in sortedLibraryBundles)
            {
                yield return ProcessBundle(bundleInfo);
            }

            foreach (BundleInfo bundleInfo in regularBundles)
            {
                yield return ProcessBundle(bundleInfo);
            }

            // Step 4: Wait for all bundles to be processed and register assets
            if (BundlesToProcess > 0)
            {
                StartCoroutine(WaitToRegister());
            }
        }

        private List<BundleInfo> SortBundlesByDependencies(List<BundleInfo> bundles)
        {
            Dictionary<string, BundleInfo> bundlesByName = bundles.ToDictionary(b => b.BundleName);
            List<BundleInfo> sortedBundles = new List<BundleInfo>();
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> processing = new HashSet<string>();

            foreach (BundleInfo bundle in bundles)
            {
                if (!visited.Contains(bundle.BundleName))
                {
                    if (!TopologicalSort(bundle, bundlesByName, visited, processing, sortedBundles))
                    {
                        LethalMin.Logger.LogFatal($"Circular dependency detected involving mod: {bundle.BundleName}");
                    }
                }
            }

            //log the sorted bundles for debugging
            LethalMin.Logger.LogDebug("Sorted bundles by dependencies:");
            foreach (BundleInfo sortedBundle in sortedBundles)
            {
                LethalMin.Logger.LogDebug($"- {sortedBundle.BundleName} (Library: {sortedBundle.IsLibraryBundle})");
            }

            return sortedBundles;
        }

        private bool TopologicalSort(BundleInfo bundle, Dictionary<string, BundleInfo> bundlesByName,
                                    HashSet<string> visited, HashSet<string> processing, List<BundleInfo> sortedBundles)
        {
            // Check for circular dependency
            if (processing.Contains(bundle.BundleName))
            {
                return false; // Circular dependency detected
            }

            // If already visited, skip
            if (visited.Contains(bundle.BundleName))
            {
                return true;
            }

            // Mark as being processed
            processing.Add(bundle.BundleName);

            // Process all dependencies first
            foreach (string dependency in bundle.Dependencies)
            {
                if (bundlesByName.TryGetValue(dependency, out BundleInfo dependencyBundle))
                {
                    if (!TopologicalSort(dependencyBundle, bundlesByName, visited, processing, sortedBundles))
                    {
                        return false; // Circular dependency detected
                    }
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Bundle {bundle.BundleName} depends on {dependency}, but it was not found");
                }
            }

            // Mark as processed
            processing.Remove(bundle.BundleName);
            visited.Add(bundle.BundleName);

            // Add to sorted list
            sortedBundles.Add(bundle);

            return true;
        }

        private IEnumerator ProcessBundle(BundleInfo bundleInfo)
        {
            if (bundleInfo.Bundle != null && !bundleInfo.Processed)
            {
                LethalMinLibrary.LMLmodInfo? modInfo = bundleInfo.ModInfo as LethalMinLibrary.LMLmodInfo;
                if (modInfo == null)
                    LethalMin.Logger.LogMessage($"Processing bundle [{bundleInfo.BundleName}]");
                else
                    LethalMin.Logger.LogMessage($"Processing bundle [{modInfo.ModName} {modInfo.Version}]");

                // Process the bundle based on its type
                if (bundleInfo.IsLibraryBundle && LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
                {
                    gameObject.GetComponent<LibAssetBundleLoader>().ProcessLoadedLibBundle(bundleInfo.Bundle);
                }
                else
                {
                    ProcessLoadedBundle(bundleInfo.Bundle);
                }

                bundleInfo.Processed = true;
                BundlesProcessed++;

                LethalMin.Logger.LogInfo($"Ratio: {BundlesToProcess} - {BundlesProcessed}");
            }

            yield return null;
        }

        private IEnumerator LoadBundleCoroutine(string bundlePath)
        {
            LethalMin.Logger.LogInfo($"Loading bundle: {Path.GetFileNameWithoutExtension(bundlePath)}");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            if (request.assetBundle != null)
            {
                string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
                loadedBundles[bundleName] = request.assetBundle;

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

            if (LethalMin.IsDependencyLoaded("NoteBoxz.LethalMinLibrary"))
            {
                ClearCache();
            }
            LethalMin.Logger.LogMessage($"All bundles processed: {BundlesProcessed}/{BundlesToProcess}");
        }

        void ClearCache()
        {
            TypeConverter.ClearCache();
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