using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using TotT;
using ThunderRoad;
using UnityEngine;
using Object = System.Object;

namespace BendTime {
	public class TimeController {
		public EffectInstance slowTimeEffectInstance = null;
		private EffectData bubbleEffectData;
		private AnimationCurve bubbleScaleCurveOverTime;

		/// <summary>
		/// The instance of this class to be accessed from anywhere
		/// </summary>
		public static TimeController Instance { get; } = new TimeController();

		private TimeController() { }
		
		/// <summary>
		/// Keeps track of whether or not time is frozen
		/// </summary>
		private bool isTimeFrozen;

		/// <summary>
		/// The public version of isTimeFrozen
		/// </summary>
		/// ReSharper disable once MemberCanBePrivate.Global
		public bool IsTimeFrozen
		{
			get => isTimeFrozen;
			set {
				if (value == isTimeFrozen) return;
				
				if (value)
					FreezeTime();
				else
					UnFreezeTime();
			}
		}

		/// <summary>
		/// Freezes time
		/// </summary>
		/// ReSharper disable once MemberCanBePrivate.Global
		public void FreezeTime(bool playEffects = false) {
			if (isTimeFrozen)
				return;
			isTimeFrozen = true;
			
			foreach (var item in Item.all.Where(item => item.itemId != "GrooveSlinger.Dishonored.Bolt" || item.itemId != "GrooveSlinger.Dishonored.SleepDart" || item.itemId != "GrooveSlinger.Dishonored.StingBolt")) {
				FreezeItem(item);
			}

			foreach (var creature in Creature.all) {
				creature.ragdoll.physicTogglePlayerRadius = 1000f;
				creature.ragdoll.physicToggleRagdollRadius = 1000f;
				FreezeCreature(creature);
			}

			foreach (var particle in UnityEngine.Object.FindObjectsOfType<EffectParticle>()) {
				particle.rootParticleSystem.Pause();
			}

			foreach (var particle in UnityEngine.Object.FindObjectsOfType<EffectVfx>()) {
				particle.vfx.pause = true;
			}

			foreach (var particle in UnityEngine.Object.FindObjectsOfType<ParticleSystem>()) {
				particle.Pause();
			}

			foreach (var source in UnityEngine.Object.FindObjectsOfType<AudioSource>()) {
				if(!source.gameObject.GetComponent<Speaker>())
					source.Pause();
			}
			
			CameraEffects.SetSepia(1.0f);
			GameManager.local.StartCoroutine(BubbleCoroutine());

			if (!playEffects) return;
			SlowMotionAudio(true);
		}

		/// <summary>
		/// Unfreezes time
		/// </summary>
		/// ReSharper disable once MemberCanBePrivate.Global
		public void UnFreezeTime(bool playEffects = false) {
			if (!isTimeFrozen)
				return;
			isTimeFrozen = false;

			foreach (var item in Item.all)
			{
				UnFreezeItem(item);
			}

			foreach (var creature in Creature.all)
			{
				creature.ragdoll.physicTogglePlayerRadius = 5f;
				creature.ragdoll.physicToggleRagdollRadius = 3f;
				UnFreezeCreature(creature);
			}
			
			foreach (var particle in UnityEngine.Object.FindObjectsOfType<EffectParticle>()) {
				particle.rootParticleSystem.Play();
			}

			foreach (var particle in UnityEngine.Object.FindObjectsOfType<EffectVfx>()) {
				particle.vfx.pause = false;
			}

			foreach (var particle in UnityEngine.Object.FindObjectsOfType<ParticleSystem>()) {
				particle.Play();
			}

			foreach (var source in UnityEngine.Object.FindObjectsOfType<AudioSource>()) {
				if(!source.gameObject.GetComponent<Speaker>())
					source.UnPause();
			}
			
			CameraEffects.SetSepia(0.0f);
			
			if(playEffects)
				SlowMotionAudio(false);
		}

