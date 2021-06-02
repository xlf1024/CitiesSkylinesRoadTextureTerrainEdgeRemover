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
    [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by harmony")]
    class TerrainPatchRefreshPatch
    {
#if DEBUG
        [HarmonyDebug]
#endif
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool lastFieldLoadedWasSurfMapA = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld)
                {
                    lastFieldLoadedWasSurfMapA = instruction.LoadsField(typeof(TerrainPatch).GetField("m_surfaceMapA"));
                }


                if (lastFieldLoadedWasSurfMapA && instruction.Calls(typeof(Texture2D).GetMethod("SetPixel", new Type[] { typeof(int), typeof(int), typeof(Color) })))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return CodeInstruction.Call(typeof(TerrainPatchRefreshPatch), "SetSurfaceMapAPixelReplacement");
                    Debug.Log("inserted setPixel");
                }
                else if (lastFieldLoadedWasSurfMapA && instruction.Calls(typeof(Texture2D).GetMethod("Apply", new Type[] { typeof(bool) })))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return CodeInstruction.Call(typeof(TerrainPatchRefreshPatch), "ApplySurfaceMapAReplacement");
                    Debug.Log("inserted apply");
                }
                else
                {
                    yield return instruction;
                }

            }
        }

        public static void SetSurfaceMapAPixelReplacement(Texture2D surfaceMapA, int x, int y, Color color, TerrainPatch patch)
        {
            if (Settings.TempDisable)
            {
                surfaceMapA.SetPixel(x, y, color);
                return;
            }
            var newcolor = ChangeColor(color);
            if (Settings.EraseClipping)
            {
                surfaceMapA.SetPixel(x, y, newcolor);
            }
            else
            {
                var replacedSurfaceMapA = SubstituteTextureManager.GetOrCreateSubstituteTexture(patch);
                replacedSurfaceMapA.SetPixel(x, y, newcolor);
                surfaceMapA.SetPixel(x, y, color);
            }

        }
        public static void ApplySurfaceMapAReplacement(Texture2D surfaceMapA, bool updateMipmaps, TerrainPatch patch)
        {
            surfaceMapA.Apply(updateMipmaps);
            if (Settings.TempDisable || Settings.EraseClipping) return;

            var replacedSurfaceMapA = SubstituteTextureManager.GetOrCreateSubstituteTexture(patch);
            replacedSurfaceMapA.Apply(updateMipmaps);
        }
        static Color32 ChangeColor(Color32 original)
        {
            var newcolor = original;
            switch ((Modes)Settings.Mode.value)
            {
                case Modes.Erase:
                    {
                        newcolor.b = 127;// 0.498039216f;
                        newcolor.a = 127;// 0.498039216f;

                        break;
                    }
                case Modes.Clamp:
                    {
                        byte min = Util.Clamp((byte)Settings.Strength.value, 0, 127);
                        byte max = (byte)(255 - min);
                        newcolor.b = Util.Clamp(original.b, min, max);
                        newcolor.a = Util.Clamp(original.a, min, max);
                        break;
                    }
                case Modes.Scale:
                    {
                        byte strength = Util.Clamp((byte)Settings.Strength.value, 0, 128);
                        byte keep = (byte)(128 - strength);
                        byte offset = strength;
                        newcolor.b = (byte)(((original.b * keep) >> 7) + offset);
                        newcolor.a = (byte)(((original.a * keep) >> 7) + offset);
                        break;
                    }
            }
            return newcolor;

        }
    }
}