using UnityEngine;

using System.Collections;
using Unity.Netcode;
using LethalMin.Utils;
using LethalMin.Pikmin;
using GameNetcodeStuff;
using System.Linq;
using System.Threading.Tasks;

namespace LethalMin
{
    public class GlowPikminAI : PikminAI
    {
        public Glowmob? TargetGlowMob = null;
        public AudioClip TransformSeedSFX = null!;
        public AnimationClip ToGlowMobAnim = null!;
        public AnimationClip FromGlowMobAnim = null!;
        public AnimationClip WarpInAnim = null!;
        public AnimationClip WarpOutAnim = null!;
        public AudioClip[] FlashBangStartVoice = null!;
        public AudioClip[] BornBallVoice = null!;
        public AudioClip[] WarpStartSFX = null!;
        public AudioClip[] WarpEndSFX = null!;
        public bool HasDoneGlowMobAnim = false;
        public bool ShouldReturnToPreviousLeader = false;
        public bool ShouldTurnintoSeed = false;
        Coroutine? SetToRouteine = null;
        Coroutine? BurstRoutine = null;
        float initalGlowmobDistance = 10;

        public override void CallResetMethods(
        bool RemoveLeader = true,
        bool DropItem = true,
        bool RemoveEnemy = true,
        int CollisionMode = 1,
        bool Unlatch = true,
        bool RemoveTask = true,
        bool RemoveOverridePositions = true,
        bool SetLayingFalse = true)
        {
            base.CallResetMethods(RemoveLeader, DropItem, RemoveEnemy, CollisionMode, Unlatch, RemoveTask, RemoveOverridePositions);
            if (CollisionMode >= 0)
            {
                CancleGlowmob();
            }
        }

        public override void StartThrow()
        {
            base.StartThrow();
            CancleGlowmob();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (TargetGlowMob != null)
            {
                TargetGlowMob.PikminInGlowmob.Remove(this);
            }
            TargetGlowMob = null!;
        }

        public override void Start()
        {
            base.Start();

            animController.AddAnimationCondition(() => TargetGlowMob, ToGlowMobAnim, 50);

            if (SpawnProps.SpawnSound == "BornB")
            {
                creatureVoice.PlayOneShot(BornBallVoice[enemyRandom.Next(0, BornBallVoice.Length)]);
            }

            if (IsServer)
            {
                StartCoroutine(WaitForSpawn());
            }
        }

        IEnumerator WaitForSpawn()
        {
            yield return new WaitForSeconds(Random.Range(0.1f, 1.0f));

            if (Random.value <= LethalMin.GlowOddsToTurnIntoSeed || LethalMin.OnCompany)
            {
                ShouldTurnintoSeed = true;
            }
            else
            {
                ShouldTurnintoSeed = false;
            }
            SetShouldTurnintoSeedClientRpc(ShouldTurnintoSeed);
        }

        [ClientRpc]
        public void SetShouldTurnintoSeedClientRpc(bool Val)
        {
            ShouldTurnintoSeed = Val;
        }

        public override void Update()
        {
            base.Update();

            Unselectable = TargetGlowMob;

            if (!TargetGlowMob && HasDoneGlowMobAnim)
            {
                RevertGlowMobAnim();
            }
        }

        public override void UpdateLocalSnapping()
        {
            base.UpdateLocalSnapping();
            if (!agent.enabled && TargetGlowMob != null)
            {
                if (leader == null)
                {
                    CancleGlowmob();
                    return;
                }
                if (!TargetGlowMob.PikminInGlowmob.Contains(this))
                {
                    float distance = Vector3.Distance(transform.position, TargetGlowMob.transform.position);
                    float speedFactor = Mathf.Lerp(50f, 10f, Mathf.Clamp01(distance / initalGlowmobDistance));
                    //LethalMin.Logger.LogInfo($"{gameObject.name}: {speedFactor}");
                    transform.position = Vector3.Lerp(transform.position, TargetGlowMob.transform.position, Time.deltaTime * speedFactor);
                    transform.LookAt(TargetGlowMob.transform);
                    if (Vector3.Distance(transform.position, TargetGlowMob.transform.position) < 5f && !HasDoneGlowMobAnim && SetToRouteine == null)
                    {
                        SetToRouteine = StartCoroutine(GlowMobAnim());
                    }
                    if (Vector3.Distance(transform.position, TargetGlowMob.transform.position) < 1f)
                    {
                        TargetGlowMob.PikminInGlowmob.Add(this);
                        LethalMin.Logger.LogDebug($"{gameObject.name}: added to {TargetGlowMob.gameObject.name}");
                    }
                }
                else
                {
                    transform.position = TargetGlowMob.transform.position;
                    transform.rotation = TargetGlowMob.transform.rotation;
                }
            }
        }

