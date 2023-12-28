using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Jotunn.Configs;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;

namespace MoreTreeStumps
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class MoreTreeStumps : BaseUnityPlugin
    {
        public const string PluginGUID = "com.warp.moretreestumps";
        public const string PluginName = "MoreTreeStumps";
        public const string PluginVersion = "1.0.0";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();


        private AssetBundle moreTreeStumpsBundle;
        private GameObject coreStumpPrefab;
        private GameObject oakStumpPrefab;
        private GameObject firStumpPrefab;

        private void AddlocalizationsEnglish()
        {
            Localization = LocalizationManager.Instance.GetLocalization();
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
              {"$PineStump_warp", "Pine" },
              {"$OakStump_warp", "Oak" },
              {"$FirStump_warp", "Fir" },
            });
        }

        private void JSONS()
        {
            if (moreTreeStumpsBundle == null)
            {
                return;
            }

            TextAsset[] textAssets = moreTreeStumpsBundle.LoadAllAssets<TextAsset>();

            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace("_MoreTreeStumps", "");
                Localization.AddJsonFile(lang, textAsset.text);
            }
        }

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("MoreTreeStumps has loaded");

            // Subscribe to the OnVanillaPrefabsAvailable event
            PrefabManager.OnVanillaPrefabsAvailable += OnPrefabsAvailable;

        }

        private void OnPrefabsAvailable()
        {

            // Load assets and add vegetation here
            LoadAssets();
            AddVegetation();
            AddlocalizationsEnglish();
            JSONS();

            // Unsubscribe if you only want to execute this once
            PrefabManager.OnVanillaPrefabsAvailable -= OnPrefabsAvailable;
        }

        private void LoadAssets()
        {
            moreTreeStumpsBundle = AssetUtils.LoadAssetBundleFromResources("moretreestumps_bundle");
            coreStumpPrefab = moreTreeStumpsBundle?.LoadAsset<GameObject>("CoreStump");
            oakStumpPrefab = moreTreeStumpsBundle?.LoadAsset<GameObject>("OakStump");
            firStumpPrefab = moreTreeStumpsBundle?.LoadAsset<GameObject>("FirStump");

            LogResourceNamesAndCheckErrors();
        }

        // Define the vegetation configuration
        VegetationConfig coreStumpConfig = new VegetationConfig
        {
            Biome = Heightmap.Biome.BlackForest,
            BlockCheck = true,
            Max = 0.5f,
            ScaleMin = 1.73f,
            ScaleMax = 1.74f,

        };

        VegetationConfig oakStumpConfig = new VegetationConfig
        {
            Biome = Heightmap.Biome.Meadows,
            BlockCheck = true,
            Max = 0.5f,
            ScaleMin = 2.7f,
            ScaleMax = 2.8f,

        };

        VegetationConfig firStumpConfig = new VegetationConfig
        {
            Biome = Heightmap.Biome.Mountain,
            BlockCheck = true,
            Max = 0.5f,
            ScaleMin = 2.5f,
            ScaleMax = 2.6f,

        };

        private void AddVegetation()
        {
            // Ensure all prefabs are loaded
            if (coreStumpPrefab == null || oakStumpPrefab == null || firStumpPrefab == null)
            {
                Jotunn.Logger.LogError("One or more tree stump prefabs are not loaded.");
                return;
            }

            ConfigureDestructible(coreStumpPrefab, 0, 80);
            ConfigureDestructible(oakStumpPrefab, 1, 120f);
            ConfigureDestructible(firStumpPrefab, 1, 60f);

            ConfigureDropOnDestroyed(coreStumpPrefab, "coreWood", 4, 5);
            ConfigureDropOnDestroyed(oakStumpPrefab, "fineWood", 4, 5);
            ConfigureDropOnDestroyed(firStumpPrefab, "wood", 4, 5);

            ConfigureHoverText(coreStumpPrefab, "$PineStump_warp");
            ConfigureHoverText(oakStumpPrefab, "$OakStump_warp");
            ConfigureHoverText(firStumpPrefab, "$FirStump_warp");

            CustomVegetation coreStumpVegetation = new CustomVegetation(coreStumpPrefab, false, coreStumpConfig);
            CustomVegetation oakStumpVegetation = new CustomVegetation(oakStumpPrefab, false, oakStumpConfig);
            CustomVegetation firStumpVegetation = new CustomVegetation(firStumpPrefab, false, firStumpConfig);

            ZoneManager.Instance.AddCustomVegetation(coreStumpVegetation);
            ZoneManager.Instance.AddCustomVegetation(oakStumpVegetation);
            ZoneManager.Instance.AddCustomVegetation(firStumpVegetation);
        }

        private void ConfigureDestructible(GameObject prefab, int minToolTier, float health)
        {
            var destructible = prefab.GetComponent<Destructible>() ?? prefab.AddComponent<Destructible>();
            destructible.m_minToolTier = minToolTier;
            destructible.m_health = health;

            // Set up destroyed and hit effects
            GameObject destroyedEffectPrefab = PrefabManager.Cache.GetPrefab<GameObject>("vfx_stubbe");
            GameObject destroyedSoundPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_wood_break");
            GameObject hitEffectPrefab = PrefabManager.Cache.GetPrefab<GameObject>("vfx_SawDust");
            GameObject hitSoundPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_tree_hit");

            destructible.m_destroyedEffect.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = destroyedEffectPrefab },
                new EffectList.EffectData { m_prefab = destroyedSoundPrefab }
            };

            destructible.m_hitEffect.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = hitEffectPrefab },
                new EffectList.EffectData { m_prefab = hitSoundPrefab }
            };
        }

        private void ConfigureDropOnDestroyed(GameObject prefab, string itemName, int minStack, int maxStack)
        {
            var dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>() ?? prefab.AddComponent<DropOnDestroyed>();
            dropOnDestroyed.m_dropWhenDestroyed.m_drops = new List<DropTable.DropData>
            {
                new DropTable.DropData
                {
                    m_item = PrefabManager.Instance.GetPrefab(itemName),
                    m_stackMin = minStack,
                    m_stackMax = maxStack,
                    m_weight = 1f
                },
            };
        }

        private void ConfigureHoverText(GameObject prefab, string hoverText)
        {
            if (prefab == null)
            {
                Jotunn.Logger.LogError("Prefab is null. Cannot add HoverText.");
                return;
            }

            // Check if HoverText component already exists, if not, add it
            HoverText hoverTextComponent = prefab.GetComponent<HoverText>();
            if (hoverTextComponent == null)
            {
                hoverTextComponent = prefab.AddComponent<HoverText>();
            }

            // Set the hover text
            hoverTextComponent.m_text = hoverText;
        }

    }


}
}