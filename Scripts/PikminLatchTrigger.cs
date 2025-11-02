using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LethalMin
{
    public enum PikminLatchTriggerType
    {
        PikminEntersTrigger,
        TriggerEntersPikmin,
    }

    public enum LatchTriggerStateToSet
    {
        Attack,
        Stuck
    }

    public class PikminLatchTrigger : MonoBehaviour
    {
        [Tooltip($"The state that the pikmin must be in to be landed on the latch trigger (Leave empty for any state)")]
        public List<Pintent> StateCondisions = new List<Pintent>();
        [Tooltip($"The state that the pikmin will be switched to when landing on the trigger")]
        public LatchTriggerStateToSet StateToSet = LatchTriggerStateToSet.Stuck;
        [Tooltip("The max number of pikmin that can be on the trigger at once")]
        public int MaxAmmount = 100;
        [Tooltip($"The time it takes for the pikmin to be killed, recomended for it to be higher than the time to escape varible")]
        public float KillTimer = -1f;
        [Tooltip($"The ammount of time a player needs to whistle for a pikmin to escape (set to -255 to be unescapeable)")]
        public float WhistleTime = 0f;
        [Tooltip($"Allow pikmin to latch on via OnCollisionEnter")]
        public bool AllowBaseLatchOn = true;
        [Tooltip($"Incase you want the transform the pikmin latch to and the trigger be 2 differnt things.")]
        public Transform? OverrideLatchObject;
        [HideInInspector]
        public List<PikminAI> PikminOnLatch = new List<PikminAI>();
        public Transform? OverrideLookAtObject;
        [Tooltip($"The Network Addon, needed if you don't have any other way of syncing pikmin latching onto the trigger")]
        public PikminLatchTriggerNetworkAddon networkAddon = null!;

        public (Pintent, int) GetPikminState()
        {
            switch (StateToSet)
            {
                case LatchTriggerStateToSet.Attack:
                    return (Pintent.Attack, PikminAI.WORK);
                case LatchTriggerStateToSet.Stuck:
                    return (Pintent.Stuck, PikminAI.PANIC);
                default:
                    LethalMin.Logger.LogError($"Invalid state to set: {StateToSet}");
                    return (Pintent.Stuck, PikminAI.PANIC);
            }
        }

        [Header("Events (Called for every player)")]
        public UnityEvent<PikminAI> OnPikminDirectLatch;
        public UnityEvent<PikminAI> OnPikminLatch;
        public UnityEvent<PikminAI> OnPikminUnlatch;
        public UnityEvent<PikminAI> OnPikminHit;

        // Constructor to initialize events
        public PikminLatchTrigger()
        {
            OnPikminLatch = new UnityEvent<PikminAI>();
            OnPikminUnlatch = new UnityEvent<PikminAI>();
            OnPikminHit = new UnityEvent<PikminAI>();
            OnPikminDirectLatch = new UnityEvent<PikminAI>();
        }

        // Methods to invoke events
        public void InvokePikminDirectLatch(PikminAI pikmin)
        {
            OnPikminDirectLatch.Invoke(pikmin);
        }

        public void InvokePikminLatch(PikminAI pikmin)
        {
            OnPikminLatch.Invoke(pikmin);
        }

        public void InvokePikminUnlatch(PikminAI pikmin)
        {
            OnPikminUnlatch.Invoke(pikmin);
        }

        public void InvokePikminHit(PikminAI pikmin)
        {
            OnPikminHit.Invoke(pikmin);
        }

        public void RemoveAllPikmin(int Mode)
        {
            if (LethalMin.YeetAfterLatchOn)
            {
                Mode = 0;
            }
            const int KNOCKOFF = 0;
            const int TURNONRB = 1;
            const int TURNONNA = 2;
            const int TURNONNAYAY = 3;
            List<PikminAI> pikminToRemove = new List<PikminAI>(PikminOnLatch);
            switch (Mode)
            {
                case KNOCKOFF:
                    foreach (PikminAI ai in pikminToRemove)
                    {
                        UnlatchPikmin(ai);
                        // Calculate direction away from the latch trigger with upward force
                        Vector3 awayDirection = -ai.transform.forward;
                        Vector3 knockbackDirection = (awayDirection + Vector3.up * 0.5f).normalized;
                        ai.ApplyKnockBack(direction: knockbackDirection, LethalMin.YeetAfterLatchOn.InternalValue ? 25f : 10f);
                    }
                    break;
                case TURNONRB:
                    foreach (PikminAI ai in pikminToRemove)
                    {
                        UnlatchPikmin(ai);
                    }
                    break;
                case TURNONNA:
                    foreach (PikminAI ai in pikminToRemove)
                    {
                        ai.SetToIdle();
                        ai.SetCollisionMode(1);
                    }
                    break;
                case TURNONNAYAY:
                    foreach (PikminAI ai in pikminToRemove)
                    {
                        ai.SetToIdle();
                        ai.SetCollisionMode(1);
                        ai.DoYay();
                    }
                    break;
            }
        }

        public virtual bool TryLatch(PikminAI pikmin, Vector3 Point, bool IsDirectHit = true, bool DoCheckOnly = false)
        {
            if (PikminOnLatch.Count >= MaxAmmount)
            {
                LethalMin.Logger.LogWarning($"Latch failed because max ammount reached ({MaxAmmount})");
                return false;
            }
            if (PikminOnLatch.Contains(pikmin))
            {
                LethalMin.Logger.LogWarning($"Latch failed because Pikmin is already latching ({pikmin.DebugID})");
                return false;
            }
            if (!StateCondisions.ToList().Contains(pikmin.CurrentIntention))
            {
                //LethalMin.Logger.LogWarning($"Latch failed because Pikmin is not in valid state ({pikmin.DebugID})");
                return false;
            }
            if (pikmin.CurrentLatchTrigger != null)
            {
                LethalMin.Logger.LogWarning($"Latch failed because Pikmin is on another trigger ({pikmin.DebugID})");
                return false;
            }
            if (DoCheckOnly)
            {
                return true;
            }
            if (networkAddon != null)
            {
                networkAddon.LatchPikminServerRpc(pikmin.NetworkObject, Point, IsDirectHit);
            }
            LatchPikmin(pikmin, Point, IsDirectHit);
            return true;
        }
        public virtual void LatchPikmin(PikminAI pikmin, Vector3 LandPos, bool IsDirectHit = true)
        {
            LethalMin.Logger.LogDebug($"{gameObject.name} Latching Pikmin: {pikmin.DebugID}");
            pikmin.LatchPikmin(this, LandPos);
            PikminOnLatch.Add(pikmin);
            InvokePikminLatch(pikmin);
            if (IsDirectHit)
            {
                InvokePikminDirectLatch(pikmin);
            }
        }

        public virtual void UnlatchPikmin(PikminAI pikmin)
        {
            LethalMin.Logger.LogInfo($"Unlatching Pikmin: {pikmin.DebugID}");
            pikmin.UnlatchPikmin();
            PikminOnLatch.Remove(pikmin);
            InvokePikminUnlatch(pikmin);
        }

        public virtual void OnDestroy()
        {
            RemoveAllPikmin(2); // TURNONNA
        }
    }
}