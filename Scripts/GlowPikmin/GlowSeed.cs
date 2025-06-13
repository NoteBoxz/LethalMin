using UnityEngine;
using System.Collections;
using Unity.Netcode;
using LethalMin.Pikmin;
using GameNetcodeStuff;

namespace LethalMin
{
    public class GlowSeed : GrabbableObject
    {
        public PikminType GlowType = null!;
        public Renderer mainRenderer = null!;
        public Material OnMat = null!, OffMat = null!;
        public GameObject BaseMesh = null!;
        public Animator BurstAnimator = null!;
        public AudioClip BurstSFX = null!;
        public AnimationCurve grenadeFallCurve = null!;
        public AnimationCurve grenadeVerticalFallCurve = null!;
        public AnimationCurve grenadeVerticalFallCurveNoBounce = null!;

        public RaycastHit grenadeHit;
        public Ray grenadeThrowRay;

        private int stunGrenadeMask = 268437761;
        private PlayerControllerB playerThrownBy = null!;
        ScanNodeProperties sNode = null!;

        public void Awake()
        {
            sNode = GetComponentInChildren<ScanNodeProperties>();
        }

        public override void LateUpdate()
        {
            base.LateUpdate();

            sNode.gameObject.SetActive(LethalMin.MakeItemsScanable && !isHeld && !isHeldByEnemy);

            bool guardCondition = StartOfRound.Instance.shipIsLeaving
                               || StartOfRound.Instance.inShipPhase
                               || PikminManager.IsTooManyPikminOnMap
                               || (playerHeldBy != null && !playerHeldBy.isInsideFactory && !Lumiknull.TimeForGlowPikminToExist)
                               || (playerHeldBy == null && !Lumiknull.TimeForGlowPikminToExist);

            Material[] mats = mainRenderer.sharedMaterials;
            mats[0] = !guardCondition ? OnMat : OffMat;
            mainRenderer.sharedMaterials = mats;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            playerThrownBy = playerHeldBy;
            if (IsOwner)
            {
                playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination());
                GetComponentInChildren<AudioSource>().PlayOneShot(itemProperties.throwSFX);
            }
        }

        public override void FallWithCurve()
        {
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, grenadeFallCurve.Evaluate(fallTime));
            if (magnitude > 5f)
            {
                base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), grenadeVerticalFallCurveNoBounce.Evaluate(fallTime));
            }
            else
            {
                base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), grenadeVerticalFallCurve.Evaluate(fallTime));
            }
            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
        }

        public Vector3 GetGrenadeThrowDestination()
        {
            Vector3 position = base.transform.position;
            Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
            grenadeThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            position = ((!Physics.Raycast(grenadeThrowRay, out grenadeHit, 12f, stunGrenadeMask, QueryTriggerInteraction.Ignore)) ? grenadeThrowRay.GetPoint(10f) : grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f));
            Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
            grenadeThrowRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 30f, stunGrenadeMask, QueryTriggerInteraction.Ignore))
            {
                return grenadeHit.point + Vector3.up * 0.05f;
            }
            return grenadeThrowRay.GetPoint(30f);
        }



        public override void OnHitGround()
        {
            base.OnHitGround();
            if (IsOwner && playerThrownBy != null)
            {
                if (StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.inShipPhase)
                {
                    return;
                }
                if (PikminManager.IsTooManyPikminOnMap)
                {
                    LethalMin.Logger.LogWarning($"Glow pikmin can't spawn, max amount of pikmin reached {LethalMin.MaxPikmin.InternalValue}");
                    return;
                }
                if (!playerThrownBy.isInsideFactory && !Lumiknull.TimeForGlowPikminToExist)
                {
                    LethalMin.Logger.LogWarning($"Glow pikmin can't spawn before {LethalMin.LumiknullActivateTime} outside");
                    return;
                }
                SpawnGlowPikminServerRpc(OwnerClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnGlowPikminServerRpc(ulong ThrowerID)
        {
            SpawnGlowPikminClientRpc(ThrowerID);
        }

        [ClientRpc]
        public void SpawnGlowPikminClientRpc(ulong ThrowerID)
        {
            if (playerHeldBy != null)
            {
                playerHeldBy.DiscardHeldObject();
            }

            Leader? leader = LethalMin.GetLeaderViaID(ThrowerID);
            if (leader != null)
            {
                playerThrownBy = leader.Controller;
            }
            else
            {
                LethalMin.Logger.LogWarning($"Glow pikmin can't spawn, thrower not found {ThrowerID}");
                return;
            }

            grabbable = false;
            BaseMesh.SetActive(false);
            BurstAnimator.Play("GBBurstAnim");
            GetComponentInChildren<AudioSource>().PlayOneShot(BurstSFX);


            if (IsServer)
            {
                PikminSpawnProps props = new PikminSpawnProps();
                props.SpawnAnimation = "PluckedGseed";
                props.SpawnSound = "BornB";
                props.IsOutside = !playerThrownBy.isInsideFactory;
                props.PlayerID = playerThrownBy.OwnerClientId;

                PikminManager.instance.SpawnPikminOnServer(GlowType, transform.position, transform.rotation, props);
            }

            StartCoroutine(WaitForBurst());
        }

        public IEnumerator WaitForBurst()
        {
            yield return new WaitForSeconds(2f);
            IncrumentDestoryCountServerRpc();

        }
        public int DespawnCount = 0;

        [ServerRpc(RequireOwnership = false)]
        public void IncrumentDestoryCountServerRpc()
        {
            DespawnCount++;
            if (DespawnCount >= StartOfRound.Instance.connectedPlayersAmount + 1)
            {
                LethalMin.Logger.LogInfo($"All Clients marked to despawn {gameObject.name}");
                NetworkObject.Despawn(true);
            }
        }

        Vector3 sellPosition = Vector3.zero;
        public override void ReactToSellingItemOnCounter()
        {
            base.ReactToSellingItemOnCounter();

            GetComponentInChildren<AudioSource>().PlayOneShot(GlowType.SoundPack.HurtVoice[0]);
            sellPosition = transform.position;

            StartCoroutine(WaitToSpawnGhost());
        }

        IEnumerator WaitToSpawnGhost()
        {
            yield return new WaitForSeconds(1.5f);

            LethalMin.Logger.LogInfo($"Glowseed despawned due to selling on counter {gameObject.name}");
            PikminGhost ghost = GameObject.Instantiate(LethalMin.PikminGhostPrefab, sellPosition, transform.rotation).
            GetComponent<PikminGhost>();
            ghost.LostType = GlowType;
            ghost.InMemoryof = $"glowseed ({(int)NetworkObjectId})";
            ghost.ghostRandom = new System.Random(StartOfRound.Instance.randomMapSeed + (int)NetworkObjectId);
        }   
    }
}