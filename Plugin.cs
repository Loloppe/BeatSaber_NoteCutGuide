using HarmonyLib;
using IPA;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace NoteCutGuide {
	//[Plugin(RuntimeOptions.SingleStartInit)]
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
		internal static float BlueAngle = -1f;
		internal static float RedAngle = -1f;

		[Init]
		public Plugin(IPALogger logger, IPA.Config.Config conf) {
			Instance = this;
			Log = logger;
			//Config.Instance = conf.Generated<Config>();
			harmony = new Harmony("Kinsi55.BeatSaber.NoteCutGuide");
		}

		[OnEnable]
		public void OnEnable() {
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public void OnActiveSceneChanged(Scene arg0, Scene scene) {
			Blue = new Vector2(-1, -1);
			Red = new Vector2(-1, -1);
			BlueData = null;
			RedData = null;
			BlueGuide = null;
			RedGuide = null;
			BlueAngle = -1f;
			RedAngle = -1f;
		}

		[OnDisable]
		public void OnDisable() {
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			harmony.UnpatchSelf();
		}
	}
}
