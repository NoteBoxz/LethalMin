using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Linq;
using LethalMin.Patches;
namespace LethalMin
{

    public class Sprout : NetworkBehaviour
    {
        public PikminType PminType;
        private InteractTrigger interactTrigger;
        [SerializeField] private GameObject pikminPrefab;
        public bool IsBeingPlucked;
        AudioSource aud;

        [ClientRpc]
        public void InitalizeTypeClientRpc(int Type = -1)
        {
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
                transform.Find("mesh/SK_chr_Pikmin_Yellow_00.002").GetComponent<Renderer>().material = PminType.SproutMaterial;
            }
        }
        void Start()
        {
            interactTrigger = transform.Find("InteractTrigger").GetComponent<InteractTrigger>();
            interactTrigger.onInteract.AddListener((PlayerControllerB player) => Pluck(player));
            interactTrigger.onStopInteract.AddListener((PlayerControllerB player) => PluckStop(player));
            interactTrigger.onInteractEarly.AddListener((PlayerControllerB player) => PluckAnim(player));
            pikminPrefab = LethalMin.pikminPrefab;
            aud = transform.Find("SFX").GetComponent<AudioSource>();

            if (raycastOrigin == null)
            {
                raycastOrigin = transform.Find("RaycastOrigin");
                if (raycastOrigin == null)
                {
                    LethalMin.Logger.LogError("RaycastOrigin child not found!");
                    return;
                }
            }
        }
        public void PluckStop(PlayerControllerB player)
        {
            if (player != null)
            {
                //PlayerControllerBPatch.RevertAnim(player.playerBodyAnimator, PlayerControllerBPatch.originalController);
                LethalMin.Logger.LogInfo($"Plucking for R e A   l");
            }
            else
            {
                //PlayerControllerBPatch.RevertAnim(StartOfRound.Instance.localPlayerController.playerBodyAnimator, PlayerControllerBPatch.originalController);
                LethalMin.Logger.LogInfo("Plucking but player is null");
            }
            SetInteractableClientRpc(true);
            IsBeingPlucked = false;
        }

        public void Pluck(PlayerControllerB player)
        {
            LethalMin.Logger.LogInfo("pluckonsproutcalled!");
            if (player == null)
            {
                LethalMin.Logger.LogInfo("Plucking but player is null");
                player = GameNetworkManager.Instance.localPlayerController;
            }

            if (LethalMin.SkipPluckAnimation.Value)
            {
                // Skip animation and spawn Pikmin immediately
                PluckServerRpc();
                PluckServerRpc(player.NetworkObject);
            }
            else
            {
                // Existing pluck logic
                IsBeingPlucked = false;
            }
        }
        [ClientRpc]
        public void SetInteractableClientRpc(bool value)
        {
            interactTrigger.interactable = value;
        }

