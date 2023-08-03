using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace BlueMageHelper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 460),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##ConfigTabBar"))
        {
            General();

            About();
        }
        ImGui.EndTabBar();
    }

    private void General()
    {
        if (!ImGui.BeginTabItem("General"))
            return;

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
        {
            Plugin.MainWindow.SourceOptions = null;
            Configuration.Save();
        }

        ImGui.EndTabItem();
    }

    private static void About()
    {
        if (!ImGui.BeginTabItem("About"))
            return;

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("AboutContent", new Vector2(0, -buttonHeight)))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextUnformatted("Author:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedGold, Plugin.Authors);

            ImGui.TextUnformatted("Discord:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedGold, "@infi");

            ImGui.TextUnformatted("Version:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedOrange, Plugin.Version);
        }

        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("AboutBottomBar", new Vector2(0, 0), false, 0))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
            if (ImGui.Button("Discord Thread"))
                Dalamud.Utility.Util.OpenLink("https://canary.discord.com/channels/581875019861328007/1067487937735970846");
            ImGui.PopStyleColor();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DPSRed);
            if (ImGui.Button("Issues"))
                Dalamud.Utility.Util.OpenLink("https://github.com/Infiziert90/BlueMageHelper");
            ImGui.PopStyleColor();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.12549f, 0.74902f, 0.33333f, 0.6f));
            if (ImGui.Button("Ko-Fi Tip"))
                Dalamud.Utility.Util.OpenLink("https://ko-fi.com/infiii");
            ImGui.PopStyleColor();
        }
        ImGui.EndChild();

        ImGui.EndTabItem();
    }
}
