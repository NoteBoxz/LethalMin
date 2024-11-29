namespace LethalMin
{
    using UnityEngine;

    public class ShipPhaseOnionContainer : MonoBehaviour
    {
        public void LateUpdate()
        {
            if (transform.childCount > 0)
            {
                // Tween the children to move in a circle around the parent
                float radius = 20f;
                float angle = Time.time * 2f;
                foreach (Transform child in transform)
                {
                    child.position = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    angle += 2f * Mathf.PI / transform.childCount;
                }
            }
        }
    }
}