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
    [HarmonyPatch(typeof(TerrainManager), "GetSurfaceMapping")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by harmony")]
    class TerrainManagerGetSurfaceMappingPatch
    {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var instruction in instructions)
            {
                if (instruction.LoadsField(typeof(TerrainPatch).GetField("m_surfaceMapA")))
                {
                    yield return CodeInstruction.Call(typeof(SubstituteTextureManager),"GetOrCreateSubstituteTexture");
                }
                else
                {
                    yield return instruction;
                }
            }
        }

    }
}