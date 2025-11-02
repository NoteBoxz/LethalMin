using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Patches;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LethalMin
{
    public class PikminEnemy : NetworkBehaviour
    {
        public EnemyAI enemyScript = null!;
        public List<PikminLatchTrigger> LatchTriggers = null!;
        public float DamageCounter = 0;
        public bool IsDead = false;
        public bool CanBeAttacked = true;
        public bool OverrideCanDie = false;
        public bool CanShakeOffPikmin = true;
        public float MaxShakeOffVelocity = 2f;
        float ShakeCooldown = 0.5f;
        public UnityEvent<float> OnHit = new UnityEvent<float>();

        //IcePikmin
        public float FreezeCounter = 0;
        public bool IsFrozen = false;
        public bool HasShattered = false;
        public float FreezeDuration = 3.0f;
        Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        Vector3 OOB = new Vector3(0, -1000, 0); // Out of bounds position to teleport the enemy when frozen


        protected virtual void Start()
        {
            foreach (PikminLatchTrigger PLtrigger in LatchTriggers)
            {
                PLtrigger.OnPikminHit.AddListener(pikmin => HitEnemy(pikmin.pikminType.GetAttackStrength(pikmin.CurrentGrowthStage), pikmin));
                PLtrigger.OnPikminDirectLatch.AddListener(HandleDirectHit);
            }
            if (enemyScript == null)
            {
                LethalMin.Logger.LogWarning($"EnemyAI component not found on the GameObject ({gameObject.name})");
                enemyScript = GetComponent<EnemyAI>();
            }
            if (enemyScript == null)
            {
                LethalMin.Logger.LogFatal($"EnemyAI component not found on the GameObject fallback failed ({gameObject.name})");
                enabled = false;
                return;
            }

            PikminManager.instance.PikminEnemies.Add(this);
        }

        public override void OnDestroy()
        {
            PikminManager.instance.PikminEnemies.Remove(this);
        }

        protected virtual void Update()
        {
            if (enemyScript.isEnemyDead && !IsDead)
            {
                RemoveAndDisableTriggers();

                if (IsOwner && IsFrozen)
                    ShatterServerRpc();

                IsDead = true;
                enabled = false;
            }
            if (IsFrozen)
            {
                enemyScript.inSpecialAnimation = true;
                enemyScript.SetClientCalculatingAI(false);
            }

            ShakeCooldown -= Time.deltaTime;
            if (MaxShakeOffVelocity > 1)
            {
                MaxShakeOffVelocity -= Time.deltaTime * 0.5f;
            }

            if (!LethalMin.NoKnockbackCheat && CanShakeOffPikmin && enemyScript.IsOwner && ShakeCooldown <= 0 && enemyScript.agent.velocity.magnitude > MaxShakeOffVelocity)
            {
                ShakeCooldown = Random.Range(1.0f, 5.0f);
                MaxShakeOffVelocity = enemyScript.agent.velocity.magnitude - 1.5f;
                if (MaxShakeOffVelocity < 0.5f)
                {
                    MaxShakeOffVelocity = 0.5f;
                }
                bool ShouldShake = false;
                foreach (PikminLatchTrigger PLtrigger in LatchTriggers)
                {
                    if (PLtrigger.PikminOnLatch.Count > 0)
                    {
                        ShouldShake = true;
                        break;
                    }
                }
                if (ShouldShake)
                {
                    // Track Pikmin we've already processed using HashSet for quick lookups
                    HashSet<PikminAI> processedPikmin = new HashSet<PikminAI>();
                    List<(NetworkObjectReference netRef, PikminAI pikmin, float rngValue)> pikminShakeData = new List<(NetworkObjectReference, PikminAI, float)>();

                    // Collect all pikmin across latch triggers, avoiding duplicates
                    foreach (PikminLatchTrigger PLtrigger in LatchTriggers)
                    {
                        foreach (PikminAI pikmin in PLtrigger.PikminOnLatch)
                        {
                            // Skip if we've already processed this Pikmin
                            if (processedPikmin.Contains(pikmin))
                                continue;

                            processedPikmin.Add(pikmin);
                            float rng = Random.Range(0.0f, 1.0f);
                            pikminShakeData.Add((pikmin.NetworkObject, pikmin, rng));
                        }
                    }

                    // Sort by RNG value once
                    pikminShakeData.Sort((a, b) => a.rngValue.CompareTo(b.rngValue));

                    // Extract network data for RPC
                    NetworkObjectReference[] netRefs = pikminShakeData.Select(data => data.netRef).ToArray();
                    float[] rngValues = pikminShakeData.Select(data => data.rngValue).ToArray();

                    // Create dictionary for local processing - now safe since we've removed duplicates
                    Dictionary<PikminAI, float> pikminRngDict = pikminShakeData.ToDictionary(
                        data => data.pikmin,
                        data => data.rngValue
                    );

                    // Send to server and process locally
                    ShakePikminServerRpc(MaxShakeOffVelocity, netRefs, rngValues);
                    ShakePikmin(pikminRngDict);
                }
            }

            if (HasShattered)
            {
                enemyScript.transform.position = OOB;
            }

            if (IsDead)
            {
                return;
            }

            if (IsOwner && IsFrozen)
            {
                if (FreezeDuration > 0)
                {
                    FreezeDuration -= Time.deltaTime;
                }
                else
                {
                    UnfreezeEnemyServerRpc();
                    FreezeDuration = 3;
                }
                if (!enemyScript.inSpecialAnimation)
                {
                    UnfreezeEnemyServerRpc();
                    FreezeDuration = 3;
                }
            }
        }

        public virtual void HandleDirectHit(PikminAI? pikminWhoHit = null)
        {
            if (pikminWhoHit != null && pikminWhoHit.pikminType.DamageDeltUpLanding > 0)
            {
                float val = pikminWhoHit.pikminType.DamageDeltUpLanding * pikminWhoHit.pikminType.GetAttackStrength(pikminWhoHit.CurrentGrowthStage);
                HitEnemy(val, pikminWhoHit);
                LethalMin.Logger.LogInfo($"{pikminWhoHit.DebugID}: landed on enemy with: {val}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HitEnemyServerRpc(float Damage, NetworkObjectReference PikRef)
        {
            HitEnemyClientRpc(Damage, PikRef);
        }
        [ServerRpc(RequireOwnership = false)]
        public void HitEnemyServerRpc(float Damage)
        {
            HitEnemyClientRpc(Damage);
        }

        [ClientRpc]
        public void HitEnemyClientRpc(float Damage, NetworkObjectReference PikRef)
        {
            if (PikRef.TryGet(out NetworkObject obj))
            {
                if (obj.TryGetComponent(out PikminAI pikmin))
                {
                    HitEnemy(Damage, pikmin);
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to get PikminAI from reference: {PikRef}");
                }
            }
            else
            {
                LethalMin.Logger.LogWarning($"Failed to get NetworkObject from reference: {PikRef}");
            }
        }
        [ClientRpc]
        public void HitEnemyClientRpc(float Damage)
        {
            HitEnemy(Damage);
        }

        public virtual void HitEnemy(float Damage, PikminAI? pikminWhoHit = null)
        {
            LethalMin.Logger.LogDebug($"{pikminWhoHit?.DebugID}: Hitting enemy with: {Damage}");
            OnHit.Invoke(Damage);
            DamageCounter += Damage;

            if (pikminWhoHit != null && pikminWhoHit is IcePikminAI)
            {
                int HP = EnemyAIPatch.EnemyHPs.ContainsKey(enemyScript.enemyType) ? EnemyAIPatch.EnemyHPs[enemyScript.enemyType] : enemyScript.enemyHP;
                FreezeCounter += 0.05f;

                if (IsOwner && FreezeCounter >= HP && !IsFrozen)
                {
                    FreezeEnemyServerRpc(transform.position, transform.rotation.eulerAngles); // Freeze the enemy if the freeze counter exceeds the HP
                    FreezeEnemy(transform.position, transform.rotation.eulerAngles);
                    LethalMin.Logger.LogInfo($"{enemyScript.gameObject.name} frozen by Ice Pikmin due to freeze counter: {FreezeCounter}");
                    return;
                }
            }

            if (DamageCounter >= 1 && IsOwner)
            {
                Leader? pikminLeader = null;

                if (pikminWhoHit != null)
                {
                    pikminLeader = pikminWhoHit.previousLeader;
                }

                if (pikminLeader != null)
                {
                    DamageEnemyServerRpc(DamageCounter, pikminLeader.NetworkObject);
                }
                else
                {
                    DamageEnemyServerRpc(DamageCounter);
                }
                DamageEnemy(DamageCounter, pikminLeader);
            }
        }

        [ServerRpc]
        public void DamageEnemyServerRpc(float Counter)
        {
            DamageEnemyClientRpc(Counter);
        }
        [ServerRpc]
        public void DamageEnemyServerRpc(float Counter, NetworkObjectReference leaderRef)
        {
            DamageEnemyClientRpc(Counter, leaderRef);
        }

        [ClientRpc]
        public void DamageEnemyClientRpc(float Counter)
        {
            if (!IsOwner)
                DamageEnemy(Counter);
        }
        [ClientRpc]
        public void DamageEnemyClientRpc(float Counter, NetworkObjectReference leaderRef)
        {
            if (IsOwner)
            {
                return;
            }
            if (leaderRef.TryGet(out NetworkObject obj))
            {
                if (obj.TryGetComponent(out Leader pikminLeader))
                {
                    DamageEnemy(Counter, pikminLeader);
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to get Leader from reference: {leaderRef}");
                }
            }
            else
            {
                LethalMin.Logger.LogWarning($"Failed to get NetworkObject from reference: {leaderRef}");
            }
        }


        public void DamageEnemy(float Counter, Leader? pikminLeader = null)
        {
            DamageCounter = Counter;

            int damageToApply = Mathf.FloorToInt(DamageCounter);

            // Apply full damage instead of just 1
            enemyScript.HitEnemy(damageToApply, pikminLeader == null ? null : pikminLeader.Controller, true);

            // Retain fractional damage for next time
            DamageCounter -= damageToApply;

            LethalMin.Logger.LogDebug($"Applied {damageToApply} damage to {enemyScript.gameObject.name}, remaining counter: {DamageCounter}");
        }

        [ServerRpc]
        public void ShakePikminServerRpc(float MaxShakeOffVelocity, NetworkObjectReference[] PikRef, float[] RNG)
        {
            ShakePikminClientRpc(MaxShakeOffVelocity, PikRef, RNG);
        }
        [ClientRpc]
        public void ShakePikminClientRpc(float MaxShakeOffVelocity, NetworkObjectReference[] PikRef, float[] RNG)
        {
            if (enemyScript.IsOwner)
            {
                return;
            }
            if (PikRef.Length != RNG.Length)
            {
                LethalMin.Logger.LogWarning($"PikRef and RNG arrays have different lengths: {PikRef.Length} != {RNG.Length}");
            }
            this.MaxShakeOffVelocity = MaxShakeOffVelocity;
            Dictionary<PikminAI, float> PikminRNG = new Dictionary<PikminAI, float>();
            for (int i = 0; i < PikRef.Length; i++)
            {
                NetworkObjectReference refPik = PikRef[i];
                if (refPik.TryGet(out NetworkObject obj))
                {
                    if (obj.TryGetComponent(out PikminAI pikmin))
                    {
                        PikminRNG.Add(pikmin, RNG[i]);
                    }
                    else
                    {
                        LethalMin.Logger.LogWarning($"Failed to get PikminAI from reference: {refPik}");
                    }
                }
                else
                {
                    LethalMin.Logger.LogWarning($"Failed to get NetworkObject from reference: {refPik}");
                }
            }
            ShakePikmin(PikminRNG.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value));
        }
        public virtual void ShakePikmin(Dictionary<PikminAI, float> PikminRNG)
        {
            foreach (KeyValuePair<PikminAI, float> kvp in PikminRNG)
            {
                if (kvp.Value > kvp.Key.pikminType.ShakeEndurance)
                {
                    kvp.Key.TryUnlatchPikmin();
                    Vector3 awayDirection = -kvp.Key.transform.forward;
                    Vector3 knockbackDirection = (awayDirection + Vector3.up * 0.5f).normalized;
                    kvp.Key.ApplyKnockBack(direction: knockbackDirection, force: MaxShakeOffVelocity / 2);
                    kvp.Key.CanGetBackUp = true;
                }
                LethalMin.Logger.LogInfo($"{kvp.Key.DebugID}: Shooken with {kvp.Key.pikminType.ShakeEndurance}/{kvp.Value} at {MaxShakeOffVelocity} velocity");
            }
        }

        public virtual void RemoveAndDisableTriggers()
        {
            List<PikminLatchTrigger> TMPtriggers = new List<PikminLatchTrigger>(LatchTriggers);
            foreach (PikminLatchTrigger PLtrigger in TMPtriggers)
            {
                if (PLtrigger.networkAddon != null)
                {
                    if (PLtrigger.networkAddon.IsOwner)
                        PLtrigger.networkAddon.RemoveAllPikminServerRpc(3);
                }
                else
                {
                    PLtrigger.RemoveAllPikmin(3);
                }
                PLtrigger.OnPikminHit.RemoveAllListeners();
                PLtrigger.enabled = false;
            }
        }

        public virtual void OnAddedToEnemy(EnemyAI enemy)
        {

        }



        [ServerRpc]
        public void FreezeEnemyServerRpc(Vector3 freezePosition, Vector3 freezeRotation)
        {
            FreezeEnemyClientRpc(freezePosition, freezeRotation);
        }
        [ClientRpc]
        public void FreezeEnemyClientRpc(Vector3 freezePosition, Vector3 freezeRotation)
        {
            FreezeEnemy(freezePosition, freezeRotation);
        }
        public virtual void FreezeEnemy(Vector3 freezePosition, Vector3 freezeRotation)
        {
            if (IsFrozen || enemyScript.isEnemyDead)
                return;

            FreezeDuration = 12.0f;
            IsFrozen = true;
            FreezeCounter = 0;
            enemyScript.serverPosition = freezePosition;
            enemyScript.serverRotation = freezeRotation;
            if (IsOwner)
            {
                enemyScript.agent.Warp(freezePosition);
            }

            enemyScript.creatureSFX.PlayOneShot(LethalMin.assetBundle.LoadAsset<AudioClip>("Assets/LethalMin/wav_bnk_AkB_Ambience_CaveAquarium/Amb_CaveAquarium_WaterBox_FreezePikminAdd.wav"));

            //store the original materials for unfreezing later
            originalMaterials.Clear();
            Renderer[] renderers = enemyScript.GetComponentsInChildren<Renderer>(true);
            Material Imat = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Types/Ice Pikmin/IceMat.mat");
            Imat.color = new Color(Imat.color.r, Imat.color.g, Imat.color.b, 1f);
            foreach (Renderer renderer in renderers)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    mats[i] = renderer.sharedMaterials[i];
                }
                originalMaterials[renderer] = mats;

                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    newMaterials[i] = Imat;
                }
                renderer.sharedMaterials = newMaterials;
            }
            LethalMin.Logger.LogInfo($"{enemyScript.gameObject.name} frozen for {FreezeDuration} seconds.");
        }

        [ServerRpc]
        public void UnfreezeEnemyServerRpc()
        {
            UnfreezeEnemyClientRpc();
        }
        [ClientRpc]
        public void UnfreezeEnemyClientRpc()
        {
            UnfreezeEnemy();
        }
        public virtual void UnfreezeEnemy()
        {
            if (!IsFrozen || enemyScript.isEnemyDead)
                return;

            FreezeDuration = 12.0f;
            IsFrozen = false;
            FreezeCounter = 0;
            enemyScript.inSpecialAnimation = false;
            enemyScript.SetClientCalculatingAI(true);

            enemyScript.creatureSFX.PlayOneShot(LethalMin.assetBundle.LoadAsset<AudioClip>("Assets/LethalMin/wav_bnk_AkB_Ambience_CaveAquarium/Amb_CaveAquariumSwamp_WaterBox(14).wav"));

            //store the original materials for unfreezing later
            foreach (KeyValuePair<Renderer, Material[]> kvp in originalMaterials)
            {
                Renderer renderer = kvp.Key;
                Material[] originalMats = kvp.Value;
                if (renderer != null && originalMats != null)
                {
                    renderer.sharedMaterials = originalMats;
                }
            }
            originalMaterials.Clear(); // Clear the dictionary after unfreezing to free up memory
            LethalMin.Logger.LogInfo($"{enemyScript.gameObject.name} unfrozen after {FreezeDuration} seconds.");
        }

        [ServerRpc]
        public void ShatterServerRpc()
        {
            ShatterClientRpc();
        }
        [ClientRpc]
        public void ShatterClientRpc()
        {
            Shatter();
        }
        public virtual void Shatter()
        {
            if (HasShattered)
                return;

            HasShattered = true;

            // Play shattering sound
            if (enemyScript.creatureSFX != null)
            {
            }

            // Create a container for all the shards
            GameObject shardsContainer = new GameObject($"{enemyScript.gameObject.name}_IceShards");
            AudioSource audio = shardsContainer.AddComponent<AudioSource>();
            audio.spatialBlend = 1.0f; // 3D sound
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 5f;
            audio.maxDistance = 15f;
            AudioClip shatterSound = LethalMin.assetBundle.LoadAsset<AudioClip>("Assets/LethalMin/wav_bnk_AkB_Ambience_CaveAquarium/Amb_CaveAquarium_WaterBox_UnFreeze.wav");
            audio.PlayOneShot(shatterSound);

            // Get renderers and process each mesh
            List<Renderer> allRenderers = new List<Renderer>();
            if (enemyScript.meshRenderers != null)
                allRenderers.AddRange(enemyScript.meshRenderers);
            if (enemyScript.skinnedMeshRenderers != null)
                allRenderers.AddRange(enemyScript.skinnedMeshRenderers);

            // Get ice material for the shards
            Material iceMat = LethalMin.assetBundle.LoadAsset<Material>("Assets/LethalMin/Types/Ice Pikmin/IceMat.mat");

            // Process each renderer
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null)
                    continue;
                if (!renderer.gameObject.activeSelf)
                    continue;

                // Get mesh from the renderer
                Mesh originalMesh = null!;
                if (renderer is MeshRenderer)
                {
                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                        originalMesh = meshFilter.mesh;
                }
                else if (renderer is SkinnedMeshRenderer skinnedMesh)
                {
                    originalMesh = new Mesh();
                    skinnedMesh.BakeMesh(originalMesh);
                }

                if (originalMesh == null)
                    continue;
                if (!originalMesh.canAccess)
                    continue;

                // Number of chunks to create
                int chunkCount = 10; // Adjust based on complexity

                // Divide the mesh into chunks
                Vector3[] vertices = originalMesh.vertices;
                int[] triangles = originalMesh.triangles;
                Vector2[] uv = originalMesh.uv;

                // Split triangles into groups for chunks
                int trianglesPerChunk = triangles.Length / chunkCount;
                if (trianglesPerChunk < 3)
                    trianglesPerChunk = 3;

                for (int i = 0; i < chunkCount && i * trianglesPerChunk < triangles.Length; i++)
                {
                    // Create a new mesh for this chunk
                    Mesh chunkMesh = new Mesh();
                    List<Vector3> chunkVertices = new List<Vector3>();
                    List<int> chunkTriangles = new List<int>();
                    List<Vector2> chunkUV = new List<Vector2>();

                    // Add a portion of triangles to this chunk
                    int startIdx = i * trianglesPerChunk;
                    int endIdx = Mathf.Min(startIdx + trianglesPerChunk, triangles.Length);
                    Dictionary<int, int> vertexMapping = new Dictionary<int, int>();

                    for (int t = startIdx; t < endIdx; t += 3)
                    {
                        if (t + 2 >= triangles.Length)
                            break;

                        for (int j = 0; j < 3; j++)
                        {
                            int originalVertexIndex = triangles[t + j];

                            if (!vertexMapping.ContainsKey(originalVertexIndex))
                            {
                                vertexMapping[originalVertexIndex] = chunkVertices.Count;
                                chunkVertices.Add(vertices[originalVertexIndex]);

                                if (uv.Length > originalVertexIndex)
                                    chunkUV.Add(uv[originalVertexIndex]);
                                else if (uv.Length > 0)
                                    chunkUV.Add(uv[0]); // Fallback
                            }

                            chunkTriangles.Add(vertexMapping[originalVertexIndex]);
                        }
                    }

                    // If we have valid geometry, create a game object for this chunk
                    if (chunkTriangles.Count >= 3 && chunkVertices.Count >= 3)
                    {
                        chunkMesh.vertices = chunkVertices.ToArray();
                        chunkMesh.triangles = chunkTriangles.ToArray();
                        if (chunkUV.Count == chunkVertices.Count)
                            chunkMesh.uv = chunkUV.ToArray();
                        chunkMesh.RecalculateNormals();
                        chunkMesh.RecalculateBounds();

                        // Create game object for this chunk
                        GameObject chunk = new GameObject($"Chunk_{i}");
                        chunk.transform.parent = shardsContainer.transform;
                        chunk.transform.position = renderer.transform.position;
                        chunk.transform.rotation = renderer.transform.rotation;
                        chunk.transform.localScale = renderer.transform.lossyScale;

                        // Add mesh components
                        MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
                        meshFilter.mesh = chunkMesh;
                        MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();

                        // Apply material - either ice material or original
                        if (iceMat != null)
                            meshRenderer.material = iceMat;
                        else if (renderer.sharedMaterial != null)
                            meshRenderer.material = renderer.sharedMaterial;

                        // Add physics
                        MeshCollider collider = chunk.AddComponent<MeshCollider>();
                        collider.convex = true;
                        Rigidbody rb = chunk.AddComponent<Rigidbody>();
                        LayerMask nonPlayerCollision = 8;
                        collider.excludeLayers = nonPlayerCollision; // Exclude player layer to avoid collision issues
                        rb.excludeLayers = nonPlayerCollision; // Exclude player layer to avoid collision issues                   

                        // Apply random force to scatter the pieces
                        Vector3 explosionCenter = renderer.bounds.center;
                        rb.AddExplosionForce(
                            UnityEngine.Random.Range(5f, 10f),
                            explosionCenter,
                            renderer.bounds.size.magnitude,
                            0.2f,
                            ForceMode.Impulse
                        );

                        // Add random torque
                        rb.AddTorque(
                            UnityEngine.Random.Range(-5f, 5f),
                            UnityEngine.Random.Range(-5f, 5f),
                            UnityEngine.Random.Range(-5f, 5f),
                            ForceMode.Impulse
                        );
                    }
                }
            }

            // Move the enemy out of bounds
            enemyScript.transform.position = OOB;

            // Destroy the shards container after 30 seconds
            Destroy(shardsContainer, 30f);

            LethalMin.Logger.LogInfo($"{enemyScript.gameObject.name} has shattered into mesh chunks and is now out of bounds.");
        }

        protected virtual IEnumerator DestroyAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }
}
