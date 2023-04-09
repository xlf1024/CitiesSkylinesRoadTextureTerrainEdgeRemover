using System.Reflection;
using System.Threading;
using HarmonyLib;
using static RoadTextureTerrainEdgeRemover.HarmonyExtensions;

namespace RoadTextureTerrainEdgeRemover
{
    public static class Patcher
    {
        private const string HarmonyId = "xlf1024.RoadTextureTerrainEdgeRemover";

        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;

            UnityEngine.Debug.Log("ROTTERdam: Patching...");

            patched = true;

            // Apply your patches here!
#if DEBUG
            Harmony.DEBUG = true;
#endif
            var harmony = new Harmony(HarmonyId);
            if (Settings.EnableEdgeFilter) harmony.PatchWithAnnotation(Assembly.GetExecutingAssembly(), typeof(EdgeFilterPatch));
            if ((Modes)Settings.Mode.value != Modes.None) harmony.PatchWithAnnotation(Assembly.GetExecutingAssembly(), typeof(LegacyModePatch));
#if DEBUG
            if (Settings.EnableDebugOverlay) harmony.PatchWithAnnotation(Assembly.GetExecutingAssembly(), typeof(DebugOverlayPatch));
#endif
        }
        public static void RepatchAll()
        {
            UnityEngine.Debug.Log("ROTTERdam: re-applying patches due to changed settings");
            object simLock = typeof(SimulationManager).GetField("m_simulationFrameLock", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SimulationManager.instance);
            lock (simLock)
            {
                do
                {
                    Monitor.Wait(simLock, 0);
                } while ((bool)typeof(SimulationManager).GetField("m_inSimulationStep", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SimulationManager.instance));


                UnpatchAll();
                PatchAll();
                SubstituteTextureManager.RegenerateCache();
            }
        }
        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            patched = false;

            UnityEngine.Debug.Log("ROTTERdam: Reverted...");
        }

    }
}