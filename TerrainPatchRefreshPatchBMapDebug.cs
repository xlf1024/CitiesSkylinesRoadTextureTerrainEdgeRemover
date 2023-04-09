using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace RoadTextureTerrainEdgeRemover
{
    [DebugOverlayPatch]
    [HarmonyPatch]
    [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by harmony")]
    class TerrainPatchRefreshPatchBMapDebug
    {
#if DEBUG
        [HarmonyDebug]
#endif
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
#if DEBUG
            Debug.Log("ROTTERdam: Applying TerrainPatch::Refresh transpiler for debug surface paint");
#endif
            bool lastFieldLoadedWasSurfMapB = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld)
                {
                    lastFieldLoadedWasSurfMapB = instruction.LoadsField(typeof(TerrainPatch).GetField("m_surfaceMapB"));
                }


                if (lastFieldLoadedWasSurfMapB && instruction.Calls(typeof(Texture2D).GetMethod("SetPixel", new Type[] { typeof(int), typeof(int), typeof(Color) })))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return CodeInstruction.Call(typeof(TerrainPatchRefreshPatchBMapDebug), "SetSurfaceMapBPixelReplacement");
                    Debug.Log("ROTTERdam: inserted setPixel");
                }
                else
                {
                    yield return instruction;
                }

            }
        }

        public static void SetSurfaceMapBPixelReplacement(Texture2D surfaceMapB, int x, int y, Color color, TerrainPatch patch)
        {
            surfaceMapB.SetPixel(x, y, Colors[SimulationManager.instance.m_currentFrameIndex % Colors.Length]);
        }

        readonly static Color32[] Colors = new Color32[] { new Color32(0, 0, 0, 0), new Color32(255, 0, 0, 0), new Color32(0, 255, 0, 0), new Color32(0, 0, 255, 0), new Color32(0, 0, 0, 255) };
    }
}
