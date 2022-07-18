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

			if(Config.Instance.Enabled) {
				var isDot = noteController.noteData.cutDirection == NoteCutDirection.Any;
				var bpm = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.level.beatsPerMinute;
				var ignore = false;

				var renderer = guide.GetComponent<MeshRenderer>();
				if(Config.Instance.Bloom) {
					renderer.material.shader = Shader.Find("Unlit");
				} else {
					renderer.material.shader = Plugin.DefaultShader;
				}

				var noteData = noteController.noteData;

				// Reset the position just in case
				guide.position = guide.parent.position;
				guide.rotation = guide.parent.rotation;

				// Change scale according to config
				guide.localScale = new Vector3(Config.Instance.Width, Config.Instance.Height, Config.Instance.Depth);

				// Add an offset to the position
				guide.transform.localPosition = new Vector3(0, 0.3f);

				// For the rotation
				var angle = 0f;
				// Current time in beat
				var time = 0f;
				// Used to calculate angle between two notes
				var lastPos = new Vector2(-1, -1);
				// Used to find window/stack/tower/etc
				NoteData lastND = null;
				// Used to reset last guide if necessary
				Transform lastGuide = null;
				// Gotta convert to beat from second
				var lastTime = 0f;
				var baseValueAngle = 0f;
				var cutDirection = noteData.cutDirection;

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

				// Current Note Position
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

				switch(cutDirection) {
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

				// This check is to skip the first blue and red note detected (angle-wise)
				if(lastPos != new Vector2(-1, -1) && lastND != null) {
					// Convert time
					time = Helper.SecToBeat(noteData.time, bpm);
					lastTime = Helper.SecToBeat(lastND.time, bpm);

					// If it's the end of a pattern, need to swap lastPos with the tail
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

					// Pepega Clap
					if(isDot) {
						angle -= 90;

						if(angle < 0) {
							angle += 360;
						}

						// Sliders with arrow head, reset head
						if(time - lastTime < 0.2 && time - lastTime > 0 && isDot && lastND.cutDirection != NoteCutDirection.Any) {
							if(noteController.noteData.colorType == ColorType.ColorA) {
								Plugin.RedHead = true;
							} else if(noteController.noteData.colorType == ColorType.ColorB) {
								Plugin.BlueHead = true;
							}

							lastGuide.transform.rotation = Quaternion.identity;
							lastGuide.position = lastGuide.parent.position;
							lastGuide.localPosition = new Vector2(0, 0.3f);
						} else if(time - lastTime < 0.25 && time - lastTime > 0 && lastND.cutDirection == NoteCutDirection.Any) { // Dot Sliders
							if(noteController.noteData.colorType == ColorType.ColorA) {
								if(Plugin.RedHead) {
									guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle);
									angle = 0;
								} else {
									ignore = true;
								}
							} else if(noteController.noteData.colorType == ColorType.ColorB) {
								if(Plugin.BlueHead) {
									guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle);
									angle = 0;
								} else {
									ignore = true;
								}
							}
						} else if(time == lastTime) { // Dot Pattern, ignore
							ignore = true;
						} else if(lastND.cutDirection == NoteCutDirection.Any) { // Flip if we can't find it
							angle = lastGuide.eulerAngles.z - 180;
						}
					}

					if(angle < 0) {
						angle += 360;
					}

					if(!isDot) {
						// Normalize the angle to fit the note direction
						if(baseValueAngle == 0 && angle > 180) {
							baseValueAngle = 360;
						}

						var defaultValue = 0f;

						// This is counter-clockwise, we make all of them point right >>> (which is 0 degree)
						switch(cutDirection) {
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
							}
							else {
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
						} else if(angle >= baseValueAngle - Config.Instance.Angle && angle <= baseValueAngle + Config.Instance.Angle) { // Angle is within degree of the base angle
							guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle);
						} else { // Reset
							guide.transform.RotateAround(guide.parent.position, Vector3.forward, -defaultValue);
						}
					} else if(isDot) {
						guide.transform.RotateAround(guide.parent.position, Vector3.forward, angle);
					}
				}

				// Activate
				if(ignore) {
					lastGuide.gameObject.SetActive(false);
					guide.gameObject.SetActive(false);
				} else if(Config.Instance.Ignore && isDot) {
					guide.gameObject.SetActive(false);
				} else {
					guide.gameObject.SetActive(true);
				}


				// Save position, note data, guide and angle per color
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

				if(Config.Instance.Rainbow) {
					guide.GetComponent<MeshRenderer>().material.color = Helper.Rainbow();
				} else if(Config.Instance.Color) {
					if(noteController.noteData.colorType == ColorType.ColorA) {
						guide.GetComponent<MeshRenderer>().material.color = Config.Instance.Left;
					}
					else if(noteController.noteData.colorType == ColorType.ColorB) {
						guide.GetComponent<MeshRenderer>().material.color = Config.Instance.Right;
					}
				} else {
					guide.GetComponent<MeshRenderer>().material.color = ColorNoteVisuals_noteColor(ref __instance);
				}
			} else {
				guide.gameObject.SetActive(false);
			}
		}
	}
}
