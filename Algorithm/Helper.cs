using UnityEngine;

namespace NoteCutGuide.Algorithm {
	internal class Helper {
		public static Color Rainbow() {
			Plugin.RainbowPls += Config.Instance.Speed;

			if(Plugin.RainbowPls > 1f) {
				Plugin.RainbowPls = 0f;
			}

			return Color.HSVToRGB(Plugin.RainbowPls, 1f, 1f);
		}
	}
}
