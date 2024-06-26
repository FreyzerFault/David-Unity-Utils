using System.Reflection;
using DavidUtils.DevTools.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.UI.Text
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	public class DynamicText : MonoBehaviour
	{
		[SerializeField] private AttributesExposer textExposer;

		private TMP_Text textLabel;
		private TMP_Text TextLabel => textLabel ??= GetComponent<TMP_Text>();

		[Header("Wrap Between")]
		public string prefix = "";
		public string suffix = "";

		private void OnEnable()
		{
			textExposer.LoadExposedFields();
			textExposer.OnFieldSelected += HandleValueChange;
		}

		private void OnDisable() => textExposer.OnFieldSelected -= HandleValueChange;

		private void Update() => UpdateText();

		public void HandleValueChange(MemberInfo info)
		{
			UpdateText();
			RepaintAll();
		}

		private void UpdateText() => TextLabel?.SetText(textExposer.StringValue != null ? $"{prefix}{textExposer.StringValue}{suffix}" : TextLabel.text);

		private static void RepaintAll()
		{
#if UNITY_EDITOR
			SceneView.RepaintAll();
#endif
		}
	}
}
