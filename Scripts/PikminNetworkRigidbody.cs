using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class PikminNetworkRigidbody : NetworkBehaviour
    {
        [Header("Sync Configuration")]
        //[SerializeField] private float positionThreshold = 0.5f;
        [SerializeField] private float velocityThreshold = 0.5f;
        [SerializeField] private float angularVelocityThreshold = 0.5f;
        [SerializeField] private float syncSmoothingFactor = 15f;
        [SerializeField] private bool syncVelocity = true;
        [SerializeField] private bool syncAngularVelocity = true;

        // Component references
        private Rigidbody rb = null!;
        private PikminNetworkTransfrom networkTransform = null!;

        // Sync state
        private Vector3 networkVelocity;
        private Vector3 networkAngularVelocity;
        private float lastSyncTime;
        private bool hasInitialized = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            networkTransform = GetComponent<PikminNetworkTransfrom>();

            if (rb == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: PikminNetworkRigidbody requires a Rigidbody component!");
                enabled = false;
                return;
            }

            if (networkTransform == null)
            {
                LethalMin.Logger.LogError($"{gameObject.name}: PikminNetworkRigidbody requires a PikminNetworkTransfrom component!");
                enabled = false;
                return;
            }
        }

        public void OnEnable()
        {
            if (!IsOwner)
            {
                // Non-owners should disable physics and just follow the network state
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            
            networkVelocity = rb.velocity;
            networkAngularVelocity = rb.angularVelocity;
            hasInitialized = true;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
        }

        private void FixedUpdate()
        {
            if (!hasInitialized) return;
            
            if (IsOwner)
            {
                CheckAndUpdateState();
            }
            else
            {
                // Apply received state to the rigidbody on non-owner clients
                if (syncVelocity)
                {
                    rb.velocity = Vector3.Lerp(rb.velocity, networkVelocity, Time.fixedDeltaTime * syncSmoothingFactor);
                }
                
                if (syncAngularVelocity)
                {
                    rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, networkAngularVelocity, Time.fixedDeltaTime * syncSmoothingFactor);
                }
            }
        }

        private void CheckAndUpdateState()
        {
            bool shouldSync = false;
            
            // Check velocity threshold
            if (syncVelocity && Vector3.Distance(rb.velocity, networkVelocity) > velocityThreshold)
            {
                shouldSync = true;
            }
            
            // Check angular velocity threshold
            if (!shouldSync && syncAngularVelocity && Vector3.Distance(rb.angularVelocity, networkAngularVelocity) > angularVelocityThreshold)
            {
                shouldSync = true;
            }
            
            if (shouldSync)
            {
                UpdateNetworkState(rb.velocity, rb.angularVelocity);
            }
        }

        private void UpdateNetworkState(Vector3 velocity, Vector3 angularVelocity)
        {
            networkVelocity = velocity;
            networkAngularVelocity = angularVelocity;
            lastSyncTime = Time.time;
            
            if (!IsServer)
            {
                UpdateStateServerRpc(velocity, angularVelocity);
            }
            else
            {
                UpdateStateClientRpc(velocity, angularVelocity);
            }
        }

        [ServerRpc]
        private void UpdateStateServerRpc(Vector3 velocity, Vector3 angularVelocity)
        {
            networkVelocity = velocity;
            networkAngularVelocity = angularVelocity;
            UpdateStateClientRpc(velocity, angularVelocity);
        }

        [ClientRpc]
        private void UpdateStateClientRpc(Vector3 velocity, Vector3 angularVelocity)
        {
            if (!IsOwner)
            {
                networkVelocity = velocity;
                networkAngularVelocity = angularVelocity;
                lastSyncTime = Time.time;
            }
        }

        // Public API for teleporting with physics
        public void TeleportRigidbody(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            if (IsOwner)
            {
                // Use the network transform to teleport position/rotation
                networkTransform.Teleport(position, rotation);
                
                // Update velocity state
                UpdateNetworkState(velocity, angularVelocity);
                
                // Apply directly to rigidbody on owner
                rb.velocity = velocity;
                rb.angularVelocity = angularVelocity;
            }
        }
    }
}