using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Generators;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Geometry_Generators
{
	[CustomEditor(typeof(VoronoiGenerator), true)]
	public class VoronoiGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var voronoiGen = (VoronoiGenerator)target;

			base.OnInspectorGUI();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			// Boton para generar Voronoi
			if (GUILayout.Button("Generate Voronoi"))
				voronoiGen.Run();

			if (GUILayout.Button("New Voronoi"))
			{
				voronoiGen.RandomizeSeeds();
				voronoiGen.Run();
			}

			if (GUILayout.Button("Reset"))
				voronoiGen.Initialize();

			EditorGUILayout.EndHorizontal();
		}

		private void OnSceneGUI()
		{
			var voronoiGen = target as VoronoiGenerator;

			if (voronoiGen == null) return;

			if (voronoiGen.voronoi == null || voronoiGen.voronoi.Seeds == null) return;

			Vector2[] seeds = voronoiGen.voronoi.Seeds;

			for (var i = 0; i < seeds.Length; i++)
			{
				EditorGUI.BeginChangeCheck();

				Vector2 seed = seeds[i];
				Transform transform = voronoiGen.transform;
				Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(seed.ToVector3xz());
				pos = Handles.PositionHandle(pos, transform.rotation);

				if (!EditorGUI.EndChangeCheck()) continue;

				Undo.RecordObject(target, "Update Seed");

				Vector2 newLocalPos = transform.worldToLocalMatrix.MultiplyPoint3x4(pos).ToVector2xz();
				voronoiGen.voronoi.MoveSeed(i, newLocalPos.Clamp01());

				break;
			}
		}
	}
}
