using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using UnityEngine;

namespace DavidUtils.Rendering.Extensions
{
	public static class LineRendererExtensions
	{
		public const float DEFAULT_THICKNESS = .1f;
		public const int DEFAULT_SMOOTHNESS = 5;
		public static Material DefaultLineMaterial => new(Shader.Find("Sprites/Default"));

		#region MODIFY LINE

		public static void CopyLineRendererPoints(this LineRenderer lr, LineRenderer other)
		{
			lr.positionCount = other.positionCount;
			var points = new Vector3[other.positionCount];
			other.GetPositions(points);
			lr.SetPositions(points);
		}

		public static Vector3[] GetPoints(this LineRenderer lr)
		{
			var points = new Vector3[lr.positionCount];
			lr.GetPositions(points);
			return points;
		}

		public static void SetPoints(this LineRenderer lr, Vector3[] points)
		{
			if (points == null) return;

			if (lr.positionCount != points.Length)
				lr.positionCount = points.Length;
			lr.SetPositions(points);
		}

		public static void SetPolygon(this LineRenderer lr, Polygon polygon, bool XZplane = true) =>
			lr.SetPoints(XZplane ? polygon.Vertices3D_XZ : polygon.Vertices3D_XY);

		#endregion


		#region INSTANTIATION

		public static LineRenderer ToLineRenderer(
			Transform parent,
			string name = "Line",
			Vector3[] points = null,
			Color[] colors = null,
			float thickness = DEFAULT_THICKNESS,
			int smoothness = DEFAULT_SMOOTHNESS,
			Material material = null,
			bool loop = false
		)
		{
			GameObject obj = UnityUtils.InstantiateEmptyObject(parent, name);
			var lr = obj.AddComponent<LineRenderer>();

			if (colors is { Length: > 1 })
			{
				lr.colorGradient = colors.ToGradient();
				lr.colorGradient.mode = GradientMode.Fixed;
			}
			else
			{
				lr.startColor = lr.endColor = colors is { Length: 1 } ? colors[0] : Color.gray;
			}

			lr.startWidth = lr.endWidth = thickness;
			lr.numCapVertices = lr.numCornerVertices = smoothness;
			lr.sharedMaterial = material != null ? material : DefaultLineMaterial;
			lr.loop = loop;

			lr.SetPoints(points);

			// Posiciones de Vertices locales al Transform del GameObject
			lr.useWorldSpace = false;

			return lr;
		}

		public static LineRenderer DefaultLineRenderer(Transform parent = null) => ToLineRenderer(parent);

		#endregion


		#region WIRE SHAPES

		// POLYLINE
		public static LineRenderer ToLineRenderer(
			this Polyline polyline, Transform parent, Color color = default,
			float thickness = DEFAULT_THICKNESS,
			int smoothness = DEFAULT_SMOOTHNESS, bool XZplane = true
		) => ToLineRenderer(
			parent,
			"Line",
			polyline.points,
			new[] { color },
			thickness,
			smoothness,
			loop: polyline.loop
		);

		// TRIANGLE
		public static LineRenderer ToLineRenderer(
			this Triangle triangle, Transform parent, string name = "Triangle Line", Color color = default,
			float thickness = DEFAULT_THICKNESS,
			int smoothness = DEFAULT_SMOOTHNESS, bool XZplane = true
		) => ToLineRenderer(
			parent,
			name,
			XZplane ? triangle.Vertices3D_XZ : triangle.Vertices3D_XY,
			new[] { color },
			thickness,
			smoothness,
			loop: true
		);

		// POLYGON
		public static LineRenderer ToLineRenderer(
			this Polygon polygon, Transform parent, Color color = default,
			float thickness = DEFAULT_THICKNESS,
			int smoothness = DEFAULT_SMOOTHNESS, bool XZplane = true
		) => ToLineRenderer(
			parent,
			"Polygon Line",
			XZplane ? polygon.Vertices3D_XZ : polygon.Vertices3D_XY,
			new[] { color },
			thickness,
			smoothness,
			loop: true
		);

		#endregion
	}
}
