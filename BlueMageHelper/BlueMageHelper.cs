using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using static BlueMageHelper.SpellSources;

namespace BlueMageHelper
{
    public sealed class BlueMageHelper : IDalamudPlugin
    {
        public string Name => "Blue Mage Helper";

        private DalamudPluginInterface PluginInterface { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUI { get; init; }
        private Framework Framework { get; init; }
        private GameGui GameGui { get; init; }
        
        private const int blank_text_textnode_index = 54;
        private const int spell_number_textnode_index = 62;
        private const int region_textnode_index = 57;
        private const int region_image_index = 56;
        private const int unlearned_node_index = 63;
        
        private string lastSeenSpell = string.Empty;
        private string lastOrgText = string.Empty;
        
        public BlueMageHelper(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] GameGui gameGui)
        {
            this.PluginInterface = pluginInterface;
            this.Framework = framework;
            this.GameGui = gameGui;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.PluginUI = new PluginUI(this.Configuration);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += AOZNotebook_addon_manager;
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
            return Sources.ContainsKey(spell_number) ? Sources[spell_number] : new SpellSource($"No data for spell #{spell_number}");
        }

        public void Dispose()
        {
            this.PluginUI.Dispose();
            Framework.Update -= AOZNotebook_addon_manager;
        }

        private void DrawUI()
        {
            this.PluginUI.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUI.SettingsVisible = true;
        }
    }
}
