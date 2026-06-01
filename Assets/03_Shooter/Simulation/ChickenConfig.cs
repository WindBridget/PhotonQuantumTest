using Photon.Deterministic;
using UnityEngine;

namespace Quantum.Shooter
{
	/// <summary>
	/// Configuration asset for chicken entities in the game, defining spawn parameters and movement behavior.
	/// Used to control the spawning and behavior of chickens that players hunt.
	/// </summary>
	public class ChickenConfig : AssetObject
	{
		[Tooltip("Reference to the chicken entity prototype to spawn")]
		public AssetRef<EntityPrototype> ChickenPrototype;

		[Tooltip("Maximum number of chickens that can exist in the game")]
		public int TotalCount = 20;
		[Tooltip("Radius from center point where chickens can spawn")]
		public FP SpawnRadius = 70;
		[Tooltip("Minimum height for chicken spawning")]
		public FP SpawnHeightMin = -20;
		[Tooltip("Maximum height for chicken spawning")]
		public FP SpawnHeightMax = 20;
		[Tooltip("Minimum movement speed for chickens")]
		public FP SpeedMin = 2;
		[Tooltip("Maximum movement speed for chickens")]
		public FP SpeedMax = 10;
		[Tooltip("Maximum angle in degrees that chickens can deviate when calculating spawn direction")]
		public FP DirectionDispersion = 30;
	}
}
