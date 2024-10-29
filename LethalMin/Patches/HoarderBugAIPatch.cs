using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;
using GameNetcodeStuff;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalMin.Patches
{
    [HarmonyPatch(typeof(HoarderBugAI))]
    public class HoarderBugAIPatch
    {
    }
}