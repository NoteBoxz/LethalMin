using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class CompanyPikminEnemy : NetworkBehaviour
    {
        public List<CompanyMonsterCollisionDetect> Detects = new List<CompanyMonsterCollisionDetect>();
        void Start()
        {
            Detects = Object.FindObjectsOfType<CompanyMonsterCollisionDetect>().ToList();
            //Sort by name
            Detects = Detects.OrderBy(x => x.name).ToList();
        }
        [ServerRpc(RequireOwnership = false)]
        public void KillPikminServerRpc(NetworkObjectReference PikRef, int DetectIndex)
        {
            KillPikminClientRpc(PikRef, DetectIndex);
        }
        [ClientRpc]
        public void KillPikminClientRpc(NetworkObjectReference PikRef, int DetectIndex)
        {
            if (PikRef.TryGet(out NetworkObject netObj) && netObj.gameObject.TryGetComponent(out PikminAI ai))
            {
                ai.DeathSnapToPos = Detects[DetectIndex].transform;
                ai.OverrideDelay = 0.75f;
                ai.KillEnemy();
            }
            else
            {
                LethalMin.Logger.LogError($"Failed to get PikminAI from {PikRef} in {Detects[DetectIndex].name}");
            }
        }
    }
}
