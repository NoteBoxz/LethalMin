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
        private bool _visuallyDisabled = false;
        private ColorBlock _originalColors;

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

            if (baseButton != null)
            {
                _originalColors = baseButton.colors;
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
            return baseButton.interactable && isHeld && holdTime >= minimumHoldTime;
        }

        public float GetHoldTime()
        {
            return holdTime;
        }

        public void SetVisuallyDisabled(bool disabled)
        {
            if (_visuallyDisabled == disabled) return;

            _visuallyDisabled = disabled;

            if (baseButton == null) return;

            if (disabled)
            {
                // Store original colors if needed
                _originalColors = baseButton.colors;

                // Create new colors that match the disabled state
                ColorBlock disabledAppearance = baseButton.colors;
                disabledAppearance.colorMultiplier = 0.5f; // This is typically how disabled buttons look
                baseButton.colors = disabledAppearance;

                // If the button has a text component, make it appear disabled too
                Text[] texts = baseButton.GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 0.5f);
                }
            }
            else
            {
                // Restore original colors
                baseButton.colors = _originalColors;

                // Restore text colors if needed
                Text[] texts = baseButton.GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
                }
            }
        }

        public bool IsVisuallyDisabled()
        {
            return _visuallyDisabled;
        }
    }
}