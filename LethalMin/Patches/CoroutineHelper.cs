using UnityEngine;
using System.Collections;

namespace LethalMin.Patches
{
    public class CoroutineHelper : MonoBehaviour
    {
        private static CoroutineHelper _instance;

        public static CoroutineHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CoroutineHelper");
                    _instance = go.AddComponent<CoroutineHelper>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public Coroutine StartCoroutineHelper(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        public void StopCoroutineHelper(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }
}