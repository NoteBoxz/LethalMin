using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    public class EnemyAIPatch
    {
        public static HashSet<EnemyType> EnemiesModified = new HashSet<EnemyType>();
        public static Dictionary<EnemyType, int> EnemyHPs = new Dictionary<EnemyType, int>();


        [HarmonyPatch(nameof(EnemyAI.Start))]
        [HarmonyPostfix]
        private static void start(EnemyAI __instance)
        {
            try
            {
                if (__instance.enemyType != LethalMin.PikminEnemyType)
                    PikminManager.instance.EnemyAIs.Add(__instance);

                if (__instance.enemyType != LethalMin.PikminEnemyType && !EnemiesModified.Contains(__instance.enemyType))
                {
                    LethalMin.Logger.LogWarning($"Enemy Type {__instance.enemyType.enemyName} does not have latch triggers");
                    AddPikminEnemyToEnemyAI(__instance.enemyType);
                }

                //LethalMin.Logger.LogInfo($"{__instance.gameObject.name} has spawned. index: {__instance.thisEnemyIndex}");
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch enemyAI start for {__instance.gameObject.name} due to: {e}");
            }
        }

        [HarmonyPatch(nameof(EnemyAI.OnDestroy))]
        [HarmonyPostfix]
        private static void destory(EnemyAI __instance)
        {
            try
            {
                if (__instance.enemyType != LethalMin.PikminEnemyType)
                {
                    PikminManager.instance.EnemyAIs.Remove(__instance);
                    if (PikminManager.instance.ConvertedAIs.Contains(__instance))
                    {
                        PikminManager.instance.ConvertedAIs.Remove(__instance);
                    }
                }
            }
            catch (System.Exception e)
            {
                LethalMin.Logger.LogError($"Failed to patch enemyAI destory for {__instance.gameObject.name} due to: {e}");
            }
        }


        public static void AddPikminEnemyToEnemyAI(EnemyType enemy)
        {
            if (enemy != LethalMin.PikminEnemyType && EnemiesModified.Add(enemy) && enemy.enemyPrefab.GetComponent<PikminEnemy>() == null)
            {
                PikminEnemy enemyPikmin = null!;
                EnemyAI enemyAI = enemy.enemyPrefab.GetComponentInChildren<EnemyAI>();
                if (enemyAI == null)
                {
                    LethalMin.Logger.LogError($"Failed to get EnemyAI for {enemy.enemyName}");
                    return;
                }

                // Check for custom override first
                if (enemyAI.TryGetComponent(out CustomPikminEnemyOverrideDef customOverride))
                {
                    PikminEnemy? pEnemy = enemy.enemyPrefab.AddComponent(customOverride.CustomPikminEnemyOverrideType) as PikminEnemy;
                    if (pEnemy == null)
                    {
                        LethalMin.Logger.LogError($"Failed to add custom PikminEnemy component of type {customOverride.CustomPikminEnemyOverrideType} to {enemy.enemyName}");
                        return;
                    }
                    enemyPikmin = pEnemy;
                    GameObject.Destroy(customOverride);
                }
                else
                {
                    // Dictionary mapping enemy AI types to their corresponding Pikmin enemy types
                    var enemyTypeMap = new Dictionary<System.Type, System.Type>
                    {
                        { typeof(ClaySurgeonAI), typeof(ClaySurgeonPikminEnemy) },
                        { typeof(CrawlerAI), typeof(CrawlerPikminEnemy) },
                        { typeof(SandSpiderAI), typeof(SandSpiderPikminEnemy) },
                        { typeof(HoarderBugAI), typeof(HoarderBugPikminEnemy) },
                        { typeof(MouthDogAI), typeof(MouthDogPikminEnemy) },
                        { typeof(BlobAI), typeof(BlobPikminEnemy) },
                        { typeof(RedLocustBees), typeof(RedLocustBeesPikminEnemy) },
                        { typeof(CaveDwellerAI), typeof(CaveDwellerPikminEnemy) },
                        { typeof(RadMechAI), typeof(RadMechPikminEnemy) },
                        { typeof(ForestGiantAI), typeof(ForestGiantPikminEnemy) },
                        { typeof(JesterAI), typeof(JesterPikminEnemy) },
                        { typeof(ButlerEnemyAI), typeof(ButlerEnemyPikminEnemy) },
                        { typeof(BaboonBirdAI), typeof(BaboonBirdPikminEnemy) },
                        { typeof(MaskedPlayerEnemy), typeof(MaskedPlayerPikminEnemy) }
                    };

                    // Try to get the corresponding Pikmin enemy type, or use the default if not found
                    System.Type enemyType = enemyAI.GetType();
                    if (enemyTypeMap.TryGetValue(enemyType, out System.Type pikminType))
                    {
                        PikminEnemy? component = enemy.enemyPrefab.AddComponent(pikminType) as PikminEnemy;
                        if (component == null)
                        {
                            LethalMin.Logger.LogError($"Failed to add {pikminType.Name} component to {enemy.enemyName}");
                            return;
                        }
                        enemyPikmin = component;
                    }
                    else
                    {
                        PikminEnemy? component = enemy.enemyPrefab.AddComponent<PikminEnemy>();
                        if (component == null)
                        {
                            LethalMin.Logger.LogError($"Failed to add default PikminEnemy component to {enemy.enemyName}");
                            return;
                        }
                        enemyPikmin = component;
                    }
                }

                enemyPikmin.enemyScript = enemyAI;
                int AllTriggers = 0, AddedTriggers = 0;
                enemyPikmin.LatchTriggers = new List<PikminLatchTrigger>();
                foreach (EnemyAICollisionDetect detect in enemy.enemyPrefab.GetComponentsInChildren<EnemyAICollisionDetect>(true))
                {
                    if (enemy == LethalMin.PuffminEnemyType)
                    {
                        break;
                    }

                    AllTriggers++;

                    if (detect.GetComponent<PikminLatchTrigger>() != null)
                    {
                        continue;
                    }
                    PikminLatchTrigger PLtrigger = AddLatchTriggerToColider(detect.gameObject, enemy.enemyPrefab.transform);
                    enemyPikmin.LatchTriggers.Add(PLtrigger);
                    AddedTriggers++;
                }
                enemyPikmin.LatchTriggers.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
                enemyPikmin.OnAddedToEnemy(enemyAI);

                LethalMin.Logger.LogMessage($"Registered {enemy.enemyName} As Pikmin Enemy, Added ({AddedTriggers}) latch triggers");
            }
        }
        public static void FillinEnemyHPs()
        {
            EnemyHPs.Clear();
            foreach (EnemyType type in LethalMin.EnemyTypes)
            {
                if (!EnemyHPs.ContainsKey(type))
                {
                    try
                    {
                        EnemyHPs.Add(type, type.enemyPrefab.GetComponent<EnemyAI>().enemyHP);
                    }
                    catch (System.Exception e)
                    {
                        // this means the enemy type does not have an EnemyAI component, so we can't get the HP
                        LethalMin.Logger.LogError($"Failed to get HP for {type.enemyName} due to: {e}");
                        EnemyHPs.Add(type, 1); // default to 1 if it fails
                    }
                }
            }
        }

        public static PikminLatchTrigger AddLatchTriggerToColider(GameObject go, Transform LookAtObject)
        {
            PikminLatchTrigger PLtrigger = go.AddComponent<PikminLatchTrigger>();
            PLtrigger.StateCondisions.Add(Pintent.Thrown);
            PLtrigger.StateToSet = LatchTriggerStateToSet.Attack;
            PLtrigger.OverrideLookAtObject = LookAtObject;
            PLtrigger.AllowBaseLatchOn = false;

            return PLtrigger;
        }
    }
}
