using HarmonyLib;
using UnityEngine;
using NoteCutGuide.Algorithm;

namespace NoteCutGuide.HarmonyPatches {
	[HarmonyPatch(typeof(ColorNoteVisuals), nameof(ColorNoteVisuals.HandleNoteControllerDidInit))]
	static class GuideInitializer {
		static void Postfix(ref NoteControllerBase noteController, ref ColorManager ____colorManager) {
			var guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
			GameObject.Destroy(guide.GetComponent<BoxCollider>());
			var renderer = guide.GetComponent<MeshRenderer>();
			var NoteCube = noteController.transform.Find("NoteCube").GetComponentInChildren<CutoutEffect>();
			renderer.material = NoteCube.GetComponentInChildren<MeshRenderer>().material;
			guide.transform.SetParent(NoteCube.transform);
			var data = noteController.noteData;

			if(guide == null)
				return;

			if(!Config.Instance.Enabled || BS_Utils.Plugin.LevelData == null || BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData == null) {
				return;
			}

			if(data.cutDirection == NoteCutDirection.Any) {
				return;
			}

			// No GN or DA. The plugin is not compatible with Pro Mode/Strict Angle, so removed for performance.
			if(BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.ghostNotes || BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.disappearingArrows ||
				BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.proMode || BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.strictAngles) {
				return;
			}

			// This is probably not optimal, but idk how to do this better
			if(Config.Instance.HMD) {
				CameraUtils.Core.VisibilityUtils.SetLayerRecursively(guide, CameraUtils.Core.VisibilityLayer.HmdOnly);
			} else {
				CameraUtils.Core.VisibilityUtils.SetLayerRecursively(guide, CameraUtils.Core.VisibilityLayer.Default);
			}

			// Reset the position just in case
			guide.transform.position = guide.transform.parent.position;
			guide.transform.rotation = guide.transform.parent.rotation;

			// Change scale according to config
			guide.transform.localScale = new Vector3(Config.Instance.Width, Config.Instance.Height, Config.Instance.Depth);

			// Add an offset to the position
			guide.transform.localPosition = new Vector3(Config.Instance.X, Config.Instance.Y, Config.Instance.Z);

			// Bloom
			if(Config.Instance.Bloom) {
				renderer.material.shader = Shader.Find("UI/Default");
			}
			
			if(Config.Instance.Rainbow) { // Random colors
				if(Config.Instance.Bloom) {
					renderer.material.color = ColorWithAlpha(renderer.material.color = Helper.Rainbow(), Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorWithAlpha(renderer.material.color = Helper.Rainbow(), 1f); 
				}
			} else if(Config.Instance.Color) { // Custom colors
				if(Config.Instance.Bloom) {
					if(data.colorType == ColorType.ColorA) { 
						renderer.material.color = ColorWithAlpha(Config.Instance.Left, Config.Instance.Brightness);
					} else if(data.colorType == ColorType.ColorB) {
						renderer.material.color = ColorWithAlpha(Config.Instance.Right, Config.Instance.Brightness);
					}
				} else {
					if(data.colorType == ColorType.ColorA) { 
						renderer.material.color = ColorWithAlpha(Config.Instance.Left, 1f);
					} else if(data.colorType == ColorType.ColorB) {
						renderer.material.color = ColorWithAlpha(Config.Instance.Right, 1f);
					}
				}
			} else { // Default colors
				if(Config.Instance.Bloom) {
					renderer.material.color = ColorWithAlpha(____colorManager.ColorForType(data.colorType), Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorWithAlpha(____colorManager.ColorForType(data.colorType), 1f);
				}
			}
		}

		public static Color ColorWithAlpha(Color color, float alpha) {
			color.a = alpha;
			return color;
		}
	}
}
