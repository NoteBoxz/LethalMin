using System;
using UnityEngine;
namespace LethalMin
{
    [CreateAssetMenu(menuName = "LethalMin/OnionFuseRules", order = 1)]
    public class OnionFuseRules : ScriptableObject
    {
        [Tooltip("An array of OnionType values representing the onions that can fuse together.")]
        public OnionType[] CompatibleOnions;

        [Header("Not Implemented")]

        [Tooltip("An array of Texture2D objects representing the textures to be used for fusing onions. If this is null, the textures will be generated automatically.")]
        public Texture2D[] FuseTextures;

        [Tooltip("Determines whether onions should fuse in order or not.")]
        public bool FuseInOrder;

        [Tooltip("Specifies whether Fuseion textures should be generated automatically.")]
        public bool GenerateFuseionTextures;

        [Tooltip("The main onion prefab to be instantiated when fusing occurs.")]
        public GameObject MainOnionPrefab;

        [Tooltip("The path to the onion's mesh that will be replaced with the fused mesh.")]
        public string OnionMeshPath;

        [Tooltip("The fused onion meshes, (Note: The first mesh should be the 2 onion fused, the second should be the 3 onion fused, etc.)")]
        public GameObject[] OnionMeshes;

        [HideInInspector]
        public bool HasBeenRegistered;
        [HideInInspector]
        public int FuseID;
        [HideInInspector]
        public string version = "0.2.21";

        /// <summary>
        /// Checks if an onion can fuse with the given OnionType.
        /// </summary>
        /// <param name="onion">The OnionType to check compatibility with.</param>
        /// <returns>True if the onion can fuse; False otherwise.</returns>
        public bool CanFuseWith(OnionType onion)
        {
            return Array.Exists(CompatibleOnions, element => element == onion);
        }
    }
}