using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace NoteCutGuide {
	[Plugin(RuntimeOptions.DynamicInit)]
	public class Plugin {
		internal static Plugin Instance;
		internal static IPALogger Log;
		internal static Harmony harmony;

		// Rainbow
		internal static float RainbowPls = 0f;

		[Init]
		public Plugin(IPALogger logger, IPA.Config.Config conf) {
			Instance = this;
			Log = logger;
			Config.Instance = conf.Generated<Config>();
			harmony = new Harmony("Kinsi55.BeatSaber.NoteCutGuide");
		}

		[OnEnable]
		public void OnEnable() {
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			BSMLSettings.instance.AddSettingsMenu("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance);
			GameplaySetup.instance.AddTab("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance, MenuType.All);
		}

		[OnDisable]
		public void OnDisable() {
			harmony.UnpatchSelf();
			BSMLSettings.instance.RemoveSettingsMenu(Config.Instance);
			GameplaySetup.instance.RemoveTab("NoteCutGuide");
		}
	}
}
