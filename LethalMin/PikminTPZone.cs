using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class PikminTPZone : MonoBehaviour
    {
        public Transform Destination;

        public void OnTriggerEnter(Collider other)
        {
            if (!enabled) { return; }
            EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component3 != null && component3.mainScript.enemyType == LethalMin.pikminEnemyType && component3.mainScript.TryGetComponent(out PikminAI min)
            && min.currentBehaviourStateIndex == (int)PState.Working && min.targetItem != null && min.targetItem.PikminOnItemList[0] == min)
            {
                LethalMin.Logger.LogInfo($"{name} Warping Pikmin {min.name} to position: {Destination.position}");
                min.agent.Warp(Destination.position);
            }
        }
    }
}