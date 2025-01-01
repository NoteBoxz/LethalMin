using ElevatorMod.Patches;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMin.Patches.OtherMods
{
    [HarmonyPatch(typeof(EndlessElevator))]
    public static class EndlessElevatorPatch
    {
        class ElevatorPikmin
        {
            public Vector3 position;
            public Quaternion rotation;
            public PikminType type;
            public PlayerControllerB leader;
            public int GrowStage;
            public string Pname;

            public ElevatorPikmin(PikminAI pikmin)
            {
                position = pikmin.transform.position;
                rotation = pikmin.transform.rotation;
                type = pikmin.PminType;
                if (pikmin.currentLeader != null)
                    leader = pikmin.currentLeader.Controller;
                GrowStage = pikmin.GrowStage;
                Pname = pikmin.name;
            }
            public void SpawnPikmin()
            {
                LethalMin.Logger.LogInfo($"Spawning {Pname} from elevator.");
                if (leader == null)
                {
                    LethalMin.Logger.LogWarning($"Leader is null, spawning {Pname} without leader.");
                    PikminManager.Instance.SpawnInPikminServerRpc(position, rotation, new NetworkObjectReference(), GrowStage, type.PikminTypeID, false, Pname);
                }
                else
                {
                    PikminManager.Instance.SpawnInPikminServerRpc(position, rotation, leader.NetworkObject, GrowStage, type.PikminTypeID, false, Pname);
                }
            }
        }

        public static List<PikminAI> PikminInElevator = new List<PikminAI>();
        static List<ElevatorPikmin> PikminToSave = new List<ElevatorPikmin>();
        public static Transform ElevatorPos;
        public static bool DontDoTriggerPatch = false;
        public static Collider PikminZone;
        private static InputAction debugWarpAction;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPostfix(EndlessElevator __instance, ref PlayerPhysicsRegion ___playerPhysicsRegion_elevator)
        {
            if (__instance.IsServer)
            {
                __instance.OnDoneGenerate.AddListener(RespawnPikmin);
                __instance.OnStopMove.AddListener(WarpPikminOnElevatorStop);
            }

            ElevatorPos = ___playerPhysicsRegion_elevator.transform;

            PikminZone = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Collider>();
            PikminZone.transform.SetParent(ElevatorPos, false);
            PikminZone.transform.localPosition = new Vector3(0, 2, 0);
            PikminZone.transform.localScale = new Vector3(4.7964f, 3.1f, 4.9f);
            PikminZone.transform.SetParent(__instance.elevatorInteriorAnimator.transform);
            PikminZone.name = "Pikmin Elevator Trigger";
            PikminZone.GetComponent<Collider>().isTrigger = true;
            GameObject.Destroy(PikminZone.GetComponent<Renderer>());

            //PikminZone.GetComponent<Renderer>().material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
            //Visualize the elevator's coliders.
            // foreach (var item in __instance.GetComponents<BoxCollider>())
            // {
            //     GameObject cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //     Renderer renderer = cubeA.GetComponent<Renderer>();
            //     renderer.material = AssetLoader.LoadAsset<Material>("Assets/LethalminAssets/Pikmin/Materials/DebugMin.mat");
            //     renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            //     renderer.material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            //     cubeA.transform.localScale = Vector3.Scale(item.size, item.transform.lossyScale);
            //     cubeA.transform.position = item.transform.TransformPoint(item.center);
            //     cubeA.transform.SetParent(item.transform, true);
            //     GameObject.Destroy(cubeA.GetComponent<Collider>());
            // }

            // debugWarpAction = new InputAction("DebugWarpPikmin", InputActionType.Button, "<Keyboard>/f12");
            // debugWarpAction.performed += ctx => WarpPikminToLeader();
            // debugWarpAction.Enable();
        }

        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        public static void OnTriggerEnterPatch(Collider other)
        {
            //DISABLED UNTIL DEV FIXES COLIDERS
            if (DontDoTriggerPatch) { return; }

            // PikminAI Componet = other.GetComponentInParent<PikminAI>();
            // if (Componet != null && !PikminInElevator.Contains(Componet))
            // {
            //     PikminInElevator.Add(Componet);
            //     //LethalMin.Logger.LogInfo("Pikmin entered the elevator: " + Componet.name);
            // }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        public static void OnTriggerExitPatch(Collider other)
        {
            //DISABLED UNTIL DEV FIXES COLIDERS
            if (DontDoTriggerPatch) { return; }

            // PikminAI Componet = other.GetComponentInParent<PikminAI>();
            // if (Componet != null && PikminInElevator.Contains(Componet))
            // {
            //     PikminInElevator.Remove(Componet);
            //     LethalMin.Logger.LogInfo("Pikmin exited the elevator: " + Componet.name);
            // }
        }

        [HarmonyPatch("GenerateNewFloor")]
        [HarmonyPrefix]
        public static void GenerateNewFloorPatch(EndlessElevator __instance)
        {
            if (__instance.firstTimeElevator)
            {
                LethalMin.Logger.LogInfo("First time elevator, not saving Pikmin.");
            }

            if (!__instance.IsServer || __instance.firstTimeElevator) { return; }

            DontDoTriggerPatch = true;
            PikminInElevator.Clear();

            LethalMin.Logger.LogInfo("Saving Pikmin in elevator.");

            foreach (var item in GameObject.FindObjectsOfType<PikminAI>())
            {
                if (PikminZone.bounds.Contains(item.transform.position))
                {
                    PikminInElevator.Add(item);
                    LethalMin.Logger.LogInfo("Pikmin found in elevator: " + item.name);
                }
            }

            foreach (GameObject item in __instance.playersInElevator)
            {
                LeaderManager lm = item.GetComponentInChildren<LeaderManager>();

                if (lm != null)
                {
                    foreach (PikminAI pikmin in lm.followingPikmin)
                    {
                        if (!PikminInElevator.Contains(pikmin))
                        {
                            if (pikmin == null) { continue; }

                            float formerStop = pikmin.agent.stoppingDistance;
                            pikmin.agent.stoppingDistance = 0;
                            pikmin.agent.Warp(item.transform.position);
                            pikmin.transform2.Teleport(item.transform.position, pikmin.transform.rotation, pikmin.transform.localScale);
                            pikmin.agent.stoppingDistance = formerStop;
                            PikminInElevator.Add(pikmin);
                            LethalMin.Logger.LogInfo("Added Pikmin to list: " + pikmin.name);
                        }
                    }
                }
            }

            foreach (PikminAI min in PikminInElevator)
            {
                if (min == null) { continue; }
                ElevatorPikmin Emin = new ElevatorPikmin(min);
                if (PikminToSave.Contains(Emin)) { continue; }
                LethalMin.Logger.LogInfo("Saving Pikmin: " + min.name);
                PikminToSave.Add(Emin);
            }

            DontDoTriggerPatch = false;
        }

        public static void RespawnPikmin()
        {
            LethalMin.Logger.LogInfo($"Respawning: {PikminToSave.Count} Pikmin from elevator.");
            foreach (var pikmin in PikminToSave)
            {
                pikmin.SpawnPikmin();
            }
            PikminToSave.Clear();
        }

        public static void WarpPikminOnElevatorStop()
        {
            EndlessElevator __instance = GameObject.FindObjectOfType<EndlessElevator>();

            foreach (var item in __instance.playersInElevator)
            {
                LeaderManager lm = item.GetComponentInChildren<LeaderManager>();
                if (lm != null)
                {
                    foreach (var pikmin in lm.followingPikmin)
                    {
                        if (pikmin == null) { continue; }
                        float formerStop = pikmin.agent.stoppingDistance;
                        pikmin.agent.stoppingDistance = 0;
                        pikmin.agent.Warp(item.transform.position);
                        pikmin.transform2.Teleport(item.transform.position, pikmin.transform.rotation, pikmin.transform.localScale);
                        pikmin.agent.stoppingDistance = formerStop;
                    }
                }
            }
        }
        public static void WarpPikminToLeader()
        {
            foreach (PikminAI Pikmin in PikminManager.GetPikminEnemies())
            {
                if (Pikmin.currentLeader != null)
                {
                    Pikmin.agent.Warp(Pikmin.currentLeader.transform.position);
                }
            }
        }
    }
}