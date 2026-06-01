using System;
using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum.Shooter
{
	/// <summary>
	/// Manages core gameplay mechanics including player lifecycle (spawning, respawning, death),
	/// scoring system for chicken kills, and player spawn point selection.
	/// </summary>
	[Preserve]
	public unsafe class GameplaySystem : SystemMainThreadFilter<GameplaySystem.Filter>,
		ISignalOnPlayerAdded, ISignalOnPlayerRemoved, ISignalPlayerFell, ISignalEntityKilled, ISignalEntityDied
	{
		public struct Filter
		{
			public EntityRef      Entity;
			public ShooterPlayer* Player;
			public Health*        Health;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Health->IsFinished(frame))
			{
				RespawnPlayer(frame, filter.Entity);
			}
		}

		void ISignalOnPlayerAdded.OnPlayerAdded(Frame frame, PlayerRef playerRef, bool firstTime)
		{
			var runtimePlayer = frame.GetPlayerData(playerRef);
			var playerEntity = frame.Create(runtimePlayer.PlayerAvatar);

			frame.AddOrGet<PlayerLink>(playerEntity, out var playerLink);
			playerLink->PlayerRef = playerRef;

			RespawnPlayer(frame, playerEntity);
		}

		void ISignalOnPlayerRemoved.OnPlayerRemoved(Frame frame, PlayerRef playerRef)
		{
			foreach (var pair in frame.GetComponentIterator<PlayerLink>())
			{
				if (pair.Component.PlayerRef != playerRef)
					continue;

				// Destroy player entity
				frame.Destroy(pair.Entity);
			}
		}

		void ISignalPlayerFell.PlayerFell(Frame frame, EntityRef entity)
		{
			frame.Unsafe.GetPointer<Health>(entity)->ApplyDamage(frame, entity, 1000);
		}

		void ISignalEntityKilled.EntityKilled(Frame frame, EntityRef entity, EntityRef killerEntity)
		{
			if (frame.Unsafe.TryGetPointer(killerEntity, out ShooterPlayer* player) == false)
				return;

			if (frame.Has<Chicken>(entity))
			{
				// Chicken killed, let's add it to the counter
				player->ChickenKills += 1;
			}
			else if (frame.Unsafe.TryGetPointer(entity, out ShooterPlayer* victim))
			{
				// When a player is killed, they lose three chicken kill points
				victim->ChickenKills = Math.Max(victim->ChickenKills - 3, 0);
			}

			CheckBestHunter(frame);
		}

		void ISignalEntityDied.EntityDied(Frame frame, EntityRef entity)
		{
			if (frame.Unsafe.TryGetPointer(entity, out KCC* kcc) == false)
				return;

			// Disable player's movement
			kcc->SetActive(false);
		}

		private void RespawnPlayer(Frame frame, EntityRef entity)
		{
			var spawnData = GetSpawnData(frame);

			// Reset player position, rotation and movement capabilities
			var kcc = frame.Unsafe.GetPointer<KCC>(entity);
			kcc->SetActive(true);
			kcc->Teleport(frame, spawnData.Position);
			kcc->SetLookRotation(spawnData.Rotation);

			frame.Unsafe.GetPointer<Health>(entity)->Revive();

			// Sync player input rotation with player's new look direction
			var playerRef = frame.Unsafe.GetPointer<PlayerLink>(entity)->PlayerRef;
			frame.Events.ResetLookRotation(playerRef, kcc->GetLookRotation());
		}

		private void CheckBestHunter(Frame frame)
		{
			// Find player with highest chicken kill count and update the global best hunter
			EntityRef bestHunter = default;
			int bestHunterKills = 0;

			foreach (var pair in frame.Unsafe.GetComponentBlockIterator<ShooterPlayer>())
			{
				if (pair.Component->ChickenKills > bestHunterKills)
				{
					bestHunter = pair.Entity;
					bestHunterKills = pair.Component->ChickenKills;
				}
			}

			if (bestHunter.IsValid)
			{
				var gameplay = frame.Unsafe.GetPointerSingleton<ShooterGameplay>();
				gameplay->BestHunter = frame.Unsafe.GetPointer<PlayerLink>(bestHunter)->PlayerRef;
			}
		}

		private (FPVector3 Position, FPQuaternion Rotation) GetSpawnData(Frame frame)
		{
			// For simplicity spawn points are scene entities. An elegant approach is to bake spawn data
			// into a custom scene asset. See Quantum SimpleFPS sample for an example implementation.

			int currentSpawnPoint = 0;
			int randomSpawnPoint = frame.RNG->Next(0, frame.ComponentCount<SpawnPoint>());

			foreach (var pair in frame.Unsafe.GetComponentBlockIterator<SpawnPoint>())
			{
				if (currentSpawnPoint == randomSpawnPoint)
				{
					var transform = frame.Unsafe.GetPointer<Transform3D>(pair.Entity);
					// Add random offset within spawn point radius for variation
					var position = transform->Position + frame.RNG->InUnitCircle(true).XOY * pair.Component->Radius;
					return (position, transform->Rotation);
				}

				currentSpawnPoint++;
			}

			return (FPVector3.Zero, FPQuaternion.Identity);
		}
	}
}
