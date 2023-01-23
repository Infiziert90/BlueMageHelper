﻿using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using static BlueMageHelper.SpellSources;

namespace BlueMageHelper.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private int selectedSpell;
    private static Vector2 size = new(80, 80);

    public MainWindow(Plugin plugin) : base("Spell book", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(370, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
            
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var keyList = Sources.Keys.ToList();
        var stringList = Sources.Select(x => $"{x.Key} - {x.Value.Name}").ToArray();
        ImGui.Combo("##spellSelector", ref selectedSpell, stringList, stringList.Length);
        if (selectedSpell + 1 < stringList.Length)
        {
            ImGui.SameLine();
            if (Dalamud.Interface.Components.ImGuiComponents.IconButton(1, FontAwesomeIcon.Plus)) selectedSpell++;
        }
        if (selectedSpell > 0)
        {
            ImGui.SameLine();
            if (Dalamud.Interface.Components.ImGuiComponents.IconButton(0, FontAwesomeIcon.Minus)) selectedSpell--;
        }

        ImGuiHelpers.ScaledDummy(10);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (Sources.Any())
        {
            var spell = Sources[keyList[selectedSpell]];
            DrawIcon(spell.Icon);
            ImGuiHelpers.ScaledDummy(10);
            
            ImGui.BeginChild("Content", new Vector2(0, -30), false, 0);
            if (spell.Source.Info != "")
            {
                ImGui.TextUnformatted($"{(spell.Source.Type != RegionType.Buy ? "Mob" : "Info")}: {spell.Source.Info}");
            }
            if (spell.Source.TerritoryType != null)
            {
                ImGui.TextUnformatted(!spell.Source.IsDuty
                    ? $"Region: {spell.Source.PlaceName}"
                    : $"Duty: {spell.Source.DutyName}");
            }
            if (spell.Source.MapLink != null)
            {
                if (ImGui.Selectable($"Coords: {spell.Source.MapLink.CoordinateString}##mapCoords"))
                {
                    Plugin.SetMapMarker(spell.Source.MapLink);
                }
            }

            if (spell.Source.TerritoryType != null && spell.Source.Type != RegionType.Buy)
            {
                var combos = Sources.Where(x => x.Key != keyList[selectedSpell] && spell.Source.CompareTerritory(x.Value.Source)).ToArray();
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
                            selectedSpell = keyList.FindIndex(x => x == key);
                        }
                    }
                }
            }

            if (spell.Source.AcquiringTips != "")
            {
                ImGuiHelpers.ScaledDummy(5);
                ImGui.Separator();
                ImGuiHelpers.ScaledDummy(5);
                ImGui.TextUnformatted($"Acquisition Tips:");
                foreach (var tip in spell.Source.AcquiringTips.Split("\n"))
                {
                    ImGui.Bullet();
                    ImGui.PushTextWrapPos();
                    ImGui.TextUnformatted(tip);
                }
            }
            ImGui.EndChild();
            
            ImGui.BeginChild("BottomBar", new Vector2(0,0), false, 0);
            ImGui.TextDisabled("Data sourced from ffxiv.consolegameswiki.com");
            ImGui.EndChild();
        }
    }
    
    public static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, size);
    }
}
