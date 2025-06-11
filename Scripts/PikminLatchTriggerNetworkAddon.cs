using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LethalMin
{
    public class PikminLatchTriggerNetworkAddon : NetworkBehaviour
    {
        public PikminLatchTrigger MainTrigger = null!;

        [ServerRpc(RequireOwnership = false)]
        public void RemoveAllPikminServerRpc(int Mode)
        {
            RemoveAllPikminClientRpc(Mode);
        }
        [ClientRpc]
        public void RemoveAllPikminClientRpc(int Mode)
        {
            MainTrigger.RemoveAllPikmin(Mode);
        }


        [ServerRpc(RequireOwnership = false)]
        public void LatchPikminServerRpc(NetworkObjectReference PikRef, Vector3 LandPos, bool IsDirectHit = true)
        {
            LatchPikminClientRpc(PikRef, LandPos, IsDirectHit);
        }
        [ClientRpc]
        public void LatchPikminClientRpc(NetworkObjectReference PikRef, Vector3 LandPos, bool IsDirectHit = true)
        {
            if (IsOwner)
            {
                return;
            }

            NetworkObject pikObj;
            if (PikRef.TryGet(out pikObj) && pikObj.TryGetComponent(out PikminAI pikmin))
            {
                MainTrigger.LatchPikmin(pikmin, LandPos, IsDirectHit);
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to latch Pikmin: {PikRef}");
            }
        }
    
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            MainTrigger.RemoveAllPikmin(2);
        }
    }
}