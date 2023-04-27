﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace BlueMageHelper
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool ShowHintEvenIfUnlocked = false;
        public bool ShowOnlyUnlearned = false;


        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
