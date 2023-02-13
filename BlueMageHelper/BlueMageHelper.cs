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
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using static BlueMageHelper.SpellSources;

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
        public MainWindow MainWindow = null!;
        public ConfigWindow ConfigWindow = null!;
        
        private const int BlankTextTextnodeIndex = 54;
        private const int SpellNumberTextnodeIndex = 62;
        private const int RegionTextnodeIndex = 57;
        private const int RegionImageIndex = 56;
        private const int UnlearnedNodeIndex = 63;
        
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

            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);
            
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);
            
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens a small guide book"
            });
            
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += AozNotebookAddonManager;
            
            TexturesCache.Initialize();
            
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

        private void AozNotebookAddonManager(Framework framework)
        {
            try
            {
                var addonPtr = GameGui.GetAddonByName("AOZNotebook", 1);
                if (addonPtr == nint.Zero)
                    return;
                SpellbookWriter(addonPtr);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                PluginLog.Verbose("Blue Mage Helper caught an exception: "+ e);
            }
        }

        private unsafe void SpellbookWriter(IntPtr addonPtr)
        {
            AtkUnitBase* spellbookBaseNode = (AtkUnitBase*)addonPtr;
            if (spellbookBaseNode->UldManager.NodeListCount < UnlearnedNodeIndex + 1)
                return;

            AtkTextNode* unlearnedTextNode = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[UnlearnedNodeIndex];
            if (!unlearnedTextNode->AtkResNode.IsVisible && !Configuration.ShowHintEvenIfUnlocked)
                return;

            AtkTextNode* emptyTextnode = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[BlankTextTextnodeIndex];
            AtkTextNode* spellNumberTextnode = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[SpellNumberTextnodeIndex];
            AtkTextNode* region = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[RegionTextnodeIndex];
            AtkImageNode* regionImage = (AtkImageNode*)spellbookBaseNode->UldManager.NodeList[RegionImageIndex];
            var spellNumberString = spellNumberTextnode->NodeText.ToString();
            spellNumberString = spellNumberString[1..]; // Remove the # from the spell number
            
            // Try to preserve last seen org text
            if (spellNumberString != lastSeenSpell) lastOrgText = emptyTextnode->NodeText.ToString();
            lastSeenSpell = spellNumberString;
            
            var spellSource = GetHintText(spellNumberString);
            emptyTextnode->SetText($"{(lastOrgText != "" ? $"{lastOrgText}\n" : "")}{spellSource.Info}");
            emptyTextnode->AtkResNode.ToggleVisibility(true);
            
            // Change region if needed
            spellSource.SetRegion(region, regionImage);
        }

        private static SpellSource GetHintText(string spellNumber) =>
            Sources.TryGetValue(spellNumber, out var spell) ? spell.Source : new SpellSource($"No data #{spellNumber}");
        
        public void Dispose()
        {
			TexturesCache.Instance?.Dispose();
			
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            Framework.Update -= AozNotebookAddonManager;
        }
        
        private void OnCommand(string command, string args) => MainWindow.IsOpen = true;
        private void DrawUI() => WindowSystem.Draw();
        private void DrawConfigUI() => ConfigWindow.IsOpen = true;
        public void SetMapMarker(MapLinkPayload map) => GameGui.OpenMapWithMapLink(map);

        #region internal
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
        #endregion
    }
}
