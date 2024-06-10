using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DavidUtils.UI.Text
{
	[ExecuteAlways]
	public class DynamicText : MonoBehaviour
	{
		public struct FieldToText
		{
			public string fieldName;
			public string text;

			public FieldToText(string name, string value) => (fieldName, text) = (name, value);

			public FieldToText(FieldInfo field, object targetObj)
				: this(field.Name, field.GetValue(targetObj).ToString())
			{
			}

			public FieldToText(PropertyInfo prop, object targetObj)
			{
				fieldName = prop.Name;
				text = prop.GetValue(targetObj).ToString();
			}
		}

		public MonoBehaviour targetObj;

		public FieldToText[] targetFieldOptions = Array.Empty<FieldToText>();
		[HideInInspector] public int fieldIndex;
		private FieldToText SelectedField => targetFieldOptions[fieldIndex];

		private TMP_Text textLabel;
		private TMP_Text TextLabel => textLabel ??= GetComponent<TMP_Text>();

		private void Awake() => InitializeOptions();

		public void InitializeOptions()
		{
			FieldInfo[] fields = targetObj.GetType().GetFields();
			PropertyInfo[] props = targetObj.GetType()
				.GetProperties()
				.Where(p => !p.GetCustomAttributes<ObsoleteAttribute>(true).Any())
				.ToArray();

			// Convierte FieldInfos y PropertyInfos en FieldToText
			targetFieldOptions = fields.Select(f => new FieldToText(f, targetObj))
				.Concat(props.Select(p => new FieldToText(p, targetObj)))
				.ToArray();
		}

		public void UpdateText() => TextLabel.text = SelectedField.text;

		private void OnValidate()
		{
			if (targetFieldOptions.Length == 0) InitializeOptions();
		}

		private PropertyInfo[] Properties => targetObj.GetType()
			.GetProperties()
			.Where(p => !p.GetCustomAttributes<ObsoleteAttribute>(true).Any())
			.ToArray();

		private FieldInfo[] Fields => targetObj.GetType().GetFields();
	}
}
