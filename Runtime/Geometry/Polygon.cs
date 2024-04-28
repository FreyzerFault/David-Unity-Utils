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
		

		#region DEBUG
		#if UNITY_EDITOR

		public void OnDrawGizmosWire(Matrix4x4 mTRS, float margin = 0, float thickness = 1, Color color = default)
		{
			if (vertices == null || vertices.Length == 0) return;

			Vector3[] verticesInWorld = VerticesToWorldSpace(mTRS);
			Vector3 centroidInWorld = ToWorldSpace(mTRS);
			if (margin != 0) 
				verticesInWorld = verticesInWorld.Select(v => v + (centroidInWorld - v).normalized * margin).ToArray();

			GizmosExtensions.DrawPolygonWire(verticesInWorld, thickness, color);
			DrawGizmosCentroid(centroidInWorld);
		}

		public void OnDrawGizmos(Matrix4x4 mTRS, float margin = 0, Color color = default)
		{
			if (vertices == null || vertices.Length == 0) return;
			
			Vector3[] verticesInWorld = VerticesToWorldSpace(mTRS);
			Vector3 centroidInWorld = ToWorldSpace(mTRS);
			if (margin != 0) 
				verticesInWorld = verticesInWorld.Select(v => v + (centroidInWorld - v).normalized * margin).ToArray();

			GizmosExtensions.DrawPolygon(verticesInWorld, color);
			DrawGizmosCentroid(centroidInWorld);
		}

		private void DrawGizmosCentroid(Vector3 pos)
		{
			Gizmos.color = Color.grey;
			Gizmos.DrawSphere(pos, 0.1f);
		}
		
		private Vector3 ToWorldSpace(Matrix4x4 matrixTRS) => matrixTRS.MultiplyPoint3x4(centroid.ToVector3xz());
		
		private Vector3[] VerticesToWorldSpace(Matrix4x4 matrixTRS) => 
			vertices.Select(v => matrixTRS.MultiplyPoint3x4(v.ToVector3xz())).ToArray();

		#endif
		#endregion
	}
}
