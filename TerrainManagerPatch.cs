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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by harmony")]
    class TerrainManagerPatch
    {
        static readonly System.Collections.Generic.Dictionary<Texture2D, Texture2D> SubstituteTextures = new System.Collections.Generic.Dictionary<Texture2D, Texture2D>(); //TODO: replace with 9-entry array


        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainManager), "GetSurfaceMapping")]
        static void GetSurfaceMappingPostfix(ref Vector3 worldPos, ref Texture _SurfaceTexA, ref Texture _SurfaceTexB, ref Vector4 _SurfaceMapping)
        {
            _SurfaceTexA = GetOrCreateSubstituteTexture(_SurfaceTexA as Texture2D);
#if DEBUG
            Debug.Log("TerrainManager::GetSurfaceMapping");
#endif
        }

        // Texture2D::Apply serves as a detection mechanism on whether or not the current run of TerrainPatch::Refresh() actually changed the normal maps.
        // All branches that write to SurfaceMapA call Texture2D::Apply(), so it serves as an indicator on whether we need to erase normal map data or not.
        static bool TextureApplyCalled = false;
        static bool ForceUpdate = false;
        static void OnTextureApplyCalled()
        {
            TextureApplyCalled = true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetManager), "UpdateNodeRenderer")]
        static void ThreadChecker()
        {
            Debug.Log("in sim thread?: " + (Thread.CurrentThread == Singleton<SimulationManager>.instance.m_simulationThread));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
        static void RefreshPrefix()
        {
            TextureApplyCalled = false;
        }
#if DEBUG
        [HarmonyDebug]
#endif
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
        static IEnumerable<CodeInstruction> RefreshTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            int sinceSurfaceMapALoaded = 10;
            bool injected = false;
            bool lastFieldLoadedWasSurfMapA = false;
            foreach (var instruction in instructions)
            {
                sinceSurfaceMapALoaded++;
                if (instruction.opcode == OpCodes.Ldfld)
                {
                    lastFieldLoadedWasSurfMapA = instruction.LoadsField(typeof(TerrainPatch).GetField("m_surfaceMapA"));
                }
                if (lastFieldLoadedWasSurfMapA && instruction.Calls(typeof(Texture2D).GetMethod("SetPixel", new Type[] { typeof(int), typeof(int), typeof(Color) })))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return CodeInstruction.Call(typeof(TerrainManagerPatch), "SetSurfaceMapAPixelReplacement");
                }else if (lastFieldLoadedWasSurfMapA && instruction.Calls(typeof(Texture2D).GetMethod("Apply", new Type[] { typeof(bool) })))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return CodeInstruction.Call(typeof(TerrainManagerPatch), "ApplySurfaceMapAReplacement");
                }
                else
                {
                    yield return instruction;
                }

            }
        }
        //TODO: Maybe replace with transpiler so that the normal maps arent created in the first place?
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
        static void RefreshPostfix(bool updateWater, uint waterFrame, TerrainPatch __instance)
        {
            if (!Settings.TempDisable)
            {
                if (TextureApplyCalled || ForceUpdate)
                {
                    //if (__instance.m_surfaceMapA != null) UpdateSubstituteTexture(__instance.m_surfaceMapA);
#if DEBUG
                    Debug.Log("TerrainPatch::Refresh");
#endif
                }
            }
        }

        public static void SetSurfaceMapAPixelReplacement(Texture2D surfaceMapA, int x, int y, Color color, TerrainPatch patch)
        {
            var replacedSurfaceMapA = GetOrCreateSubstituteTexture(surfaceMapA);
            var newcolor = changeColor(color);
            replacedSurfaceMapA.SetPixel(x, y, newcolor);
            if(replacedSurfaceMapA != surfaceMapA)
            {
                surfaceMapA.SetPixel(x, y, color);
            }

        }
        public static void ApplySurfaceMapAReplacement(Texture2D surfaceMapA, bool argo, TerrainPatch patch)
        {
            var replacedSurfaceMapA = GetOrCreateSubstituteTexture(surfaceMapA);
            replacedSurfaceMapA.Apply(argo);
            if (replacedSurfaceMapA != surfaceMapA)
            {
                surfaceMapA.Apply(argo);
            }
        }
        public static Texture2D GetOrCreateSubstituteTexture(Texture2D surfaceMapA)
        {
            if (Settings.EraseClipping || Settings.TempDisable) return surfaceMapA;

            Texture2D surfaceMapAwithoutNormal;
            if (!SubstituteTextures.TryGetValue(surfaceMapA, out surfaceMapAwithoutNormal))
            {
                surfaceMapAwithoutNormal = Texture2D.Instantiate<Texture2D>(surfaceMapA);
                SubstituteTextures.Add(surfaceMapA, surfaceMapAwithoutNormal);
#if DEBUG
                Debug.Log("Substitute texture count:" + SubstituteTextures.Count);
#endif
            }
            return surfaceMapAwithoutNormal;
        }
        public static void UpdateSubstituteTexture(Texture2D surfaceMapA)
        {
            UpdateSubstituteTexture(surfaceMapA, GetOrCreateSubstituteTexture(surfaceMapA));
        }
        public static void UpdateSubstituteTexture(Texture2D surfaceMapA, Texture2D surfaceMapAwithoutNormal)
        {
            if (surfaceMapA.width != surfaceMapAwithoutNormal.width || surfaceMapA.height != surfaceMapAwithoutNormal.height)
            {
                surfaceMapAwithoutNormal.Resize(surfaceMapA.width, surfaceMapA.height);
            }
            var buffer = surfaceMapA.GetPixels32();
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = changeColor(buffer[i]);
            }
            surfaceMapAwithoutNormal.SetPixels32(buffer);
            surfaceMapAwithoutNormal.Apply();
        }
        static Color32 changeColor(Color32 original)
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

        // Reassign all surface maps; force normals regeneration (necessary in case Settings.EraseClipping changed); and force all Nets to refetch their Textures.
        public static void RegenerateCache()
        {
            var simulationManager = Singleton<SimulationManager>.instance;
            if (Thread.CurrentThread != simulationManager.m_simulationThread)
            {
                simulationManager.AddAction(RegenerateCache);
                return;
            }
            //ForceUpdate = true;
            Debug.Log("regenerating surface texture cache");
            Settings.LogSettings();
            SubstituteTextures.Clear();
            //TerrainModify.RefreshAllModifications();

            var terrainManager = Singleton<TerrainManager>.instance;
            for (int i = 0; i < terrainManager.m_patches.Length; i++)
            {
                TerrainPatch terrainPatch = terrainManager.m_patches[i];
                terrainPatch.m_surfaceModified.AddArea(0, 0, 1080, 1080);//values from TerrainPatch constructor
                //terrainPatch.Refresh(false, 0); //has to happen on UI thread; happens automatically anyway
            }
            NetManager netManager = Singleton<NetManager>.instance;
            for (ushort i = 0; i < netManager.m_nodes.m_buffer.Length; i++)
            {
                netManager.UpdateNodeRenderer(i, false);
            }
            for (ushort i = 0; i < netManager.m_segments.m_buffer.Length; i++)
            {
                netManager.UpdateSegmentRenderer(i, false);
            }
            ForceUpdate = false;
        }
    }
}