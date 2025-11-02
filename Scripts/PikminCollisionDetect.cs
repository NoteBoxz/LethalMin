using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public class PikminCollisionDetect : EnemyAICollisionDetect
    {
        public PikminAI mainPikmin = null!;
        public List<PikminEffectTrigger> CurFXtriggers = new List<PikminEffectTrigger>();
        public float TriggerBuffer = 0.5f;
        void Start()
        {
            if (mainScript == null)
                mainScript = GetComponentInParent<EnemyAI>();
            if (mainPikmin == null)
                mainPikmin = mainScript.GetComponent<PikminAI>();
        }
        public new virtual void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);

            if (!mainPikmin.IsOwner)
            {
                return;
            }

            if (mainPikmin.currentBehaviourStateIndex == PikminAI.PANIC &&
            mainPikmin.LeaderWhistling == null &&
            other.TryGetComponent(out PikminNoticeZone zone) &&
            zone.LeaderScript != null && zone.CanSavePikmin && zone.Active)
            {
                //LethalMin.Logger.LogInfo($"{zone.LeaderScript.Controller.playerUsername}: {mainPikmin.DebugID} is whistling");
                mainPikmin.LeaderWhistling = zone.LeaderScript;
                mainPikmin.LeaderWhistlingZone = zone;
            }

            if (TriggerBuffer >= 0)
            {
                TriggerBuffer -= Time.deltaTime;
                return;
            }

            if (mainPikmin.agent.enabled &&
                other.TryGetComponent(out PikminEffectTrigger FXtrigger) &&
                FXtrigger.Mode == PikminEffectMode.Persitant &&
                !CurFXtriggers.Contains(FXtrigger) &&
                !FXtrigger.IsPikminResistantToTrigger(mainPikmin))
            {
                if (FXtrigger.GetAnimString() == "WaterPanic" && LethalMin.UselessBluesCheat)
                    return;
                CurFXtriggers.Add(FXtrigger);
                mainPikmin.StartPanicingServerRpc((int)FXtrigger.Mode, (int)FXtrigger.EffectType, FXtrigger.GetAnimString());
            }
            TriggerBuffer = 0.5f;
        }
        public virtual void OnTriggerEnter(Collider other)
        {
            if (!mainPikmin.IsOwner)
            {
                return;
            }

            if (mainPikmin.CurrentIntention != Pintent.Panicing &&
                other.TryGetComponent(out PikminEffectTrigger FXtrigger) &&
                FXtrigger.Mode == PikminEffectMode.Limited &&
                !FXtrigger.IsPikminResistantToTrigger(mainPikmin))
            {
                if (FXtrigger.GetAnimString() == "WaterPanic" && LethalMin.UselessBluesCheat)
                    return;
                mainPikmin.StartPanicingServerRpc((int)FXtrigger.Mode, (int)FXtrigger.EffectType, FXtrigger.GetAnimString());
            }
        }
        public virtual void OnTriggerExit(Collider other)
        {
            if (!mainPikmin.IsOwner)
            {
                return;
            }

            if (mainPikmin.currentBehaviourStateIndex == PikminAI.PANIC &&
            mainPikmin.LeaderWhistling != null &&
            other.TryGetComponent(out PikminNoticeZone zone) &&
            mainPikmin.LeaderWhistling == zone.LeaderScript && zone.Active)
            {
                //LethalMin.Logger.LogInfo($"{zone.LeaderScript.Controller.playerUsername}: {mainPikmin.DebugID} exited whistling");
                mainPikmin.LeaderWhistling = null;
                mainPikmin.LeaderWhistlingZone = null;
            }

            if (mainPikmin.agent.enabled &&
            other.TryGetComponent(out PikminEffectTrigger FXtrigger) &&
            FXtrigger.Mode == PikminEffectMode.Persitant &&
            CurFXtriggers.Contains(FXtrigger))
            {
                CurFXtriggers.Remove(FXtrigger);
                mainPikmin.StopPanicingServerRpc();
            }
        }
    }
}
