using TMPro;
using UnityEngine;
using Quantum;

namespace Starter.Platformer
{
	/// <summary>
	/// Main UI script for Platformer sample.
	/// </summary>
	public class UIPlatformer : QuantumSceneViewComponent<SceneContext>
	{
		[Header("References")]
		public CanvasGroup CanvasGroup;
		public TextMeshProUGUI Instructions;
		public TextMeshProUGUI CoinsCount;
		public TextMeshProUGUI WinnerText;

		// Track previous values to avoid unnecessary UI updates
		private int _lastCoins = -1;
		private PlayerRef _winner;

		public override void OnUpdateView()
		{
			var frame = PredictedFrame;
			if (frame == null || frame.Exists(ViewContext.LocalPlayerEntity) == false)
			{
				// Hide UI if frame is invalid or local player entity doesn't exist
				CanvasGroup.alpha = 0f;
				return;
			}

			var gameplay = frame.GetSingleton<PlatformerGameplay>();

			// Update winner display when game is won
			if (_winner != gameplay.Winner)
			{
				_winner = gameplay.Winner;

				var winnerData = frame.GetPlayerData(_winner);
				WinnerText.text = winnerData != null ? $"We have a winner!\n{winnerData.PlayerNickname}" : string.Empty;
			}

			var player = frame.Get<PlatformerPlayer>(ViewContext.LocalPlayerEntity);

			// Only update coin-related UI if count has changed
			if (_lastCoins == player.CollectedCoins)
				return;

			CanvasGroup.alpha = 1f;
			_lastCoins = player.CollectedCoins;

			// Update coin count and instructions based on progress
			CoinsCount.text = $"\u00d7{_lastCoins}";
			Instructions.text = _lastCoins >= gameplay.MinCoinsToWin ? "Run to the TOP!" : $"Collect {gameplay.MinCoinsToWin} COINS";
		}
	}
}
