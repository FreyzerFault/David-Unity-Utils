using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public struct Polygon
	{
		// Vertices in Counter-Clockwise order
		public Vector2[] vertices;
		public Vector2 centroid;

		public Polygon(Vector2[] vertices, Vector2 centroid = default)
		{
			this.vertices = vertices;
			this.centroid = centroid;
		}

		private Vector3 ToWorldSpace(Matrix4x4 matrixTRS) => matrixTRS.MultiplyPoint3x4(centroid.ToVector3xz());

		private Vector3[] VerticesToWorldSpace(Matrix4x4 matrixTRS) =>
			vertices.Select(v => matrixTRS.MultiplyPoint3x4(v.ToVector3xz())).ToArray();

		#region TESTS

		// TEST Point is inside Polygon
		// Using Cross Product
		// Only works on CONVEX polygons
		public bool Contains_CrossProd(Vector2 point)
		{
			// TEST Point LEFT for each Edge
			for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
			{
				Vector2 a = vertices[j], b = vertices[i];
				RelativePos relativePos = PointRelativePos(a, b, point);
				if (relativePos != RelativePos.LEFT) return false;
			}

			return true;
		}

		private enum RelativePos { LEFT, RIGHT, ON }

		private RelativePos PointRelativePos(Vector2 a, Vector2 b, Vector2 point)
		{
			Vector2 ab = b - a, ap = point - a;

			// Cross Product
			float cross = ab.x * ap.y - ab.y * ap.x;

			// AB x AP > 0 => RIGHT => OUTSIDE
			return cross > 0 ? RelativePos.RIGHT :
				cross < 0 ? RelativePos.LEFT :
				RelativePos.ON;
		}

		// TEST Point is inside Polygon
		// Uses RayCasting
		// It is less efficient but works on CONCAVE polygons
		public bool Contains_RayCast(Vector2 point)
		{
			var contains = false;
			for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
			{
				Vector2 a = vertices[j], b = vertices[i];
				if (b.y > point.y != a.y > point.y &&
				    point.x < (a.x - b.x) * (point.y - b.y) / (a.y - b.y) + b.x)
					contains = !contains;
			}

			return contains;
		}

		#endregion


#if UNITY_EDITOR

		#region DEBUG

		public void OnDrawGizmosWire(
			Matrix4x4 mTRS, float scale, float thickness = 1, Color color = default, bool projectOnTerrain = false
		)
		{
			if (vertices == null || vertices.Length == 0) return;

			Vector3 centroidInWorld = ToWorldSpace(mTRS);
			Vector3[] verticesInWorld = VerticesToWorldSpace(mTRS)
				.Select(v => centroidInWorld + (v - centroidInWorld) * scale)
				.ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygonWire_OnTerrain(verticesInWorld, thickness, color);
			else
				GizmosExtensions.DrawPolygonWire(verticesInWorld, thickness, color);
		}

		public void OnDrawGizmos(Matrix4x4 mTRS, float scale, Color color = default, bool projectOnTerrain = false)
		{
			if (vertices == null || vertices.Length == 0) return;

			Vector3 centroidInWorld = ToWorldSpace(mTRS);
			Vector3[] verticesInWorld = VerticesToWorldSpace(mTRS)
				.Select(v => centroidInWorld + (v - centroidInWorld) * scale)
				.ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygon_OnTerrain(verticesInWorld, color);
			else
				GizmosExtensions.DrawPolygon(verticesInWorld, color);
		}

		#endregion

#endif
	}
}
