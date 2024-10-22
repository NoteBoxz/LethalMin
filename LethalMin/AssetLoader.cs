using UnityEngine;
using System.IO;
using System.Reflection;
using System;

namespace LethalMin
{
    public static class AssetLoader
    {
        private static AssetBundle? _assetBundle;

        public static AssetBundle LoadAssetBundle()
        {
            if (_assetBundle == null)
            {
                string? assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (assemblyLocation == null)
                {
                    throw new InvalidOperationException("Unable to determine assembly location.");
                }

                string bundlePath = Path.Combine(assemblyLocation, "lethalminassets");
                _assetBundle = AssetBundle.LoadFromFile(bundlePath);

                if (_assetBundle == null)
                {
                    throw new InvalidOperationException("Failed to load LethalMin AssetBundle.");
                }
            }
            return _assetBundle;
        }

        public static T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            if (!assetName.EndsWith(".asset") && !assetName.EndsWith(".prefab") && !assetName.EndsWith(".wav") && !assetName.EndsWith(".mat")
            && !assetName.EndsWith(".ogg") && !assetName.EndsWith(".mp3") && !assetName.EndsWith(".mp4") && !assetName.EndsWith(".anim")
            && !assetName.EndsWith(".png") && !assetName.EndsWith(".jpg") && !assetName.EndsWith(".mesh"))
            {
                assetName = $"{assetName}.asset";
            }
            AssetBundle bundle = LoadAssetBundle();

            T asset = bundle.LoadAsset<T>(assetName);

            if (!bundle.Contains(assetName))
            {
                throw new InvalidOperationException($"Asset not found in bundle: {assetName}");
            }
            if (asset == null)
            {
                throw new InvalidOperationException($"Failed to load asset: {assetName}. Asset exists in bundle but couldn't be loaded.");
            }
            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"ASSETLOADER: Loaded {assetName}");
            return asset;
        }
    }
}