using System;
using ThunderRoad;
using UnityEngine;

namespace BendTime {
	public class SpellBendTime : SpellCastCharge{
		public string startEffectId;
		public string endEffectId;
		public string loopEffectId;
		
		protected EffectData startData;
		protected EffectData endData;
		protected EffectData loopData;
		
		public EffectInstance startInstance;
		public EffectInstance endInstance;

		// ReSharper disable once UnusedMember.Global
		public SpellBendTime Clone()
		{
			return base.MemberwiseClone() as SpellBendTime;
		}
		
		public override void OnCatalogRefresh()
		{
			base.OnCatalogRefresh();
			if (!string.IsNullOrEmpty(startEffectId))
				startData = Catalog.GetData<EffectData>(startEffectId, true);
			if (!string.IsNullOrEmpty(endEffectId))
				endData = Catalog.GetData<EffectData>(endEffectId, true);
			if (!string.IsNullOrEmpty(loopEffectId))
				loopData = Catalog.GetData<EffectData>(loopEffectId, true);
		}

		public override void Load(SpellCaster spellCaster, Level level) {
			base.Load(spellCaster, level);
			Debug.Log("Loaded bend time spell");
		}

		public override void Unload() {
			base.Unload();
			TimeController.Instance.loopInstance?.End();
			TimeController.Instance.loopInstance?.Despawn();
			endInstance?.Despawn();
			startInstance?.Despawn();
			
			if(TimeController.Instance.IsTimeFrozen)
				TimeController.Instance.UnFreezeTime();
			
			Debug.Log("Unloaded bend time spell");
		}

		public override void Fire(bool active) {
			base.Fire(active);
			if (!active) return;
			
			if (TimeController.Instance.IsTimeFrozen) {
				TimeController.Instance?.UnFreezeTime();
				TimeController.Instance.loopInstance?.End();
				TimeController.Instance.loopInstance?.Despawn();
				endInstance = endData?.Spawn(spellCaster.magic, false, Array.Empty<Type>());
				endInstance?.Play();
			} else {
				TimeController.Instance?.FreezeTime();
				startInstance = startData?.Spawn(spellCaster.magic, false, Array.Empty<Type>());
				startInstance?.Play();
				TimeController.Instance.loopInstance = loopData?.Spawn(spellCaster.magic, false, Array.Empty<Type>());
				TimeController.Instance.loopInstance?.Play();
			}
		}
	}
	
	public static class Helpers
	{
		public static bool Toggle(this bool value)
		{
			return !value;
		}
	}
}