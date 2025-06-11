using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public class PikminScanNodeProperties : ScanNodeProperties
    {
        public int VisualNodeType = 2;
        public PiklopediaEntry? PiklopediaEntry = null!;
        public bool OverrideEnemyGlobalNotif = true;

        public enum ScanNodeType
        {
            PointOfInterest,
            Enemy,
            Item,
        }

        void Awake()
        {
            if (OverrideEnemyGlobalNotif && !LethalMin.EnemyIDsOverridenByPiklopedia.Contains(creatureScanID))
            {
                LethalMin.EnemyIDsOverridenByPiklopedia.Add(creatureScanID);
            }
        }
    }
}
