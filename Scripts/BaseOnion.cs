using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class BaseOnion : Onion, IGenerationSwitchable
    {
        public OnionSoundPack pack = null!;
        public Animator anim = null!;
        public AudioSource audioSource = null!;
        public Transform MeshConatiner = null!;
        public GameObject FuniOnionMesh = null!;
        public GameObject OnionMesh = null!;
        public Transform ItemHoverPosition = null!;
        public InteractTrigger IntTrigger = null!;
        public BaseOnionFusionProperties fusionProperties = null!; // Custom fusion properties scriptable object, if any
        float leaveCheckInterval = 0.1f;
        bool IsLeavingPlanet;
        List<Renderer> renderersToEnable = new List<Renderer>();

        public void Awake()
        {
        }

        public override void Start()
        {
            base.Start();

            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>();
            }

            if (onionType.OnionOverrideModelPrefab == null)
            {
                OnionMesh = Instantiate(LethalMin.DefultOnionMesh, MeshConatiner);
            }
            else
            {
                OnionMesh = Instantiate(onionType.OnionOverrideModelPrefab, MeshConatiner);
            }
            foreach (Renderer r in OnionMesh.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled)
                {
                    renderersToEnable.Add(r);
                    r.enabled = false; // Disable all renderers to prevent flicker
                }
            }

            GenerationManager.Instance.Register(this);

            SwitchGeneration(PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.OnionModelGeneration.InternalValue));

            AllClimbLinks = GetComponentsInChildren<PikminLinkAnimation>(true).ToList();
            anim.Play("Land");
            StartCoroutine(WaitToShow());
        }

        IEnumerator WaitToShow()
        {
            yield return new WaitForSeconds(0.1f);
            foreach (Renderer r in renderersToEnable)
            {
                if (r != null)
                {
                    r.enabled = true; // Enable all renderers after a short delay
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (StartOfRound.Instance.shipIsLeaving && !IsLeavingPlanet)
            {
                if (leaveCheckInterval >= 0)
                {
                    leaveCheckInterval -= Time.deltaTime;
                }
                else
                {
                    LethalMin.Logger.LogDebug($"LethalMin: Checking if we can leave planet {gameObject.name}! {GetTypesCanHoldNotInsideOnion()}");
                    if (GetTypesCanHoldNotInsideOnion() == 0)
                    {
                        LeavePlanet();
                    }
                    leaveCheckInterval = 0.1f;
                }
            }
            if (LethalMin.FuniOnion)
            {
                MeshConatiner.gameObject.SetActive(false);
                FuniOnionMesh.SetActive(true);
            }
            else
            {
                MeshConatiner.gameObject.SetActive(true);
                FuniOnionMesh.SetActive(false);
            }
        }

        public override void SuckItemIntoOnion(PikminItem item, PikminType targetType = null!)
        {
            if (item == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null item when sucking into onion!");
                return;
            }
            if (item.settings == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null item settings when sucking into onion!");
                return;
            }
            if (item.settings.CanProduceSprouts)
            {
                if (item.settings.SproutsToSpawn <= 0)
                {
                    LethalMin.Logger.LogWarning($"LethalMin: item {item.gameObject.name} sprouts to spawn is less than or equal to 0!");
                    return;
                }
                if (!onionType.TypesCanHold.ToList().Contains(targetType))
                {
                    LethalMin.Logger.LogWarning($"LethalMin: item {item.gameObject.name} for {targetType.PikminName} cannot be sucked into onion of type {onionType.TypeName}!");
                    return;
                }
                LethalMin.Logger.LogInfo($"LethalMin: Sucking item {item.gameObject.name} into onion of type {onionType.TypeName}!");
                StartCoroutine(AnimateItemIntoOnion(item, targetType));
            }
        }
        public IEnumerator AnimateItemIntoOnion(PikminItem item, PikminType targetType)
        {
            LethalMin.Logger.LogInfo($"LethalMin: Animate item {item.gameObject.name} into onion of type {onionType.TypeName}!");
            item.ItemScript.grabbable = false;
            item.settings.GrabableToPikmin = false;
            item.settings.OverrideGrabbableToEnemeis = false;
            item.ItemScript.grabbableToEnemies = false;
            item.ItemScript.enabled = false;
            Transform itemTransform = item.ItemScript.transform;
            Vector3 startPosition = itemTransform.position;
            Quaternion startRotation = itemTransform.rotation;
            Vector3 startScale = itemTransform.localScale;

            // Collect all renderers and their original material states
            List<Renderer> renderers = item.ItemScript.gameObject.GetComponentsInChildren<Renderer>().ToList();
            renderers.AddRange(item.ExtraRenderers.Where(er => !renderers.Contains(er)));
            renderers.RemoveAll(r => r == null);

            // Store original emission settings and enable emission on all materials
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    // Check if it's an HDRP lit material
                    if (material.HasProperty("_EmissiveColor"))
                    {
                        // Enable emission
                        material.SetInt("_UseEmissiveIntensity", 1); // 1 to use intensity
                        material.SetFloat("_EmissiveExposureWeight", 1.0f);
                        material.SetColor("_EmissiveColor", Color.white);
                        //I need to set the emmisive color to a higher value if I want the emmisiion to be stronger fsm
                        // if (material.HasProperty("_EmissiveIntensity"))
                        // {
                        //     material.SetFloat("_EmissiveIntensity", 999f);
                        // }
                    }
                }
            }

            // Play audio if available
            audioSource.PlayOneShot(pack.VacItemSound);
            anim.SetTrigger("Inhale");

            // Step 1: Shake the item a bit (oscillate for half second)
            float shakeTime = 0.5f;
            float elapsed = 0f;
            float shakeIntensity = 150f;

            while (elapsed < shakeTime)
            {
                // Rotate side to side
                itemTransform.rotation =
                Quaternion.Euler(0, Mathf.Sin(elapsed * 10f) * shakeIntensity, 0) * startRotation;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Step 2: Move the item to hover position with a rising motion
            float hoverTime = 1.0f;
            elapsed = 0f;

            // Make sure we have a valid hover position
            if (ItemHoverPosition == null)
            {
                LethalMin.Logger.LogWarning("ItemHoverPosition is null in onion!");
                yield break;
            }

            Vector3 targetPosition = ItemHoverPosition.position;

            while (elapsed < hoverTime)
            {
                float t = elapsed / hoverTime;

                // Use easeInOut curve for smoother movement
                float smoothT = Mathf.SmoothStep(0, 1, t);

                // Move position with slight upward arc
                item.ItemScript.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

                // Rotate slowly
                itemTransform.rotation = Quaternion.Slerp(startRotation,
                                                        Quaternion.Euler(0, 360f * smoothT, 0) * startRotation,
                                                        smoothT);

                // Scale down at the end
                itemTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Step 3: Snap to hover position and scale down
            itemTransform.position = targetPosition;
            itemTransform.localScale = Vector3.zero;
            itemTransform.rotation = Quaternion.Euler(0, 0, 0);

            // Play onion animation if available
            audioSource.Stop();
            audioSource.PlayOneShot(pack.SuckInItemSound);
            anim.SetTrigger("Suck");

            // Code ran after the animation
            if (item.ItemScript.TryGetComponent(out RagdollGrabbableObject ragdoll))
            {
                if (IsServer)
                    SetPlayerToBeRevived(ragdoll, targetType.PikminTypeID);

                yield break;

            }

            if (LethalMin.AllowOnionToReviveMaskeds &&
            item.hackEnemyGrabbableObject != null && item.hackEnemyGrabbableObject.ai is MaskedPlayerEnemy)
            {
                if (IsServer)
                    SetEnemyToBeRevived(item.hackEnemyGrabbableObject);

                yield break;
            }

            int sproutsToSpawn = item.settings.SproutsToSpawn;
            if (item.settings.PerferedType == targetType && item.settings.PerferedTypeMultipler > 0)
            {
                sproutsToSpawn = Mathf.RoundToInt(item.settings.SproutsToSpawn * item.settings.PerferedTypeMultipler);
            }
            AddSproutsToSpawn(targetType, sproutsToSpawn);
            item.IncrumentDestoryCountServerRpc();
        }

        public override void SpawnSprouts()
        {
            SpawnSproutsRoutine = StartCoroutine(SproutSpawnRoutine());
            LethalMin.Logger.LogInfo($"LethalMin: Sprouts spawned from onion {gameObject.name}!");
        }

        IEnumerator SproutSpawnRoutine()
        {
            GameObject prefab = LethalMin.SproutPrefab;
            foreach (PikminType type in TypesToSpawn.Keys.ToList())
            {
                if (TypesToSpawn[type] > 0)
                {
                    int sproutsToSpawn = TypesToSpawn[type];
                    int b = 0;
                    anim.SetTrigger("Exhale");
                    audioSource.PlayOneShot(pack.SproutSound);
                    for (int i = 0; i < sproutsToSpawn; i++)
                    {
                        if (i + PikminManager.instance.PikminAIs.Count >= LethalMin.MaxPikmin.InternalValue
                        && IsServer)
                        {
                            LethalMin.Logger.LogInfo($"Adding sprout directly into onion instead of spawning {gameObject.name}!");
                            AddPikminClientRpc(new PikminData()
                            {
                                TypeID = type.PikminTypeID,
                                GrowthStage = 0,
                                DebugName = $"{type.PikminName}_{PikUtils.GenerateRandomString(new System.Random(StartOfRound.Instance.randomMapSeed + i))}",
                                BirthDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            });
                        }
                        else if (IsServer)
                        {
                            LethalMin.Logger.LogInfo($"Spawning sprout {i} of {sproutsToSpawn} for {type.PikminName} from onion {gameObject.name}!");
                            Vector3 spawnPos = GetPositionToSpawnSprout();
                            Quaternion RandomYRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            Sprout sprout = Instantiate(prefab, spawnPos, Quaternion.identity).GetComponent<Sprout>();
                            sprout.pikminType = type;
                            sprout.IsPersistant = true;
                            sprout.NetworkObject.Spawn();
                            sprout.InitalizeClientRpc(SproutSpawnPos.position, spawnPos, RandomYRotation, type.PikminTypeID);
                        }

                        b++;
                        if (b >= 2)
                        {
                            b = 0;
                            anim.SetTrigger("Exhale");
                            audioSource.PlayOneShot(pack.SproutSound);
                            yield return new WaitForSeconds(0.55f);
                        }
                    }
                    TypesToSpawn[type] = 0;
                }
            }
            SpawnSproutsRoutine = null;
        }

        public override void RevivePlayerViaOnion(PlayerControllerB player, int TypeID)
        {
            base.RevivePlayerViaOnion(player, TypeID);
            audioSource.PlayOneShot(pack.PlayerReviveSound);
        }

        public void LeavePlanet()
        {
            anim.Play("TakeOff");
            IsLeavingPlanet = true;
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

            OnionModelRefernces modelRefernces = GetComponentInChildren<OnionModelRefernces>();
            bool hasFound = false;

            void applyGeneration(OnionModelGeneration gen)
            {
                anim = gen.Animator;
                MatchBeam(gen.SummonBeam);
                ClimbLinks = gen.ClimbLinks;
                ItemDropPos = gen.ItemDropPos;
                SproutSpawnPos = gen.SproutSpawnPos;
                ItemHoverPosition = gen.ItemHoverPos;
                if (gen.SoundPack == null)
                {
                    gen.SoundPack = LethalMin.DefaultOnionSoundPack;
                }
                pack = gen.SoundPack;
                if (gen.SummonBeam.GetComponent<Collider>() != null)
                {
                    gen.SummonBeam.GetComponent<Collider>().enabled = false;
                }
                gen.SummonBeam.GetComponent<Renderer>().material.color =
                new Color(onionType.OnionColor.r, onionType.OnionColor.g, onionType.OnionColor.b, gen.SummonBeam.GetComponent<Renderer>().material.color.a);
                fusionProperties = gen.FusionProperties;
                fusionProperties.MainOnionRenderer = gen.MainOnionRenderer;
                fusionProperties.ApplyFusionProperties(onionType, fusedTypes);
            }

            void useDefultGeneration()
            {
                modelRefernces.Model.SetActive(true);
                anim = modelRefernces.Animator;
                MatchBeam(modelRefernces.SummonBeam);
                ClimbLinks = modelRefernces.ClimbLinks;
                ItemDropPos = modelRefernces.ItemDropPos;
                SproutSpawnPos = modelRefernces.SproutSpawnPos;
                ItemHoverPosition = modelRefernces.ItemHoverPos;
                if (modelRefernces.SoundPack == null)
                {
                    modelRefernces.SoundPack = LethalMin.DefaultOnionSoundPack;
                }
                pack = modelRefernces.SoundPack;
                if (modelRefernces.SummonBeam.GetComponent<Collider>() != null)
                {
                    modelRefernces.SummonBeam.GetComponent<Collider>().enabled = false;
                }
                fusionProperties = modelRefernces.FusionProperties;
                fusionProperties.MainOnionRenderer = modelRefernces.MainOnionRenderer;
                fusionProperties.ApplyFusionProperties(onionType, fusedTypes);
            }

            foreach (OnionModelGeneration gen in modelRefernces.Generations)
            {
                if (!PikChecks.IsGenerationValid(gen))
                {
                    LethalMin.Logger.LogError($"Generation {gen.Generation} is not valid for this Onion! Generation: {generation}, Type: {onionType.TypeName}");
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
                LethalMin.Logger.LogWarning($"Failed to find a valid generation for Onion! Generation: {generation}, Type: {onionType.TypeName}");
                useDefultGeneration();
            }
        }

        public void MatchBeam(GameObject go = null!)
        {
            Renderer render = go.GetComponent<Renderer>();
            Material mat = render.material;
            Collider collider = go.GetComponent<Collider>();
            Mesh? mesh = null;
            if (go.TryGetComponent(out MeshFilter meshF))
            {
                mesh = meshF.sharedMesh;
            }
            if (go.TryGetComponent(out SkinnedMeshRenderer skinnedMesh))
            {
                mesh = skinnedMesh.sharedMesh;
            }

            IntTrigger.transform.position = go.transform.position;
            IntTrigger.transform.rotation = go.transform.rotation;
            IntTrigger.transform.localScale = go.transform.lossyScale;
            IntTrigger.GetComponent<Renderer>().material = mat;
            IntTrigger.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        public void OnBeamInteract(PlayerControllerB controller)
        {
            OnionHUDManager.instance.SetCurrentOnion(this);
            OnionHUDManager.instance.OpenMenu();
        }

    }
}
