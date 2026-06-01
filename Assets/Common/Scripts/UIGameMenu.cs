using System;
using System.Threading;
using System.Threading.Tasks;
using Photon.Client;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Input = UnityEngine.Input;

namespace Starter
{
	/// <summary>
	/// Manages the in-game menu UI, handling multiplayer networking, player connections,
	/// cursor locking, and game session management. Provides functionality for joining/leaving
	/// game rooms and toggling menu visibility.
	/// </summary>
	public class UIGameMenu : MonoBehaviour
	{
		[Header("Start Game Setup")]
		[Tooltip("Specifies which game mode player should join - e.g. Platformer, ThirdPersonCharacter")]
		public string GameModeIdentifier;
		public RuntimeConfig RuntimeConfig;
		public RuntimePlayer RuntimePlayer;

		[Header("Debug")]
		[Tooltip("For debug purposes it is possible to force single-player game (starts faster)")]
		public bool ForceLocalMode;

		[Header("UI Setup")]
		public CanvasGroup PanelGroup;
		public TMP_InputField RoomText;
		public TMP_InputField NicknameText;
		public TextMeshProUGUI StatusText;
		public GameObject StartGroup;
		public GameObject DisconnectGroup;

		private RealtimeClient _client;
		private CancellationTokenSource _cancellationTokenSource;

		// Stores disconnect reason to display after scene reload
		private static string _shutdownStatus;

		public async void StartGame()
		{
			try
			{
				if (_client != null)
				{
					await Disconnect();
				}

				PlayerPrefs.SetString("PlayerName", NicknameText.text);
				RuntimePlayer.PlayerNickname = NicknameText.text;

				_cancellationTokenSource = new CancellationTokenSource();
				var cancellationToken = _cancellationTokenSource.Token;

				// Force local mode in editor for faster testing
				var mode = ForceLocalMode && Application.isEditor ? DeterministicGameMode.Local : DeterministicGameMode.Multiplayer;

				if (mode == DeterministicGameMode.Multiplayer)
				{
					StatusText.text = "Connecting to a room...";

					// Configure matchmaking to ensure players only join rooms with matching game modes
					var matchmakingArguments = new MatchmakingArguments
					{
						PhotonSettings = new AppSettings(PhotonServerSettings.Global.AppSettings),
						RoomName = string.IsNullOrEmpty(RoomText.text) == false ? RoomText.text : null,
						PluginName = "QuantumPlugin",
						MaxPlayers = Quantum.Input.MAX_COUNT,
						CustomProperties = new PhotonHashtable {["GameMode"] = GameModeIdentifier},
						CustomLobbyProperties = new[] {"GameMode"},
					};

					if (matchmakingArguments.AsyncConfig == null)
					{
						matchmakingArguments.AsyncConfig = AsyncConfig.Global;
						matchmakingArguments.AsyncConfig.CancellationToken = cancellationToken;
					}

					_client = await MatchmakingExtensions.ConnectToRoomAsync(matchmakingArguments);

					// Verify connected room has matching game mode
					var roomGameMode = (string)_client.CurrentRoom.CustomProperties["GameMode"];
					if (roomGameMode != GameModeIdentifier)
					{
						throw new InvalidOperationException($"Trying to connect to a room with game mode {roomGameMode} while being in the {GameModeIdentifier} scene.");
					}

					_client.CallbackMessage.Listen<UIGameMenu, OnDisconnectedMsg>(this, OnDisconnectMessage);
				}

				// Initialize and start the game session
				var sessionRunnerArguments = new SessionRunner.Arguments
				{
					RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
					GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
					ClientId = _client != null ? _client.UserId : null,
					RuntimeConfig = RuntimeConfig,
					SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
					GameMode = mode,
					PlayerCount = Quantum.Input.MAX_COUNT,
					Communicator = _client != null ? new QuantumNetworkCommunicator(_client) : null,
					CancellationToken = cancellationToken,
				};

				StatusText.text = "Starting game session...";

				// Start the simulation
				var runner = (QuantumRunner)await SessionRunner.StartAsync(sessionRunnerArguments);

				StatusText.text = "Connected";

				// Add local player to the simulation
				runner.Game.AddPlayer(0, RuntimePlayer);

				StatusText.text = "";
				PanelGroup.gameObject.SetActive(false);
			}
			catch (Exception exception)
			{
				Debug.LogWarning(exception);
				StatusText.text = $"Connection Failed: {exception.Message}";

				await Disconnect();
			}

			_cancellationTokenSource = null;
		}

		public async void DisconnectClicked()
		{
			await Disconnect();
		}

		public async void BackToMenu()
		{
			await Disconnect();

			SceneManager.LoadScene(0);
		}

		public void TogglePanelVisibility()
		{
			if (PanelGroup.gameObject.activeSelf && QuantumRunner.Default == null)
				return; // Panel cannot be hidden if the game is not running

			PanelGroup.gameObject.SetActive(!PanelGroup.gameObject.activeSelf);
		}

		private void OnEnable()
		{
			Application.targetFrameRate = 60;

			// Load or generate random player nickname
			var nickname = PlayerPrefs.GetString("PlayerName");
			if (string.IsNullOrEmpty(nickname))
			{
				nickname = "Player" + UnityEngine.Random.Range(10000, 100000);
			}

			NicknameText.text = nickname;

			// Display any shutdown status from previous session
			StatusText.text = _shutdownStatus != null ? _shutdownStatus : string.Empty;
			_shutdownStatus = null;
		}

		private void Update()
		{
			// Toggle menu visibility with Enter/Esc keys
			var keyboard = Keyboard.current;
			if (keyboard != null)
			{
				if (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
				{
					TogglePanelVisibility();
				}
			}

			if (PanelGroup.gameObject.activeSelf)
			{
				// Disable UI controls while connecting or in game
				bool isConnectingOrRunning = _cancellationTokenSource != null || QuantumRunner.Default != null;

				StartGroup.SetActive(isConnectingOrRunning == false);
				DisconnectGroup.SetActive(isConnectingOrRunning);
				RoomText.interactable = isConnectingOrRunning == false;
				NicknameText.interactable = isConnectingOrRunning == false;

				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}

		public async Task Disconnect()
		{
			try
			{
				if (_cancellationTokenSource != null)
				{
					_cancellationTokenSource.Cancel();
					_cancellationTokenSource = null;
				}

				StatusText.text = "Disconnecting...";
				PanelGroup.interactable = false;

				if (_client != null)
				{
					_client.CallbackMessage.UnlistenAll(this);
					_client = null;
				}

				await QuantumRunner.ShutdownAllAsync();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			// Reload scene to reset all network objects
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		private void OnDisconnectMessage(OnDisconnectedMsg message)
		{
			// Handle unexpected disconnects (e.g. connection lost)
			_shutdownStatus = $"Shutdown: {message.cause}";
			Debug.LogWarning(_shutdownStatus);

			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}
}
