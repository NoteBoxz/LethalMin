using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Pikmin
{
    public class PikminOnlyZone : MonoBehaviour
    {
        public float warpDistance = 2f; // Distance to warp outside the bounds

        public void OnTriggerEnter(Collider other)
        {
            if (!enabled) { return; }
            if (!other.CompareTag("Enemy")) { return; }
            EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component3 != null && component3.mainScript.enemyType != LethalMin.PikminEnemyType && component3.mainScript.IsOwner)
            {
                //Warp the navmesh on to a point outside of the trigger's bounds
                Vector3 directionToAgent = (component3.mainScript.transform.position - transform.position).normalized;
                Vector3 warpPosition = component3.mainScript.transform.position + directionToAgent * warpDistance;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(warpPosition, out hit, warpDistance, NavMesh.AllAreas))
                {
                    component3.mainScript.agent.Warp(hit.position);

                    LethalMin.Logger.LogMessage($"{name} Warped enemy {component3.mainScript.name} to position: {hit.position}");
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to find a valid NavMesh position to warp enemy {component3.mainScript.name}");
                }
            }
        }
    }
}