        public override void HandleIdleStateOnOwnerClient()
        {
            if (CurrentIntention == Pintent.Idle && ShouldReturnToPreviousLeader && previousLeader != null)
            {
                if (leader != null)
                {
                    LethalMin.Logger.LogInfo($"{gameObject.name}: {leader.gameObject.name} is not null, setting ShouldReturnToPreviousLeader to false");
                    ShouldReturnToPreviousLeader = false;
                    base.HandleIdleStateOnOwnerClient();
                    return;
                }
                if (!Lumiknull.TimeForGlowPikminToExist && !previousLeader.Controller.isInsideFactory)
                {
                    LethalMin.Logger.LogWarning($"{gameObject.name}: Glow pikmin follow outside before {LethalMin.LumiknullActivateTime.InternalValue} outside");
                    ShouldReturnToPreviousLeader = false;
                    base.HandleIdleStateOnOwnerClient();
                    return;
                }
                ReassignLeaderServerRpc(previousLeader.NetworkObject);
                ReassignLeader(previousLeader);
                return;
            }
            base.HandleIdleStateOnOwnerClient();
        }

        public override PikminItem? GetClosestPikminItem(float overrideDetectionRadius = -1)
        {
            PikminItem? itm = base.GetClosestPikminItem(overrideDetectionRadius);

            if (Lumiknull.TimeForGlowPikminToExist)
            {
                return itm;
            }
            
            return null;
        }

        public override void AssignLeader(Leader leader, bool SwitchState = true, bool PlayAnim = true)
        {
            base.AssignLeader(leader, SwitchState, PlayAnim);
            ShouldReturnToPreviousLeader = false;
            if (this.leader != null && this.leader.glowmob == null && IsServer)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name}: {this.leader.gameObject.name} has no glowmob, spawning one");
                this.leader.SpawnGlowMob(leader.OwnerClientId);
            }
            if (this.leader != null && this.leader.glowmob != null && this.leader.glowmob.IsDoingGlowmob)
            {
                LethalMin.Logger.LogInfo($"{gameObject.name}: {this.leader.gameObject.name} has glowmob active after assign, setting to glowmod");
                SetToGlowMod(this.leader.glowmob);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReassignLeaderServerRpc(NetworkObjectReference LeaderRef)
        {
            Leader? leader = null;
            NetworkObject? leaderObj;
            if (LeaderRef.TryGet(out leaderObj)
            && leaderObj.TryGetComponent(out leader) && leader != null)
            {
                if (leader.OwnerClientId != OwnerClientId)
                {
                    ChangeOwnershipOfEnemy(leader.OwnerClientId);
                }

                ReassignLeaderClientRpc(LeaderRef);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to Reassign leader to {leaderObj?.name}");
                if (leader == null)
                    LethalMin.Logger.LogError($"{DebugID}: Leader is null");
                if (leaderObj == null)
                    LethalMin.Logger.LogError($"{DebugID}: LeaderObj is null");
            }
        }
        [ClientRpc]
        public void ReassignLeaderClientRpc(NetworkObjectReference LeaderRef)
        {
            if (IsOwner)
            {
                return;
            }

            Leader? leader = null;
            NetworkObject? leaderObj;
            if (LeaderRef.TryGet(out leaderObj)
            && leaderObj.TryGetComponent(out leader) && leader != null)
            {
                ReassignLeader(leader);
            }
            else
            {
                LethalMin.Logger.LogError($"{DebugID}: Failed to Reassign leader to {leaderObj?.name}");
                if (leader == null)
                    LethalMin.Logger.LogError($"{DebugID}: Leader is null");
                if (leaderObj == null)
                    LethalMin.Logger.LogError($"{DebugID}: LeaderObj is null");
            }
        }

        public virtual void ReassignLeader(Leader leader)
        {
            StartCoroutine(TeleportTo(RoundManager.Instance.GetRandomNavMeshPositionInRadius(leader.transform.position, 5)));
            AssignLeader(leader, true, false);
            ShouldReturnToPreviousLeader = false;
            isOutside = !leader.Controller.isInsideFactory;
        }

        public override void SetToIdle()
        {
            base.SetToIdle();

            if (PreviousIntention != Pintent.Idle && PreviousIntention != Pintent.Thrown)
                StartCoroutine(WaitToReturn());
        }

        public IEnumerator WaitToReturn()
        {
            yield return new WaitForSeconds(1f);
            if (leader == null && previousLeader != null && previousLeader.Controller.isPlayerControlled)
            {
                ShouldReturnToPreviousLeader = true;
            }
        }

        public override void WarpToMatchLeaderDoors(bool isInside)
        {
            if (!Lumiknull.TimeForGlowPikminToExist && !isInside)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: Glow pikmin follow outside before {LethalMin.LumiknullActivateTime.InternalValue} outside");
                CancleGlowmob();
                SetToIdle();
                return;
            }
            base.WarpToMatchLeaderDoors(isInside);
        }