		/// <summary>
		/// Freezes either an item or a creature
		/// </summary>
		/// <param name="gameObject">The item or creatures game object</param>
		internal void FreezeGameObject(GameObject gameObject)
		{
			var interactive = gameObject.GetComponent<Item>();
			if (interactive != null)
				FreezeItem(interactive);
			
			var creature = gameObject.GetComponent<Creature>();
			if (creature != null)
				FreezeCreature(creature);
		}

		/// <summary>
		/// Freezes a creture
		/// </summary>
		/// <param name="creature">The creature to freeze</param>
		/// ReSharper disable once MemberCanBePrivate.Global
		public static void FreezeCreature(Creature creature) {
			if (creature == Player.currentCreature) return;
			
			creature.brain.instance?.Stop();
			if (creature.animator != null) {
				creature.animator.enabled = true;
				creature.animator.speed = 0;
			}

			if (creature.locomotion != null) {
				creature.locomotion.MoveStop();
				creature.locomotion.allowTurn = false;
			}

			if (creature.brain.navMeshAgent != null)
				creature.brain.navMeshAgent.isStopped = true;

			if (creature.ragdoll.isGrabbed) return;
			
			foreach (var ragdollPart in creature.ragdoll.parts)
				TimeController.Instance.FreezeRigidbody(ragdollPart.rb);
		}

		/// <summary>
		/// Un-freezes a creature
		/// </summary>
		/// <param name="creature">The creature to unfreeze</param>
		/// ReSharper disable once MemberCanBePrivate.Global
		public void UnFreezeCreature(Creature creature) {
			if (creature == Player.currentCreature) return;
			
			creature.brain.instance?.Start();
			
			if (creature.brain.navMeshAgent != null)
				creature.brain.navMeshAgent.enabled = true;
			
			if (creature.animator != null)
				creature.animator.speed = 1f;
			
			if (creature.locomotion != null) 
				creature.locomotion.allowTurn = true;
			
			foreach (var ragdollPart in creature.ragdoll.parts) {
				UnFreezeRigidbody(ragdollPart.rb);
			}
		}

		/// <summary>
		/// Freezes a rigidbody
		/// </summary>
		/// <param name="rigidbody">The rigidbody to freeze</param>
		/// ReSharper disable once MemberCanBePrivate.Global
		/// ReSharper disable once MemberCanBeMadeStatic.Global
		public void FreezeRigidbody(Rigidbody rigidbody)
		{
			var data = rigidbody.gameObject.GetComponent<StoredPhysicsData>();
			if (data == null)
			{
				data = rigidbody.gameObject.AddComponent<StoredPhysicsData>();
			}
			if (data != null)
			{
				data.StoreDataFromRigidBody(rigidbody);
			}
			rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			rigidbody.useGravity = false;
		}

		/// <summary>
		/// Un-freezes a rigidbody
		/// </summary>
		/// <param name="rigidbody">The rigidbody to unfreeze</param>
		/// ReSharper disable once MemberCanBePrivate.Global
		/// ReSharper disable once MemberCanBeMadeStatic.Local
		public void UnFreezeRigidbody(Rigidbody rigidbody)
		{
			if (rigidbody.constraints == RigidbodyConstraints.None) return;

			rigidbody.constraints = RigidbodyConstraints.None;
			rigidbody.useGravity = true;
			rigidbody.ResetInertiaTensor();
			var data = rigidbody.gameObject.GetComponent<StoredPhysicsData>();
			if (data == null)
				data = rigidbody.gameObject.AddComponent<StoredPhysicsData>();
			if (data != null)
				data.SetRigidbodyFromStoredData(rigidbody);
		}

		/// <summary>
		/// Freezes an item
		/// </summary>
		/// <param name="item">The item to freeze</param>
		public void FreezeItem(Item item) {
			// Don't freeze if item is part of the body, being tk'd, or already frozen.
			if (item.data.type == ItemData.Type.Body || item.isTelekinesisGrabbed || item.rb.constraints == RigidbodyConstraints.FreezeAll) return;

			// Don't freeze things held by the player
			if ((Player.local.handLeft.ragdollHand.grabbedHandle &&
					Player.local.handLeft.ragdollHand.grabbedHandle.item == item)
				|| (Player.local.handRight.ragdollHand.grabbedHandle &&
					Player.local.handRight.ragdollHand.grabbedHandle.item == item)) {
				return;
			}

			FreezeRigidbody(item.rb);
		}

