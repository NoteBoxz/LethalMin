using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Utils;
using TMPro;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;

namespace LethalMin
{
    public class FreezeableWater : MonoBehaviour
    {
        public TMP_Text InWaterText = null!, NeededInText = null!, DeviderText = null!;
        public List<PikminAI> PikminInWater = new List<PikminAI>();
        public GameObject WaterCounter = null!;
        public GameObject FrozenWater = null!;
        public QuicksandTrigger waterTrigger = null!;
        public NavMeshSurface FrozenWaterSurface = null!;
        public Collider FrozenWaterColider = null!;
        public PikminEffectTrigger WaterEffectTrigger = null!;
        public AudioSource audioSource = null!;
        public int FreezeStrengthRequired = 5;
        public int CurrentFreezeStrength => PikminInWater.Count > 0 ? PikminInWater.Select(pikmin => pikmin.CurrentCarryStrength).Sum() : 0;
        public bool IsFrozen = false;
        public bool IsServer = false;
        public float FrozenTimer = 0;
        public FloodWeather? flood = null;
        public Vector3 FrozePos = Vector3.zero;


        void Start()
        {
            SpawnWaterCounter();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // Logarithmic rolloff for realistic sound distance attenuation
            audioSource.minDistance = 5f; // Minimum distance for sound to be heard
            audioSource.maxDistance = 15f; // Maximum distance for sound to be heard
            flood = GetComponentInParent<FloodWeather>();
            if (flood != null)
            {
                FreezeStrengthRequired = 100;
                LethalMin.Logger.LogInfo($"{gameObject.name} is under flood weather");
            }
            else
            {
                //Calculate the freeze strength required based on the water's scale
                Vector3 Scale = transform.localScale;
                float magnitude = Mathf.Max(Scale.x, Scale.y, Scale.z); // Get the largest scale component
                float devision = magnitude / 2.5f;
                int RoundedDevision = Mathf.CeilToInt(devision); // Round up to the nearest whole number
                FreezeStrengthRequired = Mathf.Clamp(RoundedDevision, 1, 100); // Ensure it's between 1 and 100
            }
            waterTrigger = GetComponent<QuicksandTrigger>();
            WaterEffectTrigger = waterTrigger.GetComponent<PikminEffectTrigger>();
            FrozenWaterColider = waterTrigger.GetComponent<Collider>();
            FrozenWater = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Types/Ice Pikmin/FrozenWater.prefab"), transform);
            FrozenWaterSurface = FrozenWater.GetComponentInChildren<NavMeshSurface>();
            FrozenWaterSurface.BuildNavMesh();
            FrozenWater.SetActive(false);
            IsServer = PikminManager.instance.IsServer;
        }

        void Update()
        {
            if (IsServer && !IsFrozen && CurrentFreezeStrength >= FreezeStrengthRequired)
            {
                PikminManager.instance.FreezeWaterAtIndexServerRpc(PikminManager.instance.FreezeableWaters.IndexOf(this));
            }
            if (IsServer && IsFrozen && CurrentFreezeStrength < FreezeStrengthRequired)
            {
                // Reset the frozen state if we drop below the required strength
                if (FrozenTimer > 0)
                {
                    FrozenTimer -= Time.deltaTime;
                }
                else
                {
                    PikminManager.instance.UnfreezeWaterAtIndexServerRpc(PikminManager.instance.FreezeableWaters.IndexOf(this));
                }
            }
            if (IsFrozen && flood != null)
            {
                flood.transform.position = FrozePos;
            }

            WaterCounter.SetActive(PikminInWater.Count > 0);
            if (WaterCounter.activeSelf)
            {
                // Calculate average position of all Pikmin in water
                Vector3 averagePosition = Vector3.zero;

                // Remove any null entries that might have been left in the list
                PikminInWater.RemoveAll(pikmin => pikmin == null);

                // Sum up all positions
                foreach (PikminAI pikmin in PikminInWater)
                {
                    averagePosition += pikmin.transform.position;
                }

                // Divide by count to get average (center) position
                averagePosition /= PikminInWater.Count;

                // Position the counter at the average position, slightly above to be visible
                WaterCounter.transform.position = averagePosition + Vector3.up * 3f;

                // Update the counter text
                InWaterText.text = CurrentFreezeStrength.ToString();
                NeededInText.text = FreezeStrengthRequired.ToString();
            }
        }

