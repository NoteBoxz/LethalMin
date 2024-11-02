using UnityEngine;

namespace LethalMin
{
    public static class APIReciver
    {
        public static void KillPikmin(GameObject pikmin, Transform SnapToTarget, float KillTimer)
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
            if (pikminAI == null)
            {
                Debug.LogError("(LETHALMIN_API_RECIVER): PikminAI not found on Pikmin when killing via snap to position");
                return;
            }

            pikminAI.SnapPikminToPosition(SnapToTarget, false, true, KillTimer);
        }

        public static void KillPikmin(GameObject pikmin, Vector3 KnockBackForce, bool KillOnLand, float KillTimer)
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
            if (pikminAI == null)
            {
                Debug.LogError("(LETHALMIN_API_RECIVER): PikminAI not found on Pikmin when killing via knockback");
                return;
            }

            pikminAI.ApplyKnockbackServerRpc(KnockBackForce, true, KillOnLand, KillTimer);
        }
    }
}