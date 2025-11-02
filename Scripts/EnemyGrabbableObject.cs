using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class EnemyGrabbableObject : MonoBehaviour
    {
        public EnemyAI ai = null!;
        public GrabbableObject grabbableObject = null!;
        Vector3 InitalScale = Vector3.one;
        public void Awake()
        {
            ai = GetComponent<EnemyAI>();
            InitalScale = transform.localScale;
        }

        public void FixedUpdate()
        {
            if (grabbableObject == null) return;

            ai.agent.enabled = false;
            ai.enabled = false;
            transform.localScale = Vector3.Scale(InitalScale, grabbableObject.transform.localScale);
            transform.localPosition = grabbableObject.transform.position;
            transform.localRotation = grabbableObject.transform.rotation;
        }

        void LateUpdate()
        {
            if (ai.IsServer && grabbableObject == null)
            {
                LethalMin.Logger.LogInfo($"EnemyGrabbableObject: {gameObject.name} has deteached from grabbableOBJ! (lateUpdated)");
                ai.NetworkObject.Despawn(true);
                return;
            }
        }

        void OnDestroy()
        {
            if (ai.IsServer && grabbableObject != null && grabbableObject.NetworkObject.IsSpawned)
            {
                LethalMin.Logger.LogInfo($"EnemyGrabbableObject: {gameObject.name} has deteached from grabbableOBJ!");
                grabbableObject.NetworkObject.Despawn(true);
            }
        }
    }
}
