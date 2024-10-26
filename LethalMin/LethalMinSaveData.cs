using System;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json;
using LethalModDataLib.Attributes;
using LethalModDataLib.Enums;
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

    public static class OnionEzSaveData
    {
        //EzSave
        [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave, ResetWhen.OnGameOver)]
        public static List<int> OnionsCollected = new List<int>();


        [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave, ResetWhen.OnGameOver)]
        public static Dictionary<int, int[]> OnionsFused = new Dictionary<int, int[]>();


        [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave, ResetWhen.OnGameOver)]
        public static List<OnionPikminStorage> PikminStored = new List<OnionPikminStorage>();


        [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave, ResetWhen.OnGameOver)]
        public static int PikminLeftLastRound;

        public static InstancedOnionEzSaveData ConvertToInstanced()
        {
            return new InstancedOnionEzSaveData()
            {
                OnionsCollected = OnionsCollected,
                OnionsFused = OnionsFused,
                PikminStored = PikminStored,
                PikminLeftLastRound = PikminLeftLastRound
            };
        }
    }
    public class InstancedOnionEzSaveData
    {
        public List<int> OnionsCollected = new List<int>();

        public Dictionary<int, int[]> OnionsFused = new Dictionary<int, int[]>();

        public List<OnionPikminStorage> PikminStored = new List<OnionPikminStorage>();

        public int PikminLeftLastRound;

        public void ConvertFromInstanced()
        {
            OnionEzSaveData.OnionsCollected = OnionsCollected;
            OnionEzSaveData.OnionsFused = OnionsFused;
            OnionEzSaveData.PikminStored = PikminStored;
            OnionEzSaveData.PikminLeftLastRound = PikminLeftLastRound;
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