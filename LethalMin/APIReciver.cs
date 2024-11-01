using UnityEngine;

namespace LethalMin
{
    public static class APIReciver
    {
        public static void KillPikmin(GameObject pikmin)
        {
            PikminAI pikminAI = null!;
            Transform parent = pikmin.transform.parent;
            if (parent != null)
            {
                pikminAI = parent.GetComponent<PikminAI>();
            }
            if (pikminAI == null)
            {
                pikminAI = pikmin.GetComponent<PikminAI>();
            }

            //pikminAI.KillEnemy();
        }
    }
}