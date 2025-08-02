using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Utils;
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
            try
            {
                // Serialize primitive values first
                serializer.SerializeValue(ref PikminLeft);
                serializer.SerializeValue(ref PikminKilled);
                serializer.SerializeValue(ref PikminLeftBehind);
                serializer.SerializeValue(ref PikminLeftLastRound);
                // For the dictionary, we need to serialize it as arrays
                if (serializer.IsWriter)
                {
                    // Convert dictionary to arrays for serialization
                    int count = PikminRaised.Count;
                    serializer.SerializeValue(ref count);

                    if (count > 0)
                    {
                        // Create arrays to hold the data
                        ulong[] leaderIds = new ulong[count];
                        int[] raisedCounts = new int[count];

                        int i = 0;
                        foreach (var kvp in PikminRaised)
                        {
                            leaderIds[i] = kvp.Key.Controller.OwnerClientId;
                            raisedCounts[i] = kvp.Value;
                            i++;
                        }

                        // Serialize the arrays
                        serializer.SerializeValue(ref leaderIds);
                        serializer.SerializeValue(ref raisedCounts);
                    }
                }
                else // Reader
                {
                    // Deserialize count
                    int count = 0;
                    serializer.SerializeValue(ref count);

                    // Clear existing dictionary
                    PikminRaised.Clear();

                    if (count > 0)
                    {
                        // Create arrays to receive the data
                        ulong[] leaderIds = new ulong[count];
                        int[] raisedCounts = new int[count];

                        // Deserialize the arrays
                        serializer.SerializeValue(ref leaderIds);
                        serializer.SerializeValue(ref raisedCounts);

                        // Reconstruct the dictionary
                        List<ulong> IdsList = leaderIds.ToList();
                        PikminRaised.Clear();
                        foreach (PlayerControllerB control in StartOfRound.Instance.allPlayerScripts)
                        {
                            Leader player = control.gameObject.GetComponent<Leader>();
                            PikminRaised.Add(player, IdsList.Contains(player.Controller.OwnerClientId)
                            && PikChecks.IsPlayerConnected(control) ?
                            raisedCounts[IdsList.IndexOf(player.Controller.OwnerClientId)] : 0);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LethalMin.Logger.LogError($"Error serializing PikminEndOfGameStats: {ex.Message}");
                // Optionally, you can rethrow the exception or handle it as needed
            }
        }
    }
}