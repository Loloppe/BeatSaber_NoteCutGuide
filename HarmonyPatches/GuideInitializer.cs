using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using IPA.Utilities;

namespace NoteCutGuide.HarmonyPatches {
	[HarmonyPatch(typeof(ColorNoteVisuals), nameof(ColorNoteVisuals.HandleNoteControllerDidInit))]
	static class GuideInitializer {
		static FieldAccessor<ColorNoteVisuals, Color>.Accessor ColorNoteVisuals_noteColor = FieldAccessor<ColorNoteVisuals, Color>.GetAccessor("_noteColor");

		static void Postfix(ColorNoteVisuals __instance, NoteControllerBase noteController) {
			var isDot = noteController.noteData.cutDirection == NoteCutDirection.Any;

			var g = noteController.transform.Find("NoteCube/NoteCutGuide");

			if(g == null)
				return;

			g.gameObject.SetActive(!isDot);

			if(isDot)
				return;

			g.GetComponent<MeshRenderer>().material.color = ColorNoteVisuals_noteColor(ref __instance);
		}
	}
}
