using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Compats;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class Sprout : NetworkBehaviour, IGenerationSwitchable
    {
        public enum SproutPlantColor
        {
            Default,
            Red,
            Purple,
            Yellow
        }
        public Transform ModelContainer = null!;
        public InteractTrigger? interactTrigger = null;
        public Transform SpawnPos = null!;
        public PikminType pikminType = null!;
        public Animator sproutAnimator = null!;
        public AudioSource sproutAudio = null!;
        public AudioClip PlantSFX = null!, GrowSFX = null!, PullSFX = null!, PluckSFX = null!;
        public int CurrentGrowthStage = 0;
        public int MaxGrowthStage = 2;
        public float GrowTimer = 100f;
        public ulong PlayerPluckingID = 9999999999999999;
        public PikminAI pikminSpawned = null!;
        private bool HasNoGrowthStages;
        private Dictionary<int, List<GameObject>> growthObjects = new Dictionary<int, List<GameObject>>();
        public Coroutine pluckRoutine = null!;
        public bool IsPersistant = false;
        public bool IsBeingPlucked = false;
        SproutPlantColor sproutColor = SproutPlantColor.Default;

        public void Start()
        {

        }

        [ClientRpc]
        public void InitalizeClientRpc(SproutData data)
        {
            InitalizeOnLocalClient(data.Position, data.Rotaion, data.TypeID);
            SetGrowth(data.GrowthStage);
        }

        [ClientRpc]
        public void InitalizeClientRpc(Vector3 Position, Quaternion Rotaion, int TypeID)
        {
            InitalizeOnLocalClient(Position, Rotaion, TypeID);
        }

        [ClientRpc]
        public void InitalizeClientRpc(Vector3 StartPosition, Vector3 EndPosition, Quaternion Rotaion, int TypeID)
        {
            InitalizeOnLocalClient(StartPosition, Rotaion, TypeID);
            StartCoroutine(AnimateSprout(StartPosition, EndPosition, Rotaion));
        }

        public void InitalizeOnLocalClient(Vector3 Position, Quaternion Rotaion, int TypeID)
        {
            pikminType = LethalMin.GetPikminTypeByID(TypeID);
            transform.rotation = Rotaion;
            transform.position = Position;
            GrowTimer = Random.Range(50, 500);
            sproutColor = pikminType.SproutPlantColor;
            GameObject Model = null!;

            if (pikminType.SproutOverrideModel == null)
            {
                Model = Instantiate(LethalMin.DefultPikminSproutMesh, ModelContainer);
            }
            else
            {
                Model = Instantiate(pikminType.SproutOverrideModel, ModelContainer);
            }

            GenerationManager.Instance.Register(this);

            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.SproutModelGeneration.InternalValue));

            GetGrowthObjects();
            SetGrowth(0);

            LethalMin.Logger.LogDebug($"{LethalMin.GetPikminTypeByID(TypeID).PikminName} sprout initalized at ({Position},{Rotaion.eulerAngles})");
        }


        IEnumerator AnimateSprout(Vector3 StartPosition, Vector3 EndPosition, Quaternion Rotaion)
        {
            if (interactTrigger != null)
                interactTrigger.interactable = false;
            float duration = 1.5f; // Animation duration in seconds
            float elapsed = 0f;

            // Store initial transform values
            Vector3 initialScale = Vector3.zero;
            Vector3 targetScale = transform.localScale;
            Quaternion initialRotation = Quaternion.Euler(180, Rotaion.eulerAngles.y, 0); // Start upside down
            Vector3 EndPosHighY = new Vector3(EndPosition.x, StartPosition.y, EndPosition.z); // Higher Y for arc effect

            // Set initial state
            transform.position = StartPosition;
            transform.rotation = initialRotation;
            transform.localScale = initialScale;

            // Calculate a control point for curved path (higher Y for arc effect)
            Vector3 controlPoint = Vector3.Lerp(StartPosition, EndPosHighY, 0.25f) + Vector3.up * 5f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;

                // Ease-in curve for accelerating effect (cubic ease-in)
                float smoothT = t * t * t;

                // Use quadratic Bezier curve for path
                float oneMinusT = 1f - smoothT;
                Vector3 position = (oneMinusT * oneMinusT * StartPosition) +
                                   (2f * oneMinusT * smoothT * controlPoint) +
                                   (smoothT * smoothT * EndPosition);

                // Animate rotation (starting upside down, gradually rotating to final rotation)
                Quaternion rotation = Quaternion.Slerp(initialRotation, Rotaion, smoothT);

                // Animate scale with quick pop-in effect (faster than position and rotation)
                float scaleT = Mathf.Min(1f, t * 4f); // Scale reaches full size in half the time
                Vector3 scale = Vector3.Lerp(initialScale, targetScale, scaleT);

                // Apply the transformations
                transform.position = position;
                transform.rotation = rotation;
                transform.localScale = scale;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure the final state is exactly what we want
            transform.position = EndPosition;
            transform.rotation = Rotaion;
            transform.localScale = targetScale;

            sproutAudio.PlayOneShot(PlantSFX);

            // Re-enable interaction
            if (interactTrigger != null)
                interactTrigger.interactable = true;

            LethalMin.Logger.LogInfo($"Sprout animation complete: {pikminType.PikminName} at position {EndPosition}");
        }

        public override void OnDestroy()
        {
            base.OnDestroy(); // thanks zeekees :)
            GenerationManager.Instance.Unregister(this);
        }


        /// <summary>
        /// Updates the Sprout's generation based on the current generation
        /// </summary>
        public void SwitchGeneration(PikminGeneration generation)
        {
            if (LethalMin.RandomizeGenerationModels.InternalValue)
                generation = (PikminGeneration)Random.Range(0, System.Enum.GetValues(typeof(PikminGeneration)).Length);

            SproutModelRefences modelRefernces = GetComponentInChildren<SproutModelRefences>();
            bool hasFound = false;

            void applyGeneration(SproutModelGeneration gen)
            {
                sproutAnimator = gen.Animator;
                if (gen.MainRenderer == null)
                {
                    return;
                }

                if (pikminType.PikminSproutOverrideMaterial != null && pikminType.PikminSproutOverrideMaterial.Length > 0)
                {
                    Material mat = pikminType.PikminSproutOverrideMaterial[0];
                    if (!PikUtils.IsOutOfRange(pikminType.PikminSproutOverrideMaterial, (int)generation))
                        mat = pikminType.PikminSproutOverrideMaterial[(int)generation];

                    gen.MainRenderer.material = mat;
                }
                else if (pikminType.SetColorOnSprout)
                {
                    gen.MainRenderer.material.SetTexture("_BaseColorMap", null);
                    gen.MainRenderer.material.color = pikminType.PikminPrimaryColor;
                }
            }

            void applyDefaultGeneration()
            {
                modelRefernces.Model.SetActive(true);
                sproutAnimator = modelRefernces.Animator;
                if (modelRefernces.MainRenderer == null)
                {
                    return;
                }

                if (pikminType.PikminSproutOverrideMaterial != null && pikminType.PikminSproutOverrideMaterial.Length > 0)
                {
                    Material mat = pikminType.PikminSproutOverrideMaterial[0];
                    if (!PikUtils.IsOutOfRange(pikminType.PikminSproutOverrideMaterial, (int)generation))
                        mat = pikminType.PikminSproutOverrideMaterial[(int)generation];

                    modelRefernces.MainRenderer.material = mat;
                }
                else if (pikminType.SetColorOnSprout)
                {
                    modelRefernces.MainRenderer.material.color = pikminType.PikminPrimaryColor;
                }
            }

            foreach (SproutModelGeneration gen in modelRefernces.Generations)
            {
                if (!PikChecks.IsGenerationValid(gen))
                {
                    LethalMin.Logger.LogError($"Generation {gen.Generation} of type {pikminType.PikminName} is invaild for a sprout!");
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
                LethalMin.Logger.LogWarning($"Failed to find a valid generation for sprout! Generation: {generation}");
                applyDefaultGeneration();
            }
        }



        void Update()
        {
            float val = LethalMin.SproutScale.InternalValue;
            transform.localScale = new Vector3(val, val, val);
            //Only the server should update the growth stage
            if (IsServer && CurrentGrowthStage < MaxGrowthStage)
            {
                GrowTimer -= Time.deltaTime;
                if (GrowTimer <= 0)
                {
                    GrowTimer = Random.Range(40, 500);
                    SetGrowthClientRpc(CurrentGrowthStage + 1);
                }
            }
        }



        public void GetGrowthObjects()
        {
            SproutModelRefences modelReferences = GetComponentInChildren<SproutModelRefences>();
            growthObjects = new Dictionary<int, List<GameObject>>();
            growthObjects.Clear();
            MaxGrowthStage = 0;

            void ProcessPlants(List<GameObject> plants, List<Material> BudMats, List<Material> FlowerMats)
            {
                foreach (GameObject go in plants)
                {
                    if (go == null)
                    {
                        LethalMin.Logger.LogError($"A Sprout's Growth object is null");
                        continue;
                    }

                    int index = plants.IndexOf(go);
                    if (!growthObjects.ContainsKey(index))
                    {
                        growthObjects[index] = new List<GameObject>();
                    }
                    growthObjects[index].Add(go);

                    if (index == 1 && !PikUtils.IsOutOfRange(BudMats, (int)sproutColor))
                    {
                        go.GetComponent<Renderer>().material = BudMats[(int)sproutColor];
                    }
                    else if (index == 2 && !PikUtils.IsOutOfRange(FlowerMats, (int)sproutColor))
                    {
                        go.GetComponent<Renderer>().material = FlowerMats[(int)sproutColor];
                    }

                    if (index > MaxGrowthStage)
                        MaxGrowthStage = index;

                    //LethalMin.Logger.LogInfo($"Sprout Found plant {go.name} at index {index}");
                }
            }

            // Process base plants
            ProcessPlants(modelReferences.Plants, modelReferences.AltBudMaterials, modelReferences.AltFlowerMaterials);

            // Process plants from generations
            foreach (SproutModelGeneration gen in modelReferences.Generations)
            {
                ProcessPlants(gen.Plants, gen.AltBudMaterials, gen.AltFlowerMaterials);
            }

            HasNoGrowthStages = growthObjects.Count == 0;

            if (HasNoGrowthStages)
            {
                LethalMin.Logger.LogWarning($"Sprout found no growth stages");
            }
        }

        public void SetGrowth(int Stage)
        {
            if (HasNoGrowthStages)
            {
                return;
            }
            if (Stage < 0 || Stage > MaxGrowthStage)
            {
                LethalMin.Logger.LogWarning($"Sprout Invalid growth stage {Stage}");
                return;
            }
            LethalMin.Logger.LogDebug($"Sprout Setting growth stage to {Stage}");

            if (Stage > CurrentGrowthStage)
            {
                //sproutAnimator.SetTrigger("Grow");
                if (GrowSFX != null)
                    sproutAudio.PlayOneShot(GrowSFX);
            }

            CurrentGrowthStage = Stage;

            foreach (var pair in growthObjects)
            {
                foreach (var go in pair.Value)
                {
                    go.SetActive(pair.Key == Stage);
                }
            }
        }

        [ClientRpc]
        public void SetGrowthClientRpc(int Stage)
        {
            if (HasNoGrowthStages)
            {
                return;
            }
            SetGrowth(Stage);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayPullSoundServerRpc(ServerRpcParams rpcParams = default)
        {
            // Get the ID of the client that called this ServerRpc
            ulong senderId = rpcParams.Receive.SenderClientId;

            // Create ClientRpcParams that excludes the sender
            ClientRpcParams sendParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                        .Where(id => id != senderId)
                        .ToArray()
                }
            };

            // Pass the options to the ClientRpc
            PlayPullSoundClientRpc(sendParams);
        }

        [ClientRpc]
        public void PlayPullSoundClientRpc(ClientRpcParams rpcParams = default)
        {
            if (sproutAudio != null && PullSFX != null)
            {
                sproutAudio.PlayOneShot(PullSFX);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayPluckSoundServerRpc(ServerRpcParams rpcParams = default)
        {
            // Get the ID of the client that called this ServerRpc
            ulong senderId = rpcParams.Receive.SenderClientId;

            // Create ClientRpcParams that excludes the sender
            ClientRpcParams sendParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                        .Where(id => id != senderId)
                        .ToArray()
                }
            };

            // Pass the options to the ClientRpc
            PlayPluckSoundClientRpc(sendParams);
        }

        [ClientRpc]
        public void PlayPluckSoundClientRpc(ClientRpcParams rpcParams = default)
        {
            if (sproutAudio != null && PluckSFX != null)
            {
                sproutAudio.PlayOneShot(PluckSFX);
            }
        }

        /// <summary>
        /// Called while the interact button is held
        /// </summary>
        public void HoldingInteractEvent()
        {
            if (interactTrigger != null)
            {
                interactTrigger.specialCharacterAnimation = !LethalMin.SkipPluckAnimation;
            }
        }

        /// <summary>
        /// Called after the interact animation is played
        /// Or called after the interact bar is filled if there is no special animation
        /// </summary>
        public void OnInteract(PlayerControllerB player)
        {
            if (interactTrigger != null && !interactTrigger.specialCharacterAnimation)
            {
                if (player == null)
                {
                    LethalMin.Logger.LogError("Sprout OnInteract called with null player");
                    return;
                }
                sproutAudio.PlayOneShot(PluckSFX);
                PluckAndDespawnServerRpc(player.OwnerClientId);
            }
        }

        /// <summary>
        /// Called when the interact bar is filled and before the interact animation is played
        /// </summary>
        public void OnInteractEarlyOnOtherClients(PlayerControllerB player)
        {
            PlayerPluckingID = player.OwnerClientId;
            Leader leader = player.GetComponent<Leader>();

            leader.CustomAnimController.ReplaceAnimatorWithOverride();
            //sproutAnimator.SetTrigger("Pluck");
            IsBeingPlucked = true;

            if (pluckRoutine != null)
            {
                StopCoroutine(pluckRoutine);
                pluckRoutine = null!;
            }
            pluckRoutine = StartCoroutine(PluckRoutine(player));
        }

        /// <summary>
        /// Called while the interact button is held and the interact trigger is does not have a special animation
        /// Player is always null here...
        /// </summary>
        public void OnInteractEarly(PlayerControllerB player)
        {

        }

        /// <summary>
        /// When the player stops holding the interact button
        /// Player is always null here...
        /// </summary>
        public void OnStopInteract(PlayerControllerB player)
        {

        }

        /// <summary>
        /// Called when the animation is stopped
        /// Player is null on other clients
        /// </summary>
        public void OnCancleAnimation(PlayerControllerB player)
        {
            if (player == null || !player.IsOwner)
            {
                return;
            }
            LethalMin.Logger.LogInfo($"Sprout animation was cancled");

            if (pluckRoutine != null)
            {
                StopCoroutine(pluckRoutine);
                pluckRoutine = null!;
            }
            IsBeingPlucked = false;
            player.GetComponent<Leader>().CustomAnimController.RevertAnimator();

            if (interactTrigger != null)
            {
                SyncAnimCancleRpc(interactTrigger.playerScriptInSpecialAnimation.OwnerClientId);
            }
        }

        [Rpc(SendTo.NotMe)]
        public void SyncAnimCancleRpc(ulong ID)
        {
            if (StartOfRound.Instance.localPlayerController.OwnerClientId == ID)
            {
                return;
            }

            Leader? leader = LethalMin.GetLeaderViaID(ID);
            if (leader == null)
            {
                LethalMin.Logger.LogError($"Sprout Failed to find leader with ID {ID} when syncing cancle");
            }

            LethalMin.Logger.LogInfo($"Sprout animation was cancled (synced)");

            if (pluckRoutine != null)
            {
                StopCoroutine(pluckRoutine);
                pluckRoutine = null!;
            }
            IsBeingPlucked = false;
            leader?.CustomAnimController.RevertAnimator();
        }

        IEnumerator PluckRoutine(PlayerControllerB player)
        {
            float FrameToSeconds(int frame, int FPS = 60)
            {
                return (float)frame / (float)FPS;
            }

            yield return new WaitForSeconds(FrameToSeconds(25));
            sproutAudio.PlayOneShot(PullSFX);
            yield return new WaitForSeconds(FrameToSeconds(35));
            sproutAudio.PlayOneShot(PluckSFX);

            if (IsServer)
            {
                SpawnPikminServerRpc();
            }

            StartCoroutine(DespawnRoutine());

            pluckRoutine = null!;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PluckAndDespawnServerRpc(ulong ID)
        {
            SpawnPikminOnServer(false, (long)ID);
            StartDespawnRoutineClientRpc(ID);
        }
        [ClientRpc]
        public void StartDespawnRoutineClientRpc(ulong ID)
        {
            if (pluckRoutine != null)
            {
                StopCoroutine(pluckRoutine);
                pluckRoutine = null!;
            }
            if (StartOfRound.Instance.localPlayerController.OwnerClientId != ID)
                sproutAudio.PlayOneShot(PluckSFX);
            StartCoroutine(DespawnRoutine());
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPikminServerRpc(bool Despawn = false, long ID = -1)
        {
            SpawnPikminOnServer(Despawn, ID);
        }

        public void SpawnPikminOnServer(bool Despawn = false, long ID = -1)
        {
            if (pikminSpawned != null)
            {
                LethalMin.Logger.LogWarning($"Sprout has already spawned a pikmin");
                return;
            }
            if (ID != -1)
            {
                PlayerPluckingID = (ulong)ID;
            }
            PikminSpawnProps props = new PikminSpawnProps();
            props.GrowthStage = CurrentGrowthStage;
            props.PlayerID = PlayerPluckingID;
            props.SpawnAnimation = "Plucked";
            props.SpawnSound = "Born";
            props.OverrideVolume = 1;
            props.SpawnSoundDelay = 0.2f;
            props.AddToSpawnCount = true;
            LethalMin.Logger.LogInfo($"SpawnPosition: {SpawnPos.position} SpawnRotaion: {transform.rotation.eulerAngles}");
            pikminSpawned = PikminManager.instance.SpawnPikminOnServer(pikminType, SpawnPos.position, transform.rotation, props);
            if (Despawn)
            {
                NetworkObject.Despawn(true);
            }
        }

        public IEnumerator DespawnRoutine()
        {
            foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
            {
                render.enabled = false;
            }
            if (interactTrigger != null)
                interactTrigger.interactable = false;

            yield return new WaitForSeconds(2f);

            if (interactTrigger != null && interactTrigger.lockedPlayer != null)
            {
                interactTrigger.StopSpecialAnimation();
            }
            if (IsServer)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}