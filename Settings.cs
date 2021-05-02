using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RoadTextureTerrainEdgeRemover
{
    class Settings
    {
        public static string FileName => nameof(RoadTextureTerrainEdgeRemoverMod);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(FileName) == null)
            {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = FileName } });
            }
        }
        public static void OnSettingsUI(UIHelperBase helper)
        {
            Debug.Log("Make settings was called");
            helper.AddCheckbox("hide cliff texture", EraseClipping, (isChecked) => { EraseClipping.value = isChecked; TerrainManagerPatch.RegenerateCache(); });
        }

        public static SavedBool EraseClipping { get; } = new SavedBool(nameof(EraseClipping), FileName, false, true);
    }
}
