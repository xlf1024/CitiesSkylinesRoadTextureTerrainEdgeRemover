using CitiesHarmony.API;
using ColossalFramework;
using ICities;
// Make sure that "using HarmonyLib;" does not appear here!
// Only reference HarmonyLib in code that runs when Harmony is ready (DoOnHarmonyReady, IsHarmonyInstalled)

namespace RoadTextureTerrainEdgeRemover {
    public class RoadTextureTerrainEdgeRemoverMod : IUserMod, ILoadingExtension{
        // Make sure that HarmonyLib is not referenced in any way in your IUserMod implementation!
        // Instead, apply your patches from a separate static patcher class!
        // (otherwise it will fail to instantiate the type when CitiesHarmony is not installed)

        public string Name => "ROTTERdam: Road Texture Terrain Edge Remover";
        public string Description => "Keeps terrain edge artifacts from showing on roads. Also allows to hide the cliff texture globally.";

        public static ILoading Loading = null;
        public static bool Enabled = false;
        public void OnEnabled() {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
            Enabled = true;
            if(Loading != null && Loading.loadingComplete) TerrainManagerPatch.RegenerateCache();
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
            Enabled = false;
            if (Loading != null && Loading.loadingComplete) TerrainManagerPatch.RegenerateCache();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            Settings.OnSettingsUI(helper);
        }

        public void OnCreated(ILoading loading)
        {
            Loading = loading;
            if (Enabled && Loading.loadingComplete) TerrainManagerPatch.RegenerateCache();
        }

        public void OnReleased()
        {
            //Loading = null;
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            TerrainManagerPatch.RegenerateCache();
        }
        public void OnLevelUnloading()
        {

        }


    }
}