        public override void UseEntranceOnLocalClient(EntranceTeleport entrance, bool Inside, bool PlaySFX = true)
        {
            if (!Lumiknull.TimeForGlowPikminToExist && !Inside)
            {
                LethalMin.Logger.LogWarning($"{gameObject.name}: Glow pikmin carry item outside before {LethalMin.LumiknullActivateTime.InternalValue} outside");
                SetToIdle();
                return;
            }
            base.UseEntranceOnLocalClient(entrance, Inside, PlaySFX);
        }


        public void SetToGlowMod(Glowmob mob)
        {
            Invincible = true;
            TargetGlowMob = mob;
            SetCollisionMode(0);
            creatureVoice.PlayOneShot(FlashBangStartVoice[enemyRandom.Next(0, FlashBangStartVoice.Length)]);
            initalGlowmobDistance = Vector3.Distance(transform.position, TargetGlowMob.transform.position);
            LethalMin.Logger.LogDebug($"{gameObject.name}: set to {TargetGlowMob.gameObject.name}");
        }
        public void BurstFromGlowMob()
        {
            LethalMin.Logger.LogDebug($"{gameObject.name}: burst from {TargetGlowMob?.gameObject.name}");
            if (BurstRoutine != null)
            {
                StopCoroutine(BurstRoutine);
                BurstRoutine = null;
            }

            animController.PlayAnimation(FromGlowMobAnim.name);
            SetToIdle();
            SetCollisionMode(0);
            ChangeIntent(Pintent.Thrown);
            SetGrowth(0); // reset growth to 0 for burst
            BurstRoutine = StartCoroutine(GlowBurstAnim());
        }
        public void SetOffGlowMob(bool SetCollisionMod = true, bool KeepLeader = false)
        {
            if (SetCollisionMod)
            {
                ApplyPhysics(false);
                ShouldReturnToPreviousLeader = true;
            }
            else if (KeepLeader)
            {
                SetCollisionMode(1);
            }

            Invincible = false;


            if (TargetGlowMob != null)
            {
                TargetGlowMob.PikminInGlowmob.Remove(this);
            }
            TargetGlowMob = null!;

            LethalMin.Logger.LogDebug($"{gameObject.name}: set off glowmob");
        }


