using System;
using DavidUtils.Settings;
using DavidUtils.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DavidUtils
{
	/// <summary>
	///		Game Manager manage States, Scenes and global persistent data between ALL the Game
	///		SingletonPersistent: Singleton + DDOL (Don't Destroy On scene Load)
	///
	///		Change the State with GameManager.Instance.State = GameState
	///		Suscribe to onGameStateChanged in each component that depends on it
	/// </summary>
	public class GameManager : SingletonPersistent<GameManager>
	{
		public enum GameState { MainMenu, GameOver, Playing, Paused }
	
		[SerializeField] private GameState initialState = GameState.MainMenu;
		private GameState _state;
	
		public GameState State
		{
			get => _state;
			set
			{
				_state = value;
				OnStateChange(value);
			}
		}
	
		public bool IsPlaying => _state == GameState.Playing;
		public bool IsPaused => _state == GameState.Paused;
	
		public Action onGameStateChanged; // Suscribe to this to update by the GameState
	
	
		protected override void Awake()
		{
			base.Awake();
			SuscribeSceneLoading();
			State = initialState;
		
		}

		private void Start() => LoadSettings(); // SettingsManager Has to Awake before loading

		private void Update()
		{
			if (Input.GetKeyUp(KeyCode.Escape)) TogglePause();
			if (Input.GetKeyUp(KeyCode.F12)) ToggleDebugMode();
		}
	
		private void OnStateChange(GameState newState)
		{
			switch (newState)
			{
				case GameState.MainMenu:
					// TODO
					break;
				case GameState.GameOver:
					// TODO
					break;
				case GameState.Playing:
					// TODO
					break;
				case GameState.Paused:
					// TODO
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
			}

			onGameStateChanged?.Invoke();
		}


		#region SETTINGS
	
		private void LoadSettings()
		{
			_debugMode = SettingsManager.Instance.GetSetting<bool>("debugMode");
		}

		#endregion
	
	
		#region PAUSE

		public void Pause() => State = GameState.Paused;
		public void Resume() => State = GameState.Playing;
		public void TogglePause() => State = State == GameState.Paused ? GameState.Playing : GameState.Paused;

		#endregion
	

		#region SCENE MANAGEMENT

		private void SuscribeSceneLoading()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				switch (scene.name)
				{
					case "MainMenu":
						State = GameState.MainMenu;
						break;
					case "Game":
						State = GameState.Playing;
						break;
					case "GameOver":
						State = GameState.GameOver;
						break;
				}
			};
		}

		public static void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
		public static void ReloadScene() => LoadScene(SceneManager.GetActiveScene().name);
		public static void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
		}

		#endregion


		#region PLAYER DATA

		private int _winCount;
		private int _winStreak;
		private bool _win;
	
		public bool Win => _win;
		public int WinCount => _winCount;
		public int WinStreak => _winStreak;

		public void RegisterWin()
		{
			_win = true;
			_winCount++;
			_winStreak++;
		}
	
		public void RegisterLose()
		{
			_win = false;
			_winStreak = 0;
		}

		#endregion


		#region DEBUG

		private bool _debugMode = false;

		public bool DebugMode
		{
			get => _debugMode;
			set
			{
				_debugMode = value;
				onToggleDebugMode?.Invoke(_debugMode);
			}
		}
	
		public Action<bool> onToggleDebugMode;

		public bool ToggleDebugMode() => DebugMode = !DebugMode;

		#endregion
	}
}
