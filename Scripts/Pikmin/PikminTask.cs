using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace LethalMin.Pikmin
{
    public abstract class PikminTask
    {
        public PikminAI pikmin { get; protected set; } = null!;
        public Transform transform => pikmin.transform;
        public virtual bool DoYayOnTaskEnd { get; }

        public PikminTask(PikminAI pikminAssigningTo)
        {
            pikmin = pikminAssigningTo;
            OnTaskCreated();
        }

        /// <summary>
        /// Called when the task is created and assigned to the pikmin. Called on enemy client.
        /// </summary>
        public virtual void OnTaskCreated()
        {

        }

        /// <summary>
        /// Called every frame, called on every client
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// Called at a fixed interval, on the pikmin's owner client
        /// </summary>
        public virtual void IntervaledUpdate()
        {

        }

        /// <summary>
        /// Called when the task is completed and the pikmin should return to idle state.
        /// </summary>
        /// <param name="callRpc"></param>
        public virtual void TaskEnd(bool callRpc = true, bool DontDoYayAnyway = false)
        {
            if(pikmin == null)
            {
                LethalMin.Logger.LogWarning("PT: TaskEnd called but pikmin is null.");
                return;
            }
            if (pikmin.CurrentTask == this)
            {
                if (callRpc)
                    pikmin.SetToIdleServerRpc();
                else
                    pikmin.SetToIdle();

                if (DoYayOnTaskEnd && !DontDoYayAnyway)
                    pikmin.DoYay();

                pikmin.RemoveCurrentTask();
            }
        }

        /// <summary>
        /// Called when the task is interrupted by another task.
        /// </summary>
        public virtual void TaskIntercepted()
        {
            if (pikmin.CurrentTask == this)
            {
                pikmin.RemoveCurrentTask();
            }
        }
    }
}
