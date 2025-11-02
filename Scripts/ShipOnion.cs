using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin
{
    public class ShipOnion : Onion
    {
        public Animator BeamAnim = null!;
        public InteractTrigger IntTrigger = null!;
        public StartOfRound playerManager = null!;

        public override void Start()
        {
            DontChooseRandomType = true;
            DontDespawnOnGameEnd = true;
            playerManager = StartOfRound.Instance;

            base.Start();

            AllClimbLinks = GetComponentsInChildren<PikminLinkAnimation>().ToList();
        }


        public void OnBeamInteract(PlayerControllerB controller)
        {
            OnionHUDManager.instance.SetCurrentOnion(this);
            OnionHUDManager.instance.OpenMenu();

            ClimbLinks[0].AnimSpeedMultiplier = 3;
            if (NavMesh.SamplePosition(ClimbLinks[0].EndPoint.transform.position, out NavMeshHit hit, 100, NavMesh.AllAreas))
            {
                ClimbLinks[0].EndPoint.transform.position = hit.position;
            }
        }


        public override void Update()
        {
            base.Update();
            //log each condistion
            //LethalMin.Logger.LogInfo($"{playerManager.inShipPhase} {playerManager.shipIsLeaving} {PikminManager.CanPathOnMoonGlobal}");
            if (playerManager.inShipPhase || playerManager.shipIsLeaving || !PikminManager.CanPathOnMoonGlobal || !playerManager.shipHasLanded)
            {
                IntTrigger.gameObject.SetActive(false);
                BeamAnim.SetBool("Active", false);
            }
            else
            {
                IntTrigger.gameObject.SetActive(true);
                BeamAnim.SetBool("Active", true);
            }
        }
    }
}
