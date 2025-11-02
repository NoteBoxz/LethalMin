using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dissonance.Integrations.Unity_NFGO;
using GameNetcodeStuff;
using LethalMin.Patches;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace LethalMin.Utils;

public static class PikUtils
{
    public static bool IsOutOfRange<T>(IList<T> list, int index)
    {
        return index < 0 || index >= list.Count;
    }

    public static bool IsOutOfRange<T>(T[] array, int index)
    {
        return index < 0 || index >= array.Length;
    }
    public static Vector3 GetPositionOffsetedOnNavMesh(Vector3 Position, float SampleRadius = 5, float DistanceLimmit = 5f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(Position, out hit, SampleRadius, NavMesh.AllAreas))
        {
            if (Vector3.Distance(hit.position, Position) < DistanceLimmit)
            {
                return hit.position;
            }
        }
        return Position;
    }

    public static bool ListsHaveSameContent<T>(IList<T> list1, IList<T> list2, bool considerOrder = false)
    {
        // Check if either list is null
        if (list1 == null && list2 == null)
            return true;

        if (list1 == null || list2 == null)
            return false;

        // Check if lists have the same count
        if (list1.Count != list2.Count)
            return false;

        // If we need to consider order
        if (considerOrder)
        {
            // Compare elements at each position
            for (int i = 0; i < list1.Count; i++)
            {
                // Handle null cases
                if (list1[i] == null && list2[i] == null)
                    continue;

                if (list1[i] == null || list2[i] == null)
                    return false;

                if (!object.Equals(list1[i], list2[i]))
                    return false;
            }

            return true;
        }
        else
        {
            // If order doesn't matter, use a frequency approach
            var dict1 = new Dictionary<T, int>();
            var dict2 = new Dictionary<T, int>();

            // Count frequencies in first list
            foreach (T item in list1)
            {
                if (dict1.ContainsKey(item))
                    dict1[item]++;
                else
                    dict1[item] = 1;
            }

            // Count frequencies in second list
            foreach (T item in list2)
            {
                if (dict2.ContainsKey(item))
                    dict2[item]++;
                else
                    dict2[item] = 1;
            }

            // Check if dictionaries have same keys and values
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var pair in dict1)
            {
                if (!dict2.TryGetValue(pair.Key, out int count) || count != pair.Value)
                    return false;
            }

            return true;
        }
    }
    // Helper equality comparer that handles null values properly
    private class EqualityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            return x.Equals(y);
        }

        public int GetHashCode(T obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }
    }

    public static Color ParseStringToColor(string colorString, bool Normalize = true)
    {
        if (string.IsNullOrEmpty(colorString))
        {
            throw new ArgumentException("Color string cannot be null or empty.");
        }

        // Split the string by commas and parse the values
        string[] parts = colorString.Split(',');
        if (parts.Length != 4)
        {
            throw new ArgumentException("Invalid color string format. Expected format: 'R,G,B,A'");
        }

        float r = float.Parse(parts[0]);
        float g = float.Parse(parts[1]);
        float b = float.Parse(parts[2]);
        float a = float.Parse(parts[3]);

        if (Normalize)
        {
            r /= 255f;
            g /= 255f;
            b /= 255f;
            a /= 255f;
        }

        return new Color(r, g, b, a);
    }

    public static string ParseColorToString(Color color, bool Normalize = true)
    {
        if (Normalize)
        {
            return $"{color.r * 255f},{color.g * 255f},{color.b * 255f},{color.a * 255f}";
        }
        else
        {
            return $"{color.r},{color.g},{color.b},{color.a}";
        }
    }

    public static PikminGeneration ConvertCfgGenerationToPikminGeneration(CfgPikminGeneration generation)
    {
        if (generation == CfgPikminGeneration.Default)
        {
            return LethalMin.DefaultGeneration.InternalValue;
        }
        else
        {
            return (PikminGeneration)generation;
        }
    }

    public static TerminalNode CreateTerminalNode(string name, string displayText)
    {
        var node = ScriptableObject.CreateInstance<TerminalNode>();
        node.name = name;
        node.displayText = displayText;
        node.clearPreviousText = true;
        node.maxCharactersToType = 35;
        node.buyItemIndex = -1;
        node.buyVehicleIndex = -1;
        node.isConfirmationNode = false;
        node.buyRerouteToMoon = -1;
        node.displayPlanetInfo = -1;
        node.shipUnlockableID = -1;
        node.buyUnlockable = true;
        node.returnFromStorage = false;
        node.itemCost = 0;
        node.creatureFileID = -1;
        node.creatureName = "";
        node.storyLogFileID = -1;
        node.overrideOptions = false;
        node.acceptAnything = false;
        node.loadImageSlowly = false;
        node.persistentImage = false;
        node.terminalEvent = null!;

        return node;
    }

    public static TerminalKeyword CreateTerminalKeyword(string name, string word, TerminalNode resultNode)
    {
        var keyword = ScriptableObject.CreateInstance<TerminalKeyword>();
        keyword.name = name;
        keyword.word = word;
        keyword.specialKeywordResult = resultNode;
        AddKeywordToTerminal(keyword);
        return keyword;
    }

    public static void AddKeywordToTerminal(TerminalKeyword keyword)
    {
        if (TerminalPatch.nList == null)
        {
            TerminalPatch.KeywordsWaitingToBeAdded.Add(keyword);
            return;
        }
        int currentLength = TerminalPatch.nList.allKeywords.Length;
        Array.Resize(ref TerminalPatch.nList.allKeywords, currentLength + 1);
        TerminalPatch.nList.allKeywords[currentLength] = keyword;
    }
    public static void AddEventToFrame(float time, string functionToCall, AnimationClip animationClip)
    {
        // Create a new AnimationEvent
        AnimationEvent animEvent = new AnimationEvent();

        animEvent.time = time;

        // Set the function to call
        animEvent.functionName = functionToCall;

        // Add the event to the animation clip
        animationClip.AddEvent(animEvent);

        LethalMin.Logger.LogInfo($"Added event at time ({time}) for {animationClip.name}");
    }

    public static void AddEventToFrame(int frame, string functionToCall, AnimationClip animationClip)
    {
        // Create a new AnimationEvent
        AnimationEvent animEvent = new AnimationEvent();

        // Set the time of the event (convert frame to time)
        float timeAtFrame = (float)frame / animationClip.frameRate;
        animEvent.time = timeAtFrame;

        // Set the function to call
        animEvent.functionName = functionToCall;

        // Add the event to the animation clip
        animationClip.AddEvent(animEvent);

        LethalMin.Logger.LogInfo($"Added event at frame {frame} (time: {timeAtFrame}) for {animationClip.name}");
    }

    public static Vector3 GetPositionOffsetedOnGround(Vector3 Position)
    {
        RaycastHit hit;
        if (Physics.Raycast(Position, Vector3.down, out hit, LethalMin.PikminColideable))
        {
            return hit.point;
        }
        return Position;
    }

    public static string GenerateRandomString(System.Random RNG)
    {
        int length = RNG.Next(4, 8);
        const string chars = LethalMin.FullEnglishAlhabet + LethalMin.FullNumbers;
        return new string(Enumerable.Repeat(chars, length)
         .Select(s => s[RNG.Next(0, s.Length)]).ToArray());
    }

    public static string NullableName(MonoBehaviour? mono)
    {
        return mono?.gameObject.name ?? "<null>";
    }

    public static string NullableName(NetworkBehaviour? mono)
    {
        return mono?.gameObject.name ?? "<null>";
    }

    public static string ParseListToString<T>(IList<T> list, string delimiter = ", ")
    {
        if (list == null || list.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            result.Append(list[i]);
            if (i < list.Count - 1)
            {
                result.Append(delimiter);
            }
        }

        return result.ToString();
    }

    public static Leader GetLeaderFromMultiplePikmin(List<PikminAI> pikmin)
    {
        return GetLeaderFromMultiplePikmin(pikmin.ToArray());
    }
    public static Leader GetLeaderFromMultiplePikmin(PikminAI[] pikmin)
    {
        if (pikmin == null || pikmin.Length == 0)
        {
            return PikminManager.instance.Leaders[0];
        }

        Dictionary<Leader, int> leaderCounts = new Dictionary<Leader, int>();

        foreach (PikminAI p in pikmin)
        {
            if (p != null && p.leader != null)
            {
                if (leaderCounts.ContainsKey(p.leader))
                {
                    leaderCounts[p.leader]++;
                }
                else
                {
                    leaderCounts[p.leader] = 1;
                }
            }
            else if (p != null && p.previousLeader != null)
            {
                if (leaderCounts.ContainsKey(p.previousLeader))
                {
                    leaderCounts[p.previousLeader]++;
                }
                else
                {
                    leaderCounts[p.previousLeader] = 1;
                }
            }
        }

        if (leaderCounts.Count == 0)
        {
            return PikminManager.instance.Leaders[0];
        }

        return leaderCounts.OrderByDescending(x => x.Value).First().Key;
    }

    public static Vector3 GetLastPathablePoint(NavMeshAgent agent)
    {
        if (agent == null)
            return Vector3.zero;
        if (!agent.hasPath || agent.path.corners.Length == 0)
            return agent.destination;

        return agent.path.corners[agent.path.corners.Length - 1];
    }

    public static float GetDistanceToLastPathablePoint(NavMeshAgent agent)
    {
        if (agent == null || !agent.hasPath || agent.path.corners.Length == 0)
            return 0f;

        Vector3 lastPoint = agent.path.corners[agent.path.corners.Length - 1];
        return Vector3.Distance(agent.transform.position, lastPoint);
    }

    public static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = b.x - a.x;
        float dz = b.z - a.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static int CalculatePikminItemWeight(GrabbableObject item)
    {
        return CalculatePikminItemWeight(item.itemProperties);
    }
    public static int CalculatePikminItemWeight(Item Iprops)
    {
        if (!LethalMin.UseBetaItemWeightCalculation.InternalValue)
        {
            float weight = Mathf.RoundToInt(Mathf.Clamp(Iprops.weight - 1f, 0f, 100f) * 105f);
            float DevidedWeight = weight / 2.5f;
            LethalMin.Logger.LogDebug($"Calculated weight for {Iprops.itemName}: {weight} lb, Devided weight: {DevidedWeight} lb");
            return Mathf.Clamp((int)DevidedWeight, 1, int.MaxValue);
        }
        else
        {
            return Mathf.Max(
            (Iprops.weight - 1f) * 100f <= 3f ? 1 :
            Mathf.CeilToInt(((Iprops.weight - 1f) * 100f - 3f) / 10f) + 1, 1);
        }
    }

    public static EnemyAI ReviveEnemy(EnemyAI ai, Vector3 RevivePos)
    {
        ai.NetworkObject.Despawn();
        GameObject gameObject = UnityEngine.Object.Instantiate(ai.enemyType.enemyPrefab, RevivePos, Quaternion.Euler(new Vector3(0f, 0f, 0f)));
        gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
        RoundManager.Instance.SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
        return gameObject.GetComponent<EnemyAI>();
    }

    public static void RevivePlayer(PlayerControllerB player, Vector3 RevivePos)
    {
        int index = StartOfRound.Instance.allPlayerScripts.ToList().IndexOf(player);
        bool IsPlayer = StartOfRound.Instance.localPlayerController == player;

        if (!player.isPlayerDead && !player.isPlayerControlled)
        {
            LethalMin.Logger.LogWarning($"Player #{index} is not dead and not controlled, skipping revive.");
            return;
        }

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} A");
        //player.ResetPlayerBloodObjects(player.isPlayerDead);
        player.isClimbingLadder = false;
        player.clampLooking = false;
        player.inVehicleAnimation = false;
        player.disableMoveInput = false;
        player.ResetZAndXRotation();
        player.thisController.enabled = true;
        player.health = 10;
        player.hasBeenCriticallyInjured = false;
        player.disableLookInput = false;
        player.disableInteract = false;

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} B");
        player.isPlayerDead = false;
        player.isPlayerControlled = true;
        player.isInElevator = true;
        player.isInHangarShipRoom = true;
        player.isInsideFactory = false;
        player.parentedToElevatorLastFrame = false;
        player.overrideGameOverSpectatePivot = null;
        StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
        player.TeleportPlayer(RevivePos);
        player.setPositionOfDeadPlayer = false;
        player.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[index], enable: true, disableLocalArms: true);
        player.helmetLight.enabled = false;

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} C");
        player.Crouch(crouch: false);
        player.criticallyInjured = false;
        player.playerBodyAnimator?.SetBool("Limp", value: false);
        player.bleedingHeavily = false;
        player.activatingItem = false;
        player.twoHanded = false;
        player.inShockingMinigame = false;
        player.inSpecialInteractAnimation = false;
        player.freeRotationInInteractAnimation = false;
        player.disableSyncInAnimation = false;
        player.inAnimationWithEnemy = null;
        player.holdingWalkieTalkie = false;
        player.speakingToWalkieTalkie = false;

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} D");
        player.isSinking = false;
        player.isUnderwater = false;
        player.sinkingValue = 0f;
        player.statusEffectAudio.Stop();
        player.DisableJetpackControlsLocally();
        player.health = 10;

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} E");
        player.mapRadarDotAnimator.SetBool("dead", value: false);
        player.externalForceAutoFade = Vector3.zero;
        if (player.IsOwner)
        {
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
            player.hasBegunSpectating = false;
            HUDManager.Instance.RemoveSpectateUI();
            HUDManager.Instance.HideHUD(false);
            HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
            player.JumpToFearLevel(1);
            player.hinderedMultiplier = 1f;
            player.isMovementHindered = 0;
            player.sourcesCausingSinking = 0;
            player.sprintMeter = 0;
            HUDManager.Instance.UpdateHealthUI(10, hurtPlayer: false);
            LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} E2");

            //Moved into here because we aren't doing this for every player
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            SoundManager.Instance.earsRingingTimer = 0f;

            //player.reverbPreset = StartOfRound.Instance.shipReverb;
        }

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} F");
        player.voiceMuffledByEnemy = false;
        SoundManager.Instance.playerVoicePitchTargets[index] = 1f;
        SoundManager.Instance.SetPlayerPitch(1f, index);
        if (player.currentVoiceChatIngameSettings == null)
        {
            StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
        }
        if (player.currentVoiceChatIngameSettings != null)
        {
            if (player.currentVoiceChatIngameSettings.voiceAudio == null)
            {
                player.currentVoiceChatIngameSettings.InitializeComponents();
            }
            if (player.currentVoiceChatIngameSettings.voiceAudio != null)
            {
                player.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
            }
        }

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} G");
        player.bleedingHeavily = false;
        player.criticallyInjured = false;
        player.playerBodyAnimator?.SetBool("Limp", value: false);
        player.health = 10;
        player.spectatedPlayerScript = null;

        LethalMin.Logger.LogInfo($"Reviving {player.playerUsername} H");
        //StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: false, player);
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (RagdollGrabbableObject ragdoll in UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>())
            {
                if (ragdoll.ragdoll.playerObjectId != (int)player.playerClientId)
                {
                    continue;
                }
                if (ragdoll.playerHeldBy != null)
                {
                    ragdoll.playerHeldBy.DropAllHeldItemsClientRpc();
                }
                ragdoll.NetworkObject.Despawn(true);
            }
        }
        StartOfRound.Instance.livingPlayers++;
        StartOfRound.Instance.UpdatePlayerVoiceEffects();
        //StartOfRound.Instance.ResetMiscValues();
        UpdateBoxesForRevivedPlayerUI();
    }

    public static void UpdateBoxesForRevivedPlayerUI()
    {
        PlayerControllerB playerScript;
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            playerScript = StartOfRound.Instance.allPlayerScripts[i];
            if (!playerScript.isPlayerDead)
            {
                if (!PikChecks.IsPlayerConnected(playerScript))
                {
                    //LethalMin.Logger.LogInfo($"{playerScript.playerUsername} is not connected, skipping box update");
                    continue;
                }
                if (!HUDManager.Instance.spectatingPlayerBoxes.Values.Contains(playerScript))
                {
                    //LethalMin.Logger.LogInfo($"{playerScript.playerUsername} is not dead, skipping box update");
                    continue;
                }
                LethalMin.Logger.LogInfo($"{playerScript.playerUsername} Removing player spectate box since they revived");
                Animator key = HUDManager.Instance.spectatingPlayerBoxes.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == playerScript).Key;
                if (key.gameObject.activeSelf)
                {
                    for (int j = 0; j < HUDManager.Instance.spectatingPlayerBoxes.Count; j++)
                    {
                        RectTransform component = HUDManager.Instance.spectatingPlayerBoxes.ElementAt(j).Key.gameObject.GetComponent<RectTransform>();
                        if (component.anchoredPosition.y <= -70f * (float)HUDManager.Instance.boxesAdded + 1f)
                        {
                            component.anchoredPosition = new Vector2(component.anchoredPosition.x, component.anchoredPosition.y + 70f);
                        }
                    }
                    HUDManager.Instance.yOffsetAmount += 70f;
                }
                HUDManager.Instance.spectatingPlayerBoxes.Remove(key);
                UnityEngine.Object.Destroy(key.gameObject);
            }
        }
    }

    public static PikminVehicleController? GetLeaderInCar(Leader? leader)
    {
        if (leader == null || leader.Controller == null)
        {
            return null;
        }

        if (PikminManager.instance.Vehicles.Count == 0)
        {
            return null;
        }

        //Have to comment out passenger check because 
        //zeekees made it so the current passenger is not set on the server side 
        //Thanks zeekees :DDDDDDD

        if (PikminManager.instance.Vehicles.First().controller.currentDriver == leader.Controller)
        //|| PikminManager.instance.Vehicles.First().controller.currentPassenger == leader.Controller)
        {
            return PikminManager.instance.Vehicles.First();
        }

        foreach (PikminVehicleController vehicle in PikminManager.instance.Vehicles)
        {
            if (vehicle.controller.currentDriver == leader.Controller)
            //|| vehicle.controller.currentPassenger == leader.Controller)
            {
                return vehicle;
            }
        }

        return null;
    }

    public static void ShakeNearbyPlayers(ScreenShakeType type, Vector3 Position, float Range)
    {
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.IsOwner)
            {
                continue;
            }
            if (Vector3.Distance(player.transform.position, Position) <= Range)
            {
                HUDManager.Instance.ShakeCamera(type);
            }
        }
    }

    public static void StunNearbyEnemies(Vector3 Position, float Range, float StunTime)
    {
        foreach (EnemyAI enemy in PikminManager.instance.EnemyAIs)
        {
            if (enemy == null || enemy.isEnemyDead || enemy.enemyType == LethalMin.PikminEnemyType || !enemy.enemyType.canBeStunned)
            {
                continue;
            }

            if (Vector3.Distance(enemy.transform.position, Position) <= Range)
            {
                enemy.SetEnemyStunned(true, StunTime);
                LethalMin.Logger.LogInfo($"Stunned enemy {enemy.gameObject.name} at {enemy.transform.position} for {StunTime} seconds");
            }
        }
    }

    public static void ReorganizeNetworkBehaviours(NetworkObject networkObject)
    {
        if (networkObject == null)
        {
            return;
        }
        foreach (NetworkBehaviour behaviour in networkObject.ChildNetworkBehaviours)
        {
            behaviour.NetworkBehaviourId = (ushort)networkObject.ChildNetworkBehaviours.IndexOf(behaviour);
        }
    }

    public static T? CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        Component copy = destination.AddComponent(type);
        string originalName = destination.name;

        // Copy fields
        System.Reflection.FieldInfo[] fields = type.GetFields();
        foreach (System.Reflection.FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }

        // Copy properties that can be written to
        System.Reflection.PropertyInfo[] props = type.GetProperties();
        foreach (System.Reflection.PropertyInfo prop in props)
        {
            if (prop.CanWrite && prop.CanRead)
            {
                try
                {
                    prop.SetValue(copy, prop.GetValue(original, null), null);
                }
                catch { /* Some properties might throw exceptions */ }
            }
        }

        destination.name = originalName;

        return copy as T;
    }

    public static List<EnemyAI> GetAliveEnemiesNearPosition(Vector3 position, float radius)
    {
        List<EnemyAI> aliveEnemies = new List<EnemyAI>();

        foreach (EnemyAI enemy in PikminManager.instance.EnemyAIs)
        {
            if (enemy != null && !enemy.isEnemyDead && Vector3.Distance(enemy.transform.position, position) < radius)
            {
                aliveEnemies.Add(enemy);
            }
        }

        return aliveEnemies;
    }

    public static GameObject CreateDebugCube(Vector3 Pos)
    {
        GameObject go = CreateDebugCube(Color.white);
        go.transform.position = Pos;
        return go;
    }
    public static GameObject CreateDebugCube(Color color = new Color())
    {
        GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugCube.GetComponent<Collider>().enabled = false;
        debugCube.gameObject.GetComponent<Renderer>().material = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Materials/MapDotA.mat");
        debugCube.GetComponent<Renderer>().material.color = color;
        return debugCube;
    }

    /// <summary>
    /// Finds the nearest instance of a specified type to a given position.
    /// </summary>
    /// <typeparam name="T">The type of object to find</typeparam>
    /// <param name="position">The reference position to measure from</param>
    /// <param name="maxDistance">Optional maximum search distance, default is infinity</param>
    /// <param name="specificInstances">Optional array of specific instances to search through instead of finding all in scene</param>
    /// <returns>The nearest instance of type T or null if none found within the max distance</returns>
    public static T? GetClosestInstanceOfClassToPosition<T>(Vector3 position, float maxDistance = float.PositiveInfinity, IEnumerable<T>? specificInstances = null) where T : MonoBehaviour
    {
        T[] instances = specificInstances?.ToArray() ?? UnityEngine.Object.FindObjectsOfType<T>();

        if (instances == null || instances.Length == 0)
        {
            return null;
        }

        T? nearestInstance = null;
        float nearestDistance = maxDistance;

        foreach (T instance in instances)
        {
            if (instance == null || instance.gameObject == null)
                continue;

            float distance = Vector3.Distance(position, instance.transform.position);

            if (distance < nearestDistance)
            {
                nearestInstance = instance;
                nearestDistance = distance;
            }
        }

        return nearestInstance;
    }

    public static void AddTextToChangeOnLocalClient(string chatMessage, string nameOfUserWhoTyped = "")
    {
        HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat, 4f);
        if (HUDManager.Instance.ChatMessageHistory.Count >= 4)
        {
            HUDManager.Instance.chatText.text.Remove(0, HUDManager.Instance.ChatMessageHistory[0].Length);
            HUDManager.Instance.ChatMessageHistory.Remove(HUDManager.Instance.ChatMessageHistory[0]);
        }
        StringBuilder stringBuilder = new StringBuilder(chatMessage);
        stringBuilder.Replace("[playerNum0]", StartOfRound.Instance.allPlayerScripts[0].playerUsername);
        stringBuilder.Replace("[playerNum1]", StartOfRound.Instance.allPlayerScripts[1].playerUsername);
        stringBuilder.Replace("[playerNum2]", StartOfRound.Instance.allPlayerScripts[2].playerUsername);
        stringBuilder.Replace("[playerNum3]", StartOfRound.Instance.allPlayerScripts[3].playerUsername);
        chatMessage = stringBuilder.ToString();
        string item = ((!string.IsNullOrEmpty(nameOfUserWhoTyped)) ? ("<color=#FF0000>" + nameOfUserWhoTyped + "</color>: <color=#FFFF00>'" + chatMessage + "'</color>") : ("<color=#7069ff>" + chatMessage + "</color>"));
        HUDManager.Instance.ChatMessageHistory.Add(item);
        HUDManager.Instance.chatText.text = "";
        for (int i = 0; i < HUDManager.Instance.ChatMessageHistory.Count; i++)
        {
            TextMeshProUGUI textMeshProUGUI = HUDManager.Instance.chatText;
            textMeshProUGUI.text = textMeshProUGUI.text + "\n" + HUDManager.Instance.ChatMessageHistory[i];
        }
    }
}
