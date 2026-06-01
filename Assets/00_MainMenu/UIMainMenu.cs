using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starter.MainMenu
{
	public class UIMainMenu : MonoBehaviour
	{
		public GameObject ExitButton;

		public void LoadScene(int index)
		{
			SceneManager.LoadScene(index);
		}

		public void QuitGame()
		{
			Application.Quit();

			#if UNITY_EDITOR
				UnityEditor.EditorApplication.ExitPlaymode();
			#endif
		}

		private void OnEnable()
		{
			// Ensure the cursor is visible when coming back from the game
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			// Hide exit button in WebGL build
			if (Application.platform == RuntimePlatform.WebGLPlayer)
			{
				ExitButton.SetActive(false);
			}
		}
	}
}
