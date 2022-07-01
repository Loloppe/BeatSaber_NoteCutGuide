using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace NoteCutGuide.HarmonyPatches {

	[HarmonyPatch(typeof(BeatmapObjectsInstaller), "InstallBindings")]
	static class NotePrefabPatch {
		static bool isInitialized = false;

		[HarmonyPriority(int.MinValue)]
		static void Prefix(GameNoteController ____normalBasicNotePrefab) {
			if(isInitialized)
				return;

			isInitialized = true;

			var guide = GameObject.CreatePrimitive(PrimitiveType.Cube);

			GameObject.Destroy(guide.GetComponent<BoxCollider>());

			guide.name = "NoteCutGuide";


			var renderer = guide.GetComponent<MeshRenderer>();

			renderer.material = ____normalBasicNotePrefab.GetComponentInChildren<MeshRenderer>().material;

			var t = guide.AddComponent<RectTransform>();


			t.parent = ____normalBasicNotePrefab.GetComponentInChildren<CutoutEffect>().transform;


			t.localScale = new Vector3(0.05f, 0.5f, 0.4f);
			t.anchoredPosition3D = new Vector3(0, 0.3f, 0);
		}
	}
}
