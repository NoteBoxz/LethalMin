using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LethalMin.Utils
{
    public class BetterButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Button baseButton = null!;
        private bool isHeld = false;
        private float holdTime = 0f;

        [SerializeField]
        private float minimumHoldTime = 0.1f; // Minimum time to consider as "held"

        private void Start()
        {
            Button button = GetComponent<Button>();
            // Ensure there's a Button component attached
            if (button == null)
            {
                LethalMin.Logger.LogError($"BetterButton ({gameObject.name}) requires a Button component!");
                return;
            }
        }

        private void Update()
        {
            if (isHeld)
            {
                holdTime += Time.deltaTime;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isHeld = true;
            holdTime = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHeld = false;
            holdTime = 0f;
        }

        public bool IsButtonHeld()
        {
            return isHeld && holdTime >= minimumHoldTime;
        }

        public float GetHoldTime()
        {
            return holdTime;
        }
    }
}