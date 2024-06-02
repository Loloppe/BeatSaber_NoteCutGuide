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
			if(!Config.Instance.Enabled || Plugin.levelData == null) {
				guide.gameObject.SetActive(false);
				return;
			}

			if(Config.Instance.Ignore && noteController.noteData.cutDirection == NoteCutDirection.Any) {
				if(noteController.noteData.colorType == ColorType.ColorA) {
					Plugin.RedData = null;
				} else if(noteController.noteData.colorType == ColorType.ColorB) {
					Plugin.BlueData = null;
				}
				guide.gameObject.SetActive(false);
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
			guide.transform.localPosition = new Vector3(Config.Instance.X, Config.Instance.Y, Config.Instance.Z);

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
				var bpm = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.beatmapLevel.beatsPerMinute;
				time = Helper.SecToBeat(noteData.time, bpm);
				lastTime = Helper.SecToBeat(lastND.time, bpm);

				// If it's the end of a pattern, need to swap lastPos with the tail, so that the current note point to the right one.
				if(noteData.colorType == ColorType.ColorA) {
					if(Plugin.RedDataList.Count > 0 && time - lastTime > 0.1) {
						var index = Helper.FindPatternIndex(Plugin.RedDataList);
						lastPos.x = Plugin.RedList[index.Item2].x;
						lastPos.y = Plugin.RedList[index.Item2].y;
						for(int i = 0; i < Plugin.RedDataList.Count; i++) {
							if(i != index.Item1) {
								Plugin.RedGuideList[i].gameObject.SetActive(false); // Disable non-head notes
							} else { // Head
								Plugin.RedGuideList[i].gameObject.SetActive(true);
								if(Plugin.RedDataList[i].lineIndex == 0) {
									if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Down) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Up) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Right) {
										if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										} else if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										}
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.UpRight) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.DownRight) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									}
								} else if(Plugin.RedDataList[i].lineIndex == 1 || Plugin.RedDataList[i].lineIndex == 2) {
									if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Left) {
										if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										} else if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										}
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.UpLeft) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.DownLeft) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Right) {
										if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										} else if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										}
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.UpRight) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.DownRight) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									}
								} else if(Plugin.RedDataList[i].lineIndex == 3) {
									if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Down) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Up) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.Left) {
										if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										} else if(Plugin.RedDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										}
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.UpLeft) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.RedDataList[i].cutDirection == NoteCutDirection.DownLeft) {
										Plugin.RedGuideList[i].transform.RotateAround(Plugin.RedGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									}
								}
							}
						}
					}
				} else if(noteData.colorType == ColorType.ColorB) {
					if(Plugin.BlueDataList.Count > 0 && time - lastTime > 0.1) {
						var index = Helper.FindPatternIndex(Plugin.BlueDataList);
						lastPos.x = Plugin.BlueList[index.Item2].x;
						lastPos.y = Plugin.BlueList[index.Item2].y;
						for(int i = 0; i < Plugin.BlueDataList.Count; i++) {
							if(i != index.Item1) {
								Plugin.BlueGuideList[i].gameObject.SetActive(false); // Disable non-head notes
							} else { // Head
								Plugin.BlueGuideList[i].gameObject.SetActive(true);
								if(Plugin.BlueDataList[i].lineIndex == 0) {
									if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Down) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Up) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Right) {
										if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										} else if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										}
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.UpRight) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.DownRight) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									}
								} else if(Plugin.BlueDataList[i].lineIndex == 1 || Plugin.BlueDataList[i].lineIndex == 2) {
									if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Left) {
										if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										} else if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										}
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.UpLeft) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.DownLeft) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Right) {
										if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										} else if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										}
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.UpRight) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.DownRight) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									}
								} else if(Plugin.BlueDataList[i].lineIndex == 3) {
									if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Down) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Up) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.Left) {
										if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Base) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
										} else if(Plugin.BlueDataList[i].noteLineLayer == NoteLineLayer.Top) {
											Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
										}
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.UpLeft) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, Config.Instance.TDAngle);
									} else if(Plugin.BlueDataList[i].cutDirection == NoteCutDirection.DownLeft) {
										Plugin.BlueGuideList[i].transform.RotateAround(Plugin.BlueGuideList[i].parent.position, Vector3.forward, -Config.Instance.TDAngle);
									}
								}
							}
						}
					}
				}

				// Reset on slow middle lane down and up swing for TD
				if(time - lastTime > (0.5 * bpm / 200) && (noteData.cutDirection == NoteCutDirection.Down || noteData.cutDirection == NoteCutDirection.Up) && (noteData.lineIndex == 1 || noteData.lineIndex == 2)) {
					angle = 0;
				} else {
					// Find the angle using two points in a 2D space
					angle = (Mathf.Atan2(lastPos.y - currentY, lastPos.x - currentX) * 180f / Mathf.PI);
				}

				// Here we handle note of any cut direction
				if(noteController.noteData.cutDirection == NoteCutDirection.Any) {
					// Not sure why this is necessary..
					angle -= 90;

					if(angle < 0) {
						angle += 360;
					}

					// Pattern with a direction, in this case we reset the head angle
					if(time - lastTime < 0.1 && noteController.noteData.cutDirection == NoteCutDirection.Any && lastND.cutDirection != NoteCutDirection.Any) {
						if(noteController.noteData.colorType == ColorType.ColorA) {
							Plugin.RedHead = true;
						} else if(noteController.noteData.colorType == ColorType.ColorB) {
							Plugin.BlueHead = true;
						}

						// Reset
						lastGuide.transform.rotation = Quaternion.identity;
						lastGuide.position = lastGuide.parent.position;
						lastGuide.localPosition = new Vector2(0, 0.3f);
					} else if(time - lastTime < 0.1 && lastND.cutDirection == NoteCutDirection.Any) { // Pattern without a direction
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

					// If the pattern ended, we need to clear the data
					if(noteController.noteData.colorType == ColorType.ColorA) {
						if(Plugin.RedDataList.Count > 0 && time - lastTime > 0.1) {
							Plugin.RedDataList.Clear();
							Plugin.RedList.Clear();
							Plugin.RedGuideList.Clear();
						}
						if(Plugin.RedHead && time - lastTime > 0.1) {
							Plugin.RedHead = false;
						}
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						if(Plugin.BlueDataList.Count > 0 && time - lastTime > 0.1) {
							Plugin.BlueDataList.Clear();
							Plugin.BlueList.Clear();
							Plugin.BlueGuideList.Clear();
						}
						if(Plugin.BlueHead && time - lastTime > 0.1) {
							Plugin.BlueHead = false;
						}
					}

					// Reset angle if it's a window/stack/tower/etc.
					if(time - lastTime < 0.1) {
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
								Plugin.RedGuideList.Add(lastGuide);
								Plugin.RedGuideList.Add(guide);
							} else {
								Plugin.RedDataList.Add(noteController.noteData);
								Plugin.RedList.Add(currentPos);
								Plugin.RedGuideList.Add(guide);
							}
						} else if(noteController.noteData.colorType == ColorType.ColorB) {
							if(Plugin.BlueDataList.Count == 0) {
								Plugin.BlueDataList.Add(lastND);
								Plugin.BlueDataList.Add(noteData);
								Plugin.BlueList.Add(lastPos);
								Plugin.BlueList.Add(currentPos);
								Plugin.BlueGuideList.Add(lastGuide);
								Plugin.BlueGuideList.Add(guide);
							} else {
								Plugin.BlueDataList.Add(noteData);
								Plugin.BlueList.Add(currentPos);
								Plugin.BlueGuideList.Add(guide);
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
			} else if(noteController.noteData.colorType == ColorType.ColorB) {
				Plugin.Blue = currentPos;
				Plugin.BlueGuide = guide;
				Plugin.BlueData = noteData;
			}

			if(Config.Instance.Ignore && noteController.noteData.cutDirection == NoteCutDirection.Any) {
				return;
			}

			// Bloom
			var renderer = guide.GetComponent<MeshRenderer>();
			if(Config.Instance.Bloom) {
				renderer.material.shader = Shader.Find("UI/Default");
			} else {
				renderer.material.shader = Plugin.DefaultShader;
			}

			if(Config.Instance.Rainbow) { // Random colors
				if(Config.Instance.Bloom) {
					renderer.material.color = ColorExtensions.ColorWithAlpha(renderer.material.color = Helper.Rainbow(), Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorExtensions.ColorWithAlpha(renderer.material.color = Helper.Rainbow(), 1f); 
				}
			} else if(Config.Instance.Color) { // Custom colors
				if(Config.Instance.Bloom) {
					if(noteController.noteData.colorType == ColorType.ColorA) { 
						renderer.material.color = ColorExtensions.ColorWithAlpha(Config.Instance.Left, Config.Instance.Brightness);
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						renderer.material.color = ColorExtensions.ColorWithAlpha(Config.Instance.Right, Config.Instance.Brightness);
					}
				} else {
					if(noteController.noteData.colorType == ColorType.ColorA) { 
						renderer.material.color = ColorExtensions.ColorWithAlpha(Config.Instance.Left, 1f);
					} else if(noteController.noteData.colorType == ColorType.ColorB) {
						renderer.material.color = ColorExtensions.ColorWithAlpha(Config.Instance.Right, 1f);
					}
				}
			} else { // Default colors
				if(Config.Instance.Bloom) {
					renderer.material.color = ColorExtensions.ColorWithAlpha(ColorNoteVisuals_noteColor(ref __instance), Config.Instance.Brightness);
				} else {
					renderer.material.color = ColorExtensions.ColorWithAlpha(ColorNoteVisuals_noteColor(ref __instance), 1f);
				}
			}

			// Activate/Disable
			if(ignore) {
				lastGuide.gameObject.SetActive(false);
				guide.gameObject.SetActive(false);
			} else {
				guide.gameObject.SetActive(true);
			}
		}
	}
}
