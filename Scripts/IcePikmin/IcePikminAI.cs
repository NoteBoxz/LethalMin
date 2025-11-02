using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine.AI;
using GameNetcodeStuff;

namespace LethalMin
{
    public class IcePikminAI : PikminAI
    {
        public FreezeableWater? CurWaterIn = null;
        public Transform? IceSnapPos = null;
        public AnimationClip FloatingAnim = null!;
        public AnimationClip FrozenAnim = null!;
        public bool Floating => CurWaterIn != null && !CurWaterIn.IsFrozen;
        public bool Frozen => CurWaterIn != null && CurWaterIn.IsFrozen && CurWaterIn.PikminInWater.Contains(this);

        public override void Start()
        {
            base.Start();
            GameObject go = GetComponentInChildren<PikminCollisionDetect>().gameObject;
            animController.AddAnimationCondition(() => Floating, FloatingAnim, 45);
            animController.AddAnimationCondition(() => Frozen, FrozenAnim, 49);
            if (go != null)
            {
                go.AddComponent<IcePikminCollisionDetect>().mainPikmin = this;
            }
            else
            {
                LethalMin.Logger.LogError($"{gameObject.name}: PikminCollisionDetect not found in children!");
            }
        }

        public override void Update()
        {
            base.Update();
            ForceSproutGlow = Frozen;
        }

        public override void UpdateLocalSnapping()
        {
            base.UpdateLocalSnapping();
            if (IceSnapPos != null)
            {
                transform.position = IceSnapPos.position;
            }
        }

        public override void KillEnemy(bool destroy = false)
        {
            base.KillEnemy(destroy);
            if (Invincible)
            {
                return;
            }
            if (destroy && IsServer)
            {
                return;
            }

            foreach (PikminEnemy Penemy in PikminManager.instance.PikminEnemies)
            {
                if (Vector3.Distance(transform.position, Penemy.transform.position) < 5)
                {
                    Penemy.FreezeCounter += 1.0f;
                }
            }
        }

        public override void DoAIInterval()
        {
            if (CurWaterIn == null)
            {
                base.DoAIInterval();
                return;
            }

            if (currentBehaviourStateIndex == IDLE && !IsDeadOrDying && !IsAirborn)
            {
                if (!CurWaterIn.IsFrozen && !CurWaterIn.PikminInWater.Contains(this))
                    AddToWaterServerRpc(PikminManager.instance.FreezeableWaters.IndexOf(CurWaterIn));
                return;
            }
            //else
            if (CurWaterIn.PikminInWater.Contains(this))
            {
                if (CurWaterIn.IsFrozen)
                {
                    RemoveFromCurWaterServerRpc(PikminManager.instance.FreezeableWaters.IndexOf(CurWaterIn));
                }
                else
                {
                    RemoveFromWaterServerRpc(PikminManager.instance.FreezeableWaters.IndexOf(CurWaterIn));
                }
            }

            base.DoAIInterval();
        }

        [ServerRpc]
        public void AddToWaterServerRpc(int WaterID)
        {
            AddToWaterClientRpc(WaterID);
        }
        [ClientRpc]
        public void AddToWaterClientRpc(int WaterID)
        {
            AddToWater(WaterID);
        }
        public void AddToWater(int WaterID)
        {
            if (!PikminManager.instance.FreezeableWaters[WaterID].PikminInWater.Contains(this))
                PikminManager.instance.FreezeableWaters[WaterID].PikminInWater.Add(this);
        }

        [ServerRpc]
        public void RemoveFromWaterServerRpc(int WaterID)
        {
            RemoveFromWaterClientRpc(WaterID);
        }
        [ClientRpc]
        public void RemoveFromWaterClientRpc(int WaterID)
        {
            RemoveFromWater(WaterID);
        }
        public void RemoveFromWater(int WaterID)
        {
            if (PikminManager.instance.FreezeableWaters[WaterID].PikminInWater.Contains(this))
                PikminManager.instance.FreezeableWaters[WaterID].PikminInWater.Remove(this);
            Invincible = false;
        }


        public void EnterWater(FreezeableWater water)
        {
            CurWaterIn = water;
        }

        public void ExitWater()
        {
            if (!Frozen)
                RemoveFromCurWater();
        }

        [ServerRpc]
        public void RemoveFromCurWaterServerRpc(int WaterID)
        {
            RemoveFromCurWaterClientRpc(WaterID);
        }
        [ClientRpc]
        public void RemoveFromCurWaterClientRpc(int WaterID)
        {
            RemoveFromCurWater(WaterID);
        }

        public void RemoveFromCurWater(int WaterID = 0)
        {
            if (IsOwner)
            {
                if (CurWaterIn != null)
                {
                    if (CurWaterIn.PikminInWater.Contains(this))
                        CurWaterIn.PikminInWater.Remove(this);
                    LethalMin.Logger.LogInfo($"{gameObject.name}: Removed from water {CurWaterIn.name} on owner client.");
                }
            }
            else
            {
                FreezeableWater water = PikminManager.instance.FreezeableWaters[WaterID];
                if (water.PikminInWater.Contains(this))
                    water.PikminInWater.Remove(this);
                LethalMin.Logger.LogInfo($"{gameObject.name}: Removed from water {water.name} on non-owner client.");
            }
            if (IceSnapPos != null)
            {
                SetCollisionMode(1);
                Destroy(IceSnapPos.gameObject);
            }
            Invincible = false;
            CurWaterIn = null;
        }
    }
}