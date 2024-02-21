using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LethalPropHunt.Gamemode;
using LethalPropHunt.Input;
using LethalPropHunt.Patches;
using UnityEngine;
using UnityEngine.Assertions;
using BepInEx.Configuration;
using Lethal_Prop_Hunt.Patches;
using LethalPropHunt.Audio;

namespace LethalPropHunt
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.HardDependency)]
    public class PropHuntBase : BaseUnityPlugin
    {
        private const string modGUID = "jackexe.LethalPropHunt";
        private const string modName = "Letahl Prop Hunt";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static PropHuntBase Instance;

        public static ManualLogSource mls;
        public GameObject netManagerPrefab;
        public static AssetBundle bundle;

        public static LPHInputManagement InputActionsInstance = new LPHInputManagement();

        public static ConfigEntry<bool> ShowCursor { get; private set; }
        public static ConfigEntry<Vector3> Offset { get; private set; }
        public static Transform Camera { get; internal set; }
        public static Transform OriginalTransform { get; internal set; }
        public Sprite CrosshairSprite { get; private set; }

        void Awake()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            ShowCursor = Config.Bind("Options", "ShowCursor", true);
            Offset = Config.Bind("Options", "CameraOffset", new Vector3(0f, -1f, -2f));
            SetCrosshairSprite();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            if (Instance == null)
            {
                Instance = this;
            }
            var dllFolderPath = System.IO.Path.GetDirectoryName(Info.Location);
            var assetBundleFilePath = System.IO.Path.Combine(dllFolderPath, "lphassets");
            bundle = AssetBundle.LoadFromFile(assetBundleFilePath);
            netManagerPrefab = bundle.LoadAsset<GameObject>("Assets/LPHAssets/LPHAssets.prefab");
            netManagerPrefab.AddComponent<LPHNetworkHandler>();

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(PropHuntBase));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(NetworkObjectManagerPatch));
            harmony.PatchAll(typeof(HudManagerPatches));
            harmony.PatchAll(typeof(RoundManagerPatch));
            harmony.PatchAll(typeof(ShotgunPatch));
            harmony.PatchAll(typeof(ShovelPatch));
            harmony.PatchAll(typeof(GrabbableObjectPatch));
            harmony.PatchAll(typeof(AudioManager));
            harmony.PatchAll(typeof(TimeOfDayPatch));

            LethalConfigManager.SetModDescription("Configuration for Lethal Prop Hunt");
            LPHRoundManager.Init();
        }

        internal void SetCrosshairSprite()
        {
            CrosshairSprite = PlayerControllerBPatch.CreateCrosshairSprite();
        }
    }
}
