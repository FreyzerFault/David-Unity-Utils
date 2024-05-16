using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public static class ObjectGenerator
	{
		public const float DEFAULT_THICKNESS = .1f;
		public const int DEFAULT_SMOOTHNESS = 5;
		
		#region BASE OBJECTS

		public static GameObject InstantiateEmptyObject(Transform parent, string name = "New Object")
		{
			var obj = new GameObject($"Line{(name == "" ? "" : " - ")}{name}");
			obj.transform.parent = parent;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localRotation = Quaternion.identity;
			obj.transform.localScale = Vector3.one;
			return obj;
		}

		#endregion
		
		#region LINE SHAPES
		
		public static LineRenderer InstantiateTriangleWire(
			Transform parent, Triangle triangle, Color color = default,
			float thickness = DEFAULT_THICKNESS, bool XZplane = true
		) => new Polyline(triangle.Vertices, new[] { color }, thickness, DEFAULT_SMOOTHNESS, true, XZplane)
			.Instantiate(parent, "Triangle");

		public static LineRenderer InstantiatePolygonWire(
			Transform parent, Polygon polygon, float centeredScale = 1, Color color = default,
			float thickness = DEFAULT_THICKNESS,
			bool XZplane = true
		) => new Polyline(
				polygon.VerticesScaledByCenter(centeredScale), 
				new[] { color }, thickness, DEFAULT_SMOOTHNESS, true, XZplane
			).Instantiate(parent, "Polygon");

		#endregion


		#region MESH SHAPES
		
		
		public static void InstantiateMeshRenderer(
			Mesh mesh, Transform parent, out MeshRenderer mr, out MeshFilter mf, string name = ""
		)
		{
			// LINE RENDERER
			var mObj = new GameObject($"Mesh{(name == "" ? "" : " - " + name)}");
			mObj.transform.parent = parent;
			mObj.transform.localPosition = Vector3.zero;
			mObj.transform.localRotation = Quaternion.identity;
			mObj.transform.localScale = Vector3.one;

			mr = mObj.AddComponent<MeshRenderer>();
			mf = mObj.AddComponent<MeshFilter>();

			// Find Default Material
			mr.sharedMaterial = Resources.Load<Material>("Materials/Geometry Lit");

			mf.sharedMesh = mesh;
		}

		public static Mesh CreateMesh(Triangle[] triangles, bool XZplane = true, Color[] colors = null)
		{
			var mesh = new Mesh();

			var vertices = new Vector3[triangles.Length * 3];
			var indices = new int[triangles.Length * 3];
			for (var i = 0; i < triangles.Length; i++)
			{
				Triangle t = triangles[i];
				vertices[i * 3 + 0] = XZplane ? t.v3.ToV3xz() : t.v3.ToV3xy();
				vertices[i * 3 + 1] = XZplane ? t.v2.ToV3xz() : t.v2.ToV3xy();
				vertices[i * 3 + 2] = XZplane ? t.v1.ToV3xz() : t.v1.ToV3xy();
			}

			mesh.vertices = vertices;

			mesh.triangles = indices.Select((_, index) => index).ToArray();

			var normals = new Vector3[triangles.Length * 3];
			Array.Fill(normals, XZplane ? Vector3.up : Vector3.back);
			mesh.normals = normals;

			// COLOR => Duplicar cada color x3 para asignarlo a cada vertice correctamente
			if (colors != null)
				mesh.colors = colors.SelectMany(c => new[] { c, c, c }).ToArray();

			mesh.bounds = triangles.SelectMany(t => t.Vertices)
				.Select(p => XZplane ? p.ToV3xz() : p.ToV3xy())
				.ToArray()
				.GetBoundingBox();

			return mesh;
		}

		// DELAUNAY TRIANGULATION
		public static Mesh CreateMesh_Delaunay(Vector2[] points, bool XZplane = true, Color? color = null)
		{
			var delaunay = new Delaunay(points);
			delaunay.RunTriangulation();
			Triangle[] tris = delaunay.triangles.ToArray();

			return CreateMesh(tris, XZplane, color.HasValue ? new[] { color.Value } : null);
		}

		public static void InstatiateTriangle(
			Triangle tri, Transform parent, out MeshRenderer mr, out MeshFilter mf,
			Color color = default
		) =>
			InstantiateMeshRenderer(CreateMesh(new[] { tri }, true, new [] { color }), parent, out mr, out mf, "Triangle");

		public static void InstantiatePolygon_DelaunayTriangulation(
			Polygon polygon, Transform parent, out MeshRenderer mr, out MeshFilter mf, float centeredScale = .9f,
			Color color = default
		) => InstantiateMeshRenderer(
			CreateMesh_Delaunay(polygon.VerticesScaledByCenter(centeredScale), true, color),
			parent,
			out mr,
			out mf,
			"Polygon"
		);

		public static void InstantiatePolygon(
			Polygon polygon, Transform parent, out MeshRenderer mr, out MeshFilter mf, float centeredScale = .9f,
			Color color = default
		) => InstantiateMeshRenderer(
			Triangle.CreateMesh(polygon.Triangulate(centeredScale), color),
			parent,
			out mr,
			out mf,
			"Polygon"
		);

		#endregion
	}
}
