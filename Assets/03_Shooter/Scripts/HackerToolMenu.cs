using System.Text;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Starter.Shooter
{
	/// <summary>
	/// DEMO / PRESENTATION TOOL — mô phỏng một "hacker" dùng tool ngoài chỉnh giá trị trong RAM.
	///
	/// Tool ghi trực tiếp vào VERIFIED frame của Quantum (HP, tốc độ di chuyển của local player).
	/// Vì mọi client BẮT BUỘC phải tính ra cùng một frame deterministic, việc sửa giá trị chỉ trên
	/// một client sẽ làm checksum của client đó lệch khỏi các client khác. Photon Quantum phát hiện
	/// điều này và báo lỗi ChecksumError / desync (xem cửa sổ Console).
	///
	/// => Mục đích: trình diễn cơ chế chống cheat bằng checksum của Photon Quantum đang hoạt động.
	///
	/// Tool tự sinh ra CHỈ trong scene 03_Shooter. Mở/đóng bằng nút trên màn hình hoặc phím F1.
	///
	/// LƯU Ý ĐỂ DEMO RA LỖI:
	///  - Cần ÍT NHẤT 2 client cùng phòng (Multiplayer mode), vì checksum được so sánh giữa các client.
	///    Bỏ tick "Force Local Mode" trong UIGameMenu để chạy Multiplayer.
	///  - Checksum đã được bật sẵn (SessionConfig.asset: ChecksumInterval = 60).
	/// </summary>
	public class HackerToolMenu : MonoBehaviour
	{
		private const string ShooterSceneName = "03_Shooter";

		private static HackerToolMenu _instance;

		// Tự sinh tool trong scene Shooter mà không cần chỉnh sửa scene/prefab.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
			TrySpawn(SceneManager.GetActiveScene());
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawn(scene);

		private static void TrySpawn(Scene scene)
		{
			if (scene.name != ShooterSceneName)
				return;
			if (_instance != null)
				return;

			var go = new GameObject("[QuantumHackTool]");
			_instance = go.AddComponent<HackerToolMenu>();
		}

		// --- Trạng thái UI ---
		private bool _open = true;
		private Rect _window = new Rect(20, 70, 360, 10);
		private float _targetHp = 9999f;
		private float _targetSpeed = 10f;
		private readonly StringBuilder _log = new StringBuilder();

		private GUIStyle _btn;
		private GUIStyle _hackBtn;
		private GUIStyle _label;
		private GUIStyle _note;

		private void Update()
		{
			var keyboard = Keyboard.current;
			if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
				_open = !_open;
		}

		private void OnGUI()
		{
			EnsureStyles();

			// OnGUI chạy SAU Update nên đè được UIGameMenu (vốn khoá chuột mỗi frame khi đang chơi).
			// Mở panel -> mở khoá chuột để bấm được nút/slider. Bấm F1 để mở panel khi chuột đang khoá.
			if (_open)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}

			if (GUI.Button(new Rect(20, 20, 220, 36), _open ? "✕ Đóng Hack Tool (F1)" : "☠ Mở Quantum Hack Tool (F1)", _hackBtn))
				_open = !_open;

			if (_open)
				_window = GUILayout.Window(GetInstanceID(), _window, DrawWindow, "QUANTUM HACK TOOL — DEMO CHECKSUM");
		}

		private void DrawWindow(int id)
		{
			GUILayout.Space(4);

			// --- Trạng thái phiên ---
			bool hasPlayer = TryGetLocalPlayer(out var frame, out var entity);
			if (frame == null)
			{
				GUILayout.Label("● Game chưa chạy. Bấm Start để vào trận.", _note);
			}
			else if (hasPlayer == false)
			{
				GUILayout.Label($"● Đã vào trận (verified tick {frame.Number}) — chưa tìm thấy local player.", _note);
			}
			else
			{
				int curHp = 0, curMax = 0;
				float curSpeed = 0f;
				if (frame.TryGet<Health>(entity, out var h)) { curHp = h.CurrentHealth; curMax = h.MaxHealth; }
				if (frame.TryGet<Movement>(entity, out var m)) { curSpeed = m.WalkSpeedMultiplier.AsFloat; }

				GUILayout.Label($"● Verified tick: <b>{frame.Number}</b>", _label);
				GUILayout.Label($"   HP hiện tại: <b>{curHp}</b> / {curMax}", _label);
				GUILayout.Label($"   Speed multiplier: <b>{curSpeed:0.00}</b>", _label);
			}

			GUILayout.Space(8);
			GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));

			// --- HP ---
			GUILayout.Label($"HP đích: {Mathf.RoundToInt(_targetHp)}", _label);
			_targetHp = GUILayout.HorizontalSlider(_targetHp, 0f, 9999f);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set HP", _btn, GUILayout.Height(30)))
				HackHp(Mathf.RoundToInt(_targetHp));
			if (GUILayout.Button("GOD MODE (HP 9999)", _hackBtn, GUILayout.Height(30)))
			{
				_targetHp = 9999f;
				HackHp(9999);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(6);

			// --- Speed ---
			GUILayout.Label($"Speed multiplier đích: {_targetSpeed:0.0}", _label);
			_targetSpeed = GUILayout.HorizontalSlider(_targetSpeed, 0f, 20f);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set Speed", _btn, GUILayout.Height(30)))
				HackSpeed(_targetSpeed);
			if (GUILayout.Button("SPEED HACK (x10)", _hackBtn, GUILayout.Height(30)))
			{
				_targetSpeed = 10f;
				HackSpeed(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(6);
			if (GUILayout.Button("↺ Khôi phục (HP = Max, Speed = 1)", _btn, GUILayout.Height(28)))
				Restore();

			GUILayout.Space(8);
			GUILayout.Label(
				"Sau khi sửa giá trị: Photon Quantum sẽ phát hiện CHECKSUM ERROR " +
				"vì frame của bạn lệch khỏi các client khác. Theo dõi cửa sổ Console.",
				_note);

			if (_log.Length > 0)
			{
				GUILayout.Space(6);
				GUILayout.Box(_log.ToString(), GUILayout.ExpandWidth(true));
			}

			GUI.DragWindow(new Rect(0, 0, 10000, 24));
		}

		// ---------------------------------------------------------------------
		//  Lõi "hack": ghi thẳng vào verified frame của Quantum
		// ---------------------------------------------------------------------

		private static QuantumGame Game => QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;

		/// <summary>
		/// Lấy verified frame + entity của local player. Verified frame là frame "chính thức"
		/// dùng để tính checksum; sửa nó mới làm Quantum báo desync (sửa predicted frame sẽ bị rollback).
		/// </summary>
		private bool TryGetLocalPlayer(out Frame frame, out EntityRef entity)
		{
			frame = null;
			entity = default;

			var game = Game;
			if (game == null)
				return false;

			try
			{
				frame = game.Frames.Verified;
			}
			catch
			{
				frame = null;
			}
			if (frame == null)
				return false;

			foreach (var pair in frame.GetComponentIterator<PlayerLink>())
			{
				if (game.PlayerIsLocal(pair.Component.PlayerRef))
				{
					entity = pair.Entity;
					return true;
				}
			}
			return false;
		}

		private void HackHp(int hp)
		{
			if (TryGetLocalPlayer(out var frame, out var entity) == false)
			{
				Log("Chưa có local player để hack.");
				return;
			}

			if (frame.TryGet<Health>(entity, out var health) == false)
			{
				Log("Local player không có component Health.");
				return;
			}

			health.CurrentHealth = hp;
			if (health.MaxHealth < hp)
				health.MaxHealth = hp;

			frame.Set(entity, health);                 // ghi đè vào verified frame -> checksum lệch
			Log($"HACK HP = {hp} @ verified tick {frame.Number}");
		}

		private void HackSpeed(float speed)
		{
			if (TryGetLocalPlayer(out var frame, out var entity) == false)
			{
				Log("Chưa có local player để hack.");
				return;
			}

			if (frame.TryGet<Movement>(entity, out var movement) == false)
			{
				Log("Local player không có component Movement.");
				return;
			}

			movement.WalkSpeedMultiplier = FP.FromFloat_UNSAFE(speed);
			frame.Set(entity, movement);               // ghi đè vào verified frame -> checksum lệch
			Log($"HACK Speed = {speed:0.00} @ verified tick {frame.Number}");
		}

		private void Restore()
		{
			if (TryGetLocalPlayer(out var frame, out var entity) == false)
				return;

			if (frame.TryGet<Health>(entity, out var health))
			{
				health.CurrentHealth = health.MaxHealth;
				frame.Set(entity, health);
			}
			if (frame.TryGet<Movement>(entity, out var movement))
			{
				movement.WalkSpeedMultiplier = FP._1;
				frame.Set(entity, movement);
			}
			Log("Đã khôi phục HP/Speed (frame vẫn có thể đã lệch trước đó).");
		}

		private void Log(string message)
		{
			Debug.Log($"[QuantumHackTool] {message}");
			_log.Insert(0, message + "\n");
			if (_log.Length > 600)
				_log.Length = 600;
		}

		// ---------------------------------------------------------------------

		private void EnsureStyles()
		{
			if (_btn != null)
				return;

			_btn = new GUIStyle(GUI.skin.button) { fontSize = 13 };
			_hackBtn = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
			_hackBtn.normal.textColor = new Color(1f, 0.55f, 0.2f);
			_hackBtn.hover.textColor = new Color(1f, 0.7f, 0.35f);

			_label = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
			_note = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, richText = true };
			_note.normal.textColor = new Color(0.85f, 0.85f, 0.6f);
		}
	}
}
