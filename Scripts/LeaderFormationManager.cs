using System.Collections;
using System.Collections.Generic;
using LethalMin;
using LethalMin.HUD;
using LethalMin.Pikmin;
using UnityEngine;

namespace LethalMin
{
    public class LeaderFormationManager : MonoBehaviour
    {
        [Header("Formation Target")]
        public Transform targetTransform = null!;

        [Header("Formation Settings")]
        public float boxWidth = 10f;
        public float boxDepth = 10f;
        public float spacing = 1f;
        public float noiseFactor = 0.3f;

        public float UpdateInterval = 0.5f;
        public Leader leader = null!;
        private Dictionary<PikminAI, Vector3> formationPositions = new Dictionary<PikminAI, Vector3>();

        float recalculateTimer = 0;

        void OnEnable()
        {
            if (targetTransform == null)
            {
                GameObject Prefab = LethalMin.assetBundle.LoadAsset<GameObject>("Assets/LethalMin/PikminFormationTarget.prefab");
                targetTransform = Instantiate(Prefab, null).transform;
                targetTransform.gameObject.GetComponent<LeaderFormationTarget>().formationManager = this;
                leader = GetComponent<Leader>();
                targetTransform.gameObject.name = $"{leader.Controller.playerUsername}'s Formation Target";
            }

            targetTransform.gameObject.GetComponent<LeaderFormationTarget>().enabled = true;
        }

        void OnDisable()
        {
            targetTransform.gameObject.GetComponent<LeaderFormationTarget>().enabled = false;
        }

        public Vector3 GetFormationPosition(PikminAI ai)
        {
            if (formationPositions.ContainsKey(ai))
            {
                return formationPositions[ai];
            }
            return targetTransform ? targetTransform.position : transform.position;
        }
        public void RecalculateFormation()
        {
            if (!enabled || !leader.IsOwner || targetTransform == null || leader.PikminInSquad.Count == 0)
                return;


            formationPositions.Clear();

            // Calculate an appropriate grid size based on entity count
            int entitiesPerRow = Mathf.CeilToInt(Mathf.Sqrt(leader.PikminInSquad.Count));

            for (int i = 0; i < leader.PikminInSquad.Count; i++)
            {
                // Calculate grid position
                int row = i / entitiesPerRow;
                int col = i % entitiesPerRow;

                // Position relative to center of grid
                float xOffset = (col - (entitiesPerRow - 1) / 2f) * spacing;
                float zOffset = (row - (Mathf.CeilToInt((float)leader.PikminInSquad.Count / entitiesPerRow) - 1) / 2f) * spacing;

                // Constrain to box dimensions
                xOffset = Mathf.Clamp(xOffset, -boxWidth / 2, boxWidth / 2);
                zOffset = Mathf.Clamp(zOffset, -boxDepth / 2, boxDepth / 2);

                // Add randomness
                float noiseX = Random.Range(-noiseFactor, noiseFactor);
                float noiseZ = Random.Range(-noiseFactor, noiseFactor);

                // Calculate world position relative to target
                Vector3 formationPos = targetTransform.position +
                                       targetTransform.right * (xOffset + noiseX) +
                                       targetTransform.forward * (zOffset + noiseZ);

                // Store the position
                formationPositions[leader.PikminInSquad[i]] = formationPos;
            }
        }

        void Update()
        {
            if (recalculateTimer > 0)
            {
                recalculateTimer -= Time.deltaTime;
                return;
            }
            else
            {
                recalculateTimer = UpdateInterval;
            }

            // If target moves, recalculate formation
            if (leader.PikminInSquad.Count > 0 && targetTransform != null && targetTransform.hasChanged)
            {
                RecalculateFormation();
                targetTransform.hasChanged = false;
            }
        }
    }
}