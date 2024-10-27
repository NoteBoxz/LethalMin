using System;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json;
using LethalModDataLib.Attributes;
using LethalModDataLib.Enums;
using LethalModDataLib.Base;

public class LethalMinSaveDataWithLib : ModDataContainer
{
    //EzSave
    public List<int> OnionsCollected { get; set; }

    public Dictionary<int, int[]> OnionsFused { get; set; }

    public List<LethalMin.OnionPikminStorage> PikminStored { get; set; }

    public int PikminLeftLastRound { get; set; }

    protected override void PostLoad()
    {
        base.PostLoad();
        //Check if the data is null
        if (OnionsCollected == null)
        {
            LethalMin.LethalMin.Logger.LogInfo("OnionsCollected is null, creating new list");
            OnionsCollected = new List<int>();
        }
        if (OnionsFused == null)
        {
            LethalMin.LethalMin.Logger.LogInfo("OnionsFused is null, creating new dictionary");
            OnionsFused = new Dictionary<int, int[]>();
        }
        if (PikminStored == null)
        {
            LethalMin.LethalMin.Logger.LogInfo("PikminStored is null, creating new list");
            PikminStored = new List<LethalMin.OnionPikminStorage>();
        }
    }

    protected override void PostSave()
    {
        base.PostSave();
        LethalMin.LethalMin.Logger.LogInfo($"collected onions: {OnionsCollected.Count}");
        LethalMin.LethalMin.Logger.LogInfo($"fused onions: {OnionsFused.Count}");
        LethalMin.LethalMin.Logger.LogInfo($"stored onions: {PikminStored.Count}");
    }
}