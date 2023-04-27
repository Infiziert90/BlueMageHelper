using System;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace BlueMageHelper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Configuration", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var changed = false;

        ImGui.TextColored(ImGuiColors.DalamudViolet, "Vanilla Spellbook:");
        ImGui.Indent(10.0f);
        changed |= ImGui.Checkbox("Show hint even if a spell is already unlocked.", ref Configuration.ShowHintEvenIfUnlocked);
        ImGui.Unindent(10.0f);

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, "Spellbook:");
        ImGui.Indent(10.0f);
        changed |= ImGui.Checkbox("Show only unlearned spells.", ref Configuration.ShowOnlyUnlearned);
        ImGui.Unindent(10.0f);

        if (changed)
            Configuration.Save();
    }
}
