﻿using HarmonyLib;
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
			if(!Config.Instance.Enabled || Plugin.levelData == null) {
				guide.gameObject.SetActive(false);
				return;
			}

			if(noteController.noteData.cutDirection == NoteCutDirection.Any) {
				guide.gameObject.SetActive(false);
				return;
			}

			// No GN or DA. The plugin is not compatible with Pro Mode/Strict Angle, so removed for performance.
			if(Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.ghostNotes || Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.disappearingArrows ||
				Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.proMode || Plugin.levelData.GameplayCoreSceneSetupData.gameplayModifiers.strictAngles) {
				guide.gameObject.SetActive(false);
				return;
			}

			// This is probably not optimal, but idk how to do this better
			if(Config.Instance.HMD) {
				CameraUtils.Core.VisibilityUtils.SetLayerRecursively(guide, CameraUtils.Core.VisibilityLayer.HmdOnly);
			} else {
				CameraUtils.Core.VisibilityUtils.SetLayerRecursively(guide, CameraUtils.Core.VisibilityLayer.Default);
			}

			// Reset the position just in case
			guide.position = guide.parent.position;
			guide.rotation = guide.parent.rotation;

			// Change scale according to config
			guide.localScale = new Vector3(Config.Instance.Width, Config.Instance.Height, Config.Instance.Depth);

			// Add an offset to the position
			guide.transform.localPosition = new Vector3(Config.Instance.X, Config.Instance.Y, Config.Instance.Z);

			// Bloom
			var renderer = guide.GetComponent<MeshRenderer>();
			if(Config.Instance.Bloom) {
				renderer.material.shader = Shader.Find("UI/Default");
			} else {
				renderer.material.shader = Plugin.DefaultShader;
			}
			
			if(Config.Instance.Rainbow) { // Random colors
				if(Config.Instance.Bloom) {
					renderer.material.color = ColorWithAlpha(renderer.material.color = Helper.Rainbow(), Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorWithAlpha(renderer.material.color = Helper.Rainbow(), 1f); 
				}
			} else if(Config.Instance.Color) { // Custom colors
				if(Config.Instance.Bloom) {
					if(noteController.noteData.colorType == ColorType.ColorA) { 
						renderer.material.color = ColorWithAlpha(Config.Instance.Left, Config.Instance.Brightness);
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						renderer.material.color = ColorWithAlpha(Config.Instance.Right, Config.Instance.Brightness);
					}
				} else {
					if(noteController.noteData.colorType == ColorType.ColorA) { 
						renderer.material.color = ColorWithAlpha(Config.Instance.Left, 1f);
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						renderer.material.color = ColorWithAlpha(Config.Instance.Right, 1f);
					}
				}
			} else { // Default colors
				if(Config.Instance.Bloom) {
					renderer.material.color = ColorWithAlpha(ColorNoteVisuals_noteColor(ref __instance), Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorWithAlpha(ColorNoteVisuals_noteColor(ref __instance), 1f);
				}
			}

			// Activate/Disable
			guide.gameObject.SetActive(true);
		}

		public static Color ColorWithAlpha(this Color color, float alpha) {
			color.a = alpha;
			return color;
		}
	}
}
