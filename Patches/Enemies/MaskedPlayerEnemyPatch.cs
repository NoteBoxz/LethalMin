using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches.AI
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    public class MaskedPlayerEnemyPatch
    {
        public static void RegisterAsPuffminLeader(MaskedPlayerEnemy __instance)
        {
            if (__instance.gameObject.GetComponent<PuffminLeader>() != null)
            {
                LethalMin.Logger.LogWarning($"PuffminLeader script already exists on {__instance.gameObject.name}");
                return;
            }

            PuffminLeader leader = __instance.gameObject.AddComponent<PuffminLeader>();
            CustomMaskedAnimationManager customMasked = __instance.gameObject.AddComponent<CustomMaskedAnimationManager>();
            MaskedPlayerPikminEnemy pikminEnemy = __instance.gameObject.GetComponent<MaskedPlayerPikminEnemy>();

            GameObject go = new GameObject("HoldPos");
            go.transform.position = __instance.maskTypes[0].transform.position + new Vector3(0.5f, -0.3f, 0.5f);
            go.transform.rotation = __instance.maskTypes[0].transform.rotation;
            go.transform.SetParent(__instance.transform, true);
            leader.HoldPos = go.transform;
            GameObject goB = new GameObject("ThrowOrigin");
            goB.transform.position = __instance.maskTypes[0].transform.position + new Vector3(0.1f, 0, 0);
            goB.transform.rotation = __instance.maskTypes[0].transform.rotation;
            goB.transform.SetParent(__instance.transform, true);
            leader.ThrowOrigin = goB.transform;
            leader.AI = __instance;
            leader.UseOverrideThrowDirection = true;

            PuffminNoticeZone NoticeZone = GameObject.Instantiate(LethalMin.NoticeZonePrefab).GetComponent<PuffminNoticeZone>();
            NoticeZone.LeaderScript = leader;
            NoticeZone.transform.SetParent(__instance.transform);
            NoticeZone.transform.localPosition = new Vector3(0, 2, 0);
            NoticeZone.gameObject.SetActive(false);
            leader.noticeZone = NoticeZone;

            customMasked.maskedPlayerEnemy = __instance;
            customMasked.CreateWhistleConstraint();
            customMasked.CreateOverrideHandBone();

            leader.WhistleClip = LethalMin.assetBundle.LoadAsset<AudioClip>("Assets/LethalMin/Puffmin/PuffminWhistle.wav");
            leader.WhistleAudioSource = __instance.creatureSFX;

            pikminEnemy.leader = leader;
            pikminEnemy.maskedPlayerEnemy = __instance;

            LethalMin.Logger.LogDebug($"Added PuffminLeader script to {__instance.gameObject.name}");
        }

        [HarmonyPatch(nameof(MaskedPlayerEnemy.KillEnemy))]
        [HarmonyPostfix]
        public static void KillEnemyPostfix(MaskedPlayerEnemy __instance)
        {
            if (__instance.TryGetComponent(out MaskedPlayerPikminEnemy pikminEnemy) && pikminEnemy.IsLeafling)
            {
                GameObject ghost = GameObject.Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PlayerGhostPrefab.prefab"),
                __instance.transform.position, Quaternion.identity);
                PlayerGhost pg = ghost.GetComponent<PlayerGhost>();
                Renderer renderer = pg.GetComponentInChildren<Renderer>();
                renderer.material.color = new Color(0.18823529411f, 0.09803921568f, 0.20392156862f, 0.75f);
                AudioSource ghostAudio = pg.GetComponentInChildren<AudioSource>();
                ghostAudio.pitch = 0.8f; // slightly lower pitch for ghost
                pikminEnemy.IncrumentDestoryCountServerRpc();
            }
        }
    }
}
