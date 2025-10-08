using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Dusk;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.SceneManagement;

namespace LethalMin.Achivements;

public class AchivementController
{
    public object mod;
    public List<object> Achievements = new List<object>();
    public object WhatHappenedAchievement = null!;
    public AssetBundle ModAssetBundle = null!;
    public AssetBundle AchivementAssetBundle = null!;

    public AchivementController(object mod)
    {
        this.mod = mod;
        foreach (DuskAchievementDefinition achievement in DuskModContent.Achievements.Values)
        {
            if (achievement.Mod == mod)
            {
                Achievements.Add(achievement);
            }

            if (achievement.TypedKey.Key == "WhatHappened")
            {
                WhatHappenedAchievement = achievement;
            }
        }
    }

    public static bool WhatHappenedDoable()
    {
        DuskInstantAchievement? achievement = ((AchivementController)LethalMin.AchivementController).WhatHappenedAchievement as DuskInstantAchievement;
        if (achievement == null || achievement.Completed)
            return false;

        return true;
    }

    public static HashSet<Landmine> LandminesTriggeredByPikmin { get; private set; } = new HashSet<Landmine>();

    public IEnumerator CheckForWhatHappenedAchievement(Landmine landmine, List<EnemyAI> enemiesAliveBeforeExplosion)
    {
        // Wait for explosion to fully resolve
        yield return new WaitForSeconds(0.5f);

        // Verify this landmine was triggered by a pikmin
        if (!LandminesTriggeredByPikmin.Contains(landmine))
        {
            LethalMin.Logger.LogInfo("Landmine explosion not triggered by Pikmin, skipping What Happened check");
            yield break;
        }

        // Check if achievement is already completed
        DuskInstantAchievement? achievement = WhatHappenedAchievement as DuskInstantAchievement;
        if (achievement?.Completed == true)
        {
            LethalMin.Logger.LogInfo("What Happened achievement already completed, skipping check");
            yield break;
        }

        PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
        if (localPlayer == null)
            yield break;

        Vector3 explosionPos = landmine.transform.position;
        Vector3 raisedPos = explosionPos + Vector3.up * 0.5f;

        // Wait longer for enemy death states to settle
        yield return new WaitForSeconds(0.5f);

        // Check if any of the enemies that were alive before the explosion are now dead
        bool enemyKilledByExplosion = false;
        foreach (EnemyAI enemy in enemiesAliveBeforeExplosion)
        {
            if (enemy != null && enemy.isEnemyDead && !enemy.enemyType.destroyOnDeath
            && enemy.enemyType != LethalMin.PikminEnemyType)
            {
                enemyKilledByExplosion = true;
                break;
            }
        }

        if (!enemyKilledByExplosion)
        {
            LethalMin.Logger.LogInfo("No enemies killed by landmine explosion, skipping What Happened trigger");
            yield break;
        }

        PikminManager.instance.SpawnWhatHappenedTriggerRpc(raisedPos);

        LandminesTriggeredByPikmin.Remove(landmine);
    }
}