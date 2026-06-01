using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum.Shooter
{
	/// <summary>
	/// Manages the spawning, movement and lifecycle of chicken entities in the game.
	/// Chickens fly in straight lines from the edge of the map towards the center.
	/// </summary>
	[Preserve]
	public unsafe class ChickenSystem : SystemMainThreadFilter<ChickenSystem.Filter>
	{
		public struct Filter
		{
			public EntityRef    Entity;
			public Transform3D* Transform;
			public Chicken*     Chicken;
			public Health*      Health;
		}

		public override void OnInit(Frame frame)
		{
			var config = frame.FindAsset(frame.RuntimeConfig.ChickenConfig);

			// Spawn initial batch of chickens
			for (int i = 0; i < config.TotalCount; i++)
			{
				var chickenEntity = frame.Create(config.ChickenPrototype);
				RespawnChicken(frame, chickenEntity, config);
			}
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Health->IsAlive)
			{
				if (filter.Chicken->LifetimeTimer.IsRunning(frame))
				{
					// Move chicken forward along its facing direction
					filter.Transform->Position += filter.Transform->Forward * filter.Chicken->Speed * frame.DeltaTime;
				}
				else
				{
					// Chicken has exceeded its lifetime, kill it
					filter.Health->ApplyDamage(frame, filter.Entity, 1000);
				}
			}
			else if (filter.Health->IsFinished(frame))
			{
				// Death animation is finished, let's reset the chicken to a new position
				var config = frame.FindAsset(frame.RuntimeConfig.ChickenConfig);
				RespawnChicken(frame, filter.Entity, config);
			}
		}

		private void RespawnChicken(Frame frame, EntityRef entity, ChickenConfig config)
		{
			var transform = frame.Unsafe.GetPointer<Transform3D>(entity);
			var chicken = frame.Unsafe.GetPointer<Chicken>(entity);

			// Generate random spawn position on circle perimeter
			var circlePosition = frame.RNG->OnUnitCircle() * config.SpawnRadius;
			FP height = frame.RNG->Next(config.SpawnHeightMin, config.SpawnHeightMax);
			var position = new FPVector3(circlePosition.X, height, circlePosition.Y);

			// Calculate rotation to face center with some random deviation
			var rotationToCenter = FPQuaternion.LookRotation(transform->Position - position);
			var dispersionRotation = FPQuaternion.Euler(frame.RNG->InUnitCircle().XYO * config.DirectionDispersion);

			// Move chicken to the calculated start position
			transform->Teleport(frame, position, rotationToCenter * dispersionRotation);

			// Set random speed and calculate lifetime based on map size
			FP speed = frame.RNG->Next(config.SpeedMin, config.SpeedMax);
			FP lifetime = (config.SpawnRadius * 2) / speed;
			chicken->LifetimeTimer = FrameTimer.FromSeconds(frame, lifetime);
			chicken->Speed = speed;

			// Restore chicken health
			frame.Unsafe.GetPointer<Health>(entity)->Revive();
		}
	}
}
