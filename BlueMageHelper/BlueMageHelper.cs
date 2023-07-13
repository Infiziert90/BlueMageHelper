using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Timers;
using BlueMageHelper.IPC;
using BlueMageHelper.Windows;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

using static BlueMageHelper.SpellSources;
using Map = Lumina.Excel.GeneratedSheets.Map;

namespace BlueMageHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public static DataManager Data { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static CommandManager Commands { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; private set; } = null!;
        [PluginService] public static ChatGui ChatGui { get; private set; } = null!;

        public string Name => "Blue Mage Helper";
        private const string CommandName = "/spellbook";

        public static readonly string Authors = "Infi, Sl0nderman";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Blue Mage Helper");
        public MainWindow MainWindow = null!;
        public ConfigWindow ConfigWindow = null!;

        private const int BlankTextTextnodeIndex = 49;
        private const int SpellNumberTextnodeIndex = 57;
        private const int RegionTextnodeIndex = 52;
        private const int RegionImageIndex = 51;
        private const int UnlearnedNodeIndex = 58;

        private string lastSeenSpell = string.Empty;
        private string lastOrgText = string.Empty;

        private List<AozAction> AozActionsCache = null!;
        private List<AozActionTransient> AozTransientCache = null!;

        private bool OnCooldown;
        private readonly Timer Cooldown = new(3 * 1000);

        public Dictionary<string, bool> UnlockedSpells = new();

        private static ExcelSheet<Map> MapSheet = null!;
        private static ExcelSheet<MapMarker> MapMarkerSheet = null!;
        private static ExcelSheet<Aetheryte> AetheryteSheet = null!;

        public static TeleportConsumer TeleportConsumer = null!;

        public Plugin()
        {
            AozActionsCache = Data.GetExcelSheet<AozAction>()!.Where(a => a.Rank != 0).ToList();
            AozTransientCache = Data.GetExcelSheet<AozActionTransient>()!.Where(a => a.Number != 0).ToList();

            MapSheet = Data.GetExcelSheet<Map>()!;
            MapMarkerSheet = Data.GetExcelSheet<MapMarker>()!;
            AetheryteSheet = Data.GetExcelSheet<Aetheryte>()!;

            TeleportConsumer = new TeleportConsumer();

            Cooldown.AutoReset = false;
            Cooldown.Elapsed += (_, __) => OnCooldown = false;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);

            Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens a small guide book"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += AozNotebookAddonManager;
            Framework.Update += CheckLearnedSpells;

            TexturesCache.Initialize();

            try
            {
                PluginLog.Debug("Loading Spell Sources.");
                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "spells.json");
                var jsonString = File.ReadAllText(path);
                Spells = JsonConvert.DeserializeObject<Dictionary<string, Spell>>(jsonString);
            }
            catch (Exception e)
            {
                PluginLog.Error("There was a problem building the Grimoire.");
                PluginLog.Error(e.Message);
                PluginLog.Error(e.StackTrace!);
                if (e.InnerException != null)
                {
                    PluginLog.Error(e.InnerException.Message);
                    PluginLog.Error(e.InnerException.StackTrace!);
                }
            }
        }

        private void CheckLearnedSpells(Framework framework)
        {
            if (OnCooldown)
                return;

            OnCooldown = true;
            Cooldown.Start();

            if (!ClientState.IsLoggedIn)
                return;

            if (ClientState.LocalPlayer == null)
                return;

            UnlockedSpells.Clear();

            foreach (var (transient, action) in AozTransientCache.Zip(AozActionsCache).OrderBy(pair => pair.First.Number))
                UnlockedSpells.Add(transient.Number.ToString(), SpellUnlocked(action.Action.Value!.UnlockLink));
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
            Spells.TryGetValue(spellNumber, out var spell) ? spell.Source : new SpellSource($"No data #{spellNumber}");

        public void Dispose()
        {
			TexturesCache.Instance?.Dispose();

            WindowSystem.RemoveAllWindows();
            Commands.RemoveHandler(CommandName);
            Framework.Update -= AozNotebookAddonManager;
            Framework.Update -= CheckLearnedSpells;
        }

        private void OnCommand(string command, string args) => MainWindow.IsOpen = true;
        private void DrawUI() => WindowSystem.Draw();
        private void DrawConfigUI() => ConfigWindow.IsOpen = true;
        public void SetMapMarker(MapLinkPayload map) => GameGui.OpenMapWithMapLink(map);

        private unsafe bool SpellUnlocked(uint unlockLink) => UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(unlockLink);

        public static void TeleportToNearestAetheryte(SpellSource location)
        {
            var map = location.TerritoryType.Map.Value!;
            var nearestAetheryteId = MapMarkerSheet
                .Where(x => x.DataType == 3 && x.RowId == map.MapMarkerRange)
                .Select(
                    marker => new
                    {
                        distance = Vector2.DistanceSquared(
                            new Vector2(location.xCoord, location.yCoord),
                            ConvertLocationToRaw(marker.X, marker.Y, map.SizeFactor)),
                        rowId = marker.DataKey
                    })
                .MinBy(x => x.distance).rowId;

            // Support the unique case of aetheryte not being in the same map
            var nearestAetheryte = location.TerritoryTypeID == 399
                ? map.TerritoryType?.Value?.Aetheryte.Value
                : AetheryteSheet.FirstOrDefault(x => x.IsAetheryte && x.Territory.Row == location.TerritoryTypeID && x.RowId == nearestAetheryteId);

            if (nearestAetheryte == null)
                return;

            TeleportConsumer.UseTeleport(nearestAetheryte.RowId);
        }


        private static Vector2 ConvertLocationToRaw(int x, int y, float scale)
        {
            var num = scale / 100f;
            return new Vector2(ConvertRawToMap((int)((x - 1024) * num * 1000f), scale), ConvertRawToMap((int)((y - 1024) * num * 1000f), scale));
        }

        private static float ConvertRawToMap(int pos, float scale)
        {
            var num1 = scale / 100f;
            var num2 = (float)(pos * (double)num1 / 1000.0f);
            return (40.96f / num1 * ((num2 + 1024.0f) / 2048.0f)) + 1.0f;
        }

        #region internal
        private void PrintTerris()
        {
            var mapSheet = Data.GetExcelSheet<TerritoryType>()!;
            var contentSheet = Data.GetExcelSheet<ContentFinderCondition>()!;
            foreach (var match in mapSheet)
            {
                if (match.RowId == 0) continue;
                if (match.Map.Value!.PlaceName.Value!.Name == "") continue;
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
