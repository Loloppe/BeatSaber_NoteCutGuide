using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace NoteCutGuide {
	[Plugin(RuntimeOptions.DynamicInit)]
	public class Plugin {
		internal static Plugin Instance;
		internal static IPALogger Log;
		internal static Harmony harmony;

		internal static Shader DefaultShader;
		internal static int RainbowPls = 0;
		internal static BS_Utils.Gameplay.LevelData levelData = null;

		static class BsmlWrapper {
			static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

			public static void EnableUI() {
				void wrap() => BSMLSettings.instance.AddSettingsMenu("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance);
				void wrap2() => GameplaySetup.instance.AddTab("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance, MenuType.Solo);

				if(hasBsml) {
					wrap();
					wrap2();
				}
			}
			public static void DisableUI() {
				void wrap() => BSMLSettings.instance.RemoveSettingsMenu(Config.Instance);
				void wrap2() => GameplaySetup.instance.RemoveTab("NoteCutGuide");

				if(hasBsml) {
					wrap();
					wrap2();
				}
			}
		}

		[Init]
		public Plugin(IPALogger logger, IPA.Config.Config conf) {
			Instance = this;
			Log = logger;
			Config.Instance = conf.Generated<Config>();
			harmony = new Harmony("Kinsi55.BeatSaber.NoteCutGuide");
		}

		[OnEnable]
		public void OnEnable() {
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			BsmlWrapper.EnableUI();
		}

		public void OnActiveSceneChanged(Scene arg0, Scene scene) {
			if(BS_Utils.SceneNames.Game == "GameCore") {
				levelData = BS_Utils.Plugin.LevelData;
			}
		}

		[OnDisable]
		public void OnDisable() {
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			harmony.UnpatchSelf();
			BsmlWrapper.DisableUI();
		}
	}
}
