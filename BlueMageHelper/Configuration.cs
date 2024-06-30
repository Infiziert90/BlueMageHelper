using Dalamud.Configuration;
using System;

namespace BlueMageHelper
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool ShowHintEvenIfUnlocked = false;
        public bool ShowOnlyUnlearned = false;

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
