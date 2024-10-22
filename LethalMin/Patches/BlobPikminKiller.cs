using UnityEngine;

namespace LethalMin.Patches
{
    public class BlobPikminKiller : MonoBehaviour
    {
        private void OnTriggerStay(Collider other)
        {
            if (other.name == "PikminColision")
            {
                PikminAI pikmin = other.GetComponentInParent<PikminAI>();
                if (pikmin != null && !pikmin.IsDying && LethalMin.LethalHydroValue)
                {
                    // Instantly kill the Pikmin
                    pikmin.SnapPikminToPosition(transform, false, true, 1f);
                }
            }
        }
    }
}