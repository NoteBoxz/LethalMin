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
            mainCamera = getMainCamera();
            if (mainCamera != null && 2 * transform.position - mainCamera.transform.position != Vector3.zero)
            {
                transform.LookAt(2 * transform.position - mainCamera.transform.position);
            }
        }

        Camera getMainCamera()
        {
            return StartOfRound.Instance == null ? Camera.current : StartOfRound.Instance.activeCamera;
        }
    }
}