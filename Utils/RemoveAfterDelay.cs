using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin.Utils
{
    public class RemoveAfterDelay : MonoBehaviour
    {
        public float Delay = 5f;

        private void Start()
        {
            StartCoroutine(RemoveAfterDelayRoutine());
        }
        private IEnumerator RemoveAfterDelayRoutine()
        {
            yield return new WaitForSeconds(Delay);
            Destroy(gameObject);
        }
    }
}