        public void FreezeWater()
        {
            if (IsFrozen)
            {
                return;
            }
            LethalMin.Logger.LogInfo($"Freezing water at {gameObject.name} with {CurrentFreezeStrength} strength (required: {FreezeStrengthRequired})");
            if (flood == null)
            {
                FrozePos = transform.position;
            }
            else
            {
                FrozePos = flood.transform.position;
            }
            FrozenTimer = 5;
            IsFrozen = true;
            FrozenWater.SetActive(true);

            Vector3 IceTop = FrozenWaterColider.bounds.max;
            foreach (NavMeshAgent agent in FindObjectsOfType<NavMeshAgent>())
            {
                if (FrozenWaterColider.bounds.Contains(agent.transform.position))
                {
                    // Move the agent to the top of the ice, so they are not inside it
                    Vector3 newPos = new Vector3(agent.transform.position.x, IceTop.y + agent.height, agent.transform.position.z);
                    agent.Warp(newPos); // Warp to avoid NavMesh issues
                }
            }

            foreach (PikminAI pikmin in PikminInWater)
            {
                pikmin.SetToIdle();
                GameObject SnapPos = new GameObject($"({pikmin.gameObject.name})SnapPos");
                SnapPos.transform.position = pikmin.transform.position;
                if (NavMesh.SamplePosition(SnapPos.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    SnapPos.transform.position = hit.position;
                }
                if (pikmin is IcePikminAI icePikmin)
                {
                    icePikmin.IceSnapPos = SnapPos.transform;
                }
                pikmin.agent.enabled = false;
                pikmin.Invincible = true;
                pikmin.ChangeIntent(Pintent.MoveableStuck);
            }

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (FrozenWaterColider.bounds.Contains(player.transform.position))
                {
                    Vector3 newPos = new Vector3(player.transform.position.x, IceTop.y + player.thisController.height, player.transform.position.z);
                    player.transform.position = newPos; // Move the player to the top of the ice, so they are not inside it
                }
            }

            foreach (PikminCollisionDetect pikminDetect in FindObjectsOfType<PikminCollisionDetect>())
            {
                if (pikminDetect.CurFXtriggers.Contains(WaterEffectTrigger))
                {
                    pikminDetect.CurFXtriggers.Remove(WaterEffectTrigger);
                    pikminDetect.mainPikmin.StopPanicingOnLocalClient();
                }
            }

            WaterEffectTrigger.enabled = false;
            waterTrigger.enabled = false;

            audioSource.PlayOneShot(LethalMin.assetBundle.LoadAsset<AudioClip>("Assets/LethalMin/wav_bnk_AkB_Ambience_CaveAquarium/Amb_CaveAquariumSwamp_WaterBox(14).wav"));
        }

        public void UnfreezeWater()
        {
            if (!IsFrozen)
            {
                return;
            }
            LethalMin.Logger.LogInfo($"Unthawing water at {gameObject.name} with {CurrentFreezeStrength} strength (required: {FreezeStrengthRequired})");
            FrozenTimer = 5;
            IsFrozen = false;
            FrozenWater.SetActive(false);

            Bounds OffsetBounds = FrozenWaterColider.bounds;
            OffsetBounds.SetMinMax(OffsetBounds.min, new Vector3(OffsetBounds.max.x, OffsetBounds.max.y + 1f, OffsetBounds.max.z));
            Vector3 IceBottom = OffsetBounds.min;

            foreach (PikminAI pikmin in new List<PikminAI>(PikminInWater))
            {
                if (pikmin is IcePikminAI icePikmin)
                {
                    icePikmin.RemoveFromCurWater();
                }
                pikmin.SetToIdle();
            }

            foreach (NavMeshAgent agent in FindObjectsOfType<NavMeshAgent>())
            {
                if (OffsetBounds.Contains(agent.transform.position))
                {
                    // Move the agent to the top of the ice, so they are not inside it
                    Vector3 newPos = RoundManager.Instance.GetNavMeshPosition(agent.transform.position, default, 25);
                    agent.Warp(newPos); // Warp to avoid NavMesh issues
                }
            }

            WaterEffectTrigger.enabled = true;
            waterTrigger.enabled = true;

            audioSource.PlayOneShot(LethalMin.assetBundle.LoadAsset<AudioClip>("Assets/LethalMin/wav_bnk_AkB_Ambience_CaveAquarium/Amb_CaveAquarium_WaterBox_UnFreeze.wav"));
        }

        public void SpawnWaterCounter()
        {
            WaterCounter = Instantiate(LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/Types/Ice Pikmin/IceWaterCounter.prefab"));

            foreach (TMP_Text text in WaterCounter.GetComponentsInChildren<TMP_Text>())
            {
                if (text.name == "InWaterText")
                {
                    InWaterText = text;
                }
                else if (text.name == "NeededInText")
                {
                    NeededInText = text;
                }
                else if (text.name == "DeviderText")
                {
                    DeviderText = text;
                }
            }
        }
    }
}
