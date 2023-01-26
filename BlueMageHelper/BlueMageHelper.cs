using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlueMageHelper.Windows;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Loc;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using static BlueMageHelper.SpellSources;
using MapType = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType;

namespace BlueMageHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public static DataManager Data { get; set; } = null!;
        
        public string Name => "Blue Mage Helper";
        private const string CommandName = "/spellbook";
        
        public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        public Configuration Configuration { get; init; }
        private CommandManager CommandManager { get; init; }
        private Framework Framework { get; init; }
        private GameGui GameGui { get; init; }
        public WindowSystem WindowSystem = new("Blue Mage Helper");
        public Localization Loc { get; init; }
        
        private const int blank_text_textnode_index = 54;
        private const int spell_number_textnode_index = 62;
        private const int region_textnode_index = 57;
        private const int region_image_index = 56;
        private const int unlearned_node_index = 63;
        
        private string lastSeenSpell = string.Empty;
        private string lastOrgText = string.Empty;
        
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            Framework = framework;
            GameGui = gameGui;
            CommandManager = commandManager;
            
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
        
            WindowSystem.AddWindow(new MainWindow(this));
            WindowSystem.AddWindow(new ConfigWindow(this));
            
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens a small guide book"
            });
            
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += AOZNotebook_addon_manager;
            
            TexturesCache.Initialize();
            //Loc = new Localization(PluginInterface);
            
            try
            {
                PluginLog.Debug("Loading Spell Sources.");
                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "spells.json");
                SpellSources.Load(path);
            }
            catch (Exception e)
            {
                PluginLog.Error("There was a problem building the Grimoire.");
                PluginLog.Error(e.Message);
            }
        }

        private void AOZNotebook_addon_manager(Framework framework)
        {
            try
            {
                var addon_ptr = GameGui.GetAddonByName("AOZNotebook", 1);
                if (addon_ptr == IntPtr.Zero)
                    return;
                spellbook_writer(addon_ptr);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                PluginLog.Verbose("Blue Mage Helper caught an exception: "+ e);
            }
        }

        private unsafe void spellbook_writer(IntPtr addon_ptr)
        {
            //AddonAOZNotebook* spellbook_addon = (AddonAOZNotebook*)addon_ptr;
            AtkUnitBase* spellbook_base_node = (AtkUnitBase*)addon_ptr;

            if (spellbook_base_node->UldManager.NodeListCount < unlearned_node_index + 1)
                return;

            AtkTextNode* unlearned_textNode = (AtkTextNode*)spellbook_base_node->UldManager.NodeList[unlearned_node_index];
            if (!unlearned_textNode->AtkResNode.IsVisible && !this.Configuration.show_hint_even_if_unlocked)
                return;

            AtkTextNode* empty_textnode = (AtkTextNode*)spellbook_base_node->UldManager.NodeList[blank_text_textnode_index];
            AtkTextNode* spell_number_textnode = (AtkTextNode*)spellbook_base_node->UldManager.NodeList[spell_number_textnode_index];
            AtkTextNode* region = (AtkTextNode*)spellbook_base_node->UldManager.NodeList[region_textnode_index];
            AtkImageNode* regionImage = (AtkImageNode*)spellbook_base_node->UldManager.NodeList[region_image_index];
            var spell_number_string = spell_number_textnode->NodeText.ToString();
            spell_number_string = spell_number_string[1..]; // Remove the # from the spell number
            
            // Try to preserve last seen org text
            if (spell_number_string != lastSeenSpell) lastOrgText = empty_textnode->NodeText.ToString();
            lastSeenSpell = spell_number_string;
            
            var spellSource = get_hint_text(spell_number_string);
            empty_textnode->SetText($"{(lastOrgText != "" ? $"{lastOrgText}\n" : "")}{spellSource.Info}");
            empty_textnode->AtkResNode.ToggleVisibility(true);
            
            // Change region if needed
            spellSource.SetRegion(region, regionImage);
        }

        //TODO use monster IDs and perform a sheets lookup
        private SpellSource get_hint_text(string spell_number)
        {
            return Sources.ContainsKey(spell_number) ? Sources[spell_number].Source : new SpellSource($"No data for spell #{spell_number}");
        }

        public void Dispose()
        {
			TexturesCache.Instance?.Dispose();
			
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            Framework.Update -= AOZNotebook_addon_manager;
        }
        
        private void OnCommand(string command, string args)
        {
            WindowSystem.GetWindow("Grimoire")!.IsOpen = true;
        }
        
        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            WindowSystem.GetWindow("Configuration")!.IsOpen = true;
        }

        public unsafe void SetMapMarker(MapLinkPayload map)
        {
            var instance = AgentMap.Instance();
            if (instance != null)
            {
                instance->IsFlagMarkerSet = 0;
                AgentMap.Instance()->SetFlagMapMarker(map.Map.TerritoryType.Row, map.Map.RowId, map.RawX / 1000.0f, map.RawY / 1000.0f);
                instance->OpenMap(map.Map.RowId, map.Map.TerritoryType.Row, type: MapType.FlagMarker);
            }
        }

        private void PrintTerris()
        {
            var mapSheet = Data.GetExcelSheet<TerritoryType>();
            var contentSheet = Data.GetExcelSheet<ContentFinderCondition>()!;
            foreach (var match in mapSheet)
            {
                if (match.Map.IsValueCreated && match.Map.Value!.PlaceName.Value!.Name != "")
                {
                    if (match.RowId == 0) continue;
                    PluginLog.Information("---------------");
                    PluginLog.Information(match.Map.Value!.PlaceName.Value!.Name);
                    PluginLog.Information($"TerriID: {match.RowId}");
                    PluginLog.Information($"MapID: {match.Map.Row}");

                    var content = contentSheet.FirstOrDefault(x => x.TerritoryType.Row == match.RowId);
                    if (content == null) continue;
                    if (Helper.ToTitleCaseExtended(content.Name, 0) == "") continue;
                    PluginLog.Information($"Duty: {Helper.ToTitleCaseExtended(content.Name, 0)}");
                }
            }
        }        
        
        private struct Skill { public string Name; public string Icon; }
        
        private void PrintBlueSkills()
        {
            // skip the first non existing skill
            var aozActionTransients = Data.GetExcelSheet<AozActionTransient>()!.ToArray()[1..];
            var aozActions = Data.GetExcelSheet<AozAction>()!.ToArray()[1..];
            var sorted = new SortedDictionary<uint, Skill>();
            foreach (var (transient, action) in aozActionTransients.Zip(aozActions))
            {
                sorted.Add(transient.Number, new Skill() {Name = action.Action.Value!.Name.ToString(), Icon = transient.Icon.ToString()});
            }

            foreach (var (key, value) in sorted)
            {
                PluginLog.Information("---------------");
                PluginLog.Information($"Key: {key}");
                PluginLog.Information($"Icon: {value.Icon}");
                PluginLog.Information($"Name: {value.Name}");
            }
        }
    }
}
