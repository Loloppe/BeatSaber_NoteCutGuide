using System.Collections.Generic;
using UnityEngine;

namespace NoteCutGuide.Algorithm {
	internal class Helper {
		public static (int, int) FindPatternIndex(List<NoteData> noteDataList) {

			var index = new List<int>();

			for(var i = 0; i < noteDataList.Count; i++) {
				index.Add(i);
			}

			// Here we get a random direction and hope it's the right one, should be fine in 99% of case
			var cutDirection = 8;
			foreach(var noteData in noteDataList) { 
				if(noteData.cutDirection != NoteCutDirection.Any) {
					cutDirection = (int)noteData.cutDirection;
					break;
				}
			}

			for(var j = 0; j < noteDataList.Count - 1; j++) {
				for(var i = 0; i < noteDataList.Count - 1; i++) {
					var note = noteDataList[i];
					var next = noteDataList[i + 1];
					var found = false;

					if(note.cutDirection == NoteCutDirection.Any && next.cutDirection == NoteCutDirection.Any && cutDirection != 8) {
						if(next.lineIndex > note.lineIndex && (cutDirection == 3 || cutDirection == 5 || cutDirection == 7)) {
							found = true;
						}
						if(next.lineIndex < note.lineIndex && (cutDirection == 2 || cutDirection == 4 || cutDirection == 6)) {
							found = true;
						}
						if(next.noteLineLayer > note.noteLineLayer && (cutDirection == 0 || cutDirection == 4 || cutDirection == 5)) {
							found = true;
						}
						if(next.noteLineLayer < note.noteLineLayer && (cutDirection == 1 || cutDirection == 6 || cutDirection == 7)) {
							found = true;
						}
					} else if(note.cutDirection == NoteCutDirection.Down || note.cutDirection == NoteCutDirection.DownLeft || note.cutDirection == NoteCutDirection.DownRight) {
						if(next.cutDirection == NoteCutDirection.Any || next.noteLineLayer < note.noteLineLayer) {
							found = true;
						}
					}
					if(note.cutDirection == NoteCutDirection.Up || note.cutDirection == NoteCutDirection.UpLeft || note.cutDirection == NoteCutDirection.UpRight) {
						if(next.cutDirection == NoteCutDirection.Any || next.noteLineLayer > note.noteLineLayer) {
							found = true;
						}
					}
					if(note.cutDirection == NoteCutDirection.Left || note.cutDirection == NoteCutDirection.UpLeft || note.cutDirection == NoteCutDirection.DownLeft) {
						if(next.cutDirection == NoteCutDirection.Any || next.lineIndex < note.lineIndex) {
							found = true;
						}
					}
					if(note.cutDirection == NoteCutDirection.Right || note.cutDirection == NoteCutDirection.UpRight || note.cutDirection == NoteCutDirection.DownRight) {
						if(next.cutDirection == NoteCutDirection.Any || next.lineIndex > note.lineIndex) {
							found = true;
						}
					}

					if(found) {
						(noteDataList[i + 1], noteDataList[i]) = (noteDataList[i], noteDataList[i + 1]);
						(index[i + 1], index[i]) = (index[i], index[i + 1]);
					}
				}
			}

			return (index[index.Count - 1], index[0]);
		}

		public static float SecToBeat(float sec, float bpm) {
			// Convert to mili
			var ms = sec * 1000;
			var per = 60000 / bpm;
			var beat = ms / per;
			return beat;
		}

		public static Color Rainbow() {
			Color c = new Color(1, 0, 1, 1);
			switch(Plugin.RainbowPls) {
				case 0: 
					c = new Color(0, 0, 0, 1); // White
					break;
				case 1:
					c = new Color(1, 0, 0, 1); // Red
					break;
				case 2:
					c = new Color(0, 1, 0, 1); // Green
					break;
				case 3:
					c = new Color(0, 0, 1, 1); // Blue
					break;
				case 4:
					c = new Color(1, 1, 0, 1); // Yellow
					break;
				case 5:
					c = new Color(1, 0, 1, 1); // Purple
					break;
				case 6:
					c = new Color(0, 1, 1, 1); // Cyan
					break;
				case 7:
					c = new Color(1, 1, 1, 1); // Black
					break;
			}

			Plugin.RainbowPls++;

			if(Plugin.RainbowPls == 8) {
				Plugin.RainbowPls = 0;
			}

			return c;
		}
	}
}