		/// <summary>
		/// Unfreezes an item
		/// </summary>
		/// <param name="item">The item to unfreeze</param>
		public void UnFreezeItem(Item item) {
			var delay = item.gameObject.GetComponent<DelayFreeze>();
			if (delay) {
				GameObject.Destroy(delay);
			}

			UnFreezeRigidbody(item.rb);
			// Let all (moving) weapons do damage on time resuming
			if (item.rb.velocity.sqrMagnitude > 1 && item.handlers.Count == 0 && !item.isTelekinesisGrabbed) {
				item.RefreshCollision(true);
			}
		}
		
		public IEnumerator BubbleCoroutine() {
			bubbleScaleCurveOverTime = Catalog.GetData<SpellMergeGravity>("GravityMerge").bubbleScaleCurveOverTime;
			var bubbleEffect = (EffectInstance) null;
			bubbleEffectData = Catalog.GetData<EffectData>("BendTimeBubble");
			if (bubbleEffectData != null) {
				bubbleEffect = bubbleEffectData.Spawn(Player.local.creature.ragdoll.headPart.transform.position, Quaternion.identity, null, null, true, Array.Empty<Type>());
				bubbleEffect.SetIntensity(0.0f);
				bubbleEffect.Play();
			}
			yield return new WaitForFixedUpdate();
			var startTime = Time.time;
			while (IsTimeFrozen) {
				bubbleEffect?.SetIntensity(1000f);
				yield return null;
			}
			bubbleEffect?.End();
		}

		/*
		[HarmonyPatch(typeof(SpellPowerSlowTime))]
		[HarmonyPatch("Use")]
		internal static class TimeFreezePatch {
			[HarmonyPrefix]
			internal static bool Prefix(SpellCaster __instance) {
				if (TimeController.Instance.IsTimeFrozen) {
					TimeController.Instance.UnFreezeTime();
				}
				else {
					TimeController.Instance.FreezeTime();
				}

				return false;
			}
		}
		*/
		
		/*
		[HarmonyPatch(typeof(GameManager))]
		[HarmonyPatch("StopSlowMotion")]
		internal static class StopFreezePatch {
			[HarmonyPostfix]
			internal static void Postfix(GameManager __instance) {
				TimeController.Instance.UnFreezeTime();
				TimeController.Instance.SlowMotionAudio(false);
			}
		}
		*/

		/// <summary>
		/// Manages the audio for starting/stopping
		/// </summary>
		/// <param name="starting">True to start audio, false to end it</param>
		private void SlowMotionAudio(bool starting) {
			if (starting) {
				GameManager.audioMixerSnapshotSlowmo.TransitionTo(Catalog.GetData<SpellPowerSlowTime>("SlowTime")
					.enterCurve.GetLastTime());

				slowTimeEffectInstance = Catalog.GetData<EffectData>("SpellSlowTime")
					.Spawn(Player.local.creature.transform, true);
				slowTimeEffectInstance?.Play();
			}
			else {
				GameManager.audioMixerSnapshotDefault.TransitionTo(Catalog.GetData<SpellPowerSlowTime>("SlowTime")
					.exitCurve.GetLastTime());
				slowTimeEffectInstance?.End();
			}
		}


		/// <summary>
		/// Freeze creature when spawned
		/// </summary>
		[HarmonyPatch(typeof(Creature))]
		[HarmonyPatch("Awake")]
		private static class NewCreatureDataPatch {
			[HarmonyPostfix]
			private static void Postfix(Creature __instance) {
				if (TimeController.Instance.IsTimeFrozen) {
					__instance.gameObject.AddComponent<DelayFreeze>(); // delay the freeze cause the creature has to init
				}

				__instance?.ragdoll?.GetBone(__instance.jaw)?.mesh?.gameObject?.AddComponent<Speaker>();
			}
		}

