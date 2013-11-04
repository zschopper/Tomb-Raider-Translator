using System;
using System.Collections.Generic;
using System.Text;

namespace TRTR
{
    enum Task
    {
        Translate,
        Restore,
        Extract,
        TranslateSim,
        RestoreSim,
    }

    class ProcessMan
    {
        internal void LoadConfig() { }
        internal void SaveConfig() { }
        internal void GetInstalledGameList() { }
        internal void SelectGame() { }
        internal void LoadGameConfigs() { }
        internal void SelectGameConfig() { }
        internal void DoTask(Task task, GameInstance game) { }
    }
}
