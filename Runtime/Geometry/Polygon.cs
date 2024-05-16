using System;
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

		public Polygon(Vector2[] vertices, Vector2 centroid = default)
		{
			this.vertices = vertices ?? Array.Empty<Vector2>();
			this.centroid = centroid;
		}

		private Vector3 ToWorldSpace(Matrix4x4 matrixTRS) => matrixTRS.MultiplyPoint3x4(centroid.ToV3xz());

		private Vector3[] VerticesToWorldSpace(Matrix4x4 matrixTRS) =>
			vertices.Select(v => matrixTRS.MultiplyPoint3x4(v.ToV3xz())).ToArray();


		public Vector2[] VerticesScaledByCenter(float centeredScale)
		{
			Vector2 c = centroid;
			return vertices.Select(v => c + (v - c) * centeredScale).ToArray();
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

		#region MESH GENERATION

		public Triangle[] Triangulate(float centeredScale = .9f)
		{
			Vector2[] scaledVertices = VerticesScaledByCenter(centeredScale);

			var tris = new Triangle[vertices.Length];
			for (var i = 0; i < vertices.Length; i++)
				tris[i] = new Triangle(scaledVertices[i], scaledVertices[(i + 1) % vertices.Length], centroid);
			return tris;
		}

		public Mesh CreateMesh(float centeredScale = .9f, Color color = default) => Triangle.CreateMesh(Triangulate(centeredScale), color);

		public void Instantiate(
			Transform parent, out MeshRenderer mr, out MeshFilter mf, float centeredScale = .9f, Color color = default, string name = "Mesh"
		) =>
			ObjectGenerator.InstantiateMeshRenderer(CreateMesh(centeredScale, color), parent, out mr, out mf, name);

		public LineRenderer InstantiateLineRenderer(
			Transform parent, float centeredScale = .9f, Color color = default, float thickness = Polyline.DEFAULT_THICKNESS, int smoothness = Polyline.DEFAULT_SMOOTHNESS, string name = "Triangle Line"
			)
		{
			Polyline line = new Polyline(VerticesScaledByCenter(centeredScale), new[] { color }, thickness, smoothness, true, true);
			return line.Instantiate(parent, name);
		}

		#endregion

		#region DEBUG
#if UNITY_EDITOR


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

#endif
		#endregion

		public bool Equals(Polygon other) => Equals(vertices, other.vertices) && centroid.Equals(other.centroid);

		public override bool Equals(object obj) => obj is Polygon other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(vertices, centroid);
	}
}
