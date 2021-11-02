using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using ThunderRoad;
using UnityEngine;

namespace BendTime {
	public class BendTime : LevelModule {
		public bool DebugMode = true;
		public float FocusConsumption = 2f;

		/// <summary>
		/// Starts freeze controller
		/// </summary>

		private Harmony harmony;
		public override IEnumerator OnLoadCoroutine() {
			try {
				harmony = new Harmony("BendTime");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				
				if(DebugMode)
					Debug.Log($"Focus consumption set to {FocusConsumption}");
				
				Debug.Log("Bend Time successfully loaded!");
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
			return base.OnLoadCoroutine();
		}

		public override void Update() {
			base.Update();
			if (!TimeController.Instance.IsTimeFrozen) return;
			
			TimeController.Instance.slowTimeEffectInstance?.SetIntensity(Mathf.InverseLerp(Player.local.creature.mana.maxFocus, 0.0f, Player.local.creature.mana.currentFocus));
			if (!Player.local.creature.mana.ConsumeFocus(FocusConsumption * Time.deltaTime))
				TimeController.Instance.UnFreezeTime();
		}
	}
}
