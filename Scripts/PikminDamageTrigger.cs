using System.Collections;
using System.Collections.Generic;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin
{
    public enum PikminHarmTriggerDeathType
    {
        Default,
        Zap,
        Squish
    }
    public class PikminDamageTrigger : MonoBehaviour
    {
        public PikminHarmTriggerDeathType deathType = PikminHarmTriggerDeathType.Default;
        [Tooltip("If true, the pikmin will die instantly on hit.")]
        public bool InstaDeath = false;
        [Tooltip("If true, the pikmin will be destroyed on death. Causing the ghost to spawn instantly.")]
        public bool DestoryOnDeath = true;
        [Tooltip("The ammount of damage to deal to the pikmin on hit. (1 is basicly an insta-kill)")]
        public int DamageDelt = 1;
        [Tooltip("Leave empty to have all types be affected.")]
        public List<PikminHazard> HazardTypes = new List<PikminHazard>();
        public bool CallOnHazardResist = true;
        public bool DontKillInShip = false;
        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy") && other.TryGetComponent(out PikminCollisionDetect detect) && detect.mainPikmin.IsOwner)
            {
                foreach (PikminHazard hazard in HazardTypes)
                {
                    if (PikChecks.IsPikminResistantToHazard(detect.mainPikmin, hazard, CallOnHazardResist))
                    {
                        return; // Pikmin is resistant to this hazard, do nothing.
                    }
                }
                if(DontKillInShip && detect.mainPikmin.IsInShip)
                {
                    return; // Don't kill pikmin in ship.
                }
                switch (deathType)
                {
                    case PikminHarmTriggerDeathType.Default:
                        if (InstaDeath)
                        {
                            detect.mainPikmin.KillEnemyOnOwnerClient(DestoryOnDeath);
                        }
                        else
                        {
                            detect.mainPikmin.HitEnemyOnLocalClient(DamageDelt);
                        }
                        break;
                    case PikminHarmTriggerDeathType.Zap:
                        if (InstaDeath || detect.mainPikmin.enemyHP - DamageDelt <= 0)
                        {
                            detect.mainPikmin.DoZapDeath();
                            detect.mainPikmin.DoZapDeathServerRpc();
                        }
                        else
                        {
                            detect.mainPikmin.HitEnemyOnLocalClient(DamageDelt);
                        }
                        break;
                    case PikminHarmTriggerDeathType.Squish:
                        if (InstaDeath || detect.mainPikmin.enemyHP - DamageDelt <= 0)
                        {
                            detect.mainPikmin.DoSquishDeath();
                            detect.mainPikmin.DoSquishDeathServerRpc();
                        }
                        else
                        {
                            detect.mainPikmin.HitEnemyOnLocalClient(DamageDelt);
                        }
                        break;
                }
            }
        }
    }
}
