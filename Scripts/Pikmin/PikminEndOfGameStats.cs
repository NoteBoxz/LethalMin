using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;

namespace LethalMin.Pikmin
{
    public struct PikminEndOfGameStats : INetworkSerializable
    {
        public Dictionary<Leader, int> PikminRaised = new Dictionary<Leader, int>();
        public int PikminKilled;
        public int PikminLeftBehind;
        public int PikminLeft;
        public int PikminLeftLastRound;

        public PikminEndOfGameStats()
        {
        }

        public void Refresh()
        {
            PikminRaised.Clear();
            foreach (PlayerControllerB control in StartOfRound.Instance.allPlayerScripts)
            {
                Leader player = control.gameObject.GetComponent<Leader>();
                PikminRaised.Add(player, 0);
            }
            PikminLeftLastRound = PikminLeft;
            PikminLeftBehind = 0;
            PikminKilled = 0;
            PikminLeft = 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PikminLeft);
            serializer.SerializeValue(ref PikminLeftLastRound);
        }
    }
}