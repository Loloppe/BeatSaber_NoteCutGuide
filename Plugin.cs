using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
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

		// Shader
		internal static Shader DefaultShader;
		// Rainbow
		internal static float RainbowPls = 0f;
		// Modifier
		internal static BS_Utils.Gameplay.LevelData levelData = null;

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
			BSMLSettings.instance.AddSettingsMenu("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance);
			GameplaySetup.instance.AddTab("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance, MenuType.All);
		}

		public void OnActiveSceneChanged(Scene arg0, Scene scene) {
			if(BS_Utils.SceneNames.Game == scene.name) {
				levelData = BS_Utils.Plugin.LevelData;
			}
		}

		[OnDisable]
		public void OnDisable() {
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			harmony.UnpatchSelf();
			BSMLSettings.instance.RemoveSettingsMenu(Config.Instance);
			GameplaySetup.instance.RemoveTab("NoteCutGuide");
		}
	}
}
