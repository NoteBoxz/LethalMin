using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class PikminOnlyZone : MonoBehaviour
    {
        public float warpDistance = 2f; // Distance to warp outside the bounds

        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                if (other.transform.Find("PikminColision") == null && other.TryGetComponent(out NavMeshAgent agent))
                {
                    //Warp the navmesh on to a point outside of the trigger's bounds
                    Vector3 directionToAgent = (other.transform.position - transform.position).normalized;
                    Vector3 warpPosition = other.transform.position + directionToAgent * warpDistance;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(warpPosition, out hit, warpDistance, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        
                        if (LethalMin.DebugMode)
                        {
                            LethalMin.Logger.LogInfo($"Warped enemy {other.name} to position: {hit.position}");
                        }
                    }
                    else
                    {
                        LethalMin.Logger.LogWarning($"Failed to find a valid NavMesh position to warp enemy {other.name}");
                    }
                }
            }
        }
    }
}