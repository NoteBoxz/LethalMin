using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin;
using LethalMin.HUD;
using LethalMin.Pikmin;
using LethalMin.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace LethalMin
{
    public class DirectlyPathZone : MonoBehaviour
    {
        bool _isInZone;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && other.TryGetComponent(out Leader leader) && leader.IsOwner)
            {
                _isInZone = true;
                leader.DirectPikminPath = true;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && other.TryGetComponent(out Leader leader) && leader.IsOwner)
            {
                _isInZone = false;
                leader.DirectPikminPath = false;
            }
        }
        private void OnDisable()
        {
            if (_isInZone)
            {
                _isInZone = false;
                PikminManager.instance.LocalLeader.DirectPikminPath = false;
            }
        }
        private void OnDestroy()
        {
            if (_isInZone)
            {
                _isInZone = false;
                PikminManager.instance.LocalLeader.DirectPikminPath = false;
            }
        }
    }
}
