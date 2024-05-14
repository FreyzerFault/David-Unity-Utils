using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public static class ShapeDrawing
	{
		private const float DEFAULT_THICKNESS = .1f;

		#region LINE SHAPES

		public static LineRenderer InstantiateLine(
			Transform parent, Vector2[] points, Color[] colors = null, float thickness = DEFAULT_THICKNESS,
			bool XZplane = true, string name = ""
		)
		{
			// COLORS
			if (colors == null || colors.Length == 0) colors = new[] { Color.gray };
			Color color = colors[0];

			// LINE RENDERER
			var line = new GameObject($"Line{(name == "" ? "" : " - ")}{name}");
			line.transform.parent = parent;

			var lr = line.AddComponent<LineRenderer>();

			// Line Material
			lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

			// SET COLORS
			if (colors.Length > 1)
				lr.colorGradient = colors.ToGradient();
			else
				lr.startColor = lr.endColor = color;

			// THICKNESS
			lr.widthMultiplier = thickness;

			// POINTS
			lr.positionCount = points.Length;
			Vector3[] positions = points.Select(p => parent.TransformPoint(XZplane ? p.ToV3xz() : p.ToV3xy()))
				.ToArray();
			lr.SetPositions(positions);

			// Smoothness
			lr.numCapVertices = 5;
			lr.numCornerVertices = 5;

			return lr;
		}

		public static LineRenderer InstantiateTriangleWire(
			Transform parent, Delaunay.Triangle triangle, Color color = default,
			float thickness = DEFAULT_THICKNESS, bool XZplane = true
		)
		{
			LineRenderer lr = InstantiateLine(
				parent,
				triangle.Vertices,
				new[] { color },
				thickness,
				XZplane,
				"Triangle"
			);
			lr.loop = true;
			return lr;
		}

		public static LineRenderer InstantiatePolygonWire(
			Transform parent, Polygon polygon, float centeredScale = 1, Color color = default,
			float thickness = DEFAULT_THICKNESS,
			bool XZplane = true
		)
		{
			LineRenderer lr = InstantiateLine(
				parent,
				polygon.VerticesScaledByCenter(centeredScale),
				new[] { color },
				thickness,
				XZplane,
				"Polygon"
			);
			lr.loop = true;
			return lr;
		}

		#endregion


		#region MESH SHAPES

		public static void InstatiateTriangle(
			Delaunay.Triangle tri, Transform parent, out MeshRenderer mr, out MeshFilter mf,
			Color color = default
		) =>
			InstantiateMeshRenderer(CreateMesh(new[] { tri }), parent, out mr, out mf, color, "Triangle");

		public static void InstantiatePolygon_DelaunayTriangulation(
			Polygon polygon, Transform parent, out MeshRenderer mr, out MeshFilter mf, float centeredScale = .9f,
			Color color = default
		) => InstantiateMeshRenderer(
			CreateMesh_Delaunay(polygon.VerticesScaledByCenter(centeredScale)),
			parent,
			out mr,
			out mf,
			color,
			"Polygon"
		);

		public static void InstantiatePolygon(
			Polygon polygon, Transform parent, out MeshRenderer mr, out MeshFilter mf, float centeredScale = .9f,
			Color color = default
		) => InstantiateMeshRenderer(
			Delaunay.Triangle.CreateMesh(polygon.Triangulate(centeredScale), color),
			parent,
			out mr,
			out mf,
			color,
			"Polygon"
		);

		public static void InstantiateMeshRenderer(
			Mesh mesh, Transform parent, out MeshRenderer mr, out MeshFilter mf, Color color = default, string name = ""
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

		public static Mesh CreateMesh(Delaunay.Triangle[] triangles, bool XZplane = true, Color[] colors = null)
		{
			var mesh = new Mesh();

			mesh.vertices = new Vector3[triangles.Length * 3];
			var indices = new int[triangles.Length * 3];
			mesh.triangles = indices.Select((_, index) => index).ToArray();
			for (var i = 0; i < triangles.Length; i++)
			{
				Delaunay.Triangle t = triangles[i];
				mesh.vertices[i * 3 + 0] = XZplane ? t.v3.ToV3xz() : t.v3.ToV3xy();
				mesh.vertices[i * 3 + 1] = XZplane ? t.v2.ToV3xz() : t.v2.ToV3xy();
				mesh.vertices[i * 3 + 2] = XZplane ? t.v1.ToV3xz() : t.v1.ToV3xy();
			}

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
			delaunay.Run();
			Delaunay.Triangle[] tris = delaunay.triangles.ToArray();

			return CreateMesh(tris, XZplane, color.HasValue ? new[] { color.Value } : null);
		}

		#endregion
	}
}
