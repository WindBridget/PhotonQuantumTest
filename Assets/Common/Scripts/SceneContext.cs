using Quantum;
using UnityEngine;

namespace Starter
{
	/// <summary>
	/// Stores references to the local player that are shared across view components.
	/// Set by PlayerView on activation and accessed by UI components like UIShooter
	/// to determine which player entity to track.
	/// </summary>
	public class SceneContext : MonoBehaviour, IQuantumViewContext
	{
		public PlayerRef LocalPlayer;
		public EntityRef LocalPlayerEntity;
	}
}
