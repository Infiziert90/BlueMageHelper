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
        {"2", new SpellSource("(Last Boss) Gobmachine G-VI")},
        {"3", new SpellSource("Ultros")},
        {"4", new SpellSource("(2nd Boss) Zu")},
        {"5", new SpellSource("Lvl. 50 Magitek Vanguard H-2", "Northern Thanalan","(x16, y15)")},
        {"6", new SpellSource("(1st Boss) ADS")},
        {"7", new SpellSource("(Trash) Baalzephon", "The Lost City of Amdapor", RegionType.Dungeon)},
        {"8", new SpellSource("Lvl. 13. Killer Wespe", "Middle La Noscea", "(x15, y15)")},
        {"9", new SpellSource("(Last Boss) Siren")},
        {"10", new SpellSource("(2nd Boss) Coincounter")},
        {"11", new SpellSource("Lvl. 34 Basalt Golem", "Outer La Noscea", "(x14, y16)")},
        {"12", new SpellSource("Lvl. 20 Wild Boar", "East Shroud", "(x18, y24)")},
        {"13", new SpellSource("Available after learning 10 spells.", RegionType.Buy)},
        {"14", new SpellSource("(Trash) Manor Sentry")},
        {"15", new SpellSource("(Last Boss) Tonberry King")},
        {"16", new SpellSource("Lvl. 9 Trickster Imp", "Central Shroud", "(x27, y24)")},
        {"17", new SpellSource("Lvl. 7 Cave Bat", "Lower La Noscea", "(x27, y16)")},
        {"18", new SpellSource("Lvl 12-17 Treant Sapling", "North Shroud", "(x24, y28)")},
        {"19", new SpellSource("Lvl. 5-7 Goblin Fisher", "Middle La Noscea", "(x23, y21)")},
        {"20", new SpellSource("Available after learning 5 spells.", type: RegionType.Buy)},
        {"21", new SpellSource("Lvl. 12 Glide Bomb", "Western Thanalan", "(x27, y16)")},
        {"22", new SpellSource("Available after learning 20 spells.", type: RegionType.Buy)},
        {"23", new SpellSource("Lvl. 32 Qiqirn Gullroaster", "Central Thanalan", "(x26, y32)")},
        {"24", new SpellSource("Lvl. 30 Apkallu", "Eastern La Noscea", "(x27, y35)")},
        {"25", new SpellSource("Typhon")},
        {"26", new SpellSource("Ultros")},
        {"27", new SpellSource("(Last Boss) Anantaboga")},
        {"28", new SpellSource("Lvl. 31 Stroper", "Central Shroud", "(x12, y23)")},
        {"29", new SpellSource("(2nd Boss) Cuca Fera")},
        {"30", new SpellSource("Available after learning 10 spells.", type: RegionType.Buy)},
        {"31", new SpellSource("Lvl. 22 Laughing Gigantoad", "Western Thanalan", "(x14, y6)")},
        {"32", new SpellSource("Lvl. 24 Giggling Gigantoad", "Western Thanalan", "(x15, y7)")},
        {"33", new SpellSource("(Last Boss) Chimera")},
        {"34", new SpellSource("(Last Boss) Chimera")},
        {"35", new SpellSource("(First Part) Enkidu")},
        {"36", new SpellSource("Lvl. 26 Sabotender Bailaor", "Southern Thanalan", "(x16, y15)")},
        {"37", new SpellSource("(Last Boss) Kraken")},
        {"38", new SpellSource("(1st Boss) Frumious Koheel Ja")},
        {"39", new SpellSource("Clear 10 stages in The Masked Carnivale", type: RegionType.Buy)},
        {"40", new SpellSource("(1st Boss) Karlabos")},
        {"41", new SpellSource("(Last Boss) Galvanth the Dominator")},
        {"42", new SpellSource("Clear 20 Stages in The Masked Carnivale", type: RegionType.Buy)},
        {"43", new SpellSource("Lvl. 45 Lentic Mudpuppy", "Mor Dhona", "(x13, y10)")},
        {"44", new SpellSource("Garuda")},
        {"45", new SpellSource("Ifrit")},
        {"46", new SpellSource("Titan")},
        {"47", new SpellSource("Ramuh")},
        {"48", new SpellSource("Shiva")},
        {"49", new SpellSource("Leviathan")},
        {"50", new SpellSource("(Last Boss) Opinicus")},
        {"51", new SpellSource("(Last Boss) Living Liquid")},
        {"52", new SpellSource("Lvl. 51 Lone Yeti", "Coerthas Western Highlands", "(x20, y31)")},
        {"53", new SpellSource("Lvl. 50 Conodont", "Sea of Clouds", "(x26, y33)")},
        {"54", new SpellSource("(1st Boss) Faust")},
        {"55", new SpellSource("(2nd Boss) Ash", "Haukke Manor (Hard)", RegionType.Dungeon)},
        {"56", new SpellSource("Lvl. 50 Paissa", "Sea of Clouds", "(x24, y33)")},
        {"57", new SpellSource("Lvl. 59 Empuse", "Azys Lla", "(x29, y12)")},
        {"58", new SpellSource("(White Mage) Furryfoot Kupli Kipp")},
        {"59", new SpellSource("(Trash) Alexandrian Hider", "Alexander - The Breath of the Creator", RegionType.Dungeon)},
        {"60", new SpellSource("(3rd Boss) Apanda")},
        {"61", new SpellSource("(2nd Boss) Queen Hawk")},
        {"62", new SpellSource("Lvl. 59 Poroggo", "The Dravanian Hinterlands", "(x12, y34)")},
        {"63", new SpellSource("(2nd Boss) Zu")},
        {"64", new SpellSource("Lvl. 56 Dhalmel", "Sea of Clouds", "(x16, y32)")},
        {"65", new SpellSource("(Trash) White Knight")},
        {"66", new SpellSource("(Trash) Black Knight")},
        {"67", new SpellSource("(Trash) Page 64")},
        {"68", new SpellSource("(2nd Boss) Armored Weapon")},
        {"69", new SpellSource("The Manipulator")},
        {"70", new SpellSource("(2nd Boss - Trash) Sabotender Gaurdia")},
        {"71", new SpellSource("Available after learning 50 spells.", type: RegionType.Buy)},
        {"72", new SpellSource("Clear 30 Stages in The Masked Carnivale", type: RegionType.Buy)},
        {"73", new SpellSource("Lvl. 57-58 Abalathian Wamoura", "Sea of Clouds", "(x10, y17)")},
        {"74", new SpellSource("Lvl. 56 Cloud Wyvern", "The Churning Mists", "(x26, y28)")},
        {"75", new SpellSource("(Last Boss) Caduceus")},
        {"76", new SpellSource("(Trash) Mechanoscribe")},
        {"77", new SpellSource("(1st Boss) Ghrah Luminary")},
        {"78", new SpellSource("Ravana")},
        {"79", new SpellSource("Sophia")},
        {"80", new SpellSource("(Last Phase) Brute Justice")},
        {"81", new SpellSource("Lvl. 67 Ebisu Catfish", "Yanxia", "(x28, y6)")},
        {"82", new SpellSource("Lvl. 67 Ebisu Catfish", "Yanxia", "(x28, y6)")},
        {"83", new SpellSource("(2nd Boss) Dojun-maru")},
        {"84", new SpellSource("(Last Boss) Mist Dragon")},
        {"85", new SpellSource("Lakshmi")},
        {"86", new SpellSource("Phantom Train")},
        {"87", new SpellSource("(Last Boss) Tokkapchi")},
        {"88", new SpellSource("Available after reaching level 70", type: RegionType.Buy)},
        {"89", new SpellSource("(Last Boss) Genbu")},
        {"90", new SpellSource("(Last Boss) Ivon Coeurlfish")},
        {"91", new SpellSource("Lvl. 24 Master Coeurl", "Upper La Noscea", "(x8.9, y21.4)")},
        {"92", new SpellSource("Lvl. 68 Kongamato", "The Peaks", "(x11, y25)")},
        {"93", new SpellSource("Alte Roite")},
        {"94", new SpellSource("Omega")},
        {"95", new SpellSource("Available after learning 100 spells.", type: RegionType.Buy)},
        {"96", new SpellSource("Lvl. 69 Salt Dhruva", "The Lochs", "(x22, y22)")},
        {"97", new SpellSource("(1st Boss) Kelpie")},
        {"98", new SpellSource("(Corridor Wave) Sai Taisui")},
        {"99", new SpellSource("Lvl. 53 Courser Chocobo", "The Dravanian Forelands", "(x37.5, y23.7)")},
        {"100", new SpellSource("Available after learning 100 spells.", type: RegionType.Buy)},
        {"101", new SpellSource("Omega")},
        {"102", new SpellSource("(Last Boss) Qitian Dasheng")},
        {"103", new SpellSource("Suzaku")},
        {"104", new SpellSource("Tsukuyomi")},
    };
}