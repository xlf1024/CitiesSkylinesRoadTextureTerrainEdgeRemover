﻿using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace RoadTextureTerrainEdgeRemover
{

    class SubstituteTextureManager
    {
        static readonly Texture2D[] SubstituteTextures = new Texture2D[81];


        public static Texture2D GetOrCreateSubstituteTexture(TerrainPatch terrainPatch)
        {
            if (Settings.EraseClipping || Settings.TempDisable) return terrainPatch.m_surfaceMapA;

            int patchIndex = terrainPatch.m_z * 9 + terrainPatch.m_x;
            var substituteTexture = SubstituteTextures[patchIndex];
            if (substituteTexture is null)
            {
                lock (SubstituteTextures)
                {
                    substituteTexture = SubstituteTextures[patchIndex];
                    if (Settings.EraseClipping || Settings.TempDisable) return terrainPatch.m_surfaceMapA;
                    if (substituteTexture is null)
                    {
                        Texture2D surfaceMapAwithoutNormal;
                        surfaceMapAwithoutNormal = Texture2D.Instantiate<Texture2D>(terrainPatch.m_surfaceMapA);
                        SubstituteTextures[patchIndex] = surfaceMapAwithoutNormal;
                        substituteTexture = SubstituteTextures[patchIndex];
#if DEBUG
                        Debug.Log("ROTTERdam: Created Substitute texture");
#endif
                    }
                }
            }
            return substituteTexture;
        }

        // Reassign all surface maps; force normals regeneration (necessary in case Settings.EraseClipping changed); and force all Nets to refetch their Textures.
        // Call from UI thread!
        public static void RegenerateCache()
        {
            var simulationManager = Singleton<SimulationManager>.instance;
            if (Thread.CurrentThread != simulationManager.m_simulationThread)
            {
                Debug.Log("ROTTERdam: regenerating surface texture cache");
                lock (SubstituteTextures)
                {
                    for (int i = 0; i < SubstituteTextures.Length; i++)
                    {
                        SubstituteTextures[i] = null;
                    }
                }
                simulationManager.AddAction(RegenerateCache);
                return;
            }
            //ForceUpdate = true;
            Settings.LogSettings();
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
                netManager.UpdateNodeRenderer(i, true);
            }
            for (ushort i = 0; i < netManager.m_segments.m_buffer.Length; i++)
            {
                netManager.UpdateSegmentRenderer(i, true);
            }
        }
    }
}