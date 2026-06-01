using Quantum;
using UnityEngine;

namespace Starter.Platformer
{
	/// <summary>
	/// Coin object that can be picked up by player.
	/// </summary>
	public class CoinView : QuantumEntityViewComponent
	{
		[Header("References")]
		public GameObject VisualRoot;
		public ParticleSystem Particles;
		public AudioClip CollectedAudioClip;

		[Header("Setup")]
		public float RotationSpeed = 90f;

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventCoinCollected>(this, OnCoinCollected);

			// Randomize coin rotation on start
			VisualRoot.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
		}

		public override void OnUpdateView()
		{
			var coin = GetPredictedQuantumComponent<Coin>();
			bool isActive = coin.IsActive(PredictedFrame);

			// Show/hide coin visual
			VisualRoot.SetActive(isActive);

			// Start/stop particles emission
			var emission = Particles.emission;
			emission.enabled = isActive;

			if (isActive)
			{
				VisualRoot.transform.Rotate(0f, RotationSpeed * Time.deltaTime, 0f);
			}
		}

		private void OnCoinCollected(EventCoinCollected callback)
		{
			if (callback.Entity != EntityRef)
				return;

			AudioSource.PlayClipAtPoint(CollectedAudioClip, transform.position, 1f);
		}
	}
}
