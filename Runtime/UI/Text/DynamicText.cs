using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace DavidUtils.UI.Text
{
	[ExecuteAlways]
	public class DynamicText : MonoBehaviour
	{
		public MonoBehaviour targetObj;

		public KeyValuePair<string, string>[] targetFieldOptions = Array.Empty<KeyValuePair<string, string>>();
		[HideInInspector] public int fieldIndex;
		private KeyValuePair<string, string> SelectedField => targetFieldOptions[fieldIndex];

		private TMP_Text textLabel;
		private TMP_Text TextLabel => textLabel ??= GetComponent<TMP_Text>();

		private void Awake() => InitializeOptions();

		public void InitializeOptions() => targetFieldOptions = targetObj.GetType()
			.GetFields()
			.Select(f => new KeyValuePair<string, string>(f.Name, f.GetValue(targetObj).ToString()))
			.Concat(
				targetObj.GetType()
					.GetProperties()
					.Select(p => new KeyValuePair<string, string>(p.Name, p.GetValue(targetObj).ToString()))
			)
			.ToArray();

		public void UpdateText() => TextLabel.text = SelectedField.Value;

		private void OnValidate()
		{
			if (targetFieldOptions.Length == 0) InitializeOptions();
		}
	}
}
