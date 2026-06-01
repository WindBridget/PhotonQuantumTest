using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starter.Shooter
{
	/// <summary>
	/// Manages the in-game HUD elements including health indicators, hit feedback, kill counter and best player display.
	/// Updates UI state based on the local player's health, kills and game state.
	/// </summary>
	public class UIShooter : QuantumSceneViewComponent<SceneContext>
	{
		[Header("References")]
		public CanvasGroup CanvasGroup;
		public TextMeshProUGUI ChickenCount;
		public TextMeshProUGUI BestHunter;
		[Tooltip("UI elements shown when player is alive")]
		public GameObject AliveGroup;
		[Tooltip("UI elements shown when player is dead")]
		public GameObject DeathGroup;
		[Tooltip("Array of health indicator images")]
		public Image[] HealthIndicators;
		[Tooltip("Flashed when player takes damage")]
		public CanvasGroup HitIndicator;

		[Header("UI Sound Setup")]
		public AudioSource AudioSource;
		public AudioClip ChickenKillClip;
		public AudioClip HitReceivedClip;
		public AudioClip DeathClip;

		private int _lastChickens = -1;
		private int _lastHealth = -1;
		private PlayerRef _bestHunter;

		public override void OnEnable()
		{
			base.OnEnable();

			BestHunter.gameObject.SetActive(false);
		}

		public override void OnUpdateView()
		{
			// Gradually fade out the hit indicator effect
			HitIndicator.alpha = Mathf.Lerp(HitIndicator.alpha, 0f, Time.deltaTime * 2f);

			var frame = PredictedFrame;
			if (frame == null || frame.Exists(ViewContext.LocalPlayerEntity) == false)
			{
				CanvasGroup.alpha = 0f;
				return;
			}

			CanvasGroup.alpha = 1f;

			// Update best hunter display when it changes
			var gameplay = frame.GetSingleton<ShooterGameplay>();
			if (_bestHunter != gameplay.BestHunter)
			{
				_bestHunter = gameplay.BestHunter;

				var hunterData = frame.GetPlayerData(_bestHunter);
				BestHunter.text = hunterData != null ? hunterData.PlayerNickname : string.Empty;
				BestHunter.gameObject.SetActive(hunterData != null);
			}

			var health = frame.Get<Health>(ViewContext.LocalPlayerEntity);
			var player = frame.Get<ShooterPlayer>(ViewContext.LocalPlayerEntity);

			// Update health display and play damage feedback when health changes
			if (_lastHealth != health.CurrentHealth)
			{
				bool isAlive = health.IsAlive;

				if (_lastHealth > health.CurrentHealth)
				{
					HitIndicator.alpha = 1f;
					var clip = isAlive ? HitReceivedClip : DeathClip;
					AudioSource.PlayOneShot(clip);
				}

				_lastHealth = health.CurrentHealth;

				// Toggle appropriate UI groups based on alive state
				AliveGroup.SetActive(isAlive);
				DeathGroup.SetActive(isAlive == false);

				// Update health indicators
				for (int i = 0; i < HealthIndicators.Length; i++)
				{
					HealthIndicators[i].enabled = _lastHealth > i;
				}
			}

			// Update kill counter and play feedback sound when kills increase
			if (_lastChickens != player.ChickenKills)
			{
				if (player.ChickenKills > _lastChickens && player.ChickenKills > 0)
				{
					AudioSource.PlayOneShot(ChickenKillClip);
				}

				_lastChickens = player.ChickenKills;
				ChickenCount.text = $"\u00d7{_lastChickens}";
			}
		}
	}
}
