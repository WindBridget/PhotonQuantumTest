using Quantum;
using UnityEngine;

namespace Starter.Shooter
{
	/// <summary>
	/// Handles the visual representation of an entity's health state, managing alive/dead states
	/// and associated visual effects.
	/// </summary>
	public class HealthView : QuantumEntityViewComponent
	{
		[Tooltip("Root object containing visuals for alive state")]
		public GameObject VisualRoot;
		[Tooltip("Root object containing visuals for death state")]
		public GameObject DeathRoot;
		public ParticleSystem AliveParticles;

		public override void OnUpdateView()
		{
			var health = GetPredictedQuantumComponent<Health>();

			// Toggle visibility of alive/dead visual states
			VisualRoot.SetActive(health.IsAlive);
			DeathRoot.SetActive(health.IsAlive == false);

			// Control particle effects if they exist
			if (AliveParticles != null)
			{
				var emission = AliveParticles.emission;
				emission.enabled = health.IsAlive;
			}
		}
	}
}
