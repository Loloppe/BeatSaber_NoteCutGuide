using HarmonyLib;
using UnityEngine;
using IPA.Utilities;
using NoteCutGuide.Algorithm;

namespace NoteCutGuide.HarmonyPatches {
	[HarmonyPatch(typeof(ColorNoteVisuals), nameof(ColorNoteVisuals.HandleNoteControllerDidInit))]
	static class GuideInitializer {
		static FieldAccessor<ColorNoteVisuals, Color>.Accessor ColorNoteVisuals_noteColor = FieldAccessor<ColorNoteVisuals, Color>.GetAccessor("_noteColor");

		static void Postfix(ColorNoteVisuals __instance, NoteControllerBase noteController) {
			// Get BPM
			var bpm = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.level.beatsPerMinute;

			var isDot = noteController.noteData.cutDirection == NoteCutDirection.Any;
			var g = noteController.transform.Find("NoteCube/NoteCutGuide");
			var ignore = false;

			if(g == null)
				return;

			// Reset the position just in case
			g.position = g.parent.position;
			g.rotation = g.parent.rotation;

			// Change scale according to config
			g.localScale = new Vector3(Config.Instance.Width, Config.Instance.Height, Config.Instance.Depth);

			// Add an offset to the position
			g.transform.localPosition = new Vector3(0, 0.3f);

			var baseValueAngle = 0f;
			var angle = 0f;
			var time = 0f;

			if(!isDot) {
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
			}

			// Used to calculate angle between two notes
			var lastPos = new Vector2(-1, -1);
			// Used to find window/stack/tower/etc
			NoteData lastND = null;
			// Used to reset last guide if necessary
			Transform lastGuide = null;
			// Gotta convert to beat from second
			var lastTime = 0f;

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
				// Convert time
				time = Helper.SecToBeat(noteController.noteData.time, bpm);
				lastTime = Helper.SecToBeat(lastND.time, bpm);

				// If it's the end of a pattern, need to swap lastPos with the tail
				if(noteController.noteData.colorType == ColorType.ColorA) {
					if(Plugin.RedDataList.Count > 0 && lastND.time != noteController.noteData.time) {
						var index = Helper.FindPatternIndex(Plugin.RedDataList);
						lastPos.x = Plugin.RedList[index.Item2].x;
						lastPos.y = Plugin.RedList[index.Item2].y;
					}
				} else if(noteController.noteData.colorType == ColorType.ColorB) {
					if(Plugin.BlueDataList.Count > 0 && lastND.time != noteController.noteData.time) {
						var index = Helper.FindPatternIndex(Plugin.BlueDataList);
						lastPos.x = Plugin.BlueList[index.Item2].x;
						lastPos.y = Plugin.BlueList[index.Item2].y;
					}
				}

				// Find the angle using two points in a 2D space
				angle = (Mathf.Atan2(lastPos.y - currentY, lastPos.x - currentX) * 180f / Mathf.PI);
				
				if(isDot) {
					angle -= 90;
				}

				// If the sliders head is not a dot
				if(time - lastTime < 0.2 && time - lastTime > 0 && isDot && lastND.cutDirection != NoteCutDirection.Any) {
					if(noteController.noteData.colorType == ColorType.ColorA) {
						Plugin.RedHead = true;
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						Plugin.BlueHead = true;
					}
				}

				// If it's not a slider and current and last note were dots
				if(time - lastTime >= 0.2 && isDot) {
					if(noteController.noteData.colorType == ColorType.ColorA) {
						if(Plugin.RedDot) {
							angle = lastGuide.eulerAngles.z - 180;
						}
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						if(Plugin.BlueDot) {
							angle = lastGuide.eulerAngles.z - 180;
						}
					}
				}
				else if(time - lastTime < 0.2 && time - lastTime > 0 && isDot) { // Sliders
					if(noteController.noteData.colorType == ColorType.ColorA) {
						if(!Plugin.RedDot) {
							lastGuide.transform.rotation = Quaternion.identity;
							lastGuide.position = lastGuide.parent.position;
							lastGuide.localPosition = new Vector2(0, 0.3f);
						} 
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						if(!Plugin.BlueDot) {
							lastGuide.transform.rotation = Quaternion.identity;
							lastGuide.position = lastGuide.parent.position;
							lastGuide.localPosition = new Vector2(0, 0.3f);
						} 
					}
				}

				// Dot pattern, we remove the stick, this is too much of a pain to handle
				if(time - lastTime < 0.2 && isDot) {
					if(noteController.noteData.colorType == ColorType.ColorA) {
						if(Plugin.RedDot & !Plugin.RedHead) {
							ignore = true;
						}
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						if(Plugin.BlueDot && !Plugin.BlueHead) {
							ignore = true;
						}
					}
				}

				if(angle < 0) {
					angle += 360;
				}

				if(!isDot) {
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
						g.transform.RotateAround(g.parent.position, Vector3.forward, -defaultValue);
						if(noteController.noteData.colorType == ColorType.ColorA) {
							lastGuide.transform.rotation = Quaternion.identity;
							lastGuide.position = lastGuide.parent.position;
							lastGuide.localPosition = new Vector2(0, 0.3f);
						} else if(noteController.noteData.colorType == ColorType.ColorB) {
							lastGuide.transform.rotation = Quaternion.identity;
							lastGuide.position = lastGuide.parent.position;
							lastGuide.localPosition = new Vector2(0, 0.3f);
						}
					} else if(angle >= baseValueAngle - Config.Instance.Angle && angle <= baseValueAngle + Config.Instance.Angle) { // Angle is within degree of the base angle
						// Apply the rotation around the pivot point (which is the center of the note)
						g.transform.RotateAround(g.parent.position, Vector3.forward, angle);
					} else { // Reset
						g.transform.RotateAround(g.parent.position, Vector3.forward, -defaultValue);
					}
				}
				else if(isDot) { // If it's a dot, we just point directly toward note
					g.transform.RotateAround(g.parent.position, Vector3.forward, angle);
				}
			}

