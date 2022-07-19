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

			// Disable current and last guide if necessary, we still need to save the data before.
			var ignore = false;

			var noteData = noteController.noteData;

			// Reset the position just in case
			guide.position = guide.parent.position;
			guide.rotation = guide.parent.rotation;

			// Change scale according to config
			guide.localScale = new Vector3(Config.Instance.Width, Config.Instance.Height, Config.Instance.Depth);

			// Add an offset to the position
			guide.transform.localPosition = new Vector3(0, 0.3f);

			// To point right and then toward previous note
			var angle = 0f;
			// Time of the current note in beat, necessary to find sliders, etc.
			var time = 0f;
			// Time of the last note in beat, necessary to find sliders, etc.
			var lastTime = 0f;
			// Position of last note is necessary to find the angle
			var lastPos = new Vector2(-1, -1);
			// noteData of the last note, necessary to find sliders, etc.
			NoteData lastND = null;
			// Guide of the last note, necessary to reset/disable if necessary
			Transform lastGuide = null;
			// Used to compare with the available angle found, to allow/disallow the rotation
			var baseValueAngle = 0f;

			// Fetch the last note data
			if(noteData.colorType == ColorType.ColorA) {
				lastPos = Plugin.Red;
				lastND = Plugin.RedData;
				lastGuide = Plugin.RedGuide;
			} else if(noteData.colorType == ColorType.ColorB) {
				lastPos = Plugin.Blue;
				lastND = Plugin.BlueData;
				lastGuide = Plugin.BlueGuide;
			}

			// Current note position
			var currentX = 0f;
			var currentY = 0f;

			// Get X position based on lineIndex
			switch(noteData.lineIndex) {
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
			switch(noteData.noteLineLayer) {
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

			// Used to compare with the available angle found, to allow/disallow the rotation
			switch(noteData.cutDirection) {
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

			var currentPos = new Vector2(currentX, currentY);

			// This check is to skip the first blue and red note detected (only angle-wise)
			if(lastPos != new Vector2(-1, -1) && lastND != null) {
				// Find time in beat
				var bpm = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.level.beatsPerMinute;
				time = Helper.SecToBeat(noteData.time, bpm);
				lastTime = Helper.SecToBeat(lastND.time, bpm);

				// If it's the end of a pattern, need to swap lastPos with the tail, so that the current note point to the right one.
				if(noteData.colorType == ColorType.ColorA) {
					if(Plugin.RedDataList.Count > 0 && lastND.time != noteData.time) {
						var index = Helper.FindPatternIndex(Plugin.RedDataList);
						lastPos.x = Plugin.RedList[index.Item2].x;
						lastPos.y = Plugin.RedList[index.Item2].y;
					}
				} else if(noteData.colorType == ColorType.ColorB) {
					if(Plugin.BlueDataList.Count > 0 && lastND.time != noteData.time) {
						var index = Helper.FindPatternIndex(Plugin.BlueDataList);
						lastPos.x = Plugin.BlueList[index.Item2].x;
						lastPos.y = Plugin.BlueList[index.Item2].y;
					}
				}

				// Find the angle using two points in a 2D space
				angle = (Mathf.Atan2(lastPos.y - currentY, lastPos.x - currentX) * 180f / Mathf.PI);

				// Here we handle note of any cut direction
				if(noteController.noteData.cutDirection == NoteCutDirection.Any) {
					// Not sure why this is necessary..
					angle -= 90;

					if(angle < 0) {
						angle += 360;
					}

					// Pattern with a direction, in this case we reset the head angle
					if(time - lastTime < 0.2 && time - lastTime > 0 && noteController.noteData.cutDirection == NoteCutDirection.Any && lastND.cutDirection != NoteCutDirection.Any) {
						if(noteController.noteData.colorType == ColorType.ColorA) {
							Plugin.RedHead = true;
						} else if(noteController.noteData.colorType == ColorType.ColorB) {
							Plugin.BlueHead = true;
						}
						
						// Reset
						lastGuide.transform.rotation = Quaternion.identity;
						lastGuide.position = lastGuide.parent.position;
						lastGuide.localPosition = new Vector2(0, 0.3f);
					} else if(time - lastTime < 0.2 && time - lastTime > 0 && lastND.cutDirection == NoteCutDirection.Any) { // Pattern without a direction
						if(noteController.noteData.colorType == ColorType.ColorA) {
							if(Plugin.RedHead) {
								guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle); // Point to last note
								angle = 0; // Remove angle
							} else {
								ignore = true; // Remove the last two guides
							}
						} else if(noteController.noteData.colorType == ColorType.ColorB) {
							if(Plugin.BlueHead) {
								guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle); // Point to last note
								angle = 0; // Remove angle
							} else {
								ignore = true; // Remove the last two guides
							}
						}
					} else if(time == lastTime) { // Dot Pattern
						ignore = true; // Remove the last two guides
					} else if(lastND.cutDirection == NoteCutDirection.Any) { // If both note are any cut direction
						angle = lastGuide.eulerAngles.z - 180; // Flip the angle
					}
				}

				if(angle < 0) {
					angle += 360;
				}

				if(noteController.noteData.cutDirection != NoteCutDirection.Any) {
					// Normalize the angle to fit the note direction
					if(baseValueAngle == 0 && angle > 180) {
						baseValueAngle = 360;
					}

					// This is counter-clockwise, we make all of them point to the right
					var defaultValue = 0f;
					switch(noteData.cutDirection) {
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
					guide.transform.RotateAround(guide.parent.position, Vector3.forward, defaultValue);

					// Reset angle if it's a window/stack/tower/etc.
					if(lastND.time == noteData.time) {
						if(lastND.cutDirection == NoteCutDirection.Any) {
							lastGuide.gameObject.SetActive(false);
						} else {
							lastGuide.transform.rotation = Quaternion.identity;
							lastGuide.position = lastGuide.parent.position;
							lastGuide.localPosition = new Vector2(0, 0.3f);
						}
						guide.transform.RotateAround(guide.parent.position, Vector3.forward, -defaultValue);
						if(noteController.noteData.colorType == ColorType.ColorA) {
							if(Plugin.RedDataList.Count == 0) {
								Plugin.RedDataList.Add(lastND);
								Plugin.RedDataList.Add(noteData);
								Plugin.RedList.Add(lastPos);
								Plugin.RedList.Add(currentPos);
							} else {
								Plugin.RedDataList.Add(noteController.noteData);
								Plugin.RedList.Add(currentPos);
							}
						} else if(noteController.noteData.colorType == ColorType.ColorB) {
							if(Plugin.BlueDataList.Count == 0) {
								Plugin.BlueDataList.Add(lastND);
								Plugin.BlueDataList.Add(noteData);
								Plugin.BlueList.Add(lastPos);
								Plugin.BlueList.Add(currentPos);
							} else {
								Plugin.BlueDataList.Add(noteData);
								Plugin.RedList.Add(currentPos);
							}
						}
					} else if(angle >= baseValueAngle - Config.Instance.Angle && angle <= baseValueAngle + Config.Instance.Angle) { // If the angle is within limit
						guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle); // Apply
					} else {
						guide.transform.RotateAround(guide.parent.position, Vector3.forward, -defaultValue); // Otherwise, we reset to default angle (straight)
					}
				} else if(noteController.noteData.cutDirection == NoteCutDirection.Any) {
					guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle); // Apply
				}
			}

			// Save everything for the next note
			if(noteController.noteData.colorType == ColorType.ColorA) {
				Plugin.Red = currentPos;
				Plugin.RedGuide = guide;
				Plugin.RedData = noteData;
				// If the pattern ended, we need to clear the data
				if(Plugin.RedDataList.Count > 0 && lastND.time != noteController.noteData.time) {
					Plugin.RedDataList.Clear();
					Plugin.RedList.Clear();
				}
				if(Plugin.RedHead && time - lastTime >= 0.2) {
					Plugin.RedHead = false;
				}
			} else if(noteController.noteData.colorType == ColorType.ColorB) {
				Plugin.Blue = currentPos;
				Plugin.BlueGuide = guide;
				Plugin.BlueData = noteData;
				// If the pattern ended, we need to clear the data
				if(Plugin.BlueDataList.Count > 0 && lastND.time != noteController.noteData.time) {
					Plugin.BlueDataList.Clear();
					Plugin.BlueList.Clear();
				}
				if(Plugin.BlueHead && time - lastTime >= 0.2) {
					Plugin.BlueHead = false;
				}
			}

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
			if(ignore) {
				lastGuide.gameObject.SetActive(false);
				guide.gameObject.SetActive(false);
			} else if(Config.Instance.Ignore && noteController.noteData.cutDirection == NoteCutDirection.Any) {
				guide.gameObject.SetActive(false);
			} else {
				guide.gameObject.SetActive(true);
			}
		}
	}
}
