using DavidUtils.Geometry.Generators;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Geometry.Generators
{
	[CustomEditor(typeof(VoronoiGenerator), true)]
	public class VoronoiGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var voronoiGen = (VoronoiGenerator)target;

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
				voronoiGen.Reset();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			base.OnInspectorGUI();
		}
	}
}
