using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin
{
    public class PikminZap : MonoBehaviour
    {
        public PikminType LostType = null!;
        public System.Random ghostRandom = null!;
        public string InMemoryof = "<ID Not Set>";

        void Start()
        {
            StartCoroutine(SpawnGhost());
        }

        IEnumerator SpawnGhost()
        {
            yield return new WaitForSeconds(1f);
            PikminGhost ghost = GameObject.Instantiate(LethalMin.PikminGhostPrefab, transform.position, transform.rotation).
            GetComponent<PikminGhost>();
            ghost.LostType = LostType;
            ghost.InMemoryof = InMemoryof;
            ghost.ghostRandom = ghostRandom;
            yield return new WaitForSeconds(1f);
            Destroy(gameObject);
        }
    }
}
