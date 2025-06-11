using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin
{
   /// <summary>
   /// Host: The config item only takes effect when the host has it enabled.
   /// Client: The config item takes effect per client and other clients can see the effect without them needing to enable it.
   /// Local: The config item only takes effect on the local player. (Other players will not see the effect.)
   /// </summary>
   public enum ConfigItemAuthority
   {
      None = -1,
      Host,
      Client,
      Local,
      DoNotSync
   }


   public class ConfigItemAuthorityManager : NetworkBehaviour
   {
 
   }
}