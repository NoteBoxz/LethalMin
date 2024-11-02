using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Events;
using LethalMon.Behaviours;

namespace LethalMin
{
    enum Puffstate
    {
        idle,
        following,
        attacking,
        airborn
    }
    public class PuffminAI : EnemyAI, IDebuggable
    {
        public Animator? LocalAnim;
        public AudioSource? LocalSFX;
        public AudioSource? LocalVoice;
        public EnemyAI? OwnerAI;
        public Rigidbody? rb;
        GameObject? Ghost;
        public NetworkTransform? transform2;
        GameObject? PminColider,scanNode,NoticeColider;
        public float InternalAirbornTimer,AbTimer;
        public System.Random? enemyRandom;
        SphereCollider? Pcollider;

        public override void Start()
        {
            InternalAirbornTimer = LethalMin.FallTimerValue;

            base.Start();

            transform2 = GetComponent<NetworkTransform>();
            PminColider = transform.Find("PikminColision").gameObject;
            NoticeColider = transform.Find("WhistleDetection").gameObject;
            scanNode = transform.Find("ScanNode").gameObject;

            // Because the EnemyAI class uses unnessary methods for moving and syncing position in my case
            moveTowardsDestination = false;
            movingTowardsTargetPlayer = false;
            updatePositionThreshold = 9000;
            syncMovementSpeed = 0f;

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);

            // Rigidbody Components
            Pcollider = GetComponent<SphereCollider>();
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.Sleep();
            
            StartCoroutine(LateInitialize());
        }
        private IEnumerator LateInitialize()
        {
            yield return new WaitForSeconds(0.1f);  // Wait for one frame

            if (LethalMin.DebugMode)
                LethalMin.Logger.LogInfo($"Pikmin is now being spawned");

            Ghost = LethalMin.Ghost;

            //Because EnemyAI is dumb
            creatureAnimator = null;

            // Finalization 
            yield return new WaitForSeconds(0.1f);  // Wait for one frame
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetValues(typeof(PState)).Length];
            
            yield return null;  // Wait another frame
        }
    }
}