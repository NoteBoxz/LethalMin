using System;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json;
using LethalModDataLib.Attributes;
using LethalModDataLib.Enums;
using LethalModDataLib.Base;
namespace LethalMin
{
    public class LethalMinSaveDataWithLib : ModDataContainer
    {
        //EzSave
        public List<int> OnionsCollected = new List<int>();

        public Dictionary<int, int[]> OnionsFused = new Dictionary<int, int[]>();

        public List<OnionPikminStorage> PikminStored = new List<OnionPikminStorage>();

        public int PikminLeftLastRound;

        protected override void PostLoad()
        {
            base.PostLoad();
            //Check if the data is null
            if (OnionsCollected == null)
            {
                LethalMin.Logger.LogInfo("OnionsCollected is null, creating new list");
                OnionsCollected = new List<int>();
            }
            if (OnionsFused == null)
            {
                LethalMin.Logger.LogInfo("OnionsFused is null, creating new dictionary");
                OnionsFused = new Dictionary<int, int[]>();
            }
            if (PikminStored == null)
            {
                LethalMin.Logger.LogInfo("PikminStored is null, creating new list");
                PikminStored = new List<OnionPikminStorage>();
            }
        }

        protected override void PostSave()
        {
            base.PostSave();
            LethalMin.Logger.LogInfo($"collected onions: {OnionsCollected.Count}");
            LethalMin.Logger.LogInfo($"fused onions: {OnionsFused.Count}");
            LethalMin.Logger.LogInfo($"stored onions: {PikminStored.Count}");
        }
    }
}