using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LethalMin
{
    public class AnimatedOnion : Onion
    {
        // Fields
        public GameObject RealMesh, FunniMesh;
        public GameObject Beam;
        public bool FunniMode = false;
        private AudioSource Audio;
        private Animator onionAnimator;

        // Methods

        public override void Start()
        {
            base.Start();
            SetupMeshes();
            SetupAnimator();
            SetupBeam();
        }

        private void SetupBeam()
        {
            Beam = transform.Find("BeamZone").gameObject;
            Beam.SetActive(false);
            StartCoroutine(ShowBeam());
        }

        private IEnumerator ShowBeam()
        {
            yield return new WaitForSeconds(6.3f);
            Beam.SetActive(true);
        }
        private IEnumerator HideBeam()
        {
            yield return new WaitForSeconds(1.1f);
            Beam.SetActive(false);
        }

        private void SetupMeshes()
        {
            RealMesh = transform.Find("mesh/RealMesh").gameObject;
            FunniMesh = transform.Find("mesh/TempMesh").gameObject;
            AnimPos = transform.Find("PikminAnimPos");
            SucPoint = transform.Find("mesh/SK_stg_Onyon/ObjectSucPoint");
            SpiPoint = transform.Find("mesh/SK_stg_Onyon/SproutSpiPoint");
            Audio = transform.Find("Sound").GetComponent<AudioSource>();
            transform.Find("mesh").transform.rotation = Quaternion.Euler(0, OnionRandom.Next(360), 0);
        }

        private void SetupAnimator()
        {
            onionAnimator = GetComponentInChildren<Animator>();
            if (onionAnimator == null)
            {
                LethalMin.Logger.LogError("Animator component not found in children of Onion object.");
            }
        }
        [ClientRpc]
        public void SyncOnionTypeClientRpc(int newType)
        {
            type = LethalMin.GetOnionTypeById(newType);
            UpdateOnionVisuals(); // Method to update the Onion's appearance based on its type
        }
        [ClientRpc]
        public void SyncOnionTypeClientRpc(int[] newTypes)
        {
            // Get the first onion type as a base
            OnionType baseOnionType = LethalMin.RegisteredOnionTypes[newTypes[0]];

            // Create a new OnionType
            OnionType fusedOnionType = ScriptableObject.CreateInstance(typeof(OnionType)) as OnionType;
            fusedOnionType.name = $"Fusion of {string.Join(", ", newTypes)}";
            fusedOnionType.OnionTypeID = baseOnionType.OnionTypeID;
            fusedOnionType.TypeName = $"Fused {baseOnionType.TypeName}";
            fusedOnionType.OnionPrefab = baseOnionType.OnionPrefab;
            fusedOnionType.OnionMaterial = baseOnionType.OnionMaterial;
            fusedOnionType.TypesCanHold = new PikminType[0];

            List<PikminType> FusedPTypes = new List<PikminType>();

            // Combine TypesCanHold from all fused onions
            foreach (int onionId in newTypes)
            {
                OnionType onionType = LethalMin.GetOnionTypeById(onionId);
                FusedPTypes.AddRange(onionType.TypesCanHold);
                FusedTypes.Add(onionType);
                LethalMin.Logger.LogInfo($"mixing onions {onionType.TypeName} and {baseOnionType.TypeName}");
            }
            fusedOnionType.TypesCanHold = FusedPTypes.ToArray();

            type = fusedOnionType;

            foreach (var item in FindObjectsOfType<PikminAI>())
            {
                item.CheckForOnion(FindObjectsOfType<Onion>());
            }
            UpdateFusionOnionVisuals(); // Method to update the Onion's appearance based on its type
        }

        public void UpdateOnionVisuals()
        {
            if (transform.Find("mesh/RealMesh/SK_stg_Onyon.021") == null)
            {
                LethalMin.Logger.LogError("Onion mesh not found. Make sure the hierarchy is correct.");
                return;
            }

            Renderer onionRenderer = transform.Find("mesh/RealMesh/SK_stg_Onyon.021").GetComponent<Renderer>();
            if (onionRenderer == null)
            {
                LethalMin.Logger.LogError("Renderer component not found on Onion mesh.");
                return;
            }

            Material onionMaterial = null!;
            Texture2D OnionTexture = null!;
            if (type.OnionMaterial != null)
            {
                onionMaterial = type.OnionMaterial;
            }
            if (type.OnionTexture != null)
            {
                OnionTexture = type.OnionTexture;
            }

            if (onionMaterial != null)
            {
                onionRenderer.material = onionMaterial;
            }
            if (OnionTexture != null)
            {
                onionRenderer.material.mainTexture = OnionTexture;
            }
            onionRenderer.material.color = type.OnionColor;
            transform.Find("mesh/SK_stg_Onyon/root/S_j000/S_j001/S_j030/MapDot").GetComponent<Renderer>().material.color = LethalMin.GetColorFromPType(type);
        }

        public void UpdateFusionOnionVisuals()
        {
            Renderer onionRenderer = transform.Find("mesh/RealMesh/SK_stg_Onyon.021").GetComponent<Renderer>();
            switch (FusedTypes.Count)
            {
                case 2:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                    GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.TwoSideOnion;
                    break;
                case 3:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                    GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.ThreeSideOnion;
                    break;
                case 4:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                    GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.FourSideOnion;
                    break;
                case 5:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                    GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.FiveSideOnion;
                    break;
                case 6:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                    GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.SixSideOnion;
                    break;
                case 7:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                    GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.SevenSideOnion;
                    break;
                case 8:
                    transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                   GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.EightSideOnion;
                    break;
                default:
                    if (FusedTypes.Count > 2)
                        transform.Find("mesh/RealMesh/SK_stg_Onyon.021").
                       GetComponent<SkinnedMeshRenderer>().sharedMesh = LethalMin.EightSideOnion;
                    LethalMin.Logger.LogWarning($"Unsupported number of fused onions: {FusedTypes.Count}");
                    break;
            }

            List<Color> colors = new List<Color>();

            colors.Add(FusedTypes[0].OnionColor);
            foreach (var item in FusedTypes)
            {
                //LethalMin.Logger.LogInfo("Adding color to gradient: " + item.OnionColor);
                colors.Add(item.OnionColor);
            }

            Texture2D gradient = GradientTextureGenerator.Generate90DegreeGradient(colors, 0.1f);

            if (LethalMin.DebugMode)
            {
                byte[] bytes = gradient.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(Application.persistentDataPath, $"graident.png"), bytes);
            }

            onionRenderer.material.color = Color.white;
            onionRenderer.material.SetTexture("_BaseColorMap", gradient);

            transform.Find("mesh/SK_stg_Onyon/root/S_j000/S_j001/S_j030/MapDot").GetComponent<Renderer>().material.color = Color.white;
            transform.Find("mesh/SK_stg_Onyon/root/S_j000/S_j001/S_j030/MapDot").GetComponent<Renderer>().material.SetTexture("_UnlitColorMap", gradient);
        }
        
        public override void LateUpdate()
        {
            base.LateUpdate();
            UpdateMeshVisibility();
            CheckForShipLeaving();
        }

        //

        private void UpdateMeshVisibility()
        {
            RealMesh.SetActive(!LethalMin.FunniOnion);
            FunniMesh.SetActive(LethalMin.FunniOnion);
        }

        private void CheckForShipLeaving()
        {
            if (StartOfRound.Instance.shipIsLeaving && !HasDecidedToLeave)
            {
                if (PikminTargetingThisOnion > 0) { return; }
                HasDecidedToLeave = true;
                onionAnimator.SetTrigger("Leave");
                transform.Find("BeamZone/Cone").gameObject.GetComponent<Animator>().Play("HideCone");
                StartCoroutine(HideBeam());
            }
        }
        int SpawnCount = 0;

        public void DEBUG_SPAWNSEED()
        {
            SpawnPikminSeed(LethalMin.GetPikminTypeById(0));
        }

        public override void SpawnPikminSeed(PikminType pikminType)
        {
            base.SpawnPikminSeed(pikminType);
            if (!IsServer)
            {
                return;
            }
            
            if (PikminManager.GetPikminEnemies().Count >= LethalMin.MaxMinValue)
            {
                pikminInOnion.Add(new OnionPikmin(0, pikminType.PikminTypeID));
                DoSpitClientRpc();
                LethalMin.Logger.LogWarning("Max Pikmin Reached");
                return;
            }

            // Define the circle parameters
            float radius = 8f;
            float angleStep = 30f;
            float startAngle = UnityEngine.Random.Range(0f, 360f);

            // Calculate the spawn position on the circle
            float angle = startAngle + (SpawnCount * angleStep) % 360f;
            float radian = angle * Mathf.Deg2Rad;
            float spawnX = Mathf.Sin(radian) * radius;
            float spawnZ = Mathf.Cos(radian) * radius;

            Vector3 airPosition = new Vector3(
                SpiPoint.position.x + spawnX,
                SpiPoint.position.y,
                SpiPoint.position.z + spawnZ
            );

            // Raycast to find the ground position
            RaycastHit hit;
            Vector3 groundPosition;
            if (Physics.Raycast(airPosition, Vector3.down, out hit, Mathf.Infinity, LethalMin.Instance.PikminColideable))
            {
                groundPosition = hit.point;
            }
            else
            {
                groundPosition = new Vector3(airPosition.x, 0, airPosition.z); // Fallback if raycast fails
            }

            // Spawn the animated sprout prefab
            GameObject animSproutInstance = Instantiate(LethalMin.AnimSproutPrefab, SpiPoint.position, Quaternion.identity);
            animSproutInstance.GetComponent<AnimatedSprout>().NetworkObject.Spawn();
            animSproutInstance.GetComponent<AnimatedSprout>().ColorAndSyncClientRpc(pikminType.PikminTypeID);

            // Start the animation coroutine
            DoSpitClientRpc();
            DoSproutAnimationClientRpc(animSproutInstance.GetComponent<AnimatedSprout>().NetworkObject, SpiPoint.position, groundPosition, pikminType.PikminTypeID);

            SpawnCount++; // Increment the spawn count
        }

        [ClientRpc]
        public void DoSproutAnimationClientRpc(NetworkObjectReference sproutRef, Vector3 start, Vector3 end, int pikminTypeID)
        {
            GameObject sprout = null!;

            sproutRef.TryGet(out NetworkObject sproutNetObj);

            sprout = sproutNetObj.gameObject;

            StartCoroutine(AnimateSproutScale(sprout));
            StartCoroutine(AnimateSprout(sprout, start, end, LethalMin.GetPikminTypeById(pikminTypeID)));
        }

        private IEnumerator AnimateSprout(GameObject sprout, Vector3 start, Vector3 end, PikminType pikminType)
        {
            float duration = 1f;
            float elapsedTime = 0f;
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 5f; // Mid-point with some height

            // Initial rotation (upside down)
            Quaternion startRotation = Quaternion.Euler(180f, 0f, 0f);
            Quaternion endRotation = Quaternion.Euler(0f, Random.Range(-360, 360), 0f);
            float spinAmount = Random.Range(-360, 360);


            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // Calculate position along the curved path
                Vector3 m1 = Vector3.Lerp(start, midPoint, t);
                Vector3 m2 = Vector3.Lerp(midPoint, end, t);
                sprout.transform.position = Vector3.Lerp(m1, m2, t);

                // Rotate the sprout
                sprout.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

                // Add spin around the y-axis
                //sprout.transform.Rotate(Vector3.up, spinAmount * Time.deltaTime);

                yield return null;
            }
            // Ensure final position, rotation, and scale
            sprout.transform.position = end;
            sprout.transform.rotation = endRotation;

            yield return new WaitForSeconds(0.5f);

            // Replace animated sprout with actual sprout prefab
            if (IsServer)
            {
                sprout.GetComponent<NetworkObject>().Despawn(true);
                GameObject actualSprout = Instantiate(LethalMin.sproutPrefab, sprout.transform.position, sprout.transform.rotation);
                Sprout sproutScript = actualSprout.GetComponent<Sprout>();

                sproutScript.IsSaved = true;
                sproutScript.NetworkObject.Spawn();
                sproutScript.InitalizeTypeClientRpc(pikminType.PikminTypeID);
            }
        }

        private IEnumerator AnimateSproutScale(GameObject sprout)
        {
            // Initial scale (start from 0)
            Vector3 startScale = Vector3.zero;
            float val = LethalMin.SproutScale;
            Vector3 endScale = new Vector3(val, val, val);

            float scaleDuration = 0.2f;
            float scaleElapsedTime = 0f;

            while (scaleElapsedTime < scaleDuration)
            {
                scaleElapsedTime += Time.deltaTime;
                float scaleT = scaleElapsedTime / scaleDuration;

                sprout.transform.localScale = Vector3.Lerp(startScale, endScale, scaleT);

                yield return null;
            }
            sprout.transform.localScale = endScale;
        }


        [ServerRpc(RequireOwnership = false)]
        public override void AddToTypesToSpawnServerRpc(int TypeID, int Times)
        {
            base.AddToTypesToSpawnServerRpc(TypeID, Times);

            DoSuctionClientRpc();
        }

        [ClientRpc]
        public void DoVacumeClientRpc()
        {
            Audio.PlayOneShot(LethalMin.OnionVac);
            onionAnimator.SetBool("Inhaleing", true);
            SpawnCount = 0;
        }


        [ClientRpc]
        public void DoSuctionClientRpc()
        {
            Audio.PlayOneShot(LethalMin.OnionSuc);
            onionAnimator.SetBool("Inhaleing", false);
            onionAnimator.SetTrigger("Inhaled");
        }

        [ClientRpc]
        public void DoSpitClientRpc()
        {
            Audio.PlayOneShot(LethalMin.OnionSpi);
            onionAnimator.SetTrigger("Exhaled");
        }

    }
}