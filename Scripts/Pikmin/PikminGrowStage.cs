using UnityEngine;
using Newtonsoft.Json;

namespace LethalMin.Pikmin
{
    [System.Serializable]
    public class PikminGrowStage
    {
        public float Speed = 3;

        [Tooltip("Overrides the current attack strength If left at -1, it will not be applied")]
        public float AttackStrength = -1;

        [Tooltip("Overrides the current carry strength If left at -1, it will not be applied")]
        public int CarryStrength = -1;
    }
}