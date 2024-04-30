using DavidUtils.Geometry.Generators;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Geometry_Generators
{
	[CustomEditor(typeof(VoronoiGenerator))]
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
			{
				voronoiGen.Initialize();
				if (!voronoiGen.SeedsGenerated)
					voronoiGen.GenerateSeeds();
				voronoiGen.RunVoronoi();
			}

			if (GUILayout.Button("New Voronoi"))
			{
				voronoiGen.Initialize();
				voronoiGen.GenerateNewSeeds();
				voronoiGen.RunVoronoi();
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}
