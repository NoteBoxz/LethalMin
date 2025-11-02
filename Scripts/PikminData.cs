using Unity.Netcode;

namespace LethalMin
{
    public struct PikminData : INetworkSerializable
    {
        public PikminData(int typeID, int lastOnionID, int growthStage, string debugName, string birthDate)
        {
            TypeID = typeID;
            LastOnionID = lastOnionID;
            GrowthStage = growthStage;
            DebugName = debugName;
            BirthDate = birthDate;
        }

        public PikminData()
        {
            TypeID = 0;
            LastOnionID = 0;
            GrowthStage = 0;
            DebugName = "";
            BirthDate = "";
        }

        public int TypeID;
        public int LastOnionID;
        public int GrowthStage;
        public string DebugName = "";
        public string BirthDate = "";

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TypeID);
            serializer.SerializeValue(ref LastOnionID);
            serializer.SerializeValue(ref GrowthStage);
            serializer.SerializeValue(ref DebugName);
            serializer.SerializeValue(ref BirthDate);
        }

        public string DataID()
        {
            //These 3 values should aways be unique and constistant to both the PikminData and PikminAI.
            return $"{TypeID}_{DebugName}_{BirthDate}";
        }
    }
}