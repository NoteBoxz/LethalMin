using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Pikmin;
using LethalMon.Behaviours;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalMin.Utils
{
    public static class PikChecks
    {
        public static bool IsLeaderInvalid(Leader? leader)
        {
            bool NotValid;

            NotValid = leader == null || leader.Controller == null ||
            !IsPlayerConnected(leader.Controller) || !leader.IsSpawned;

            return NotValid;
        }
        public static bool IsPlayerConnected(PlayerControllerB player)
        {
            bool Valid;

            PlayerControllerB ServerPlayer = StartOfRound.Instance.allPlayerScripts[0];

            Valid = player == ServerPlayer
            || player != ServerPlayer && player.OwnerClientId != NetworkManager.ServerClientId
            && StartOfRound.Instance.ClientPlayerList.ContainsKey(player.OwnerClientId);

            return Valid;
        }
        public static bool IsGenerationValid(PikminModelGeneration gen)
        {
            if (gen == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null generation when checking is valid!");
                return false;
            }

            if (gen.Model == null
            || gen.Animator == null
            || gen.SproutTop == null && gen.Plants.Any(p => p == null))
            {
                //LethalMin.Logger.LogWarning($"LethalMin: Invalid pikmin generation for {gen.Generation}");
                return false;
            }
            return true;
        }
        public static bool IsGenerationValid(SproutModelGeneration gen)
        {
            if (gen == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null generation when checking is valid!");
                return false;
            }

            if (gen.Model == null
            || gen.Animator == null
            || gen.Plants.Any(p => p == null))
            {
                //LethalMin.Logger.LogWarning($"LethalMin: Invalid sprout generation for {gen.Generation}");
                return false;
            }
            return true;
        }
        public static bool IsGenerationValid(OnionItemModelGeneration gen)
        {
            if (gen == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null generation when checking is valid!");
                return false;
            }

            if (gen.Model == null
            || gen.Animator == null)
            {
                //LethalMin.Logger.LogWarning($"LethalMin: Invalid onion generation for {gen.Generation}");
                return false;
            }
            return true;
        }
        public static bool IsGenerationValid(OnionModelGeneration gen)
        {
            if (gen == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null generation when checking is valid!");
                return false;
            }

            if (gen.Model == null
            || gen.Animator == null
            || gen.SummonBeam == null)
            {
                //LethalMin.Logger.LogWarning($"LethalMin: Invalid onion generation for {gen.Generation}");
                return false;
            }
            return true;
        }
        public static bool IsGenerationValid(PuffminModelGeneration gen)
        {
            if (gen == null)
            {
                LethalMin.Logger.LogError($"LethalMin: null generation when checking is valid!");
                return false;
            }

            if (gen.Model == null
            || gen.Animator == null
            || gen.BodyRenderer == null)
            {
                //LethalMin.Logger.LogWarning($"LethalMin: Invalid sprout generation for {gen.Generation}");
                return false;
            }
            return true;
        }
        public static bool IsItemValid(GrabbableObject grabbableObject)
        {
            return grabbableObject != null
            && grabbableObject.grabbableToEnemies;
        }

        public static bool IsPikminResistantToHazard(PikminAI ai, PikminHazard hazard, UnityEngine.Object instance)
        {
            if (ai.pikminType.HazardsResistantTo.Contains(hazard))
            {
                ai.OnAvoidHazard(hazard, instance);
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsPikminResistantToHazard(PikminAI ai, PikminHazard hazard, bool CallOnResist = true)
        {
            if (ai.pikminType.HazardsResistantTo.Contains(hazard))
            {
                if (CallOnResist)
                    ai.OnAvoidHazard(hazard);
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsPikminResistantToHazard(PikminType type, PikminHazard hazard)
        {
            return type.HazardsResistantTo.Contains(hazard);
        }
        public static bool IsPikminItemValid(PikminItem itm)
        {
            return itm != null
            && itm.ItemScript != null
            && !itm.IsntHeldByPikmin()
            && (itm.ItemScript.grabbableToEnemies || itm.settings.OverrideGrabbableToEnemeis || itm.IsBeingCarried)
            && itm.PikminOnItem.Count < itm.GrabToPositions.Count
            && !itm.HasArrived
            && itm.settings.GrabableToPikmin
            && !PikminRoute.NodeCache.Any(node => node.IsInRange(itm.ItemScript.transform.position));
        }
        public static bool IsEnemyVaildToAttack(EnemyAI enemy)
        {
            if(enemy == null)
            {
                return false;
            }

            bool CanBeAttacked = true;
            PikminEnemy pikEnemy = enemy.GetComponent<PikminEnemy>();
            if (pikEnemy != null)
            {
                CanBeAttacked = pikEnemy.CanBeAttacked;
            }

            return
            !enemy.isEnemyDead
            && enemy.enemyType != LethalMin.PikminEnemyType
            && (enemy.enemyType.canDie || pikEnemy != null && pikEnemy.OverrideCanDie)
            && CanBeAttacked
            && !IsEnemyBlackListed(enemy)
            && (LethalMin.IsDependencyLoaded("LethalMon") && !LETHALMON_ISENEMYTAMED(enemy) || !LethalMin.IsDependencyLoaded("LethalMon"));
        }

        public static bool LETHALMON_ISENEMYTAMED(EnemyAI enemy)
        {
            TamedEnemyBehaviour tamedEnemy = enemy.GetComponentInChildren<TamedEnemyBehaviour>();
            if (tamedEnemy != null && tamedEnemy.IsTamed)
            {
                if (LethalMin.AttackOwnTamedEnemies && tamedEnemy.ownerPlayer == StartOfRound.Instance.localPlayerController)
                {
                    return false;
                }
                if (LethalMin.AttackOthersTamedEnemies && tamedEnemy.ownerPlayer != StartOfRound.Instance.localPlayerController)
                {
                    return false;
                }
                return true;
            }
            return false;
        }


        public static bool IsEnemyBlackListed(EnemyAI enemy)
        {
            if (enemy == null || enemy.enemyType == null)
            {
                return false; // Not blacklisted if the enemy is null or has no type
            }

            // Check if the enemy's type is in the blacklisted types
            return LethalMin.AttackBlacklistConfig.InternalValue.Contains(enemy.enemyType.enemyName);
        }
        public static bool IsNavMeshOnMap()
        {
            if (LethalMin.CheckNavMesh.InternalValue == false
            || LethalMin.NavmeshCheckBlacklistConfig.InternalValue.Contains(StartOfRound.Instance.currentLevel.PlanetName))
            {
                return true;
            }
            // Get the NavMesh triangulation data
            NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

            // Check if the NavMesh has any vertices
            if (navMeshData.vertices != null && navMeshData.vertices.Length > 0)
            {
                return true; // NavMesh exists
            }
            else
            {
                return false; // NavMesh does not exist
            }
        }
        public static bool IsGrabbableBodieFixed(GrabbableObject __instance)
        {
            return CleaningCompany.Plugin.instance.BodySpawns.Values.ToList().Contains(__instance.itemProperties);
        }
        public static bool DoesPikminHaveRegisteredOnion(PikminAI ai)
        {
            return DoesPikminHaveRegisteredOnion(ai.pikminType);
        }
        public static bool DoesPikminHaveRegisteredOnion(PikminType type)
        {
            foreach (OnionType onion in LethalMin.RegisteredOnionTypes.Values)
            {
                if (onion.TypesCanHold.Contains(type))
                {
                    return true;
                }
            }
            return false;
        }

        // internal static bool IsPikminItemValid(PikminItem itm)
        // {
        //     if (itm.ItemScript == null)
        //     {
        //         return false;
        //     }
        //     bool isItemScriptValid = itm.ItemScript != null;
        //     LethalMin.Logger.LogMessage($"IsPikminItemValid: ItemScript is {(isItemScriptValid ? "valid" : "null")}");

        //     bool isHeldByPikmin = !itm.IsntHeldByPikmin();
        //     LethalMin.Logger.LogMessage($"IsPikminItemValid: Item is {(isHeldByPikmin ? "held" : "not held")} by Pikmin");

        //     bool hasAvailableGrabPositions = itm.PikminOnItem.Count < itm.GrabToPositions.Count;
        //     LethalMin.Logger.LogMessage($"IsPikminItemValid: Available grab positions: {hasAvailableGrabPositions} (PikminOnItem: {itm.PikminOnItem.Count}, GrabToPositions: {itm.GrabToPositions.Count})");

        //     var nodeInRange = PikminRoute.NodeCache.FirstOrDefault(node => node.IsInRange(itm.ItemScript.transform.position));
        //     bool isNotInNodeRange = nodeInRange == null;

        //     if (isNotInNodeRange)
        //     {
        //         LethalMin.Logger.LogMessage($"IsPikminItemValid: Item '{itm.ItemScript.name}' is not in range of any node");
        //     }
        //     else
        //     {
        //         LethalMin.Logger.LogMessage($"IsPikminItemValid: Item '{itm.ItemScript.name}' is in range of node '{nodeInRange.NodeName}' ({nodeInRange.GetDist(itm.ItemScript.transform.position)} > {nodeInRange.CheckDistance})");
        //     }
        //     bool isValid = isItemScriptValid && isHeldByPikmin && hasAvailableGrabPositions && isNotInNodeRange;
        //     LethalMin.Logger.LogMessage($"IsPikminItemValid: Final result: {isValid}");

        //     return isValid;
        // }
    }
}