using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalLib.Extras;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class PikCaller : NetworkBehaviour
    {
        public string OverrideCooldownText = "Ready";
        public InteractTrigger button = null!;
        public AudioSource CallSound = null!;
        public TMP_Text CooldownText = null!;
        public TMP_Text VersionText = null!;
        public GameObject? WhistleInstance;
        public Animator FXAnimator = null!;
        public UnlockableItemDef def = null!;
        public float Cooldown;
        public bool SpedUp => StartOfRound.Instance.inShipPhase;
        string formerInteractTip = "";

        void Start()
        {
            formerInteractTip = button.hoverTip;
            VersionText.text = $"(V{MyPluginInfo.PLUGIN_VERSION})";
        }

        public void OnInteract(PlayerControllerB controller)
        {
            if (Cooldown > 0) return;
            if (StartOfRound.Instance.inShipPhase) return;

            OverrideCooldownText = "Syncing...";
            if (!IsServer)
            {
                CallPikminToBase(LethalMin.GetLeaderViaID(controller.OwnerClientId));
                CallPikminToBaseServerRpc(StartOfRound.Instance.localPlayerController.OwnerClientId);
            }
            else
            {
                CallPikminToBase(LethalMin.GetLeaderViaID(controller.OwnerClientId));
                CallPikminToBaseClientRpc(StartOfRound.Instance.localPlayerController.OwnerClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CallPikminToBaseServerRpc(ulong clientId)
        {
            OverrideCooldownText = "Server Syncing...";
            CallPikminToBaseClientRpc(clientId);
        }

        [ClientRpc]
        public void CallPikminToBaseClientRpc(ulong clientId)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.OwnerClientId) { return; }
            OverrideCooldownText = "Client Syncing...";
            CallPikminToBase(LethalMin.GetLeaderViaID(clientId));
        }

        public void CallPikminToBase(Leader? leaderCalled)
        {
            if (Cooldown > 0) return;
            CallSound.Play();
            FXAnimator.SetTrigger("doburst");
            Cooldown = LethalMin.PikminSignalCooldownCheat == -1 ? 300f : LethalMin.PikminSignalCooldownCheat;
            OverrideCooldownText = "";
            button.hoverTip = "On Cooldown";
            StartCoroutine(CallPikminToBaseCoroutine(leaderCalled));
        }

        IEnumerator CallPikminToBaseCoroutine(Leader? leaderCalled)
        {
            foreach (PikminAI ai in PikminManager.instance.PikminAIs)
            {
                if (ai.leader != null || ai.IsWildPikmin)
                {
                    continue;
                }
                ai.CallResetMethods();
                ai.ChangeIntent(Pintent.Idle);
                ai.SetCurrentTask("ReturnToShip");
                ai.SwitchToBehaviourStateOnLocalClient(PikminAI.WORK);
                yield return new WaitForEndOfFrame();
            }
        }

        private void Update()
        {
            if (Cooldown > 0)
            {
                if (SpedUp)
                {
                    Cooldown -= Time.deltaTime * 100;
                }
                else
                {
                    Cooldown -= Time.deltaTime;
                }
            }
            else if (string.IsNullOrEmpty(OverrideCooldownText))
            {
                OverrideCooldownText = "Ready";
                button.hoverTip = formerInteractTip;
            }

            if (CooldownText != null)
            {
                if (string.IsNullOrEmpty(OverrideCooldownText))
                {
                    CooldownText.text = Cooldown > 0 ? string.Format("{0:00}:{1:00}", Mathf.FloorToInt(Cooldown / 60), Mathf.FloorToInt(Cooldown % 60)) : "";
                }
                else
                {
                    CooldownText.text = OverrideCooldownText;
                }
            }

            if (LethalMin.GiantWhistleMode)
            {
                if (WhistleInstance == null)
                {
                    GameObject prefab = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Whistle/FuniShipWhile.prefab");
                    WhistleInstance = Instantiate(prefab, transform.position, Quaternion.identity);
                    WhistleInstance.transform.SetParent(StartOfRound.Instance.shipBounds.transform.parent);
                    WhistleInstance.transform.localPosition = new Vector3(-0.4155f, 5.9281f, -7.174f);
                    WhistleInstance.transform.rotation = Quaternion.Euler(22, 270, 0);
                    WhistleInstance.transform.localScale = new Vector3(3, 3, 3);
                }

                WhistleInstance.SetActive(!def.unlockable.inStorage);
            }
            else if (WhistleInstance != null)
            {
                WhistleInstance.SetActive(false);
            }
        }
    }
}
