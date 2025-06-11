using UnityEngine;

namespace Lethalm.Utils
{
    public class LookAtMainCamera : MonoBehaviour
    {
        private Camera mainCamera = null!;

        void Start()
        {
            mainCamera = Camera.current;
        }

        void LateUpdate()
        {
            if (!StartOfRound.Instance.localPlayerController.isPlayerDead)
            {
                mainCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            }
            else
            {
                mainCamera = StartOfRound.Instance.spectateCamera;
            }
            if (mainCamera != null && 2 * transform.position - mainCamera.transform.position != Vector3.zero)
            {
                transform.LookAt(2 * transform.position - mainCamera.transform.position);
            }
        }
    }
}