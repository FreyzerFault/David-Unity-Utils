using DavidUtils.Geometry.Bounding_Box;
using UnityEditor;
using UnityEngine;

namespace GENES.Editor.Geometry.Bounding_Box
{
	[CustomEditor(typeof(BoundsComponent))]
	public class BoundsComponentEditor : UnityEditor.Editor
	{
		private void OnEnable() =>
			Selection.selectionChanged += () => Tools.hidden = false;

		public override void OnInspectorGUI()
		{
			SwitchButton();
			base.OnInspectorGUI();
		}

		private void SwitchButton()
		{
			var boundsComp = (BoundsComponent) target;
			
			if (boundsComp.is2D && GUILayout.Button("Switch to 3D"))
			{
				boundsComp.SincronizeBounds();
				boundsComp.is2D = false;
			}
			else if (!boundsComp.is2D && GUILayout.Button("Switch to 2D"))
			{
				boundsComp.SincronizeBounds();
				boundsComp.is2D = true;
			}
		}

		protected virtual void OnSceneGUI()
		{
			var boundsComp = (BoundsComponent)target;

			Tools.hidden = Tools.current == Tool.Scale;

			if (Tools.current != Tool.Scale) return;

			if (boundsComp.is2D)
				ScaleHandle2D(boundsComp);
			else
				ScaleHandle3D(boundsComp);

			Handles.Label(
				boundsComp.transform.position + SceneView.currentDrawingSceneView.camera.transform.right,
				"BOUNDS"
			);
		}

		private void ScaleHandle2D(BoundsComponent boundsComp)
		{
			EditorGUI.BeginChangeCheck();

			float capSize = HandleUtility.GetHandleSize(boundsComp.transform.position);

			Bounds bounds = boundsComp.aabb2D;
			Vector3 size = boundsComp.Size2D;

			float x = size.x, y = size.y, z = size.z;

			Vector3 center = boundsComp.transform.position + bounds.center;
			Quaternion rotation = boundsComp.transform.rotation;

			Vector3 newScale = Handles.ScaleHandle(
				boundsComp.Size,
				boundsComp.transform.position,
				boundsComp.transform.rotation,
				capSize
			);


			// Handles.color = Color.red;
			// x = Handles.ScaleValueHandle(x, center + Vector3.right, rotation, capSize, Handles.CubeHandleCap, 1);
			// Handles.DrawLine(center, center + Vector3.right * x);
			//     
			// if (boundsComp.XZplane)
			// {
			//     Handles.color = Color.green;
			//     y = Handles.ScaleValueHandle(y, center + Vector3.up, rotation, capSize, Handles.CubeHandleCap, 1);
			//     Handles.DrawLine(center, center + Vector3.up * y);
			// }
			// else
			// {
			//     Handles.color = Color.blue;
			//     z = Handles.ScaleValueHandle(z, center + Vector3.forward, rotation, capSize, Handles.CubeHandleCap, 1);
			//     Handles.DrawLine(center, center + Vector3.forward * z);
			// }

			if (!EditorGUI.EndChangeCheck()) return;

			Undo.RecordObject(boundsComp, "Scale Bounds");
			// boundsComp.Size = new Vector3(x,y,z);
			boundsComp.Size2D = new Vector2(newScale.x, boundsComp.XZplane ? newScale.z : newScale.y);
		}

		private void ScaleHandle3D(BoundsComponent boundsComp)
		{
			EditorGUI.BeginChangeCheck();

			float capSize = HandleUtility.GetHandleSize(boundsComp.transform.position);
			Vector3 newScale = Handles.ScaleHandle(
				boundsComp.Size,
				boundsComp.transform.position,
				boundsComp.transform.rotation,
				capSize
			);

			if (!EditorGUI.EndChangeCheck()) return;

			Undo.RecordObject(boundsComp, "Scale Bounds");
			boundsComp.Size = newScale;
		}
	}
}
