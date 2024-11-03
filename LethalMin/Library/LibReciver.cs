using LethalMinLibrary;
using UnityEngine;

namespace LethalMin.Library
{
    public static class LibReciver
    {
        public static bool IsPikminResistantToHazard(PikminType type, HazardType hazard)
        {
            switch (hazard)
            {
                case HazardType.Poision:
                    if (type.IsResistantToPoision)
                    {
                        return true;
                    }
                    break;
                case HazardType.Fire:
                    if (type.IsResistantToFire)
                    {
                        return true;
                    }
                    break;
                case HazardType.Electricity:
                    if (type.IsResistantToElectricity)
                    {
                        return true;
                    }
                    break;
                case HazardType.Exsplosion:
                    if (type.IsresistantToExplosions)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }
        public static void KillPikmin(GameObject pikmin, Transform SnapToTarget, float KillTimer, HazardType[] type)
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
                Debug.LogError("(LETHALMIN_LIB_RECIVER): PikminAI not found on Pikmin when killing via snap to position");
                return;
            }
            foreach (var hazard in type)
            {
                if (IsPikminResistantToHazard(pikminAI.PminType, hazard))
                {
                    return;
                }
            }

            pikminAI.SnapPikminToPosition(SnapToTarget, false, true, KillTimer);
        }

        public static void KillPikmin(GameObject pikmin, Vector3 KnockBackForce, bool KillOnLand, float KillTimer, HazardType[] type)
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
                Debug.LogError("(LETHALMIN_LIB_RECIVER): PikminAI not found on Pikmin when killing via knockback");
                return;
            }
            foreach (var hazard in type)
            {
                if (IsPikminResistantToHazard(pikminAI.PminType, hazard))
                {
                    return;
                }
            }

            pikminAI.ApplyKnockbackServerRpc(KnockBackForce, true, KillOnLand, KillTimer);
        }
        public static void KillPikmin(GameObject pikmin, float KillTimer, HazardType[] type)
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
                Debug.LogError("(LETHALMIN_LIB_RECIVER): PikminAI not found on Pikmin when killing");
                return;
            }
            foreach (var hazard in type)
            {
                if (IsPikminResistantToHazard(pikminAI.PminType, hazard))
                {
                    return;
                }
            }

            pikminAI.SnapPikminToPosition(null!, false, true, KillTimer);
        }
    }
}