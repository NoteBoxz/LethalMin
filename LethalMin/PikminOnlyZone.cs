using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class PikminOnlyZone : MonoBehaviour
    {
        public float warpDistance = 2f; // Distance to warp outside the bounds

        public void OnTriggerEnter(Collider other)
        {
            EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component3 != null && component3.mainScript.enemyType != LethalMin.pikminEnemyType && component3.mainScript.TryGetComponent(out NavMeshAgent agent))
            {
                //Warp the navmesh on to a point outside of the trigger's bounds
                Vector3 directionToAgent = (component3.mainScript.transform.position - transform.position).normalized;
                Vector3 warpPosition = component3.mainScript.transform.position + directionToAgent * warpDistance;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(warpPosition, out hit, warpDistance, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);

                    if (LethalMin.DebugMode)
                    {
                        LethalMin.Logger.LogInfo($"Warped enemy {component3.mainScript.name} to position: {hit.position}");
                    }
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to find a valid NavMesh position to warp enemy {component3.mainScript.name}");
                }
            }
        }
    }
}