		/// <summary>
		/// Grabbing creatures unfreezes them
		/// </summary>
		[HarmonyPatch(typeof(HandleRagdoll))]
		[HarmonyPatch("OnGrab")]
		private static class GrabbedRagdollUnFreezePatch {
			[HarmonyPostfix]
			private static void Postfix(HandleRagdoll __instance, RagdollHand ragdollHand, float axisPosition, HandleOrientation orientation, bool teleportToHand = false) {
				if (TimeController.Instance.IsTimeFrozen) {
					Instance.UnFreezeCreature(__instance.ragdollPart.ragdoll.creature);
				}
			}
		}

		/// <summary>
		/// Letting go of creatures freezes them
		/// </summary>
		[HarmonyPatch(typeof(HandleRagdoll))]
		[HarmonyPatch("OnUnGrab")]
		private static class UnGrabbedRagdollFreezePatch {
			[HarmonyPostfix]
			private static void Postfix(HandleRagdoll __instance, RagdollHand ragdollHand, bool throwing) {
				if (TimeController.Instance.IsTimeFrozen &&
					!__instance.ragdollPart.ragdoll.isGrabbed && !__instance.ragdollPart.ragdoll.isTkGrabbed) {
					TimeController.FreezeCreature(__instance.ragdollPart.ragdoll.creature);
				}
			}
		}

		/// <summary>
		/// Disabled Update method of BrainData
		/// </summary>
		[HarmonyPatch(typeof(BrainData))]
		[HarmonyPatch("Update")]
		private static class CreatureBrainFreezePatch {
			[HarmonyPrefix]
			private static bool Prefix(BrainData __instance) {
				if (__instance != null) {
					return !TimeController.Instance.IsTimeFrozen;
				}

				return true;
			}
		}

		/// <summary>
		/// Stop talking when time freezes/is frozen
		/// </summary>
		[HarmonyPatch(typeof(BrainModuleSpeak))]
		[HarmonyPatch("LateUpdate")]
		private static class CreatureVoiceFreezePatch {
			[HarmonyPrefix]
			private static bool Prefix(BrainModuleSpeak __instance, AudioSource ___audioSource) {
				if (!Instance.IsTimeFrozen) {
					___audioSource.UnPause();
					return true;
				}
				___audioSource.Pause();
				return false;
			}
		}
		
		/// <summary>
		/// Prevent error spam when despawning
		/// </summary>
		[HarmonyPatch(typeof(Creature))]
		[HarmonyPatch("Despawn")]
		[HarmonyPatch(new System.Type[] { })]
		private static class CreatureDespawnUnfreezePatch
		{
			[HarmonyPrefix]
			private static bool Prefix(Creature __instance)
			{
				if (Instance.IsTimeFrozen)
				{
					Instance.UnFreezeCreature(__instance);
				}
				return true;
			}
		}   
		
