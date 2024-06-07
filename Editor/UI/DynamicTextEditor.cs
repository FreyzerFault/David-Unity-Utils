﻿using System.Collections.Generic;
using System.Linq;
using DavidUtils.UI.Text;
using UnityEditor;

namespace DavidUtils.Editor.UI
{
	[CustomEditor(typeof(DynamicText))]
	public class DynamicTextEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var dynamicText = (DynamicText)target;
			if (dynamicText == null) return;

			DrawDefaultInspector();

			KeyValuePair<string, string>[] targetFields = dynamicText.targetFieldOptions;
			if (targetFields.Length == 0) dynamicText.InitializeOptions();

			string[] targetFieldOptions = targetFields.Select(pair => pair.Key).ToArray();

			int index = EditorGUILayout.Popup(dynamicText.fieldIndex, targetFieldOptions);

			dynamicText.fieldIndex = index;

			dynamicText.UpdateText();
		}
	}
}
