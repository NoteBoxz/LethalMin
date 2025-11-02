using System.Collections.Generic;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine;
namespace LethalMin
{
    public class ShipPhaseOnionContainer : MonoBehaviour, IGenerationSwitchable
    {
        public class ShipPhaseOnion
        {
            public ShipPhaseOnion(OnionType type, GameObject instance)
            {
                onionType = type;
                Instance = instance;
            }
            public ShipPhaseOnion(List<OnionType> types, GameObject instance)
            {
                onionType = null!;
                fusedTypes = types;
                Instance = instance;
            }
            public GameObject Instance = null!;
            public OnionType onionType = null!;
            public List<OnionType> fusedTypes = new List<OnionType>();
            public Animator anim = null!;
            public BaseOnionFusionProperties fusionProperties = null!; // Custom fusion properties scriptable object, if any
        }
        public List<ShipPhaseOnion> shipPhaseOnions = new List<ShipPhaseOnion>();

        public void OnEnable()
        {
            // Initialize the container
            PikminManager.instance.shipPhaseOnionContainer = this;
            RefreshOnions();
            GenerationManager.Instance.Register(this);
        }


        public void RefreshOnions()
        {
            if (!PikminManager.instance.IsServer)
            {
                return;
            }
            LethalMin.Logger.LogInfo($"Refreshing onions for {gameObject.name}");

            PikminManager.instance.RemoveShipPhaseOnionsServerRpc();

            int number = 0;
            List<int> IdsToSkip = new List<int>();

            if (SaveManager.KeyExists("onionFusion"))
            {
                Dictionary<string, List<int>> onionFusionData = SaveManager.Load<Dictionary<string, List<int>>>("onionFusion");

                foreach (string key in onionFusionData.Keys)
                {
                    IdsToSkip.AddRange(onionFusionData[key]);
                    PikminManager.instance.SpawnShipPhaseOnionServerRpc(onionFusionData[key].ToArray());
                    number++;
                }
            }

            if (SaveManager.KeyExists("collectedOnions"))
            {
                List<int> OnionsCollected = SaveManager.Load<List<int>>("collectedOnions");
                foreach (int ID in OnionsCollected)
                {
                    if (IdsToSkip.Contains(ID))
                    {
                        continue;
                    }

                    OnionType type = LethalMin.GetOnionTypeByID(ID);
                    if (type == null)
                    {
                        LethalMin.Logger.LogError($"Null ID: {ID}");
                        continue;
                    }

                    PikminManager.instance.SpawnShipPhaseOnionServerRpc(ID);
                    number++;
                }
            }
        }
        public void LateUpdate()
        {
            if (!LethalMin.DontUpdateSpaceOnionPosition.InternalValue)
                transform.position = LethalMin.SpaceOnionPosition.InternalValue;
            if (transform.childCount > 0)
            {
                // Tween the children to move in a circle around the parent
                float radius = 10f;
                float angle = Time.time * 2f;
                foreach (Transform child in transform)
                {
                    child.position = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    angle += 2f * Mathf.PI / transform.childCount;
                }
            }
        }
        public void SwitchGeneration(PikminGeneration generation)
        {
            foreach (ShipPhaseOnion onion in shipPhaseOnions)
            {
                SwitchGenerationB(generation, onion);
            }
        }
        public void SwitchGenerationB(PikminGeneration generation, ShipPhaseOnion onion)
        {
            if (LethalMin.RandomizeGenerationModels.InternalValue)
                generation = (PikminGeneration)Random.Range(0, System.Enum.GetValues(typeof(PikminGeneration)).Length);

            if (onion.Instance == null)
            {
                LethalMin.Logger.LogError("SwitchGenerationB: onion.Instance is null");
                return;
            }

            OnionModelRefernces modelRefernces = onion.Instance.GetComponentInChildren<OnionModelRefernces>();
            bool hasFound = false;

            void applyGeneration(OnionModelGeneration gen)
            {
                onion.anim = gen.Animator;
                onion.fusionProperties = gen.FusionProperties;
                onion.fusionProperties.MainOnionRenderer = gen.MainOnionRenderer;
                onion.fusionProperties.ApplyFusionProperties(onion.onionType, onion.fusedTypes);
                onion.anim.SetTrigger("DoFloat");
            }

            void useDefultGeneration()
            {
                modelRefernces.Model.SetActive(true);
                onion.anim = modelRefernces.Animator;
                onion.fusionProperties = modelRefernces.FusionProperties;
                onion.fusionProperties.MainOnionRenderer = modelRefernces.MainOnionRenderer;
                onion.fusionProperties.ApplyFusionProperties(onion.onionType, onion.fusedTypes);
                onion.anim.SetTrigger("DoFloat");
            }

            foreach (OnionModelGeneration gen in modelRefernces.Generations)
            {
                gen.Model.SetActive(gen.Generation == generation);

                if (gen.Generation == generation)
                {
                    applyGeneration(gen);
                    hasFound = true;
                }
            }

            if (!hasFound)
            {
                LethalMin.Logger.LogWarning($"Failed to find a valid generation for Onion! Generation: {generation}");
                useDefultGeneration();
            }
        }
    }
}