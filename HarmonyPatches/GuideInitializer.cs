using HarmonyLib;
using NoteCutGuide.Algorithm;
using UnityEngine;

namespace NoteCutGuide.HarmonyPatches {
	[HarmonyPatch(typeof(ColorNoteVisuals), nameof(ColorNoteVisuals.HandleNoteControllerDidInit))]
	static class GuideInitializer {
		static void Postfix(ref NoteControllerBase noteController, ref Color ____noteColor) {
			if(!Config.Instance.Enabled || BS_Utils.Plugin.LevelData == null || BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData == null)
				return;

			// No GN or DA. The plugin is not compatible with Pro Mode/Strict Angle, so removed for performance.
			if(BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.ghostNotes || BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.disappearingArrows ||
				BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.proMode || BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.strictAngles)
				return;

			// Create the guide
			var guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
			GameObject.Destroy(guide.GetComponent<BoxCollider>());
			var renderer = guide.GetComponent<MeshRenderer>();
			var NoteCube = noteController.transform.Find("NoteCube");
			renderer.sharedMaterial = NoteCube.GetComponentInChildren<MeshRenderer>().sharedMaterial;
			guide.name = "NoteCutGuide";
			guide.transform.SetParent(NoteCube.transform);
			var data = noteController.noteData;
			guide.gameObject.SetActive(false);

			// If it failed skip, also not compatible with dot note.
			if(guide == null || data.cutDirection == NoteCutDirection.Any)
				return;

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
					renderer.material.color = ColorWithAlpha(____noteColor, Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorWithAlpha(____noteColor, 1f);
				}
			}

			guide.gameObject.SetActive(true);
		}

		public static Color ColorWithAlpha(Color color, float alpha) {
			color.a = alpha;
			return color;
		}
	}

	[HarmonyPatch(typeof(NoteJump), nameof(NoteJump.ManualUpdate))]
	static class DestroyGuide {
		static void Prefix(ref Transform ____rotatedObject, ref PlayerTransforms ____playerTransforms) {
			// Compatible with Custom Notes and replay, a bit wacky but oh well.
			if (____rotatedObject.position.z <= ____playerTransforms.headPseudoLocalPos.z + 2.5f) {
				var guide = ____rotatedObject?.Find("NoteCutGuide");
				if(guide != null)
					GameObject.DestroyImmediate(guide.gameObject);
			}
		}
	}
}
