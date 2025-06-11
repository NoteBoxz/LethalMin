using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public struct SproutData : INetworkSerializable
    {
        public SproutData(Sprout original)
        {
            TypeID = original.pikminType.PikminTypeID;
            GrowthStage = original.CurrentGrowthStage;
            Position = original.transform.position;
            Rotaion = original.transform.rotation;
            MoonSpawnedOn = StartOfRound.Instance.currentLevelID;
        }


        public int TypeID;
        public int GrowthStage;
        public Vector3 Position;
        public Quaternion Rotaion;
        public int MoonSpawnedOn;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TypeID);
            serializer.SerializeValue(ref GrowthStage);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotaion);
            serializer.SerializeValue(ref MoonSpawnedOn);
        }
    }
}