		/// <summary>
		/// Add force build up on creature during frozen time
		/// </summary>
		[HarmonyPatch(typeof(Damager))]
		[HarmonyPatch("AddForceCoroutine")]
		internal static class UnfreezeOnDamagePatch
		{
			[HarmonyPrefix]
			internal static bool Prefix(Damager __instance, Collider targetCollider, Vector3 impactVelocity, Vector3 contactPoint, Ragdoll ragdoll)
			{
				if (!TimeController.Instance.IsTimeFrozen) return true;
				if (ragdoll == null) return true;
				
				var localContactPoint = targetCollider.attachedRigidbody.transform.InverseTransformPoint(contactPoint);
				var velocity = __instance.data.addForceNormalize ? impactVelocity.normalized : impactVelocity;
				using (var enumerator = ragdoll.parts.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						var ragdollPart = enumerator.Current;
						if (!(ragdollPart is null) && ragdollPart.HasCollider(targetCollider) && __instance.data.addForceRagdollPartMultiplier > 0f)
						{
							var force = velocity * __instance.data.addForce * __instance.data.addForceRagdollPartMultiplier * Time.fixedDeltaTime;
							var store = ragdollPart.rb.gameObject.GetComponent<StoredPhysicsData>();
							if (store)
							{
								store.velocity += force / ragdollPart.rb.mass;
							}
						}
						else if (__instance.data.addForceRagdollOtherMultiplier > 0f)
						{
							var force = velocity * __instance.data.addForce * __instance.data.addForceRagdollOtherMultiplier * Time.fixedDeltaTime;
							if (ragdollPart is null) continue;
							var store = ragdollPart.rb.gameObject.GetComponent<StoredPhysicsData>();
							if (store)
							{
								store.velocity += force / ragdollPart.rb.mass;
							}
						}
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Unfreeze an item when it's grabbed
		/// </summary>
		[HarmonyPatch(typeof(Item))]
		[HarmonyPatch("OnGrab")]
		internal static class UnFreezeGrabbedInteractiveObjectPatch {
			[HarmonyPostfix]
			internal static void Postfix(Item __instance, Handle handle, RagdollHand ragdollHand) {
				if (ragdollHand != Player.local.handLeft.ragdollHand &&
					ragdollHand != Player.local.handRight.ragdollHand) return;
				
				Instance.UnFreezeItem(__instance);

				// Enable yoinking
				var toDrop = __instance.handlers.Where(handler => handler != Player.local.handLeft.ragdollHand && handler != Player.local.handRight.ragdollHand).ToList();

				foreach (var handler in toDrop) {
					handler.UnGrab(false);
				}
			}
		}

		/// <summary>
		/// Unfreeze an item when tk'd
		/// </summary>
		[HarmonyPatch(typeof(Item))]
		[HarmonyPatch("OnTelekinesisGrab")]
		internal static class UnFreezeTeleGrabbedInteractiveObjectPatch {
			[HarmonyPostfix]
			internal static void Postfix(Item __instance, Handle handle, SpellTelekinesis teleGrabber) {
				Instance.UnFreezeItem(__instance);
			}
		}

		/// <summary>
		/// Freeze when released by tk
		/// </summary>
		[HarmonyPatch(typeof(Item))]
		[HarmonyPatch("OnTelekinesisRelease")]
		internal static class FreezeTeleUnGrabbedInteractiveObjectPatch {
			[HarmonyPostfix]
			internal static void Postfix(Item __instance, Handle handle, SpellTelekinesis teleGrabber) {
				if (Instance.IsTimeFrozen && handle.handlers.Count == 0) {
					Instance.FreezeItem(__instance);
				}
			}
		}

		/// <summary>
		/// Freeze item when you let go of it
		/// </summary>
		[HarmonyPatch(typeof(Item))]
		[HarmonyPatch("OnUnGrab")]
		internal static class FreezeUnGrabbedInteractiveObjectPatch {
			[HarmonyPostfix]
			internal static void Postfix(Item __instance, Handle handle, RagdollHand ragdollHand, bool throwing) {
				if (Instance.IsTimeFrozen) {
					Instance.FreezeItem(__instance);
				}
			}
		}

		/// <summary>
		/// i forgot why i did this
		/// </summary>
		[HarmonyPatch(typeof(Item))]
		[HarmonyPatch("Update")]
		internal static class PenetrationFollowFreezePatch {
			[HarmonyPostfix]
			internal static void Postfix(Item __instance) {
				if (!Instance.IsTimeFrozen) return;
				
				if ((from collisionHandler in __instance.collisionHandlers from collision in collisionHandler.collisions where collision.damageStruct.penetrationJoint select collision).Any(collision => collision.damageStruct.penetrationRb.constraints != RigidbodyConstraints.FreezeAll)) {
					Instance.UnFreezeItem(__instance);
					return;
				}

				if (__instance.handlers.Count == 0 && __instance.gameObject.GetComponent<DelayFreeze>() == null) {
					if (__instance.itemId == "GrooveSlinger.Dishonored.Bolt" || __instance.itemId == "GrooveSlinger.Dishonored.SleepDart" ||
						__instance.itemId == "GrooveSlinger.Dishonored.StingBolt")
						Instance.FreezeItem(__instance);
				}

				if (__instance.handlers.Count > 0) {
					Instance.UnFreezeItem(__instance);
				}
			}
		}

		/// <summary>
		/// Manage the arrow of a bow
		/// </summary>
		[HarmonyPatch(typeof(BowString))]
		[HarmonyPatch("FixedUpdate")]
		internal static class UnFreezeBowInUsePatch {
			[HarmonyPostfix]
			internal static void Postfix(BowString __instance, Handle ___stringHandle) {
				if (!Instance.IsTimeFrozen) return;
				
				if (___stringHandle.item.rb.constraints == RigidbodyConstraints.FreezeAll) {
					if (__instance.restedArrow) {
						Instance.FreezeItem(__instance.restedArrow);
					}

					if (__instance.nockedArrow) {
						Instance.FreezeItem(__instance.nockedArrow);
					}
				}
				else {
					if (__instance.restedArrow) {
						Instance.UnFreezeItem(__instance.restedArrow);
					}

					if (__instance.nockedArrow) {
						Instance.UnFreezeItem(__instance.nockedArrow);
					}
				}
			}
		}

		/// <summary>
		/// Unfreeze the rested arrow when shot
		/// </summary>
		[HarmonyPatch(typeof(BowString))]
		[HarmonyPatch("OnShootUnrest")]
		internal static class UnFreezeShotRestArrowPatch {
			[HarmonyPrefix]
			internal static void Prefix(BowString __instance) {
				if (!Instance.IsTimeFrozen) return;
				
				if (__instance.restedArrow) {
					Instance.UnFreezeItem(__instance.restedArrow);
				}

				if (__instance.nockedArrow) {
					Instance.UnFreezeItem(__instance.nockedArrow);
				}
			}
		}

		/// <summary>
		/// Unfreeze the nocked arrow when shot
		/// </summary>
		[HarmonyPatch(typeof(BowString))]
		[HarmonyPatch("OnShootUnnock")]
		internal static class UnFreezeShotNockArrowPatch {
			[HarmonyPrefix]
			internal static void Prefix(BowString __instance) {
				if (!Instance.IsTimeFrozen) return;
				
				if (__instance.restedArrow) {
					Instance.UnFreezeItem(__instance.restedArrow);
				}

				if (__instance.nockedArrow) {
					Instance.UnFreezeItem(__instance.nockedArrow);
				}
			}
		}

		/// <summary>
		/// Delay the freeze of the arrow that way it actually gets shot
		/// </summary>
		[HarmonyPatch(typeof(BowString))]
		[HarmonyPatch("Unrest")]
		internal static class UnFreezeUnrestArrowPatch {
			[HarmonyPrefix]
			internal static void Prefix(BowString __instance) {
				if (!Instance.IsTimeFrozen) return;
				if (!__instance.restedArrow) return;
				
				Instance.UnFreezeItem(__instance.restedArrow);
				__instance.restedArrow.gameObject.AddComponent<DelayFreeze>();
			}
		}

		/// <summary>
		/// Unfreeze nocked arrow
		/// </summary>
		[HarmonyPatch(typeof(BowString))]
		[HarmonyPatch("Unnock")]
		internal static class UnFreezeUnnockArrowPatch {
			[HarmonyPrefix]
			internal static void Prefix(BowString __instance) {
				if (!Instance.IsTimeFrozen) return;
				if (!__instance.nockedArrow) return;
				
				Instance.UnFreezeItem(__instance.nockedArrow);
				__instance.nockedArrow.gameObject.AddComponent<DelayFreeze>();
			}
		}

		[HarmonyPatch(typeof(CrossbowModule))]
		[HarmonyPatch("ShootBolt")]
		internal static class FreezeCrossbowBoltPatch {
			[HarmonyPrefix]
			internal static void Prefix(CrossbowModule __instance, Item ___bolt) {
				if (!Instance.IsTimeFrozen) return;
				if (!___bolt) return;

				___bolt.gameObject.AddComponent<DelayFreeze>();
			}
		}
	}
}
