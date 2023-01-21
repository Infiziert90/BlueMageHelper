using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BlueMageHelper;

public class SpellSource
{
    public string Info;
    
    private string Region = "";
    private RegionType Type = RegionType.Default;
    private string Coords = "";
    private string FateName = "";

    public SpellSource(string info)
    {
        Info = info;
    }
    
    public SpellSource(string info, string region, string coords)
    {
        Region = region;
        Info = info;
        Coords = coords;
        Type = RegionType.OpenWorld;
    }
    
    public SpellSource(string info, string region, string fate, string coords)
    {
        Region = region;
        Info = info;
        Coords = coords;
        FateName = fate;
        Type = RegionType.Fate;
    }
    
    public SpellSource(string info, RegionType type)
    {
        Info = info;
        Type = type;
    }
    
    // Overwrite original type
    public SpellSource(string info, string region, RegionType type)
    {
        Info = info;
        Region = region;
        Type = type;
    }
    
    public unsafe void SetRegion(AtkTextNode* region, AtkImageNode* regionType)
    {
        var text = Type switch
        {
            RegionType.OpenWorld => $"{Region} - {Coords}",
            RegionType.Fate => $"{Region} - {FateName} - {Coords}",
            RegionType.Buy => "Ul'dah - Steps of Thal - (x12.5, y12.9)",
            _ => Region
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
    public static Dictionary<string, SpellSource> Sources = new()
    {
        {"1", new SpellSource("Become a Blue Mage")},
        {"2", new SpellSource("Lvl. 50 Gobmachine G-VI")},
        {"3", new SpellSource("Lvl. 50 Ultros")},
        {"4", new SpellSource("Lvl. 50 Zu")},
        {"5", new SpellSource("Lvl. 46 Abandoned Vanguard", "Northern Thanalan", "Reverse Engineering", "(x18, y15)")},
        {"6", new SpellSource("Lvl. 50 ADS")},
        {"7", new SpellSource("Lvl. 50 Baalzephon", "The Lost City of Amdapor", RegionType.Dungeon)},
        {"8", new SpellSource("Lvl. 13. Killer Wespe", "Middle La Noscea", "(x15, y15)")},
        {"9", new SpellSource("Lvl. 50 Siren")},
        {"10", new SpellSource("Lvl 47. Coincounter")},
        {"11", new SpellSource("Lvl. 50 Gogmagolem")},
        {"12", new SpellSource("Lvl. 20 Wild Boar", "East Shroud", "(x18, y24)")},
        {"13", new SpellSource("Available after learning 10 spells.", RegionType.Buy)},
        {"14", new SpellSource("Lvl. 28 Manor Sentry")},
        {"15", new SpellSource("Lvl. 50 Tonberry King")},
        {"16", new SpellSource("Lvl. 9 Trickster Imp", "Central Shroud", "(x27, y24)")},
        {"17", new SpellSource("Lvl. 7 Cave Bat", "Lower La Noscea", "(x27, y16)")},
        {"18", new SpellSource("Lvl 12-17 Treant Sapling", "North Shroud", "(x24, y28)")},
        {"19", new SpellSource("Lvl. 5-7 Goblin Fisher", "Middle La Noscea", "(x23, y21)")},
        {"20", new SpellSource("Available after learning 5 spells.", type: RegionType.Buy)},
        {"21", new SpellSource("Lvl. 12 Glide Bomb", "Western Thanalan", "(x27, y16)")},
        {"22", new SpellSource("Available after learning 20 spells.", type: RegionType.Buy)},
        {"23", new SpellSource("Lvl. 32 Qiqirn Gullroaster", "Central Thanalan", "(x26, y32)")},
        {"24", new SpellSource("Lvl. 30 Apkallu", "Eastern La Noscea", "(x27, y35)")},
        {"25", new SpellSource("Lvl. 50 Typhon")},
        {"26", new SpellSource("Lvl. 50 Ultros")},
        {"27", new SpellSource("Lvl. 50 Anantaboga")},
        {"28", new SpellSource("Lvl. 31 Stroper", "Central Shroud", "(x12, y23)")},
        {"29", new SpellSource("Lvl. 50 Cuca Fera")},
        {"30", new SpellSource("Available after learning 10 spells.", type: RegionType.Buy)},
        {"31", new SpellSource("Lvl. 22 Laughing Gigantoad", "Western Thanalan", "(x14, y6)")},
        {"32", new SpellSource("Lvl. 24 Giggling Gigantoad", "Western Thanalan", "(x15, y7)")},
        {"33", new SpellSource("Lvl. 38 Chimera")},
        {"34", new SpellSource("Lvl. 38 Chimera")},
        {"35", new SpellSource("Lvl. 50 Enkidu")},
        {"36", new SpellSource("Lvl. 26 Sabotender Bailaor", "Southern Thanalan", "(x16, y15)")},
        {"37", new SpellSource("Lvl. 50 Kraken")},
        {"38", new SpellSource("Lvl. 50 Frumious Koheel Ja")},
        {"39", new SpellSource("Clear 10 stages in The Masked Carnivale", type: RegionType.Buy)},
        {"40", new SpellSource("Lvl. 50 Karlabos")},
        {"41", new SpellSource("Lvl. 16 Galvanth the Dominator")},
        {"42", new SpellSource("Clear 20 Stages in The Masked Carnivale", type: RegionType.Buy)},
        {"43", new SpellSource("Lvl. 45 Lentic Mudpuppy", "Mor Dhona", "(x13, y10)")},
        {"44", new SpellSource("Lvl. 50 Garuda")},
        {"45", new SpellSource("Lvl. 20-50 Ifrit")},
        {"46", new SpellSource("Lvl. 50 Titan")},
        {"47", new SpellSource("Lvl. 50 Ramuh")},
        {"48", new SpellSource("Lvl. 50 Shiva")},
        {"49", new SpellSource("Lvl. 50 Leviathan")},
        {"50", new SpellSource("Lvl. 51 Opinicus")},
        {"51", new SpellSource("Lvl. ??? Living Liquid")},
        {"52", new SpellSource("Lvl. 51 Lone Yeti", "Coerthas Western Highlands", "(x20, y31)")},
        {"53", new SpellSource("Lvl. 50 Conodont", "Sea of Clouds", "(x26, y33)")},
        {"54", new SpellSource("Lvl. ??? Faust\nLvl. ??? Strum Doll")},
        {"55", new SpellSource("Lvl. ??? Everlasting Bibliotaph")},
        {"56", new SpellSource("Lvl. 50 Paissa", "Sea of Clouds", "(x24, y33)")},
        {"57", new SpellSource("Lvl. 59 Empuse", "Azys Lla", "(x29, y12)")},
        {"58", new SpellSource("Lvl. 50 Furryfoot Kupli Kipp")},
        {"59", new SpellSource("Lvl. 60 Alexandrian Hider", "Alexander - The Breath of the Creator", RegionType.Dungeon)},
        {"60", new SpellSource("Lvl. 60 Apanda")},
        {"61", new SpellSource("Lvl. 60 Queen Hawk")},
        {"62", new SpellSource("Lvl. 59 Poroggo", "The Dravanian Hinterlands", "(x12, y34)")},
        {"63", new SpellSource("Lvl. 50 Zu")},
        {"64", new SpellSource("Lvl. 56 Dhalmel", "Sea of Clouds", "(x16, y32)")},
        {"65", new SpellSource("Lvl. 57 White Knight")},
        {"66", new SpellSource("Lvl. 57 Black Knight")},
        {"67", new SpellSource("Lvl. 59 Page 64")},
        {"68", new SpellSource("Lvl. 60 Armored Weapon")},
        {"69", new SpellSource("Lvl. 60 The Manipulator")},
        {"70", new SpellSource("Lvl. 50 Sabotender Gaurdia")},
        {"71", new SpellSource("Available after learning 50 spells.", type: RegionType.Buy)},
        {"72", new SpellSource("Clear 30 Stages in The Masked Carnivale", type: RegionType.Buy)},
        {"73", new SpellSource("Lvl. 57-58 Abalathian Wamoura", "Sea of Clouds", "(x10, y17)")},
        {"74", new SpellSource("Lvl. 56 Cloud Wyvern", "The Churning Mists", "(x26, y28)")},
        {"75", new SpellSource("Lvl. 50 Caduceus")},
        {"76", new SpellSource("Lvl. 60 Mechanoscribe")},
        {"77", new SpellSource("Lvl. 60 Ghrah Luminary")},
        {"78", new SpellSource("Lvl. 53 Ravana")},
        {"79", new SpellSource("Lvl. 60 Sophia")},
        {"80", new SpellSource("Lvl. 60 Brute Justice")},
        {"81", new SpellSource("Lvl. 67 Ebisu Catfish", "Yanxia", "(x28, y6)")},
        {"82", new SpellSource("Lvl. 67 Ebisu Catfish", "Yanxia", "(x28, y6)")},
        {"83", new SpellSource("Lvl. 70 Dojun-maru")},
        {"84", new SpellSource("Lvl. 70 Mist Dragon")},
        {"85", new SpellSource("Lvl. 67 Lakshmi")},
        {"86", new SpellSource("Lvl. 70 Phantom Train")},
        {"87", new SpellSource("Lvl. 70 Tokkapchi")},
        {"89", new SpellSource("Lvl. 70 Genbu")},
        {"90", new SpellSource("Lvl. 70 Ivon Coeurlfish")},
        {"91", new SpellSource("Lvl. 24 Master Coeurl", "Upper La Noscea", "(x8.9, y21.4)")},
        {"92", new SpellSource("Lvl. 68 Kongamato", "The Peaks", "(x11, y25)")},
        {"93", new SpellSource("Lvl. 70 Alte Roite")},
        {"94", new SpellSource("Lvl. 70 Omega")},
        {"95", new SpellSource("Available after learning 100 spells.", type: RegionType.Buy)},
        {"96", new SpellSource("Lvl. 69 Salt Dhruva", "The Lochs", "(x22, y22)")},
        {"97", new SpellSource("Lvl. 70 Kelpie")},
        {"98", new SpellSource("Lvl. 70 Sai Taisui")},
        {"99", new SpellSource("Lvl. 53 Courser Chocobo", "The Dravanian Forelands", "(x37.5, y23.7)")},
        {"100", new SpellSource("Available after learning 100 spells.", type: RegionType.Buy)},
        {"101", new SpellSource("Lvl. 70 Omega")},
        {"102", new SpellSource("Lvl. 70 Qitian Dasheng")},
        {"103", new SpellSource("Lvl. 70 Suzaku")},
        {"104", new SpellSource("Lvl. 70 Tsukuyomi")},
    };
}