        public IEnumerator GlowBurstAnim()
        {
            Vector3 TargetPos = GetBurstFlyPos();
            Vector3 startingPos = transform.position;
            Vector3 startingScale = modelContainer.transform.localScale;
            Vector3 targetScale = Vector3.one;
            float duration = 0.5f;
            float elapsedTime = 0f;
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

            SetOffGlowMob(false);
            modelContainer.gameObject.SetActive(true);

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;

                // Calculate mid point that's pushed out from the direct path
                Vector3 directionToTarget = (TargetPos - startingPos).normalized;
                directionToTarget.y = 0f; // Keep it horizontal
                Vector3 midPoint = Vector3.Lerp(startingPos, TargetPos, 0.5f) + directionToTarget * 7.5f;

                // Quadratic Bezier curve calculation
                float oneMinusT = 1f - t;
                Vector3 curvedPosition = oneMinusT * oneMinusT * startingPos +
                                       2f * oneMinusT * t * midPoint +
                                       t * t * TargetPos;

                transform.LookAt(startingPos);
                transform.position = curvedPosition;
                modelContainer.transform.localScale = Vector3.Lerp(startingScale, targetScale, t * 2);
                elapsedTime += Time.deltaTime;
                yield return endOfFrame;
            }

            HasDoneGlowMobAnim = false;
            modelContainer.transform.localScale = Vector3.one;


            yield return new WaitForSeconds(0.15f - enemyRandom.Next(0, 10) * 0.01f);


            if (!IsOwner)
            {
                SetCollisionMode(1);
                yield break;
            }

            transform2.enabled = true;

            //OWNER ONLY RUNNING CODE\\

            PikminEnemy? enemy = GetClosestEnemy(25);

            if (enemy == null)
            {
                ApplyPhysics(true);
                ShouldReturnToPreviousLeader = true;
                LeaderAssesmentDelay = 0.5f;
                //LethalMin.Logger.LogWarning($"{gameObject.name}: no enemy found, applying physics");
                yield break;
            }

            if (!(pikminType.CanLatchOnToObjects
            && enemy != null
            && enemy.enemyScript.enemyType != enemyType
            && !enemy.enemyScript.isEnemyDead))
            {
                ApplyPhysics(true);
                ShouldReturnToPreviousLeader = true;
                LeaderAssesmentDelay = 0.5f;
                LethalMin.Logger.LogWarning($"{gameObject.name}: enemy is not latchable, applying physics");
                yield break;
            }

            PikminLatchTrigger trigger = null!;
            foreach (PikminLatchTrigger latchTrigger in enemy.LatchTriggers)
            {
                if (trigger == null || Vector3.Distance(latchTrigger.transform.position, transform.position) < Vector3.Distance(trigger.transform.position, transform.position))
                {
                    trigger = latchTrigger;
                }
            }

            if (trigger == null)
            {
                ApplyPhysics(true);
                ShouldReturnToPreviousLeader = true;
                LeaderAssesmentDelay = 0.5f;
                LethalMin.Logger.LogWarning($"{gameObject.name}: no latch trigger found, applying physics");
                yield break;
            }

            int Index = enemy.LatchTriggers.IndexOf(trigger);
            Vector3 approximateContactPoint = trigger.GetComponent<Collider>().ClosestPoint(TargetPos);

            if (enemy == null || !trigger.TryLatch(this, approximateContactPoint, true, true))
            {
                ApplyPhysics(true);
                ShouldReturnToPreviousLeader = true;
                LeaderAssesmentDelay = 0.5f;
                LethalMin.Logger.LogWarning($"{gameObject.name}: latch failed, applying physics");
                yield break;
            }

            float duration2 = 0.5f;
            float elapsedTime2 = 0f;
            while (elapsedTime2 < duration2)
            {
                float t = elapsedTime2 / duration2;
                transform.position = Vector3.Lerp(TargetPos, approximateContactPoint, t);
                transform.LookAt(startingPos);
                elapsedTime2 += Time.deltaTime;
                yield return endOfFrame;
            }

            yield return new WaitForSeconds(Random.Range(0.0f, 0.1f));

