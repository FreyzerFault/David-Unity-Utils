using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Geometry.Generators;
using DavidUtils.MouseInput;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Geometry_Generators
{
	[CustomEditor(typeof(VoronoiGenerator), true)]
	public class VoronoiGeneratorEditor : UnityEditor.Editor
	{
		private VoronoiGenerator VoronoiGen => target as VoronoiGenerator;

		private Vector2 LocalMousePos => VoronoiGen.transform.worldToLocalMatrix
			.MultiplyPoint3x4(MouseInputUtils.mouseWorldPosition_InScene)
			.ToV2xz();
		private Bounds2D Bounds => VoronoiGen.Bounds;
		private Vector2 MousePosNorm => Bounds.Normalize(LocalMousePos);
		private bool MouseInBounds => MousePosNorm.IsIn01();


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
				voronoiGen.Reset();

			EditorGUILayout.EndHorizontal();
		}

		private void OnSceneGUI()
		{
			// Actualiza la posicion del Mouse en Scene
			MouseInputUtils.UpdateMousePositionInScene();

			VoronoiGenerator voronoiGen = VoronoiGen;

			if (voronoiGen == null || voronoiGen.voronoi?.Seeds == null) return;

			// Esto se asegura que se mantiene seleccionado al hacer click dentro, pero si clickas fuera se deselecciona
			// Asi no necesito tener un Bounds
			if (Event.current.type == EventType.Used && voronoiGen.IsRegionHovering)
				Selection.activeGameObject = VoronoiGen.gameObject;


			// MOVE HANDLE
			if (voronoiGen.IsRegionSelected)
			{
				Vector2 seed = voronoiGen.SelectedRegion!.Value.centroid;

				EditorGUI.BeginChangeCheck();

				Transform transform = voronoiGen.transform;
				Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(seed.ToV3xz());
				pos = Handles.PositionHandle(pos, transform.rotation);

				if (EditorGUI.EndChangeCheck())
				{
					Vector2 newLocalPos = transform.worldToLocalMatrix.MultiplyPoint3x4(pos).ToV2xz().Clamp01();
					voronoiGen.MoveSeed(voronoiGen.selectedRegionIndex, newLocalPos);

					voronoiGen.HoverRegion(voronoiGen.selectedRegionIndex);

					return;
				}
			}

			if (Event.current.isMouse)
				ProcessMouseEvents();
		}

		private void ProcessMouseEvents()
		{
			int regionIndex = MouseInBounds ? VoronoiGen.voronoi.GetRegionIndex(MousePosNorm) : -1;
			switch (Event.current.type)
			{
				case EventType.MouseMove:
					VoronoiGen.hoveredRegionIndex = regionIndex;
					break;
				case EventType.MouseUp:
					VoronoiGen.selectedRegionIndex = regionIndex;
					break;
			}
		}
	}
}
