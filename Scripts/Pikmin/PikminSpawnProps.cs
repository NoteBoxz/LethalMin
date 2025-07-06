using Unity.Netcode;

namespace LethalMin.Pikmin
{
    public struct PikminSpawnProps : INetworkSerializable
    {
        public PikminSpawnProps()
        {
            SpawnAnimation = "";
            SpawnSound = "";
            GrowthStage = 0;
            PlayerID = 9999999999999999;
            SpawnSoundDelay = -1f;
            SpawnAnimationDelay = -1f;
            OverrideVolume = -1f;
            MovementBuffer = -1;
            OverrideDebugID = "";
            OverrideBirthDate = "";
            IsOutside = true;
            AddToSpawnCount = false;
            AddToSpawnCountForWild = false;
        }

        public string SpawnAnimation;
        public string SpawnSound;
        public int GrowthStage;
        public ulong PlayerID;
        public float SpawnSoundDelay;
        public float SpawnAnimationDelay;
        public float OverrideVolume;
        public float MovementBuffer;
        public string OverrideDebugID;
        public string OverrideBirthDate;
        public bool IsOutside;
        public bool AddToSpawnCount;
        public bool AddToSpawnCountForWild;

        //Allows RPCS to be called with this struct as a varible
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SpawnAnimation);
            serializer.SerializeValue(ref SpawnSound);
            serializer.SerializeValue(ref GrowthStage);
            serializer.SerializeValue(ref PlayerID);
            serializer.SerializeValue(ref SpawnSoundDelay);
            serializer.SerializeValue(ref SpawnAnimationDelay);
            serializer.SerializeValue(ref OverrideVolume);
            serializer.SerializeValue(ref OverrideDebugID);
            serializer.SerializeValue(ref OverrideBirthDate);
            serializer.SerializeValue(ref IsOutside);
            serializer.SerializeValue(ref AddToSpawnCount);
            serializer.SerializeValue(ref MovementBuffer);
            serializer.SerializeValue(ref AddToSpawnCountForWild);
        }
    }
}