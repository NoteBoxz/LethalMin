using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class OnionItem : GrabbableObject, IGenerationSwitchable
    {
        public OnionType onionType = null!;
        public bool DontChooseRandomType = false;
        public Transform ModelContainer = null!;
        public Animator anim = null!;
        public bool IsDoingCollectAnim = false;
        public Renderer triangleRender = null!;
        float DistanceCheckTimer = 0;
        ScanNodeProperties sNode = null!;
        bool StartCalled = false;

        public void Awake()
        {
            sNode = GetComponentInChildren<ScanNodeProperties>();
        }

        public override void Start()
        {
            if (StartCalled)
            {
                // Because for some reason Imperium calls start when spawning an item
                LethalMin.Logger.LogWarning($"OnionItem {gameObject.name} has already been started!");
                return;
            }
            StartCalled = true;

            base.Start();

            if (onionType == null && DontChooseRandomType == false)
            {
                onionType = GetRandomOnionType();
                LethalMin.Logger.LogInfo($"Onion Item {gameObject.name} has been assigned a random type: {onionType.name}");
            }

            //so my IDE doesn't wine about possible null refence
            if (onionType == null)
            {
                LethalMin.Logger.LogError($"Onion type {gameObject.name} is some how null????????????!?!?!??!??!?!");
                return;
            }

            triangleRender.material.color = onionType.OnionColor;

            if (onionType.OnionItemOverrideModelPrefab == null)
            {
                LethalMin.Logger.LogInfo($"Onion {onionType.TypeName} does not have an override model, using default model instead.");
                GameObject obj = Instantiate(LethalMin.DefultOnionItemModel, ModelContainer);
            }
            else
            {
                LethalMin.Logger.LogInfo($"Onion {onionType.TypeName} has an override model, using it instead.");
                GameObject obj = Instantiate(onionType.OnionItemOverrideModelPrefab, ModelContainer);
            }

            GenerationManager.Instance.Register(this);

            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.OnionItemModelGeneration.InternalValue));

            if (IsServer && PikminManager.instance.OnionsCollected.Contains(onionType.OnionTypeID))
            {
                LethalMin.Logger.LogFatal($"Onion {onionType.TypeName} was already collected, despawning it!");
                NetworkObject.Despawn(true);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            GenerationManager.Instance.Unregister(this);
        }

        /// <summary>
        /// Updates the Onion's generation based on the current generation
        /// </summary>
        public void SwitchGeneration(PikminGeneration generation)
        {
            if (LethalMin.RandomizeGenerationModels.InternalValue)
                generation = (PikminGeneration)Random.Range(0, System.Enum.GetValues(typeof(PikminGeneration)).Length);

            OnionItemModelRefernces modelRefernces = GetComponentInChildren<OnionItemModelRefernces>();
            bool hasFound = false;

            void applyGeneration(OnionItemModelGeneration gen)
            {
                anim = gen.Animator;
                if (gen.MainRenderer != null)
                    gen.MainRenderer.material.color = onionType.OnionColor;
            }
            void applyDefaultGeneration()
            {
                modelRefernces.Model.SetActive(true);
                anim = modelRefernces.Animator;
                if (modelRefernces.MainRenderer != null)
                    modelRefernces.MainRenderer.material.color = onionType.OnionColor;
            }

            foreach (OnionItemModelGeneration gen in modelRefernces.Generations)
            {
                if (!PikChecks.IsGenerationValid(gen))
                {
                    LethalMin.Logger.LogError($"Generation {gen.Generation} of type {onionType.TypeName} is invaild for an onion item!");
                    continue;
                }

                gen.Model.SetActive(gen.Generation == generation);

                if (gen.Generation == generation)
                {
                    applyGeneration(gen);
                    hasFound = true;
                }
            }

            if (!hasFound)
            {
                applyDefaultGeneration();
                LethalMin.Logger.LogWarning($"Failed to find a valid generation for {onionType.TypeName}! Generation: {generation}");
            }
        }

        public override void GrabItem()
        {
            base.GrabItem();
        }

        public override void Update()
        {
            base.Update();


            sNode.gameObject.SetActive(LethalMin.MakeItemsScanable && !isHeld && !isHeldByEnemy);

            if (IsOwner && onionType.ActivatesWhenBroughtOutside && !IsDoingCollectAnim && playerHeldBy != null && !playerHeldBy.isInsideFactory)
            {
                IsDoingCollectAnim = true;
                StartCollectingSequnceServerRpc();
            }
            if (IsOwner && isInShipRoom && !IsDoingCollectAnim)
            {
                IsDoingCollectAnim = true;
                StartCollectingSequnceServerRpc();
            }
            if (IsOwner && onionType.ActivatesWhenPlayerIsNear && !IsDoingCollectAnim)
            {
                DistanceCheckTimer -= Time.deltaTime;
                if (DistanceCheckTimer <= 0)
                {
                    DistanceCheckTimer = 1f;
                    CheckForPlayers();
                }
            }
        }

        public void CheckForPlayers()
        {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null)
                {
                    continue;
                }

                if (Vector3.Distance(player.transform.position, transform.position) <= onionType.ActivationDistance)
                {
                    IsDoingCollectAnim = true;
                    StartCollectingSequnceServerRpc();
                    return;
                }
            }
        }

        [ClientRpc]
        public void InitalizeTypeClientRpc(int TypeID)
        {
            OnionType oT = LethalMin.GetOnionTypeByID(TypeID);
            if (oT != null)
            {
                onionType = oT;
            }
            else
            {
                LethalMin.Logger.LogError($"Invalid onion type: {TypeID} when initalizeing type!");
                DontChooseRandomType = false;
            }
        }

        public OnionType GetRandomOnionType()
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + PikminManager.instance.Onions.Count);
            List<OnionType> UseableTypes = new List<OnionType>();
            foreach (OnionType type in LethalMin.RegisteredOnionTypes.Values)
            {
                if (!type.SpawnsIndoors && !type.SpawnsOutdoors)
                {
                    continue;
                }
                List<OnionItem> items = FindObjectsOfType<OnionItem>().ToList();
                bool IsTypeInvalid = false;
                foreach (OnionItem itm in items)
                {
                    if (itm.onionType == type)
                    {
                        IsTypeInvalid = true;
                    }
                }
                foreach (Onion onion in FindObjectsOfType<Onion>())
                {
                    if (onion.onionType != null)
                    {
                        if (onion.onionType == type)
                        {
                            // Don't spawn the same type of onion in the same level!
                            IsTypeInvalid = true;
                        }
                        if (onion.fusedTypes.Contains(type))
                        {
                            // Don't spawn a type that is fused with another onion in the same level!
                            IsTypeInvalid = true;
                        }
                    }
                }

                if (IsTypeInvalid)
                {
                    continue;
                }
                else
                {
                    UseableTypes.Add(type);
                }
            }
            if (UseableTypes.Count == 0)
            {
                LethalMin.Logger.LogError($"No valid onion types found for {gameObject.name}, falling back to a random type: {onionType.TypeName}");
                return LethalMin.RegisteredOnionTypes[random.Next(0, LethalMin.RegisteredOnionTypes.Count)];
            }
            else
            {
                return UseableTypes[random.Next(0, UseableTypes.Count)];
            }
        }


        [ServerRpc]
        public void StartCollectingSequnceServerRpc()
        {
            StartCollectingSequnceClientRpc();
        }


        [ClientRpc]
        public void StartCollectingSequnceClientRpc()
        {
            StartCoroutine(DoCollectAnim());
        }

        IEnumerator DoCollectAnim()
        {
            yield return new WaitForSeconds(onionType.ActivationTime);

            if (playerHeldBy != null)
            {
                playerHeldBy.DropAllHeldItems();
            }

            grabbable = false;
            IsDoingCollectAnim = true;
            grabbableToEnemies = false;

            anim.Play("Collect");

            yield return new WaitForSeconds(4f);

            if (!PikminManager.instance.OnionsCollected.Contains(onionType.OnionTypeID))
            {
                PikminManager.instance.OnionsCollected.Add(onionType.OnionTypeID);
            }

            if (IsServer)
            {
                PikminManager.instance.SpawnOnionOnServer(onionType.OnionTypeID);

                NetworkObject.Despawn(true);
            }
        }
    }
}
