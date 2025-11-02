using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using LethalMin.Pikmin;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class Lumiknull : NetworkBehaviour
    {
        public PikminType GlowPikminType = null!;
        public Transform GlowSpawnPosition = null!;
        public Animator anim = null!;
        public InteractTrigger[] triggers = new InteractTrigger[5];
        public Renderer RendToGlow = null!;
        public AudioClip OnSound = null!;
        public AudioClip SpidSound = null!;
        public AudioSource Source = null!;
        public SpriteRenderer RadarSprite = null!;
        public Sprite ActiveSprite = null!;
        public Sprite InactiveSprite = null!;
        public bool ShouldActivate = true;
        public bool IsActive = false;
        public static bool TimeForGlowPikminToExist => TimeOfDay.Instance.globalTime >= LethalMin.LumiknullActivateTime.InternalValue ||
        StartOfRound.Instance.currentLevel.currentWeather == LevelWeatherType.Eclipsed || LethalMin.OnCompany && LethalMin.GlowsUseableAtCompany;

        void LateUpdate()
        {
            if (IsServer && TimeForGlowPikminToExist && ShouldActivate && !IsActive)
            {
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (Vector3.Distance(transform.position, player.transform.position) < LethalMin.LumiknullActivateDistance.InternalValue)
                    {
                        ActivateLumiknullClientRpc();
                        break;
                    }
                }
            }
            if (IsServer && !TimeForGlowPikminToExist && IsActive)
            {
                DeactivateLumiknullClientRpc();
            }

            RadarSprite.sprite = IsActive ? ActiveSprite : InactiveSprite;

            if (Vector3.Distance(transform.position, StartOfRound.Instance.localPlayerController.transform.position) > 50)
            {
                return;
            }

            for (int i = 0; i < triggers.Length; i++)
            {
                InteractTrigger trigger = triggers[i];
                if (!IsActive)
                {
                    trigger.hoverTip = "Inactive";
                    continue;
                }
                if (StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer == null)
                {
                    trigger.hoverTip = "No Item to deposit";
                }
                else
                {
                    trigger.hoverTip = "Deposit Item";
                }
            }
        }

        [ClientRpc]
        public void ActivateLumiknullClientRpc()
        {
            ActivateLumiknull();
        }

        public void ActivateLumiknull()
        {
            StartCoroutine(TweenMaterial());
            Source.PlayOneShot(OnSound);
            IsActive = true;
        }

        IEnumerator TweenMaterial()
        {
            Color startColor = RendToGlow.materials[0].GetColor("_EmissiveColor");
            Color endColor = new Color(0.25f, 1, 0.25f, 1f) * 2f;
            float duration = 1f;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                RendToGlow.materials[0].SetColor("_EmissiveColor", Color.Lerp(startColor, endColor, t));
                RendToGlow.materials[0].SetFloat("_EmissiveIntensity", Mathf.Lerp(0, 2, t));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        [ClientRpc]
        public void DeactivateLumiknullClientRpc()
        {
            DeactivateLumiknull();
        }
        public void DeactivateLumiknull()
        {
            StartCoroutine(TweenMaterialDeactive());
            IsActive = false;
        }
        IEnumerator TweenMaterialDeactive()
        {
            Color startColor = RendToGlow.materials[0].GetColor("_EmissiveColor");
            Color endColor = new Color(0f, 0f, 0f, 1f);
            float duration = 1f;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                RendToGlow.materials[0].SetColor("_EmissiveColor", Color.Lerp(startColor, endColor, t));
                RendToGlow.materials[0].SetFloat("_EmissiveIntensity", Mathf.Lerp(2, 0, t));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        public void AttemptDepsoitItem(PlayerControllerB playerDepositing)
        {
            if (!IsActive) return;

            GrabbableObject grab = playerDepositing.currentlyHeldObjectServer;
            if (grab == null)
            {
                LethalMin.Logger.LogWarning($"Lumiknull: {playerDepositing.OwnerClientId} tried to deposit an item that is not held!");
                return;
            }
            PikminItem item = grab.GetComponentInChildren<PikminItem>();
            if (item == null)
            {
                LethalMin.Logger.LogWarning($"Lumiknull: {playerDepositing.OwnerClientId} tried to deposit an item that is not a PikminItem!");
                return;
            }

            bool check = StartOfRound.Instance.currentLevel.currentWeather == LevelWeatherType.Eclipsed &&
             TimeOfDay.Instance.globalTime < LethalMin.LumiknullActivateTime.InternalValue;

            float ammount = !check ? item.CarryStrengthNeeded : Mathf.Max(1, item.CarryStrengthNeeded / 2f);
            
            grab.grabbable = false;

            playerDepositing.DespawnHeldObject();
            AttemptDepositItemServerRpc(playerDepositing.OwnerClientId, ammount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AttemptDepositItemServerRpc(ulong playerDepositing, float CarryWeight)
        {
            AttemptDepositItemClientRpc(playerDepositing, CarryWeight);
        }

        [ClientRpc]
        public void AttemptDepositItemClientRpc(ulong playerDepositing, float CarryWeight)
        {
            if (LethalMin.GetLeaderViaID(playerDepositing, out Leader lead))
            {
                DepositeItem(CarryWeight, lead);
            }
        }

        public void DepositeItem(float CarryWeight, Leader LeaderDepoit)
        {
            PikminSpawnProps props = new PikminSpawnProps();
            props.PlayerID = LeaderDepoit.OwnerClientId;

            StartCoroutine(DoGlowSpawn(Mathf.RoundToInt(CarryWeight), LeaderDepoit.OwnerClientId));
        }

        IEnumerator DoGlowSpawn(int AmmountToSpawn, ulong LeaderID)
        {
            PikminSpawnProps props = new PikminSpawnProps();
            props.SpawnAnimation = "LumikSpawn";
            props.SpawnSound = "Born";
            props.PlayerID = LeaderID;
            props.MovementBuffer = 0.5f;

            int l = 0;
            for (int i = 0; i < AmmountToSpawn; i++)
            {
                l++;
                anim.SetTrigger("Exhale");
                Source.PlayOneShot(SpidSound);
                if (IsServer)
                {
                    Vector3 spawnPos = GetPositionToSpawnSprout();
                    // Calculate direction from Lumiknull to spawn position
                    Vector3 directionFromLumiknull = (spawnPos - transform.position).normalized;
                    // Create rotation that faces that direction
                    Quaternion spawnRotation = Quaternion.LookRotation(directionFromLumiknull);
                    PikminManager.instance.SpawnPikminOnServer(GlowPikminType, spawnPos, spawnRotation, props);
                    yield return new WaitForSeconds(0.1f);
                }
                if (l >= 2)
                {
                    l = 0;
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        [ContextMenu("TestSpawn")]
        public void TestSpawn()
        {
            Vector3 spawnPos = GetPositionToSpawnSprout(8, 100);
            // Calculate direction from Lumiknull to spawn position
            Vector3 directionFromLumiknull = (spawnPos - transform.position).normalized;
            // Create rotation that faces that direction
            Quaternion spawnRotation = Quaternion.LookRotation(directionFromLumiknull);
            Debug.Log($"Vector3{spawnPos}, Direction: {directionFromLumiknull}, Quaternion{spawnRotation}");
        }

        public virtual Vector3 GetPositionToSpawnSprout(float radius = 8, int overrideMax = -1)
        {
            float angleStep = 30f;
            float startAngle = Random.Range(0f, 360f);

            // Calculate the spawn position on the circle
            float angle = startAngle + (overrideMax == -1 ? LethalMin.MaxPikmin.InternalValue : overrideMax) * angleStep % 360f;
            float radian = angle * Mathf.Deg2Rad;
            float spawnX = Mathf.Sin(radian) * radius;
            float spawnZ = Mathf.Cos(radian) * radius;

            Vector3 airPosition = new Vector3(
                GlowSpawnPosition.position.x + spawnX,
                GlowSpawnPosition.position.y,
                GlowSpawnPosition.position.z + spawnZ
            );

            // Raycast to find the ground position
            RaycastHit hit;
            Vector3 groundPosition;
            if (Physics.Raycast(airPosition, Vector3.down, out hit, Mathf.Infinity, LethalMin.PikminColideable))
            {
                groundPosition = hit.point;
            }
            else
            {
                groundPosition = new Vector3(airPosition.x, 0, airPosition.z); // Fallback if raycast fails
            }
            return groundPosition;
        }
    }
}
