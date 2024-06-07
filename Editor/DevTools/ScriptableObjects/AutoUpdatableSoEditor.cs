using DavidUtils.DevTools.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.DevTools.ScriptableObjects
{
	[CustomEditor(typeof(AutoUpdatableSo), true)]
	public class AutoUpdatableSoEditor : UnityEditor.Editor
	{
		private bool _autoUpdate = true;

		public override void OnInspectorGUI()
		{
			var data = (AutoUpdatableSo)target;

			if (DrawDefaultInspector() && _autoUpdate) data.NotifyUpdate();

			GUILayout.Space(30);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Update")) data.NotifyUpdate();

			GUILayout.FlexibleSpace();

			_autoUpdate = EditorGUILayout.Toggle("Auto Update", _autoUpdate);

			GUILayout.EndHorizontal();
		}
	}
}
