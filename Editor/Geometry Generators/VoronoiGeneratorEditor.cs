using DavidUtils.ExtensionMethods;
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

		private void OnSceneGUI()
		{
			var voronoiGen = target as VoronoiGenerator;

			if (voronoiGen == null) return;

			Vector2[] seeds = voronoiGen.voronoi.seeds;

			for (var i = 0; i < seeds.Length; i++)
			{
				EditorGUI.BeginChangeCheck();

				Vector2 seed = seeds[i];
				Vector3 pos = voronoiGen.transform.localToWorldMatrix.MultiplyPoint3x4(seed.ToVector3xz());
				Quaternion rot = voronoiGen.transform.rotation;
				Vector3 scale = voronoiGen.transform.localScale;
				Handles.TransformHandle(ref pos, ref rot, ref scale);

				if (!EditorGUI.EndChangeCheck()) continue;

				Undo.RecordObject(target, "Update Seed");
				seeds[i] = voronoiGen.transform.worldToLocalMatrix.MultiplyPoint3x4(pos).ToVector2xz();
				voronoiGen.RunVoronoi();
				break;
			}
		}
	}
}
