using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin.Utils
{
    public class BoolValue : MonoBehaviour
    {
        public bool value = false;
        public Renderer? renderer;

        // void Start()
        // {
        //     renderer = GetComponentInChildren<Renderer>(true);
        //     renderer.gameObject.SetActive(true);
        // }
        // void Update()
        // {
        //     if (renderer != null)
        //     {
        //         renderer.material.color = value ? Color.green : Color.red;
        //     }
        // }
    }
}