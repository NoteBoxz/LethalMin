using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System.Reflection;
using Dusk;

namespace LethalMin.Achivements
{
    public class WhatHappenedLookTrigger : MonoBehaviour
    {
        [SerializeField] private float range = 15f;
        [SerializeField] private float checkInterval = 0.2f;
        private float checkTimer;

        private void LateUpdate()
        {
            AchivementController achievementController = (AchivementController)LethalMin.AchivementController;
            if (achievementController == null)
            {
                Destroy(gameObject);
                return;
            }

            DuskInstantAchievement? whatHappenedAchievement = achievementController.WhatHappenedAchievement as DuskInstantAchievement;
            if (whatHappenedAchievement == null || !AchivementController.WhatHappenedDoable())
            {
                Destroy(gameObject);
                return;
            }

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
                return;

            checkTimer = checkInterval;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (localPlayer == null)
                return;

            float distance = Vector3.Distance(localPlayer.transform.position, transform.position);
            
            // Only check line of sight if player is within range
            if (distance <= range && localPlayer.HasLineOfSightToPosition(transform.position))
            {
                bool triggered = whatHappenedAchievement.TriggerAchievement();
                LethalMin.Logger.LogInfo($"What Happened achievement triggered: {triggered}");
                Destroy(gameObject);
            }
        }
    }
}