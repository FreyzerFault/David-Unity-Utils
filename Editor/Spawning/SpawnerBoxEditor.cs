using DavidUtils.Spawning;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Spawning.Editor.Spawning
{
	[CustomEditor(typeof(SpawnerBox), true)]
	public class SpawnerBoxEditor : UnityEditor.Editor
	{
		private int _numItemsSpawned = 1;

		private void OnEnable() =>
			Selection.selectionChanged += () => Tools.hidden = false;

		public override void OnInspectorGUI()
		{
			SpawnerBox spawner = (SpawnerBox)target;

			DrawDefaultInspector();

			GUILayout.Space(30);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Spawn", GUILayout.ExpandWidth(true)))
				for (var i = 0; i < _numItemsSpawned; i++)
					if (spawner is SpawnerBoxInTerrain boxInTerrain)
						boxInTerrain.SpawnRandom();
					else
						spawner.SpawnRandom();

			GUILayout.Space(20);

			_numItemsSpawned = EditorGUILayout.IntSlider("", _numItemsSpawned, 1, 100, GUILayout.MaxWidth(300));

			GUILayout.EndHorizontal();

			if (GUILayout.Button("Clear")) spawner.Clear();

			EditorGUILayout.LabelField("Spawned Items: " + spawner.SpawnedItems.Length);
		}

		protected virtual void OnSceneGUI()
		{
			SpawnerBox spawner = (SpawnerBox)target;

			Handles.color = Color.green;
			Handles.DrawWireCube(spawner.transform.position + spawner.Center, spawner.Size);


			Tools.hidden = Tools.current == Tool.Scale;

			if (Tools.current == Tool.Scale)
				ScaleHandle(spawner);
		}

		private void ScaleHandle(SpawnerBox spawner)
		{
			EditorGUI.BeginChangeCheck();

			float capSize = HandleUtility.GetHandleSize(spawner.transform.position);
			Vector3 newScale = Handles.ScaleHandle(
				spawner.Size,
				spawner.transform.position,
				spawner.transform.rotation,
				capSize
			);

			Handles.Label(
				spawner.transform.position + SceneView.currentDrawingSceneView.camera.transform.right,
				"BOUNDS"
			);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spawner, "Scale Bounds");
				spawner.Size = newScale;
			}
		}
	}
}
