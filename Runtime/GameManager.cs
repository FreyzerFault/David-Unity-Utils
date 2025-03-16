using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DavidUtils
{
	public class GameManager : SingletonPersistent<GameManager>
	{
		public enum GameState
		{
			Playing,
			Paused
		}

		public UnityEvent<GameState> onGameStateChanged;
		[SerializeField] private GameState state = GameState.Playing;
		public bool IsPlaying => state == GameState.Playing;
		public bool IsPaused => state == GameState.Paused;

		public GameState State
		{
			get => state;
			set
			{
				state = value;
				OnStateChange(value);
			}
		}

		protected override void Awake()
		{
			base.Awake();

			OnStateChange(State);
			onGameStateChanged ??= new UnityEvent<GameState>();
		}

		private void OnDestroy() => onGameStateChanged.RemoveAllListeners();

		protected virtual void OnStateChange(GameState newState)
		{
			switch (newState)
			{
				case GameState.Playing:
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
					break;
				case GameState.Paused:
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					break;
			}

			onGameStateChanged?.Invoke(newState);
		}

		private void TogglePause() => State = State == GameState.Paused ? GameState.Playing : GameState.Paused;

		#region SCENES

		public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
		public void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		public void ExitGame() => Application.Quit();

		#endregion
	}
}
