using System.Reflection;
using TMPro;
using UnityEngine;
using DavidUtils.DevTools.Reflection;

namespace DavidUtils.UI.Text
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	public class DynamicText : MonoBehaviour
	{
		[SerializeField] private AttributesExposer textExposer;

		private TMP_Text _textLabel;
		private TMP_Text TextLabel => _textLabel ??= GetComponent<TMP_Text>();

		[Header("Wrap Between")]
		public string prefix = "";
		public string suffix = "";

		private void OnEnable()
		{
			textExposer.LoadExposedFields();
			textExposer.onFieldSelected += HandleValueChange;
		}

		private void OnDisable() => textExposer.onFieldSelected -= HandleValueChange;

		private void Update() => UpdateText();

		public void HandleValueChange(MemberInfo info)
		{
			UpdateText();
			SceneUtilities.RepaintAll();
		}

		private void UpdateText() => TextLabel?.SetText(textExposer.StringValue != null ? $"{prefix}{textExposer.StringValue}{suffix}" : TextLabel.text);

	}
}
