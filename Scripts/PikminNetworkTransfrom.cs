using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
    public class PikminNetworkTransfrom : NetworkBehaviour
    {
        [Header("Sync Configuration")]
        [SerializeField] private float positionThreshold = 1.0f;
        [SerializeField] private float rotationThreshold = 6.0f;
        [SerializeField] private float syncMovementSpeed = 0.22f;

        [Header("Debug Configuration")]

        private Vector3 networkPosition;
        private Quaternion networkRotation;
        private float lastSyncTime;

        // Interpolation
        public Vector3 tempVelocity;

        private void Start()
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }

        private void Update()
        {
            if (IsOwner)
            {
                CheckRpcUpdate();
            }
            else
            {
                InterpolatePosition();
            }
        }

        private void CheckRpcUpdate()
        {
            if (Vector3.Distance(transform.position, networkPosition) > positionThreshold ||
                Quaternion.Angle(transform.rotation, networkRotation) > rotationThreshold)// || lastSyncTime + 0.5f < Time.time && Vector3.Distance(transform.position, networkPosition) > 0.1f)
            {
                //LethalMin.Logger.LogInfo($"Syncing Position: {transform.position}, {networkPosition}, dist: {Vector3.Distance(transform.position, networkPosition)}, posthresh: {positionThreshold}");
                //LethalMin.Logger.LogInfo($"Syncing Rotaion: {transform.rotation}, {networkRotation}, ang: {Quaternion.Angle(transform.rotation, networkRotation)}, rotthresh: {rotationThreshold}");
                UpdatePosition(transform.position, transform.rotation);
            }
        }

        private void InterpolatePosition()
        {
            transform.position = Vector3.SmoothDamp(transform.position, networkPosition, ref tempVelocity, syncMovementSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, 15f * Time.deltaTime);
        }

        private void UpdatePosition(Vector3 newPosition, Quaternion newRotation)
        {
            networkPosition = newPosition;
            networkRotation = newRotation;
            lastSyncTime = Time.time;

            // Only call ServerRpc if we're not the server
            if (!IsServer)
            {
                UpdatePositionServerRpc(newPosition, newRotation);
            }
            else
            {
                // If we are the server, directly call ClientRpc
                UpdatePositionClientRpc(newPosition, newRotation);
            }
        }


        [ServerRpc]
        private void UpdatePositionServerRpc(Vector3 newPosition, Quaternion newRotation)
        {
            networkPosition = newPosition;
            networkRotation = newRotation;
            UpdatePositionClientRpc(newPosition, newRotation);
        }

        [ClientRpc]
        private void UpdatePositionClientRpc(Vector3 newPosition, Quaternion newRotation)
        {
            if (!IsOwner)
            {
                networkPosition = newPosition;
                networkRotation = newRotation;
                lastSyncTime = Time.time;
            }
        }

        public void Teleport(Vector3 position)
        {
            Teleport(position, transform.rotation, transform.localScale);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Teleport(position, rotation, transform.localScale);
        }

        public void Teleport(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (IsOwner)
            {
                if (!IsServer)
                {
                    TeleportServerRpc(position, rotation, scale);
                }
                else
                {
                    TeleportClientRpc(position, rotation, scale);
                }
            }
        }

        [ServerRpc]
        private void TeleportServerRpc(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;

            networkPosition = position;
            networkRotation = rotation;

            TeleportClientRpc(position, rotation, scale);
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            TeleportOnLocalClient(position, rotation, scale);
        }

        public void TeleportOnLocalClient(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;

            networkPosition = position;
            networkRotation = rotation;
            lastSyncTime = Time.time;
        }
        public void TeleportOnLocalClient(Vector3 position, Quaternion rotation)
        {
            TeleportOnLocalClient(position, rotation, transform.localScale);
        }
        public void TeleportOnLocalClient(Vector3 position)
        {
            TeleportOnLocalClient(position, transform.rotation, transform.localScale);
        }
    }
}