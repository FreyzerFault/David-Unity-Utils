using System;
using DavidUtils.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DavidUtils.Settings
{
	public class SettingsManager : SingletonPersistent<SettingsManager>
	{
		public SettingsData settingsData;
		public bool saveOnSceneChange = true;

		public Action onLoad;
		public Action onSave;

		protected override void Awake()
		{
			base.Awake();

			Load();

			if (!saveOnSceneChange) return;

			// Cuando carga la escena, carga las settings
			// Cuando se descarga la escena, guarda las settings
			SceneManager.sceneLoaded += (_, _) => Load();
			SceneManager.sceneUnloaded += _ => Save();
			Application.quitting += Save;
		}

		private void OnDestroy() => Save();

		public T GetSetting<T>(string sName) => settingsData.GetSetting<T>(sName);

		public void SetSetting<T>(string sName, T value) => settingsData.SetSetting(sName, value);

		public void Save()
		{
			settingsData.SaveSettings();
			onSave?.Invoke();
		}

		public void Load()
		{
			settingsData.LoadSettings();
			onLoad?.Invoke();
		}
	}
}
