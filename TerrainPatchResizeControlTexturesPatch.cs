using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace RoadTextureTerrainEdgeRemover
{

    [HarmonyPatch]
    [HarmonyPatch(typeof(TerrainPatch), "ResizeControlTextures")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by harmony")]
    class TerrainPatchResizeControlTexturesPatch
    {
        // maybe replace this with a transpiler that duplicates everything that is done to m_surfaceMapA?
        static void Postfix(TerrainPatch __instance)
        {
            if (Settings.TempDisable || Settings.EraseClipping.value) return;

            Debug.Log("resizing patch (" + __instance.m_x + "|" + __instance.m_z + ")");
            Texture2D original = __instance.m_surfaceMapA;
            Texture2D replacement = SubstituteTextureManager.GetOrCreateSubstituteTexture(__instance);

            replacement.Resize(original.width, original.height, original.format, false);
            replacement.wrapMode = original.wrapMode;
            replacement.filterMode = original.filterMode;
        }

    }
}