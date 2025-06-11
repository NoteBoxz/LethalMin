using LethalMin;
using LethalMin.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class NoticeZoneOnlyDetect : MonoBehaviour
    {
        // only used for notice zone, won't be used for collision detection
        public PikminAI mainPikmin = null!;
        void Start()
        {
            if (mainPikmin == null)
                mainPikmin = GetComponentInParent<PikminAI>();
        }
        private void OnTriggerEnter(Collider other)
        {

        }
        private void OnTriggerStay(Collider other)
        {
        }
        private void OnTriggerExit(Collider other)
        {

        }
    }
}