        // Modify the existing PluckAnim method
        public void PluckAnim(PlayerControllerB player)
        {
            if (LethalMin.SkipPluckAnimation.Value || !interactTrigger.interactable)
            {
                // Skip animation
                return;
            }
            SetInteractableClientRpc(false);

            // Existing PluckAnim logic
            if (player != null)
            {
                PikerAnimEvents pikerAnimEvents = player.playerBodyAnimator.gameObject.GetComponent<PikerAnimEvents>();
                pikerAnimEvents.sprout = this;
                pikerAnimEvents.controllerB = player;
                pikerAnimEvents.StartPluckCoroutine();
                PlayCustomAnimServerRpc(new NetworkObjectReference(player.NetworkObject));
                LethalMin.Logger.LogInfo($"Plucking E for R e A   l");
            }
            else
            {
                player = GameNetworkManager.Instance.localPlayerController;
                aud.PlayOneShot(LethalMin.PlayerPluckSound);
                PikerAnimEvents pikerAnimEvents = player.playerBodyAnimator.gameObject.GetComponent<PikerAnimEvents>();
                pikerAnimEvents.sprout = this;
                pikerAnimEvents.controllerB = player;
                pikerAnimEvents.StartPluckCoroutine();
                PlayCustomAnimServerRpc(new NetworkObjectReference(player.NetworkObject));
                LethalMin.Logger.LogInfo("Plucking E but player is null");
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void PlayCustomAnimServerRpc(NetworkObjectReference playerRef)
        {
            PlayCustomAnimClientRpc(playerRef);
        }
        [ClientRpc]
        public void PlayCustomAnimClientRpc(NetworkObjectReference playerRef)
        {
            playerRef.TryGet(out NetworkObject playerNetObj);
            GetComponentInChildren<Animator>().Play("Recorded (1)");
            PlayerControllerB player = playerNetObj.GetComponent<PlayerControllerB>();
            PlayerControllerBPatch.PlayCustomAnimation(player, 0f);
        }

        public Transform raycastOrigin;
        public LayerMask groundLayer;
        public float maxRaycastDistance = 50f;
        [ClientRpc]
        public void AdjustPositionClientRpc()
        {
            if (raycastOrigin == null)
            {
                raycastOrigin = transform.Find("RaycastOrigin");
                if (raycastOrigin == null)
                {
                    LethalMin.Logger.LogError("RaycastOrigin child not found!");
                    return;
                }
            }
            groundLayer = LethalMin.Instance.PikminColideable;
            if (raycastOrigin == null)
            {
                LethalMin.Logger.LogError("RaycastOrigin is not assigned!");
                return;
            }

            RaycastHit hit;
            if (Physics.Raycast(raycastOrigin.position, Vector3.down, out hit, maxRaycastDistance, groundLayer))
            {
                float targetY = hit.point.y;
                Vector3 currentPosition = transform.position;
                transform.position = new Vector3(currentPosition.x, targetY, currentPosition.z);
            }
            else
            {
                LethalMin.Logger.LogWarning("No ground detected within maxRaycastDistance.");
                if (IsServer)
                    NetworkObject.Despawn(true);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PluckServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            PikminManager.Instance.IncrementPikminRaised(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PluckServerRpc(NetworkObjectReference playerRef)
        {
            if (pikminPrefab != null)
            {
                playerRef.TryGet(out NetworkObject playerNetObj);
                PlayerControllerB player = playerNetObj.GetComponent<PlayerControllerB>();

                interactTrigger.enabled = false;
                GameObject pikminObj = Instantiate(pikminPrefab, transform.Find("PikSpawnPos").position, transform.Find("PikSpawnPos").rotation);
                NetworkObject networkObject = pikminObj.GetComponent<NetworkObject>();
                PikminAI pikminAI = pikminObj.GetComponent<PikminAI>();
                aud.transform.SetParent(null, true);
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    PikminManager.Instance.SpawnPikminClientRpc(new NetworkObjectReference(networkObject));
                }
                else
                {
                    LethalMin.Logger.LogError("NetworkObject component not found on Pikmin prefab.");
                }
                CreatePikminClientRPC(new NetworkObjectReference(pikminAI.NetworkObject), new NetworkObjectReference(player.NetworkObject));
            }
            else
            {
                LethalMin.Logger.LogError("Pikmin prefab is not set in the Sprout script.");
            }
        }


        [ClientRpc]
        public void CreatePikminClientRPC(NetworkObjectReference network1, NetworkObjectReference network2)
        {
            network1.TryGet(out NetworkObject PikObj);
            PikminAI script = PikObj.GetComponent<PikminAI>();
            if (script == null) { return; }
            network2.TryGet(out NetworkObject PlaObj);
            if (PlaObj.GetComponent<PlayerControllerB>() == null) { return; }
            // foreach (var item in script.GetComponentsInChildren<Renderer>(true))
            // {
            //     item.enabled = false;
            // }
            script.PreDefinedType = true;
            script.PminType = PminType;
            script.HideMeshOnStart = true;
            aud.PlayOneShot(LethalMin.PlayerPluckSound2);
            StartCoroutine(waitForInitalizePik(PikObj.GetComponent<PikminAI>(), PlaObj.GetComponent<PlayerControllerB>()));
        }
        IEnumerator waitForInitalizePik(PikminAI pikminAI, PlayerControllerB Player)
        {
            while (!pikminAI.HasInitalized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            LethalMin.Logger.LogInfo("PluckedMin");

            // foreach (var item in pikminAI.GetComponentsInChildren<Renderer>(true))
            // {
            //     item.enabled = true;
            // }

            pikminAI.ReqeustPlayBornSoundClientRpc();
            pikminAI.PlayAnimClientRpc("Pluck");
            pikminAI.AssignLeader(Player.GetComponent<PlayerControllerB>(), false);

            if (IsServer)
            {
                DespawnSproutServerRpc();
            }
        }
        void Update()
        {
            transform.localScale = new Vector3(LethalMin.SproutScale, LethalMin.SproutScale, LethalMin.SproutScale);
            interactTrigger.specialCharacterAnimation = !LethalMin.SkipPluckAnimation.Value;
        }
        [ServerRpc(RequireOwnership = false)]
        private void DespawnSproutServerRpc()
        {
            NetworkObject.Despawn(true);
            Destroy(gameObject);
        }
    }
}