using System;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json; // Add this if using Newtonsoft.Json
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