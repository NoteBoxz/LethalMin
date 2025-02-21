using System;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json;
using LethalModDataLib.Attributes;
using LethalModDataLib.Enums;
using LethalModDataLib.Base;
using System.Reflection;
using UnityEngine;

namespace LethalMin
{
    [Serializable]
    public class OnionSaveData
    {
        public List<int> OnionsCollected = new List<int>();

        [JsonConverter(typeof(DictionaryIntArrayConverter))] // Add this attribute if using Newtonsoft.Json
        public Dictionary<int, int[]> OnionsFused = new Dictionary<int, int[]>();

        public List<OnionPikminStorage> PikminStored = new List<OnionPikminStorage>();

        public List<SproutData> Sprouts = new List<SproutData>();

        public int PikminLeftLastRound;
    }

    public class OnionEzSaveData
    {
        private static bool _isUtilsLoaded;
        private static Type _SaveDataWithLibType;
        private object _instance;

        // Local storage for when mod library is not loaded
        private List<int> _onionsCollected = new List<int>();
        private Dictionary<int, int[]> _onionsFused = new Dictionary<int, int[]>();
        private List<OnionPikminStorage> _pikminStored = new List<OnionPikminStorage>();
        private List<SproutData> _sprouts = new List<SproutData>();

        public OnionEzSaveData()
        {
            _isUtilsLoaded = LethalMin.IsUsingModLib();
            if (_isUtilsLoaded)
            {
                _SaveDataWithLibType = Type.GetType("LethalMinSaveDataWithLib");
                if (_SaveDataWithLibType != null)
                {
                    _instance = Activator.CreateInstance(_SaveDataWithLibType);
                    SetData("OnionsCollected", new List<int>());
                    SetData("OnionsFused", new Dictionary<int, int[]>());
                    SetData("PikminStored", new List<OnionPikminStorage>());
                    SetData("Sprouts", new List<SproutData>());
                    SetData("PikminLeftLastRound", 0);
                }
                else
                {
                    LethalMin.Logger.LogError("Failed to find LethalMinSaveDataWithLib type.");
                }
            }
            else
            {
                LethalMin.Logger.LogInfo("ModLib not loaded, using local storage.");
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

        public List<SproutData> Sprouts
        {
            get => GetData<List<SproutData>>("Sprouts");
            set => SetData("Sprouts", value);
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
                LethalMin.Logger.LogInfo($"Attempting to get property {propertyName}");
                PropertyInfo property = _SaveDataWithLibType.GetProperty(propertyName);
                if (property == null)
                {
                    LethalMin.Logger.LogError($"Property {propertyName} not found in LethalMinSaveDataWithLib");
                    return default(T);
                }
                object value = property.GetValue(_instance);
                if (value == null)
                {
                    LethalMin.Logger.LogWarning($"Value of property {propertyName} is null");
                    return CreateNewInstance<T>();
                }
                return (T)value;
            }
            else
            {
                FieldInfo field = this.GetType().GetField($"_{propertyName.ToLower()}", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    LethalMin.Logger.LogError($"Field _{propertyName.ToLower()} not found in OnionEzSaveData");
                    return default(T);
                }
                return (T)field.GetValue(this);
            }
        }
        private T CreateNewInstance<T>()
        {
            if (typeof(T) == typeof(List<int>))
            {
                LethalMin.Logger.LogInfo("Creating new instance of List<int>");
                return (T)(object)new List<int>();
            }
            if (typeof(T) == typeof(Dictionary<int, int[]>))
            {
                LethalMin.Logger.LogInfo("Creating new instance of Dictionary<int, int[]>");
                return (T)(object)new Dictionary<int, int[]>();
            }
            if (typeof(T) == typeof(List<OnionPikminStorage>))
            {
                LethalMin.Logger.LogInfo("Creating new instance of List<OnionPikminStorage>");
                return (T)(object)new List<OnionPikminStorage>();
            }
            if (typeof(T) == typeof(List<SproutData>))
            {
                LethalMin.Logger.LogInfo("Creating new instance of List<SproutData>");
                return (T)(object)new List<SproutData>();
            }
            if (typeof(T) == typeof(int))
            {
                LethalMin.Logger.LogInfo("Creating new instance of int");
                return (T)(object)0;
            }

            // Add more types as needed

            LethalMin.Logger.LogWarning($"No matching type found for {typeof(T)}, returning default value");
            return default(T);
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

        public OnionPikmin(int growStage, int pikminTypeId)
        {
            GrowStage = growStage;
            PikminTypeID = pikminTypeId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref GrowStage);
            serializer.SerializeValue(ref PikminTypeID); // Serialize the PikminTypeID
        }
    }

    [Serializable]
    public struct SproutData : INetworkSerializable
    {
        public int GrowStage; // 0 for sprout, 1 for bud, 2 for flower
        public int PikminTypeID; // Add this field
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;
        public string SceneName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref GrowStage);
            serializer.SerializeValue(ref PikminTypeID); // Serialize the PikminTypeID
            serializer.SerializeValue(ref SceneName);
        }
    }

    public class SerializableVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SerializableVector3(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }

    public class SerializableQuaternion
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public SerializableQuaternion(Quaternion quaternion)
        {
            X = quaternion.x;
            Y = quaternion.y;
            Z = quaternion.z;
            W = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }
    }
}