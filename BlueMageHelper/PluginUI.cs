using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;

namespace BlueMageHelper
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(290, 90), ImGuiCond.Always);
            if (ImGui.Begin("Blue Mage Helper Configuration", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var configValue = this.configuration.show_hint_even_if_unlocked;
                if (ImGui.Checkbox("Show hint even if a spell is already unlocked.", ref configValue))
                {
                    this.configuration.show_hint_even_if_unlocked = configValue;
                    this.configuration.Save();
                }

                ImGui.Text("Did this plugin help?");
                ImGui.SameLine();
                if (ImGui.Button("Consider Donating"))
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://ko-fi.com/sl0nderman",
                        UseShellExecute = true
                    });
                }
            }
            ImGui.End();
        }
    }
}
