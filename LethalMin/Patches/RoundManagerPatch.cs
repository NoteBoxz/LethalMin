using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using System.IO;
using System;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPostfix]
        private static void SpawnOnions()
        {
            PikminManager.Instance.SpawnOnionItems();
        }
    }
}