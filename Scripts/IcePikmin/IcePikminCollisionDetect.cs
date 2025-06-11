using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public class IcePikminCollisionDetect : MonoBehaviour
    {
        public IcePikminAI mainPikmin = null!;

        void Start()
        {
            if (mainPikmin == null)
                mainPikmin = GetComponentInParent<IcePikminAI>();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out FreezeableWater water))
            {
                mainPikmin.EnterWater(water);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out FreezeableWater water))
            {
                mainPikmin.ExitWater();
            }
        }
    }
}
