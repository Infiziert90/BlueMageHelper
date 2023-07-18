using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using static BlueMageHelper.SpellSources;

namespace BlueMageHelper.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private int selectedSpellNumber = 1; // 0 is the first learned blu skill
    private int selectedSource = 0;
    private static Vector2 size = new(80, 80);

    public MainWindow(Plugin plugin) : base("Grimoire", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(370, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var currentSpell = selectedSpellNumber;
        var keyList = Spells.Keys.Where(num => !Configuration.ShowOnlyUnlearned || Plugin.UnlockedSpells.TryGetValue(num, out var isUnlocked) && !isUnlocked).ToList();

        if (!keyList.Any())
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, "All spells learned, nothing to show.");
            ImGui.TextColored(ImGuiColors.ParsedOrange, "[You can disable this option in the config]");
            return;
        }

        if (selectedSpellNumber >= keyList.Count)
            selectedSpellNumber = Configuration.ShowOnlyUnlearned ? 0 : 1; // 0 is the first learned blu skill, so we skip to 1 for all

        var stringList = Spells.Where(x => keyList.Contains(x.Key)).Select(x => $"{x.Key} - {x.Value.Name}").ToArray();
        ImGui.Combo("##spellSelector", ref selectedSpellNumber, stringList, stringList.Length);
        DrawArrows(ref selectedSpellNumber, stringList.Length, 0);

        if (currentSpell != selectedSpellNumber)
            selectedSource = 0;

        ImGuiHelpers.ScaledDummy(10);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (!Spells.Any())
            return;

        var selectedSpell = Spells[keyList[selectedSpellNumber]];
        DrawIcon(selectedSpell.Icon);
        ImGuiHelpers.ScaledDummy(10);

        if (ImGui.BeginChild("Content", new Vector2(0, -30), false, 0))
        {
            var source = selectedSpell.Sources[selectedSource];
            if (selectedSpell.HasMultipleSources)
            {
                var sourcesList = selectedSpell.Sources.Select(x => $"{x.Info} ").ToArray();
                ImGui.Combo("##sourcesSelector", ref selectedSource, sourcesList, sourcesList.Length);
                DrawArrows(ref selectedSource, selectedSpell.Sources.Count, 2);
                source = selectedSpell.Sources[selectedSource];
            }
            else
            {
                if (source.Type != RegionType.Unknown)
                    ImGui.TextUnformatted($"{(source.Type != RegionType.Buy ? "Mob" : "Info")}: {source.Info}");
                else
                    ImGui.TextUnformatted($"Currently Unknown");
            }

            switch (source.Type)
            {
                case RegionType.ARank:
                    ImGui.TextUnformatted($"Note: Rank A Elite Mark");
                    break;
                case RegionType.BRank:
                    ImGui.TextUnformatted($"Note: Rank B Elite Mark");
                    break;
                case RegionType.SRank:
                    ImGui.TextUnformatted($"Note: Rank S Elite Mark");
                    break;
            }

            if (source.Type != RegionType.Buy)
                ImGui.TextUnformatted($"Min Lvl: {source.DutyMinLevel}");

            if (source.TerritoryType != null)
                ImGui.TextUnformatted(!source.IsDuty ? $"Region: {source.PlaceName}" : $"Duty: {source.DutyName}");

            var isHunt = source.Type is RegionType.ARank or RegionType.BRank or RegionType.SRank;
            if (source.MapLink != null || isHunt)
            {
                if (Plugin.TeleportConsumer.IsAvailable)
                {
                    if (ImGui.Button("T"))
                        Plugin.TeleportToNearestAetheryte(source);
                    ImGui.SameLine();
                }

                if (!isHunt)
                {
                    if (!source.CurrentlyUnknown)
                    {
                        if (ImGui.Selectable($"Coords: {source.MapLink.CoordinateString}##mapCoords"))
                            Plugin.SetMapMarker(source.MapLink);
                    }
                    else
                    {
                        if (ImGui.Selectable($"Exact location currently unknown##mapCoords"))
                            Plugin.SetMapMarker(source.MapLink);
                    }
                }
                else
                {
                    if (ImGui.Selectable($"Random Location##mapCoords"))
                        Plugin.SetMapMarker(source.MapLink);
                }
            }

            ImGui.TextUnformatted($"Learned: ");
            DrawProgressSymbol(Plugin.UnlockedSpells.TryGetValue(keyList[selectedSpellNumber], out var unlocked) && unlocked);

            if (source.TerritoryType != null && source.Type != RegionType.Buy)
            {
                var combos = Spells
                    .Where(key => keyList.Contains(key.Key))
                    .Where(key => key.Key != keyList[selectedSpellNumber])
                    .Where(spell => spell.Value.Sources.Any(spellSource => source.CompareTerritory(spellSource)))
                    .ToArray();

                if (combos.Any())
                {
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.Separator();
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.TextUnformatted("Same location:");
                    foreach (var (key, value) in combos)
                    {
                        if (ImGui.Selectable($"{key} - {value.Name}"))
                        {
                            selectedSource = value.Sources.FindIndex(x => x.TerritoryTypeID == source.TerritoryTypeID);
                            selectedSpellNumber = keyList.FindIndex(val => val == key);;
                        }
                    }
                }
            }

            if (source.AcquiringTips != "")
            {
                ImGuiHelpers.ScaledDummy(5);
                ImGui.Separator();
                ImGuiHelpers.ScaledDummy(5);

                if (ImGui.CollapsingHeader($"Acquisition Tips##{selectedSpell.Icon}"))
                {
                    foreach (var tip in source.AcquiringTips.Split("\n"))
                    {
                        ImGui.Bullet();
                        ImGui.PushTextWrapPos();
                        ImGui.TextUnformatted(tip);
                        ImGui.PopTextWrapPos();
                    }
                }
            }
        }
        ImGui.EndChild();

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
            ImGui.TextDisabled("Data sourced from ffxiv.consolegameswiki.com");
        ImGui.EndChild();
    }

    private static void DrawArrows(ref int selected, int length, int id)
    {
        ImGui.SameLine();
        if (selected == 0) ImGui.BeginDisabled();
        if (Dalamud.Interface.Components.ImGuiComponents.IconButton(id, FontAwesomeIcon.ArrowLeft)) selected--;
        if (selected == 0) ImGui.EndDisabled();

        ImGui.SameLine();
        if (selected + 1 == length) ImGui.BeginDisabled();
        if (Dalamud.Interface.Components.ImGuiComponents.IconButton(id+1, FontAwesomeIcon.ArrowRight)) selected++;
        if (selected + 1 == length) ImGui.EndDisabled();
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, size);
    }

    private static void DrawProgressSymbol(bool done)
    {
        var color = done ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
        var text = done ? FontAwesomeIcon.Check.ToIconString() : FontAwesomeIcon.Times.ToIconString();

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(color, text);
        ImGui.PopFont();
    }
}
