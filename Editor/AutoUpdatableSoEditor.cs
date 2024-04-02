using DavidUtils.ScriptableObjectsUtils;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor
{
    [CustomEditor(typeof(AutoUpdatableSo), true)]
    public class AutoUpdatableSoEditor : UnityEditor.Editor
    {
        private bool _autoUpdate = true;

        public override void OnInspectorGUI()
        {
            AutoUpdatableSo data = (AutoUpdatableSo)target;

            if (DrawDefaultInspector() && _autoUpdate)
                data.NotifyUpdate();

            GUILayout.Space(30);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Update"))
                data.NotifyUpdate();

            GUILayout.Space(150);

            _autoUpdate = EditorGUILayout.Toggle("Auto Update", _autoUpdate);

            GUILayout.EndHorizontal();
        }
    }
}
