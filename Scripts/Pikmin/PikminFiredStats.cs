using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;

namespace LethalMin.Pikmin
{
    public struct PikminFiredStats : INetworkSerializable
    {
        public int TotalPikminRaised;
        public int TotalPikminLost;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TotalPikminRaised);
            serializer.SerializeValue(ref TotalPikminLost);
        }
    }
}