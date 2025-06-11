using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public class OnionHUDAnimEvents : MonoBehaviour
    {
        public GameObject OBJtoDeactivate = null!;

        public void DeactivateOBJ()
        {
            OBJtoDeactivate.SetActive(false);
        }
    }
}
