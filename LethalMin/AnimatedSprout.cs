using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LethalMin;
using System.Collections;

namespace LethalMin
{
    public class AnimatedSprout : NetworkBehaviour
    {
        
        [ClientRpc]
        public void ColorAndSyncClientRpc(int Type = -1)
        {
            PikminType PminType;
            System.Random enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + NetworkBehaviourId);
            if (Type == -1)
            {
                PminType = LethalMin.SproutTypes[enemyRandom.Next(0, LethalMin.SproutTypes.Count)];
            }
            else
            {
                PminType = LethalMin.GetPikminTypeById(Type);
            }
            if (PminType.SproutMeshPrefab != null)
            {
                transform.Find("mesh").gameObject.SetActive(false);
                GameObject sproutMesh = Instantiate(PminType.SproutMeshPrefab, transform);
            }
            else if (PminType.SproutMaterial != null)
            {
                transform.Find("mesh/_Pikmin_Yellow_00.00200.002").GetComponent<Renderer>().material = PminType.SproutMaterial;
            }
        }
    }
}