using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using System.Collections;

namespace LethalMin
{
    public class PikerAnimEvents : MonoBehaviour
    {
        public Sprout sprout;
        public PlayerControllerB controllerB;

        public void StartPluckCoroutine()
        {
            if (sprout == null || controllerB == null)
                return;
            if (StartOfRound.Instance.localPlayerController != controllerB)
                return;

            LethalMin.Logger.LogInfo("Starting PluckCoroutine");
            StartCoroutine(PluckCoroutine());
        }

        private IEnumerator PluckCoroutine()
        {
            // Wait for 1.083333 seconds
            yield return new WaitForSeconds(1.083333f);

            LethalMin.Logger.LogInfo("PluckCoroutine executing after delay");

            sprout.PluckServerRpc();
            sprout.PluckServerRpc(controllerB.NetworkObject);

            // Ensure the player can move after plucking
            controllerB.inSpecialInteractAnimation = false;
            controllerB.thisController.enabled = true;

            LethalMin.Logger.LogInfo($"Player controller enabled: {controllerB.thisController.enabled}");
            LethalMin.Logger.LogInfo($"Player in special animation: {controllerB.inSpecialInteractAnimation}");
        }
    }
}