			// Activate
			if(ignore) {
				lastGuide.gameObject.SetActive(false);
				g.gameObject.SetActive(false);
			} else {
				g.gameObject.SetActive(true);
			}

			// Save position, note data, guide and angle per color
			if(noteController.noteData.colorType == ColorType.ColorA) {
				Plugin.Red = currentPos;
				Plugin.RedData = noteController.noteData;
				Plugin.RedGuide = g;
				// If the pattern ended, we need to clear the data
				if(Plugin.RedDataList.Count > 0 && lastND.time != noteController.noteData.time) {
					Plugin.RedDataList.Clear();
					Plugin.RedList.Clear();
				}
				if(isDot) {
					Plugin.RedDot = true;
				} else {
					Plugin.RedDot = false;
				}
				if(Plugin.RedHead && time - lastTime >= 0.2) {
					Plugin.RedHead = false;
				}
			} else if(noteController.noteData.colorType == ColorType.ColorB) {
				Plugin.Blue = currentPos;
				Plugin.BlueData = noteController.noteData;
				Plugin.BlueGuide = g;
				// If the pattern ended, we need to clear the data
				if(Plugin.BlueDataList.Count > 0 && lastND.time != noteController.noteData.time) {
					Plugin.BlueDataList.Clear();
					Plugin.BlueList.Clear();
				}
				if(isDot) {
					Plugin.BlueDot = true;
				}
				else {
					Plugin.BlueDot = false;
				}
				if(Plugin.BlueHead && time - lastTime >= 0.2) {
					Plugin.BlueHead = false;
				}
			}

			

			g.GetComponent<MeshRenderer>().material.color = ColorNoteVisuals_noteColor(ref __instance);
		}
	}
}
