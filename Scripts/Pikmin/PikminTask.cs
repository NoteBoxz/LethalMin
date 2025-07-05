using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    public abstract class PikminTask
    {
        public PikminAI Owner { get; protected set; } = null!;

        public PikminTask(PikminAI owner)
        {
            Owner = owner;
            OnTaskCreated();
        }

        public virtual void OnTaskCreated()
        {
            
        }

        public virtual void Update()
        {

        }

        public virtual void IntervaledUpdate()
        {

        }

        public virtual void TaskEnd(bool callRpc = false)
        {
            if (Owner.CurrentTask == this)
            {
                if (callRpc)
                    Owner.SetToIdleServerRpc();
                else
                    Owner.SetToIdle();
                    
                Owner.RemoveCurrentTask();
            }
        }

        public virtual void TaskIntercepted()
        {
            if (Owner.CurrentTask == this)
            {
                Owner.RemoveCurrentTask();
            }
        }
    }
}
