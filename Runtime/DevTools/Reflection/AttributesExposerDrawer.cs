using System.Linq;
using System.Reflection;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.DevTools.Reflection
{
	[CustomPropertyDrawer(typeof(AttributesExposer))]
	public class AttributesExposerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			// Don't make child fields be indented
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			var exposer = (AttributesExposer)property.GetValue();

			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

			EditorGUILayout.PrefixLabel(exposer.label);

			// ONLY EXPOSED Toggle
			bool onlyExposed = EditorGUILayout.Toggle("Only Exposed", exposer.onlyExposed);
			if (exposer.onlyExposed != onlyExposed)
			{
				exposer.onlyExposed = onlyExposed;
				exposer.LoadExposedFields();
			}

			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();

			EditorGUI.indentLevel = 1;

			// OBJECT SELECTOR
			Object target = EditorGUILayout.ObjectField(
				exposer.targetObj,
				typeof(MonoBehaviour),
				true
			);

			// COMPONENT SELECTOR
			if (target is Component go)
			{
				Component[] components = go.GetComponents(typeof(Component));
				int index = components.Contains(go) ? components.FirstIndex(c => c == go) : 0;
				string[] names = components.Select(c => c.GetType().Name).ToArray();
				index = EditorGUILayout.Popup(index, names);
				target = components[index];
			}

			if (exposer.targetObj != target) exposer.LoadExposedFields(target);

			if (exposer.HasFields)
			{
				MemberInfo[] targetFields = exposer.targetFieldOptions;
				if (targetFields.Length == 0) exposer.LoadExposedFields();

				string[] targetFieldOptions = targetFields.Select(pair => pair.Name).ToArray();

				// FIELD SELECTOR
				int index = EditorGUILayout.Popup(exposer.fieldIndex, targetFieldOptions);

				exposer.SelectField(index);

				// int index = EditorGUILayout.Popup(GUIContent.none, property.FindPropertyRelative("fieldIndex").intValue,  property.FindPropertyRelative("targetFieldOptions").GetValue() as MemberInfo[]?.Select(pair => pair.Name).ToArray() ?? new string[0]);
			}

			GUILayout.EndHorizontal();

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}
	}
}
