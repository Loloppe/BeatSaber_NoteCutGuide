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

		internal static Vector2 Blue = new Vector2(-1, -1);
		internal static Vector2 Red = new Vector2(-1, -1);
		internal static NoteData BlueData = null;
		internal static NoteData RedData = null;
		internal static Transform BlueGuide = null;
		internal static Transform RedGuide = null;
		internal static bool BlueHead = false;
		internal static bool RedHead = false;
		internal static List<NoteData> BlueDataList = new List<NoteData>();
		internal static List<NoteData> RedDataList = new List<NoteData>();
		internal static List<Vector2> BlueList = new List<Vector2>();
		internal static List<Vector2> RedList = new List<Vector2>();
		internal static Shader DefaultShader;

		static class BsmlWrapper {
			static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

			public static void EnableUI() {
				void wrap() => BSMLSettings.instance.AddSettingsMenu("NoteCutGuide", "NoteCutGuide.Views.settings.bsml", Config.Instance);

				if(hasBsml)
					wrap();
			}
			public static void DisableUI() {
				void wrap() => BSMLSettings.instance.RemoveSettingsMenu(Config.Instance);

				if(hasBsml)
					wrap();
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
			Blue = new Vector2(-1, -1);
			Red = new Vector2(-1, -1);
			BlueData = null;
			RedData = null;
			BlueGuide = null;
			RedGuide = null;
			BlueHead = false;
			RedHead = false;
			BlueDataList.Clear();
			RedDataList.Clear();
			BlueList.Clear();
			RedList.Clear();
		}

		[OnDisable]
		public void OnDisable() {
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			harmony.UnpatchSelf();
			BsmlWrapper.DisableUI();
		}
	}
}
