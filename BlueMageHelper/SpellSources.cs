using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;

namespace BlueMageHelper;

public class Spell
{
    public string Name;
    public uint Icon;
    public SpellSource Source;

    public Spell(JToken j)
    {
        Name = (string) j["name"] ?? "";
        Icon = (uint) int.Parse((string) j["icon"] ?? "0");

        var s = j["source"];
        Source = new SpellSource(j["source"]);
    }
}

public class SpellSource
{
    public string Info;
    public string AcquiringTips = "";
    
    public RegionType Type = RegionType.Default;
    public TerritoryType TerritoryType = null;
    public MapLinkPayload? MapLink = null;

    public bool IsDuty = false;
    public string DutyName = "";
    public string PlaceName = "";
    
    public SpellSource(string info, string acquiringTips = "")
    {
        Info = info;
        AcquiringTips = acquiringTips;
    }

    public SpellSource(JToken source)
    {
        Info = (string) source["info"] ?? "";
        AcquiringTips = (string) source["acquiringTips"] ?? "";
        Type = source["type"]?.ToObject<RegionType>() ?? RegionType.Default;

        if (source["territoryType"] != null)
        {
            TerritoryType = Plugin.Data.GetExcelSheet<TerritoryType>()!.GetRow((uint) source["territoryType"])!;
            PlaceName = TerritoryType.PlaceName.Value!.Name;
            
            var contentSheet = Plugin.Data.GetExcelSheet<ContentFinderCondition>()!;
            var content = contentSheet.FirstOrDefault(x => x.TerritoryType.Row == TerritoryType.RowId);
            if (content != null && content.Name != "")
            {
                IsDuty = true;
                DutyName = Helper.ToTitleCaseExtended(content.Name, 0);
            }
        }
        
        if (Type == RegionType.OpenWorld && TerritoryType != null)
        {
            if (source["xCoord"] == null || source["yCoord"] == null)
            {
                throw new Exception($"Missing xCoord or yCoord for RegionType OpenWorld for {Info}.");
            }

            var xCoord = (float) source["xCoord"];
            var yCoord = (float) source["yCoord"];
            try
            {
                MapLink = new MapLinkPayload(TerritoryType.RowId, TerritoryType.Map.Row, xCoord, yCoord);
            }
            catch
            {
                PluginLog.Error($"MapLink creation failed for {Info}.");
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
    
    Default = 99,
}

public static class SpellSources
{
    public static Dictionary<string, Spell> Sources = new();
    
    public static void Load(string path)
    {
        JObject spell_sources;
        var json_string = File.ReadAllText(path);
        spell_sources = JObject.Parse(json_string);

        foreach (var (key, spell) in spell_sources)
        {
            Sources.Add(key, new Spell(spell));
        }
    }
}