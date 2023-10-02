using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace BlueMageHelper;

public class Spell
{
    public string Name;
    public uint Icon;
    public readonly List<SpellSource> Sources = new();

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public Spell() { }

    [JsonIgnore] public SpellSource Source => Sources[0];
    [JsonIgnore] public bool HasMultipleSources => Sources.Count > 1;
}

public class SpellSource
{
    public string Info;
    public string AcquiringTips = "";

    public RegionType Type = RegionType.Default;
    public uint TerritoryTypeID = 0;
    public float xCoord = 0;
    public float yCoord = 0;

    [NonSerialized] public TerritoryType TerritoryType = null;
    [NonSerialized] public MapLinkPayload? MapLink = null;

    [NonSerialized] public bool IsDuty = false;
    [NonSerialized] public string DutyName = "";
    [NonSerialized] public string DutyMinLevel = "1";
    [NonSerialized] public string PlaceName = "";

    [NonSerialized] public bool CurrentlyUnknown = false;

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public SpellSource() { }

    public SpellSource(string info)
    {
        Info = info;
    }

    public SpellSource(string info, RegionType type)
    {
        Info = info;
        Type = type;
    }

    [OnDeserialized]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void Initialize(StreamingContext _)
    {
        if (TerritoryTypeID != 0)
        {
            TerritoryType = Plugin.Data.GetExcelSheet<TerritoryType>()!.GetRow(TerritoryTypeID)!;
            PlaceName = TerritoryType.PlaceName.Value!.Name;

            var content = Plugin.Data.GetExcelSheet<ContentFinderCondition>()!
                .FirstOrDefault(content => content.TerritoryType.Row == TerritoryType.RowId);
            if (content != null && content.Name != "")
            {
                IsDuty = true;
                DutyName = Helper.ToTitleCaseExtended(content.Name, 0);
                DutyMinLevel = content.ClassJobLevelRequired.ToString();
            }
        }

        if (Type == RegionType.OpenWorld && TerritoryType != null)
        {
            if (xCoord == 0 || yCoord == 0)
            {
                CurrentlyUnknown = true;
                MapLink = new MapLinkPayload(TerritoryType.RowId, TerritoryType.Map.Row, 15, 15);
                return;
            }

            try
            {
                MapLink = new MapLinkPayload(TerritoryType.RowId, TerritoryType.Map.Row, xCoord, yCoord);
            }
            catch
            {
                Plugin.Log.Error($"MapLink creation failed for {Info}.");
            }
        }
        else if (Type == RegionType.Buy)
        {
            // Ul'dah - Steps of Thal - (x12.5, y12.9)
            TerritoryType = Plugin.Data.GetExcelSheet<TerritoryType>()!.GetRow(131)!;
            PlaceName = TerritoryType.PlaceName.Value!.Name;
            MapLink = new MapLinkPayload(TerritoryType.RowId, TerritoryType.Map.Row, 12.5f, 12.9f);
        }
    }

    public bool CompareTerritory(SpellSource other)
    {
        if (other.TerritoryType == null) return false;
        return other.TerritoryType.RowId == TerritoryType.RowId;
    }

    public unsafe void SetRegion(AtkTextNode* region, AtkImageNode* regionType)
    {
        var text = Type switch
        {
            RegionType.OpenWorld => $"{MapLink!.PlaceName} {MapLink!.CoordinateString}",
            RegionType.Buy => $"{MapLink!.PlaceName} {MapLink!.CoordinateString}",
            RegionType.Dungeon => $"{TerritoryType.PlaceName.Value!.Name}",
            _ => ""
        };

        if (text != "") region->SetText(text);
        if (Type != RegionType.Default) regionType->PartId = (ushort) Type;
    }
}

// ID = PartID
public enum RegionType
{
    OpenWorld = 2,
    Buy = 3,
    Dungeon = 13,
    Fate = 26,

    // non PartIDs
    Default = 99,
    ARank = 100,
    BRank = 101,
    SRank = 102,

    // New Patch
    Unknown = 999,
}

public static class SpellSources
{
    public static Dictionary<string, Spell> Spells = new();
}