            LatchPikminOnToEnemy(enemy, trigger.GetComponent<Collider>().ClosestPoint(transform.position), Index);
            LatchPikminOnToEnemyServerRpc(enemy.NetworkObject, trigger.GetComponent<Collider>().ClosestPoint(transform.position), Index);
        }
        public Vector3 GetBurstFlyPos(float radius = 5)
        {
            if (TargetGlowMob == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: TargetGlowMob is null, cannot calculate burst position.");
                return new Vector3(transform.position.x, transform.position.y + 5, transform.position.z);
            }
            float angleStep = 30f;
            float startAngle = enemyRandom.Next(0, 360);

            // Calculate the spawn position on the circle
            float angle = startAngle + 100 * angleStep % 360f;
            float radian = angle * Mathf.Deg2Rad;
            float spawnX = Mathf.Sin(radian) * radius;
            float spawnZ = Mathf.Cos(radian) * radius;

            Vector3 airPosition = new Vector3(
                TargetGlowMob.transform.position.x + spawnX,
                TargetGlowMob.transform.position.y + 5f,
                TargetGlowMob.transform.position.z + spawnZ
            );

            if (Physics.Linecast(transform.position, airPosition, out RaycastHit hit, 8))
            {
                airPosition = hit.point + Vector3.down * 2f;
            }

            return airPosition;
        }


        public IEnumerator GlowMobAnim()
        {
            HasDoneGlowMobAnim = true;
            Vector3 startingScale = Vector3.one;
            Vector3 targetScale = new Vector3(0.001f, 0.001f, 0.001f);

            float duration = 1f;
            float elapsedTime = 0f;
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                modelContainer.transform.localScale = Vector3.Lerp(startingScale, targetScale, t);
                elapsedTime += Time.deltaTime;
                yield return endOfFrame;
            }
            modelContainer.gameObject.SetActive(false);

            modelContainer.transform.localScale = targetScale;
            SetToRouteine = null;
        }
        public void RevertGlowMobAnim()
        {
            if (!HasDoneGlowMobAnim)
            {
                return;
            }
            if (BurstRoutine != null)
            {
                return;
            }
            if (SetToRouteine != null)
            {
                StopCoroutine(SetToRouteine);
                SetToRouteine = null;
            }
            HasDoneGlowMobAnim = false;
            modelContainer.gameObject.SetActive(true);
            modelContainer.transform.localScale = Vector3.one;
        }


        public void CancleGlowmob(bool SetCollisionMod = false, bool KeepLeader = false)
        {
            SetOffGlowMob(SetCollisionMod, KeepLeader);
            if (SetToRouteine != null)
            {
                StopCoroutine(SetToRouteine);
                SetToRouteine = null;
            }
            if (BurstRoutine != null)
            {
                StopCoroutine(BurstRoutine);
                BurstRoutine = null;
            }
            RevertGlowMobAnim();
        }


        public override void SetPikminToLeaving(Onion? onion = null)
        {
            CallResetMethods();
            SwitchToBehaviourStateOnLocalClient(LEAVING);
            ChangeIntent(Pintent.Leave);

            if (LethalMin.OnCompany && ShouldTurnintoSeed)
            {
                agent.Warp(StartOfRound.Instance.shipInnerRoomBounds.transform.position);
                transform2.TeleportOnLocalClient(StartOfRound.Instance.shipInnerRoomBounds.transform.position);
            }
            if ((IsInShip || LethalMin.OnCompany) && ShouldTurnintoSeed)
            {
                SetCollisionMode(0);
                if (IsOwner)
                {
                    TurnIntoGlowSeedServerRpc();
                }
                return;
            }

            base.SetPikminToLeaving(onion);
        }


        [ServerRpc]
        public void TurnIntoGlowSeedServerRpc()
        {
            GlowSeed seed = Instantiate(LethalMin.GlowSeedPrefab, transform.position + (Vector3.up * 2), Quaternion.identity).GetComponent<GlowSeed>();
            seed.NetworkObject.Spawn();
            TurnIntoGlowSeedClientRpc(seed.NetworkObject);
        }
        [ClientRpc]
        public void TurnIntoGlowSeedClientRpc(NetworkObjectReference seedInstance)
        {
            if (seedInstance.TryGet(out NetworkObject SeedNobj) && SeedNobj.gameObject.TryGetComponent(out GlowSeed seed))
            {
                TurnIntoGlowSeed(seed);
            }
            else
            {
                LethalMin.Logger.LogError($"{gameObject.name}: Failed to turn into glow seed, seedInstance is null or does not have GlowSeed component");
            }
        }
        public void TurnIntoGlowSeed(GlowSeed seed)
        {
            PikminModelRefernces Mref = GetComponentInChildren<PikminModelRefernces>();
            GameObject BaseMesh = Mref.transform.Find("P4").gameObject;
            GameObject burstFX = Mref.transform.Find("BurstFX").gameObject;
            BaseMesh.SetActive(false);
            burstFX.SetActive(true);
            creatureSFX.PlayOneShot(TransformSeedSFX);

            Leader PrimaryLeader = null!;
            if (leader != null)
            {
                PrimaryLeader = leader;
            }
            else if (previousLeader != null)
            {
                PrimaryLeader = previousLeader;
            }
            else
            {
                PrimaryLeader = StartOfRound.Instance.allPlayerScripts[0].GetComponent<Leader>();
            }

            if (IsInShip || LethalMin.OnCompany)
            {
                PrimaryLeader.Controller.SetItemInElevator(IsInShip, IsInShip, seed);
                seed.EnablePhysics(enable: true);
                seed.EnableItemMeshes(enable: true);
                seed.isHeld = false;
                seed.isPocketed = false;
                seed.fallTime = 0f;
                seed.startFallingPosition = transform.parent == null ?
                transform.position : transform.parent.InverseTransformPoint(transform.position);
                Vector3 targetFloorPosition = seed.GetItemFloorPosition();
                targetFloorPosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(targetFloorPosition);
                seed.targetFloorPosition = targetFloorPosition;
                seed.transform.SetParent(StartOfRound.Instance.elevatorTransform, true);
            }
            StartCoroutine(WaitForBurst());
        }

        IEnumerator WaitForBurst()
        {
            yield return new WaitForSeconds(2f);
            IncrumentDestoryCountServerRpc();
        }


        public override void SetCurrentTask(string TaskID)
        {
            if (TaskID == "ReturnToShip")
            {
                SetToIdle();
                isOutside = true;
                StartCoroutine(TeleportTo(StartOfRound.Instance.shipInnerRoomBounds.transform.position));
                return;
            }
            base.SetCurrentTask(TaskID);
        }

        [ServerRpc]
        public void TeleportToServerRpc(Vector3 pos)
        {
            TeleportToClientRpc(pos);
        }
        [ClientRpc]
        public void TeleportToClientRpc(Vector3 pos)
        {
            StartCoroutine(TeleportTo(pos));
        }
        public IEnumerator TeleportTo(Vector3 pos)
        {
            LethalMin.Logger.LogInfo($"{gameObject.name}: teleporting to {pos}");
            animController.WalkingCondition.enabled = false;
            animController.PlayAnimation(WarpInAnim.name);
            AudioClip clip = WarpStartSFX[enemyRandom.Next(0, WarpStartSFX.Length)];
            creatureVoice.PlayOneShot(clip);
            yield return new WaitForSeconds(Mathf.Max(clip.length, WarpInAnim.length) + 0.5f);
            if (IsOwner)
            {
                agent.Warp(pos);
            }
            else
            {
                transform2.TeleportOnLocalClient(pos);
            }
            animController.PlayAnimation(WarpOutAnim.name);
            AudioClip clip2 = WarpEndSFX[enemyRandom.Next(0, WarpEndSFX.Length)];
            creatureVoice.PlayOneShot(clip2);
            yield return new WaitForSeconds(Mathf.Max(clip2.length, WarpOutAnim.length) + 0.1f);
            animController.WalkingCondition.enabled = true;
        }
    }
}