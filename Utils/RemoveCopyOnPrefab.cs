using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin.Utils
{
    public class RemoveCopyOnPrefab : MonoBehaviour
    {
        public string ReplacementAffix = "";
        void Awake()
        {
            gameObject.name = $"{gameObject.name.Replace("(Clone)", $"{ReplacementAffix}")}";
            Destroy(this);
        }
    }
}
