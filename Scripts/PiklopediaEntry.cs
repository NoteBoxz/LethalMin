using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace LethalMin
{
    [CreateAssetMenu(fileName = "CustomPiklopediaEntry", menuName = "Pikmin/CustomPiklopediaEntry", order = 0)]
    public class PiklopediaEntry : ScriptableObject
    {
        [Tooltip("The Piklopedia entry name that will show on the Piklopedia page")]
        public string EntryName = "";
        [Tooltip("The terminal node for the piklopedia")]
        public TerminalNode PiklopediaNode = null!;
        [Tooltip("The terminal keyword for the piklopedia (can be null)")]
        public TerminalKeyword? PiklopediaKeyword = null;
        
        [HideInInspector]
        public int PiklopediaID = -1;
    }
}
