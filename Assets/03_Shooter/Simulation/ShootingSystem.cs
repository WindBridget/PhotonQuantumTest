using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum.Shooter
{
	/// <summary>
	/// Handles player shooting mechanics, including hit detection and damage application.
	/// Works in conjunction with player input and physics systems.
	/// </summary>
	[Preserve]
	public unsafe class ShootingSystem : SystemMainThreadFilter<ShootingSystem.Filter>
	{
		public struct Filter
		{
			public EntityRef          Entity;
			public PlayerLink*        PlayerLink;
			public Shooting*          Shooting;
			public Health*            Health;
			public KCC*               KCC;
			public PhysicsCollider3D* Collider;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Health->IsAlive == false)
				return;

			var input = frame.GetPlayerInput(filter.PlayerLink->PlayerRef);
			if (input->Fire.WasPressed)
			{
				// Temporarily disable shooter's collider to prevent self-hits
				filter.Collider->Enabled = false;

				Fire(frame, ref filter);

				filter.Collider->Enabled = true;
			}
		}

		private void Fire(Frame frame, ref Filter filter)
		{
			var fromPosition = GetFirePosition(ref filter);
			var toPosition = fromPosition + filter.KCC->Data.LookDirection * filter.Shooting->MaxDistance;

			// Configure raycast to detect all types of physics objects
			var hitOptions = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo;
			var nullableHit = frame.Physics3D.Linecast(fromPosition, toPosition, filter.Shooting->HitMask, hitOptions);

			if (nullableHit.HasValue == false)
			{
				// No hit - fire event with end position and no hit data
				frame.Events.Fired(filter.Entity, toPosition, default, default);
				return;
			}

			var hit = nullableHit.Value;

			// Check if hit entity has health component and apply damage
			if (frame.Unsafe.TryGetPointer(hit.Entity, out Health* health))
			{
				int damageDone = health->ApplyDamage(frame, hit.Entity, 1);
				if (damageDone > 0)
				{
					if (health->IsAlive == false)
					{
						frame.Signals.EntityKilled(hit.Entity, filter.Entity);
					}
				}
			}

			frame.Events.Fired(filter.Entity, hit.Point, hit.Normal, hit.Entity);
		}

		private FPVector3 GetFirePosition(ref Filter filter)
		{
			// Calculate fire position based on camera setup:
			// 1. Start from KCC position and offset to camera pivot point
			var pivotPosition = filter.KCC->Position + filter.Shooting->CameraPivotOffset;

			// 2. Apply look rotation to handle offset since handle is relative to pivot
			return pivotPosition + filter.KCC->Data.LookRotation * filter.Shooting->CameraHandleOffset;
		}
	}
}
