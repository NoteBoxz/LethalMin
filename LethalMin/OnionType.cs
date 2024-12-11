using System;
using LethalMin;
using UnityEngine;
namespace LethalMin
{
	[CreateAssetMenu(menuName = "LethalMin/OnionType", order = 1)]
	public class OnionType : ScriptableObject
	{
		[Header("Distinguishing Information")]
		[Tooltip("The onion's color")]
		public Color OnionColor;
		[Tooltip("The Onion's texture")]
		public Texture2D? OnionTexture;
		public Material? OnionMaterial;
		[Tooltip("The name of the onion's type.")]
		public string TypeName = "";

		[Tooltip("Set by mod, do not change.")]
		[HideInInspector]
		public int OnionTypeID;

		[Header("Onion Stats")]
		[Tooltip("The types of pikmin that the onion can hold.")]
		public PikminType[] TypesCanHold;

		[Tooltip("Whether the onion can create sprouts.")]
		public bool CanCreateSprouts = true;

		[Header("Spawning")]
		[Tooltip("Whether the onion should spawn in as an item.")]
		public bool SpawnInAsItem;

		[Tooltip("The item mesh that the onion should spawn in as. (will be set to defult if empty)")]
		public GameObject OnionItemMeshPrefab;

		[Header("Unused / Not Implemented")]
		
		[Tooltip("The onion's custom script")]
		public Onion? OnionScript;

		[Tooltip("The onion's icon")]
		public Sprite? OnionIcon;

		[Tooltip("Whether the onion can be fused with other onions.")]
		public bool CanBeFused;

		[Tooltip("The fuse rules for the onion.")]
		public OnionFuseRules FuesingRules;

		[Tooltip("The onion's prefab. (will be set to defult if empty)")]
		public GameObject OnionPrefab;

		[HideInInspector]
		public bool HasBeenRegistered;

		[HideInInspector]
		public string version = "0.2.15";
	}
}