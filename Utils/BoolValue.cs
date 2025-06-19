using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin.Utils
{
    public class BoolValue : MonoBehaviour
    {
        public bool value = false;
        // public Renderer? renderer;

        // void Start()
        // {
        //     GameObject dbc = PikUtils.CreateDebugCube(Color.red);
        //     dbc.transform.SetParent(transform);
        //     dbc.transform.localPosition = Vector3.zero;
        //     dbc.transform.localScale = Vector3.one * 0.1f;
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