using ColossalFramework;
using HarmonyLib;
using System;
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
            Debug.Log("TerrainManager::GetSurfaceMapping");
        }

        // GetDetailHeight serves as a detection mechanism on whether or not the current run of TerrainPatch::Refresh() actually changed the normal maps.
        // All branches that write to SurfaceMapA call TerrainManager::GetDetailHeight(), so it serves as an indicator on whether we need to erase normal map data or not.
        static bool GetDetailHeightCalled = false;
        static bool ForceUpdate = false;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainManager), "GetDetailHeight")]
        static void GetDetailHeightPrefix()
        {
            GetDetailHeightCalled = true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
        static void RefreshPrefix()
        {
            GetDetailHeightCalled = false;
        }
        //TODO: Maybe replace with transpiler so that the normal maps arent created in the first place?
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
        static void RefreshPostfix(bool updateWater, uint waterFrame, TerrainPatch __instance)
        {
            if (GetDetailHeightCalled || ForceUpdate)
            {
                if (__instance.m_surfaceMapA != null) UpdateSubstituteTexture(__instance.m_surfaceMapA);
                Debug.Log("TerrainPatch::Refresh");
            }
        }

        public static Texture2D GetOrCreateSubstituteTexture(Texture2D surfaceMapA)
        {
            if (Settings.EraseClipping) return surfaceMapA;

            Texture2D surfaceMapAwithoutNormal;
            if (!SubstituteTextures.TryGetValue(surfaceMapA, out surfaceMapAwithoutNormal))
            {
                surfaceMapAwithoutNormal = Texture2D.Instantiate<Texture2D>(surfaceMapA);
                SubstituteTextures.Add(surfaceMapA, surfaceMapAwithoutNormal);
                Debug.Log("Substitute texture count:" + SubstituteTextures.Count);
            }
            return surfaceMapAwithoutNormal;
        }
        public static void UpdateSubstituteTexture(Texture2D surfaceMapA)
        {
            UpdateSubstituteTexture(surfaceMapA, GetOrCreateSubstituteTexture(surfaceMapA));
        }
        public static void UpdateSubstituteTexture(Texture2D surfaceMapA, Texture2D surfaceMapAwithoutNormal)
        {
            if(surfaceMapA.width != surfaceMapAwithoutNormal.width || surfaceMapA.height != surfaceMapAwithoutNormal.height)
            {
                surfaceMapAwithoutNormal.Resize(surfaceMapA.width, surfaceMapA.height);
            }
            var buffer = surfaceMapA.GetPixels32();
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i].b = 127;// 0.498039216f;
                buffer[i].a = 127;// 0.498039216f;
            }
            surfaceMapAwithoutNormal.SetPixels32(buffer);
            surfaceMapAwithoutNormal.Apply();
        }

        // Reassign all surface maps; force normals regeneration (necessary in case Settings.EraseClipping changed); and force all Nets to refetch their Textures.
        public static void RegenerateCache()
        {
            ForceUpdate = true;
            Debug.Log("regenerating surface texture cache");
            SubstituteTextures.Clear();
            TerrainManager terrainManager = Singleton<TerrainManager>.instance;
            for (int i = 0; i < terrainManager.m_patches.Length; i++)
            {
                TerrainPatch terrainPatch = terrainManager.m_patches[i];
                terrainPatch.m_surfaceModified.AddArea(0, 0, 1080, 1080);//values from TerrainPatch constructor
                terrainPatch.Refresh(false, 0);
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