using UnityEngine.Scripting;

namespace Quantum.ThirdPersonCharacter
{
	/// <summary>
	/// Handles basic spawning and despawning of players.
	/// </summary>
	[Preserve]
	public unsafe class GameplaySystem : SystemSignalsOnly, ISignalOnPlayerAdded, ISignalOnPlayerRemoved
	{
		void ISignalOnPlayerAdded.OnPlayerAdded(Frame frame, PlayerRef playerRef, bool firstTime)
		{
			RespawnPlayer(frame, playerRef);
		}

		void ISignalOnPlayerRemoved.OnPlayerRemoved(Frame frame, PlayerRef playerRef)
		{
			foreach (var pair in frame.GetComponentIterator<PlayerLink>())
			{
				if (pair.Component.PlayerRef != playerRef)
					continue;

				frame.Destroy(pair.Entity);
			}
		}

		/// <summary>
		/// Spawns a player at a random position within a 6x6 square centered at origin
		/// </summary>
		private void RespawnPlayer(Frame frame, PlayerRef playerRef)
		{
			var runtimePlayer = frame.GetPlayerData(playerRef);
			var playerEntity = frame.Create(runtimePlayer.PlayerAvatar);

			// Link the entity to the player reference for tracking
			frame.AddOrGet<PlayerLink>(playerEntity, out var playerLink);
			playerLink->PlayerRef = playerRef;

			// Position the player randomly in the XZ plane
			var transform = frame.Unsafe.GetPointer<Transform3D>(playerEntity);
			transform->Position.X = frame.RNG->Next(-3, 3);
			transform->Position.Z = frame.RNG->Next(-3, 3);
		}
	}
}
