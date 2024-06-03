using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public struct Polygon : IEquatable<Polygon>
	{
		// Vertices in Counter-Clockwise order
		public Vector2[] vertices;
		public Vector2 centroid;

		public int VextexCount => vertices.Length;

		public Vector3[] Vertices3D_XZ => vertices.Select(v => v.ToV3xz()).ToArray();
		public Vector3[] Vertices3D_XY => vertices.Select(v => v.ToV3xy()).ToArray();

		public Polygon(IEnumerable<Vector2> vertices, Vector2 centroid)
		{
			this.vertices = vertices?.ToArray() ?? Array.Empty<Vector2>();
			this.centroid = centroid;
		}

		public Polygon(IEnumerable<Vector2> vertices) : this(vertices, default) => UpdateCentroid();

		public void UpdateCentroid() => centroid = vertices.Center();

		public Vector3 GetCentroidInWorld(Matrix4x4 matrixTRS) => matrixTRS.MultiplyPoint3x4(centroid.ToV3());

		public Vector3[] GetVerticesInWorld(Matrix4x4 matrixTRS) =>
			vertices.Select(v => matrixTRS.MultiplyPoint3x4(v.ToV3())).ToArray();

		public Vector2[] VerticesScaledByCenter(float centeredScale)
		{
			Vector2 c = centroid;
			return vertices.Select(v => c + (v - c) * centeredScale).ToArray();
		}

		public Polygon ScaleByCenter(float centeredScale) =>
			Mathf.Approximately(centeredScale, 1) ? this : new Polygon(VerticesScaledByCenter(centeredScale), centroid);

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

		#region MESH

		/// <summary>
		///     Triangula creando un triangulo por arista, siendo el centroide el tercer vertice
		/// </summary>
		public Triangle[] Triangulate()
		{
			var tris = new Triangle[vertices.Length];
			for (var i = 0; i < vertices.Length; i++)
				tris[i] = new Triangle(vertices[i], vertices[(i + 1) % vertices.Length], centroid);
			return tris;
		}

		#endregion

		#region DEBUG

#if UNITY_EDITOR


		public void OnDrawGizmosWire(
			Matrix4x4 mTRS, float thickness = 1, Color color = default, bool projectOnTerrain = false
		)
		{
			if (vertices == null || vertices.Length == 0) return;

			Vector3[] verticesInWorld = GetVerticesInWorld(mTRS);

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygonWire_OnTerrain(verticesInWorld, thickness, color);
			else
				GizmosExtensions.DrawPolygonWire(verticesInWorld, thickness, color);
		}

		public void OnDrawGizmos(Matrix4x4 mTRS, Color color = default, bool projectOnTerrain = false)
		{
			if (vertices == null || vertices.Length == 0) return;

			// Vector3 centroidInWorld = GetCentroidInWorld(mTRS);
			Vector3[] verticesInWorld = GetVerticesInWorld(mTRS);

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygon_OnTerrain(verticesInWorld, color);
			else
				GizmosExtensions.DrawPolygon(verticesInWorld, color);
		}

#endif

		#endregion

		public bool Equals(Polygon other) => Equals(vertices, other.vertices) && centroid.Equals(other.centroid);

		public override bool Equals(object obj) => obj is Polygon other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(vertices, centroid);
	}
}
