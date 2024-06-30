using Dalamud.IoC;
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
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

using static BlueMageHelper.SpellSources;

namespace BlueMageHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public static IDataManager Data { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static ICommandManager Commands { get; private set; } = null!;
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static ITextureProvider Texture { get; private set; } = null!;

        private const string CommandName = "/spellbook";

        public const string Authors = "Infi, Sl0nderman";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        public Configuration Configuration { get; init; }
        private WindowSystem WindowSystem = new("Blue Mage Helper");
        public MainWindow MainWindow;
        private ConfigWindow ConfigWindow;

        private const int BlankTextTextnodeIndex = 49;
        private const int SpellNumberTextnodeIndex = 57;
        private const int RegionTextnodeIndex = 52;
        private const int RegionImageIndex = 51;
        private const int UnlearnedNodeIndex = 58;

        private string lastSeenSpell = string.Empty;
        private string lastOrgText = string.Empty;

        public List<AozAction> AozActionsCache;
        public List<AozActionTransient> AozTransientCache;

        private bool OnCooldown;
        private readonly Timer Cooldown = new(3 * 1000);

        public Dictionary<string, bool> UnlockedSpells = new();

        private static ExcelSheet<MapMarker> MapMarkerSheet = null!;
        private static ExcelSheet<Aetheryte> AetheryteSheet = null!;

        public static TeleportConsumer TeleportConsumer = null!;

        public Plugin()
        {
            AozActionsCache = Data.GetExcelSheet<AozAction>()!.Where(a => a.Rank != 0).ToList();
            AozTransientCache = Data.GetExcelSheet<AozActionTransient>()!.Where(a => a.Number != 0).ToList();

            MapMarkerSheet = Data.GetExcelSheet<MapMarker>()!;
            AetheryteSheet = Data.GetExcelSheet<Aetheryte>()!;

            TeleportConsumer = new TeleportConsumer();

            Cooldown.AutoReset = false;
            Cooldown.Elapsed += (_, __) => OnCooldown = false;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

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

            try
            {
                Log.Debug("Loading Spell Sources.");
                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "spells.json");
                var jsonString = File.ReadAllText(path);
                Spells = JsonConvert.DeserializeObject<Dictionary<string, Spell>>(jsonString);
            }
            catch (Exception e)
            {
                Log.Error("There was a problem building the Grimoire.");
                Log.Error(e.Message);
                Log.Error(e.StackTrace!);
                if (e.InnerException != null)
                {
                    Log.Error(e.InnerException.Message);
                    Log.Error(e.InnerException.StackTrace!);
                }
            }
        }

        private void CheckLearnedSpells(IFramework framework)
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

        private void AozNotebookAddonManager(IFramework framework)
        {
            try
            {
                var addonPtr = GameGui.GetAddonByName("AOZNotebook");
                if (addonPtr == nint.Zero)
                    return;
                SpellbookWriter(addonPtr);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Log.Verbose("Blue Mage Helper caught an exception: "+ e);
            }
        }

        private unsafe void SpellbookWriter(IntPtr addonPtr)
        {
            AtkUnitBase* spellbookBaseNode = (AtkUnitBase*)addonPtr;
            if (spellbookBaseNode->UldManager.NodeListCount < UnlearnedNodeIndex + 1)
                return;

            AtkTextNode* unlearnedTextNode = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[UnlearnedNodeIndex];
            if (!unlearnedTextNode->AtkResNode.IsVisible() && !Configuration.ShowHintEvenIfUnlocked)
                return;

            AtkTextNode* emptyTextnode = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[BlankTextTextnodeIndex];
            AtkTextNode* spellNumberTextnode = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[SpellNumberTextnodeIndex];
            AtkTextNode* region = (AtkTextNode*)spellbookBaseNode->UldManager.NodeList[RegionTextnodeIndex];
            AtkImageNode* regionImage = (AtkImageNode*)spellbookBaseNode->UldManager.NodeList[RegionImageIndex];
            var spellNumberString = spellNumberTextnode->NodeText.ToString()[1..]; // Remove the # from the spell number

            // Try to preserve last seen org text
            if (spellNumberString != lastSeenSpell)
                lastOrgText = emptyTextnode->NodeText.ToString();
            lastSeenSpell = spellNumberString;

            var spellSource = GetHintText(spellNumberString);
            emptyTextnode->SetText($"{(lastOrgText != "" ? $"{lastOrgText}\n" : "")}{spellSource.Info}");
            emptyTextnode->AtkResNode.ToggleVisibility(true);

            // Change region if needed
            if (spellSource.Type != RegionType.Unknown)
                spellSource.SetRegion(region, regionImage);
        }

        private static SpellSource GetHintText(string spellNumber) =>
            Spells.TryGetValue(spellNumber, out var spell) ? spell.Source : new SpellSource($"Currently Unknown");

        public void Dispose()
        {
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
                Log.Information("---------------");
                Log.Information(match.Map.Value!.PlaceName.Value!.Name);
                Log.Information($"TerriID: {match.RowId}");
                Log.Information($"MapID: {match.Map.Row}");

                var content = contentSheet.FirstOrDefault(x => x.TerritoryType.Row == match.RowId);
                if (content == null) continue;
                if (Helper.ToTitleCaseExtended(content.Name, 0) == "") continue;
                Log.Information($"Duty: {Helper.ToTitleCaseExtended(content.Name, 0)}");
            }
        }

        private void PrintBlueSkills()
        {

            // skip the first non existing skill
            var aozActionTransients = Data.GetExcelSheet<AozActionTransient>()!.Skip(1);
            var aozActions = Data.GetExcelSheet<AozAction>()!.Skip(1);

            Dictionary<string, Spell> spells = new();
            foreach (var (transient, action) in aozActionTransients.Zip(aozActions))
            {
                spells.Add(transient.Number.ToString(), new Spell() {Name = action.Action.Value!.Name.ToString(), Icon = transient.Icon, Sources = { new SpellSource("", RegionType.Unknown) }});
            }

            Log.Information(JsonConvert.SerializeObject(spells, Formatting.Indented));
        }
        #endregion
    }
}
