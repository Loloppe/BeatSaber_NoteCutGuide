using System;
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

			// Reset the position just in case
			g.position = g.parent.position;
			g.rotation = g.parent.rotation;

			// Add an offset to the position
			g.transform.localPosition = new Vector3(0, 0.3f);

			var baseValueAngle = 0f;
			var angle = 0f;

			switch(noteController.noteData.cutDirection) {
				case NoteCutDirection.Up:
					baseValueAngle = 270f;
					break;
				case NoteCutDirection.Down:
					baseValueAngle = 90f;
					break;
				case NoteCutDirection.Right:
					baseValueAngle = 180f;
					break;
				case NoteCutDirection.UpLeft:
					baseValueAngle = 315f;
					break;
				case NoteCutDirection.UpRight:
					baseValueAngle = 225f;
					break;
				case NoteCutDirection.DownLeft:
					baseValueAngle = 45f;
					break;
				case NoteCutDirection.DownRight:
					baseValueAngle = 135f;
					break;
			}

			// Used to calculate angle between two notes
			var lastPos = new Vector2(-1, -1);
			// Used to find window/stack/tower/etc
			NoteData lastND = null;
			// Used to reset last guide if necessary
			Transform lastGuide = null;

			if(noteController.noteData.colorType == ColorType.ColorA) {
				lastPos = Plugin.Red;
				lastND = Plugin.RedData;
				lastGuide = Plugin.RedGuide;
			} else if(noteController.noteData.colorType == ColorType.ColorB) {
				lastPos = Plugin.Blue;
				lastND = Plugin.BlueData;
				lastGuide = Plugin.BlueGuide;
			}

			// Current Note Position
			var currentX = 0f;
			var currentY = 0f;

			// Get X position based on lineIndex
			switch(noteController.noteData.lineIndex) {
				case 0:
					currentX = -0.9f;
					break;
				case 1:
					currentX = -0.3f;
					break;
				case 2:
					currentX = 0.3f;
					break;
				case 3:
					currentX = 0.9f;
					break;
			}

			// Get Y position based on noteLineLayer
			switch(noteController.noteData.noteLineLayer) {
				case NoteLineLayer.Base:
					currentY = 0.3f;
					break;
				case NoteLineLayer.Upper:
					currentY = 0.9f;
					break;
				case NoteLineLayer.Top:
					currentY = 1.5f;
					break;
			}

			var currentPos = new Vector2(currentX, currentY);

			// This check is to skip the first blue and red note detected (angle-wise)
			if(lastPos != new Vector2(-1, -1) && lastND != null) {
				// Find the angle using two points in a 2D space
				angle = (Mathf.Atan2(lastPos.y - currentY, lastPos.x - currentX) * 180f / Mathf.PI);
				if(angle < 0) {
					angle += 360;
				}

				var defaultValue = 0f;

				// This is counter-clockwise, we make all of them point right >>> (which is 0 degree)
				switch(noteController.noteData.cutDirection) {
					case NoteCutDirection.Up:
						defaultValue = 90f;
						break;
					case NoteCutDirection.Down:
						defaultValue = -90f;
						break;
					case NoteCutDirection.Right:
						defaultValue = 180f;
						break;
					case NoteCutDirection.UpLeft:
						defaultValue = 45f;
						break;
					case NoteCutDirection.UpRight:
						defaultValue = 135f;
						break;
					case NoteCutDirection.DownLeft:
						defaultValue = -45f;
						break;
					case NoteCutDirection.DownRight:
						defaultValue = -135f;
						break;
				}

				g.transform.RotateAround(g.parent.position, Vector3.forward, defaultValue);

				// Normalize the angle to fit the note direction
				if(baseValueAngle == 0 && angle > 180) {
					baseValueAngle = 360;
				}

				// Reset angle if it's a window/stack/tower/etc.
				if(lastND.time == noteController.noteData.time) {
					lastGuide.transform.rotation = Quaternion.identity;
					lastGuide.position = lastGuide.parent.position;
					lastGuide.localPosition = new Vector2(0, 0.3f);
					g.transform.RotateAround(g.parent.position, Vector3.forward, -defaultValue);
				}else if(angle >= baseValueAngle - 45 && angle <= baseValueAngle + 45) { // Angle is within 45 degree of the base angle
					 // Apply the rotation around the pivot point (which is the center of the note)
					g.transform.RotateAround(g.parent.position, Vector3.forward, angle);
				}
			}

			// Activate
			g.gameObject.SetActive(!isDot);

			// Save position, note data, guide and angle per color
			if(noteController.noteData.colorType == ColorType.ColorA) {
				Plugin.Red = currentPos;
				Plugin.RedData = noteController.noteData;
				Plugin.RedGuide = g;
			} else if(noteController.noteData.colorType == ColorType.ColorB) {
				Plugin.Blue = currentPos;
				Plugin.BlueData = noteController.noteData;
				Plugin.BlueGuide = g;
			}

			if(isDot) {
				lastGuide.transform.rotation = Quaternion.identity;
				lastGuide.position = lastGuide.parent.position;
				lastGuide.localPosition = new Vector2(0, 0.3f);
				return;
			}

			g.GetComponent<MeshRenderer>().material.color = ColorNoteVisuals_noteColor(ref __instance);
		}
	}
}
