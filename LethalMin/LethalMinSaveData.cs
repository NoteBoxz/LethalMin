using System;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json;
using LethalModDataLib.Attributes;
using LethalModDataLib.Enums;
using LethalModDataLib.Base;
using System.Reflection;
namespace LethalMin
{
    [Serializable]
    public class OnionSaveData
    {
        public List<int> OnionsCollected = new List<int>();

        [JsonConverter(typeof(DictionaryIntArrayConverter))] // Add this attribute if using Newtonsoft.Json
        public Dictionary<int, int[]> OnionsFused = new Dictionary<int, int[]>();

        public List<OnionPikminStorage> PikminStored = new List<OnionPikminStorage>();

        public int PikminLeftLastRound;
    }

    public class OnionEzSaveData
    {
        private static bool _isUtilsLoaded;
        private static Type _SaveDataWithLibType;
        private static object _instance;

        // Local storage for when mod library is not loaded
        private List<int> _onionsCollected = new List<int>();
        private Dictionary<int, int[]> _onionsFused = new Dictionary<int, int[]>();
        private List<OnionPikminStorage> _pikminStored = new List<OnionPikminStorage>();
        private int _pikminLeftLastRound;

        static OnionEzSaveData()
        {
            _isUtilsLoaded = LethalMin.IsUsingModLib();
            if (_isUtilsLoaded)
            {
                _SaveDataWithLibType = Type.GetType("LethalMin.LethalMinSaveDataWithLib");
                if (_SaveDataWithLibType != null)
                {
                    _instance = Activator.CreateInstance(_SaveDataWithLibType);
                }
            }
        }

        public List<int> OnionsCollected
        {
            get => GetData<List<int>>("OnionsCollected");
            set => SetData("OnionsCollected", value);
        }

        public Dictionary<int, int[]> OnionsFused
        {
            get => GetData<Dictionary<int, int[]>>("OnionsFused");
            set => SetData("OnionsFused", value);
        }

        public List<OnionPikminStorage> PikminStored
        {
            get => GetData<List<OnionPikminStorage>>("PikminStored");
            set => SetData("PikminStored", value);
        }

        public int PikminLeftLastRound
        {
            get => GetData<int>("PikminLeftLastRound");
            set => SetData("PikminLeftLastRound", value);
        }

        private T GetData<T>(string propertyName)
        {
            if (_isUtilsLoaded && _instance != null)
            {
                PropertyInfo property = _SaveDataWithLibType.GetProperty(propertyName);
                return (T)property.GetValue(_instance);
            }
            else
            {
                return (T)this.GetType().GetField($"_{propertyName.ToLower()}", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
            }
        }

        private void SetData<T>(string propertyName, T value)
        {
            if (_isUtilsLoaded && _instance != null)
            {
                PropertyInfo property = _SaveDataWithLibType.GetProperty(propertyName);
                property.SetValue(_instance, value);
            }
            else
            {
                this.GetType().GetField($"_{propertyName.ToLower()}", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, value);
            }
        }

        public void Save()
        {
            if (_isUtilsLoaded && _instance != null)
            {
                MethodInfo saveMethod = _SaveDataWithLibType.GetMethod("Save");
                saveMethod.Invoke(_instance, null);
            }
            else
            {
                // Implement local save logic here if needed
                LethalMin.Logger.LogInfo("Local save not implemented");
            }
        }

        public void Load()
        {
            if (_isUtilsLoaded && _instance != null)
            {
                MethodInfo loadMethod = _SaveDataWithLibType.GetMethod("Load");
                loadMethod.Invoke(_instance, null);
            }
            else
            {
                // Implement local load logic here if needed
                LethalMin.Logger.LogInfo("Local load not implemented");
            }
        }
    }

    [Serializable]
    public struct OnionPikminStorage
    {
        public int ID;
        public OnionPikmin[] Pikmin;
    }

    [Serializable]
    public struct OnionFusions
    {
        public int ID;
        public int[] OnionIDs;
    }

    [Serializable]
    public struct OnionPikmin : INetworkSerializable
    {
        public int GrowStage; // 0 for leaf, 1 for bud, 2 for flower
        public int PikminTypeID; // Add this field

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref GrowStage);
            serializer.SerializeValue(ref PikminTypeID); // Serialize the PikminTypeID
        }
    }
}