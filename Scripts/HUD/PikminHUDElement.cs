using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class PikminHUDElement : MonoBehaviour
    {
        public CanvasGroup group = null!;
        public float targetAlpha;
        public float tweenSpeed;
        public bool IsTweening = false;
        public float tolerance = 0.1f; // Define a tolerance range

        void Update()
        {
            if (Mathf.Abs(group.alpha - targetAlpha) > tolerance && !IsTweening)
            {
                //LethalMin.Logger.LogInfo($"{gameObject.name} Started Tweening!");
                IsTweening = true;
            }

            if (IsTweening)
            {
                group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.deltaTime * tweenSpeed);

                if (Mathf.Abs(group.alpha - targetAlpha) <= tolerance)
                {
                    //LethalMin.Logger.LogInfo($"{gameObject.name} Stopped Tweening!");
                    group.alpha = targetAlpha;
                    IsTweening = false;
                }
            }
        }
    }
}