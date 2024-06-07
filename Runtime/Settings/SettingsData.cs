using System;
using System.Collections.Generic;
using DavidUtils.DevTools.CustomAttributes;
using UnityEngine;

namespace DavidUtils.Settings
{
	// Add this to a child class
	[CreateAssetMenu(fileName = "Settings", menuName = "Settings")]
	public class SettingsData : ScriptableObject
	{
		public enum SettingType
		{
			Float,
			Int,
			String,
			Bool,
			Color
		}

		[SerializeField] public List<Setting> _settings = new();

		public T GetSetting<T>(string settingName)
		{
			settingName = settingName.ToLower();
			if (!HasSetting(settingName)) throw new Exception($"Setting {settingName} not found");

			return (T)_settings.Find(setting => setting.Name == settingName).Value;
		}

		public void SetSetting<T>(string settingName, T newValue)
		{
			settingName = settingName.ToLower();
			if (!HasSetting(settingName)) throw new Exception($"Setting {settingName} not found");

			_settings.Find(setting => setting.Name == settingName).Set(newValue);
		}

		private bool HasSetting(string settingName)
		{
			settingName = settingName.ToLower();
			return _settings.Exists(setting => setting.Name == settingName);
		}

		// LOAD & SAVE Settings to PlayerPrefs
		public void LoadSettings()
		{
			foreach (Setting setting in _settings) setting.Load();
		}

		public void SaveSettings()
		{
			foreach (Setting setting in _settings) setting.Save();
		}

		[Serializable]
		public struct Setting
		{
			[SerializeField] private string name;

			public SettingType type;

			[ConditionalField("type", false, SettingType.Int)]
			public int valueInt;

			[ConditionalField("type", false, SettingType.Float)]
			public float valueFloat;

			[ConditionalField("type", false, SettingType.Bool)]
			public bool valueBool;

			[ConditionalField("type", false, SettingType.String)]
			public string valueString;

			[ConditionalField("type", false, SettingType.Color)]
			public Color valueColor;

			public Setting(string name, SettingType type)
			{
				this.name = name;
				this.type = type;
				valueInt = 0;
				valueFloat = 0;
				valueBool = false;
				valueString = "";
				valueColor = Color.magenta;
			}

			public string Name => name.ToLower();

			public object Value
			{
				get =>
					type switch
					{
						SettingType.Int => valueInt,
						SettingType.Float => valueFloat,
						SettingType.Bool => valueBool,
						SettingType.String => valueString,
						SettingType.Color => valueColor,
						_ => null
					};
				set
				{
					switch (type)
					{
						case SettingType.Float:
							valueFloat = (float)value;
							break;
						case SettingType.Int:
							valueInt = (int)value;
							break;
						case SettingType.String:
							valueString = (string)value;
							break;
						case SettingType.Bool:
							valueBool = (bool)value;
							break;
						case SettingType.Color:
							valueColor = (Color)value;
							break;
					}
				}
			}

			public void Set<T>(T value) => Value = value;

			public object Load()
			{
				if (!PlayerPrefs.HasKey(Name)) return null;

				switch (type)
				{
					case SettingType.Int:
						valueInt = PlayerPrefs.GetInt(Name);
						break;
					case SettingType.Float:
						valueFloat = PlayerPrefs.GetFloat(Name);
						break;
					case SettingType.Bool:
						valueBool = PlayerPrefs.GetInt(Name) == 1;
						break;
					case SettingType.String:
						valueString = PlayerPrefs.GetString(Name);
						break;
					case SettingType.Color:
						string hexColor = PlayerPrefs.GetString(Name);
						if (!ColorUtility.TryParseHtmlString(hexColor, out valueColor))
							throw new FormatException(
								$"Color stored in non Hex Color Format: {hexColor}"
							);
						break;
				}

				return null;
			}

			public void Save()
			{
				switch (type)
				{
					case SettingType.Int:
						PlayerPrefs.SetInt(Name, valueInt);
						break;
					case SettingType.Float:
						PlayerPrefs.SetFloat(Name, valueFloat);
						break;
					case SettingType.Bool:
						PlayerPrefs.SetInt(Name, valueBool ? 1 : 0);
						break;
					case SettingType.String:
						PlayerPrefs.SetString(Name, valueString);
						break;
					case SettingType.Color:
						string hexColor = ColorUtility.ToHtmlStringRGB(valueColor);
						PlayerPrefs.SetString(Name, hexColor);
						break;
				}
			}

			public override string ToString() => $"{name}: {Value}";

			public override bool Equals(object obj) =>
				obj is Setting setting
				&& string.Equals(Name, setting.Name, StringComparison.CurrentCultureIgnoreCase);

			public override int GetHashCode() => name.GetHashCode();
		}
	}
}
