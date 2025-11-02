using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin
{
    public class PlayerGhost : MonoBehaviour
    {
        public Transform LookAt = null!;
        Renderer renderer = null!;
        Vector3 StartingPosition;
        Color StartingColor;
        float RemoveTimer;
        Vector3 lookatLocalPos = new Vector3(0, 0, 0);
        AudioSource audioSource = null!;

        void Start()
        {
            renderer = GetComponentInChildren<Renderer>();
            StartingColor = renderer.material.color;

            audioSource = GetComponent<AudioSource>();
            audioSource.rolloffMode = LethalMin.GlobalGhostSFX ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Linear;

            StartingPosition = transform.position;
            if (LookAt.transform.parent == transform)
            {
                lookatLocalPos = LookAt.transform.localPosition;
                LookAt.transform.SetParent(null, true);
            }
        }

        public void Update()
        {
            audioSource.rolloffMode = LethalMin.GlobalGhostSFX ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Linear;
            LookAt.position = new Vector3(
                transform.position.x + lookatLocalPos.x,
                transform.position.y + lookatLocalPos.y,
                transform.position.z + lookatLocalPos.z);
            transform.position = new Vector3(transform.position.x, transform.position.y + Time.deltaTime * 1.5f, transform.position.z);
            if (Vector3.Distance(StartingPosition, transform.position) > 10)
            {
                renderer.material.color = new Color
                (renderer.material.color.r,
                renderer.material.color.g,
                renderer.material.color.b,
                renderer.material.color.a - Time.deltaTime * 0.5f);
                RemoveTimer += Time.deltaTime * 0.5f;
            }
            if (RemoveTimer > 1)
            {
                Destroy(gameObject);
            }
        }
    }
}
