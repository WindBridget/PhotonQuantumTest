using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum.Platformer
{
	/// <summary>
	/// System that handles falling platform behavior - platforms fall when stepped on by a player
	/// and reset to their original position after a delay.
	/// </summary>
	[Preserve]
	public unsafe class FallingPlatformSystem : SystemMainThreadFilter<FallingPlatformSystem.Filter>, ISignalOnCollisionEnter3D
	{
		public struct Filter
		{
			public EntityRef          Entity;
			public FallingPlatform*   Platform;
			public Transform3D*       Transform;
			public PhysicsBody3D*     PhysicsBody;
			public PhysicsCollider3D* Collider;
		}

		public override void OnInit(Frame frame)
		{
			// Store initial positions of all falling platforms for reset
			foreach (var pair in frame.Unsafe.GetComponentBlockIterator<FallingPlatform>())
			{
				pair.Component->OriginalPosition = frame.Unsafe.GetPointer<Transform3D>(pair.Entity)->Position;
			}
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Platform->FallTimer.HasStoppedThisFrame(frame))
			{
				// Start platform falling by enabling gravity and applying downward impulse
				// We change collision layer instead of disabling collider to allow physics simulation
				filter.Collider->Layer = UnityEngine.LayerMask.NameToLayer("Ignore Raycast");
				filter.PhysicsBody->GravityScale = 1;
				filter.PhysicsBody->AddLinearImpulse(FPVector3.Down * 30);

				filter.Platform->ResetTimer = FrameTimer.FromSeconds(frame, filter.Platform->ResetTime);

				frame.Events.PlatformFell(filter.Entity);
			}
			else if (filter.Platform->ResetTimer.HasStoppedThisFrame(frame))
			{
				// Reset platform to original state
				filter.Collider->Layer = 0;
				filter.PhysicsBody->GravityScale = 0;
				filter.PhysicsBody->Velocity = default;
				filter.Transform->Position = filter.Platform->OriginalPosition;

				// Reset timers
				filter.Platform->FallTimer = default;
				filter.Platform->ResetTimer = default;
			}
		}

		/// <summary>
		/// Detect collision with player and start fall timer.
		/// This works because platform is a dynamic (= non-kinematic) physics body and player has standard collider
		/// that is slightly larger than collision shape defined in KCC settings.
		/// </summary>
		void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
		{
			if (frame.Has<PlatformerPlayer>(info.Other) == false)
				return;

			if (frame.Unsafe.TryGetPointer(info.Entity, out FallingPlatform* fallingPlatform) == false)
				return;

			if (fallingPlatform->FallTimer.IsSet)
				return; // Platform fall already scheduled

			fallingPlatform->FallTimer = FrameTimer.FromSeconds(frame, fallingPlatform->FallDelay);
		}
	}
}
