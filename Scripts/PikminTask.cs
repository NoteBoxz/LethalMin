using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    public enum TaskStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Interrupted
    }
    public abstract class PikminTask
    {
        public TaskStatus Status { get; protected set; } = TaskStatus.NotStarted;
        public PikminAI Owner { get; protected set; } = null!;

        public PikminTask(PikminAI owner)
        {
            Owner = owner;
        }

        public virtual void Update()
        {
            
        }

        public virtual void IntervaledUpdate()
        {

        }
    }
}
