using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class PikminTeleportTrigger : MonoBehaviour
    {
        public Transform Destination = null!;

        public void OnTriggerEnter(Collider other)
        {
            if (!enabled) { return; }
            PikminCollisionDetect pikminCollisionDetect = other.GetComponent<PikminCollisionDetect>();
            if (pikminCollisionDetect == null) { return; }
            PikminAI pikmin = pikminCollisionDetect.mainPikmin;
            if (pikmin == null) { return; }
            if (!pikmin.IsOwner) { return; }

            if (pikmin.currentBehaviourStateIndex == 2 || pikmin.ReturnToShipRoute != null)
            {
                pikmin.agent.Warp(Destination.position);
                pikmin.transform.position = Destination.position;
            }
        }
    }
}