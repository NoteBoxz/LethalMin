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
        bool IsDoingSpawning = false;
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
                if (PikminInField.Values.Count > 0) { return; }
                HasDecidedToLeave = true;
                onionAnimator.SetTrigger("Leave");
                transform.Find("BeamZone/Cone").gameObject.GetComponent<Animator>().Play("HideCone");
                StartCoroutine(HideBeam());
            }
        }

        public override void SpawnPikmin(PikminType pikminType)
        {
            base.SpawnPikmin(pikminType);

            Vector3 SpawnPos = Vector3.zero;

            float spawnX = Random.Range(-8f, 8f);
            float spawnZ = Random.Range(-8f, 8f);

            if(spawnX <= 0)
            {
                Mathf.Clamp(spawnX, -2, -8);
            }
            else
            {
                Mathf.Clamp(spawnX, 2, 8);
            }
            if (spawnZ <= 0)
            {
                Mathf.Clamp(spawnZ, -2, -8);
            }
            else
            {
                Mathf.Clamp(spawnZ, 2, 8);
            }

            SpawnPos = new Vector3(spawnX, SpiPoint.position.y, spawnZ);

            GameObject SproutInstance2 = Instantiate(LethalMin.sproutPrefab, SpawnPos, Quaternion.identity);
            Sprout SproteScript2 = SproutInstance2.GetComponent<Sprout>();

            SproteScript2.NetworkObject.Spawn();
            SproteScript2.InitalizeTypeClientRpc(pikminType.PikminTypeID);
            DoSpitClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public override void AddToTypesToSpawnServerRpc(int TypeID)
        {
            base.AddToTypesToSpawnServerRpc(TypeID);

            DoSuctionClientRpc();
        }

        [ClientRpc]
        public void DoVacumeClientRpc()
        {
            Audio.PlayOneShot(LethalMin.OnionVac);
            onionAnimator.SetBool("Inhaleing", true);
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