using HarmonyLib;
using UnityEngine;
using IPA.Utilities;
using NoteCutGuide.Algorithm;

namespace NoteCutGuide.HarmonyPatches {
	[HarmonyPatch(typeof(ColorNoteVisuals), nameof(ColorNoteVisuals.HandleNoteControllerDidInit))]
	static class GuideInitializer {
		static FieldAccessor<ColorNoteVisuals, Color>.Accessor ColorNoteVisuals_noteColor = FieldAccessor<ColorNoteVisuals, Color>.GetAccessor("_noteColor");

		static void Postfix(ColorNoteVisuals __instance, NoteControllerBase noteController) {
			var guide = noteController.transform.Find("NoteCube/NoteCutGuide");

			if(guide == null)
				return;

			// The guide need to be disabled before returning.
			if(!Config.Instance.Enabled) {
				guide.gameObject.SetActive(false);
				return;
			}

			if(Plugin.levelData == null) {
				guide.gameObject.SetActive(false);
				return;
			}

			// No GN or DA. The plugin is not compatible with Pro Mode/Strict Angle, so removed for performance.
			if(Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.ghostNotes || Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.disappearingArrows ||
				Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.proMode || Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.strictAngles) {
				guide.gameObject.SetActive(false);
				return;
			}

			var noteData = noteController.noteData;

			// Reset the position just in case
			guide.position = guide.parent.position;
			guide.rotation = guide.parent.rotation;

			// Change scale according to config
			guide.localScale = new Vector3(Config.Instance.Width, Config.Instance.Height, Config.Instance.Depth);

			// Add an offset to the position
			guide.transform.localPosition = new Vector3(Config.Instance.X, Config.Instance.Y, Config.Instance.Z);

			// Fake bloom
			var renderer = guide.GetComponent<MeshRenderer>();
			if(Config.Instance.Bloom) {
				renderer.material.shader = Shader.Find("Unlit");
			} else {
				renderer.material.shader = Plugin.DefaultShader;
			}

			// Fake bloom is not compatible with any of this
			if(!Config.Instance.Bloom) {
				if(Config.Instance.Rainbow) {
					renderer.material.color = Helper.Rainbow(); // Random colors
				} else if(Config.Instance.Color) {
					if(noteController.noteData.colorType == ColorType.ColorA) { // Custom colors
						renderer.material.color = Config.Instance.Left;
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						renderer.material.color = Config.Instance.Right;
					}
				} else {
					renderer.material.color = ColorNoteVisuals_noteColor(ref __instance); // Default colors
				}
			}

			// Activate/Disable
			if(Config.Instance.Ignore && noteController.noteData.cutDirection == NoteCutDirection.Any) {
				guide.gameObject.SetActive(false);
			} else {
				guide.gameObject.SetActive(true);
			}
		}
	}
}
