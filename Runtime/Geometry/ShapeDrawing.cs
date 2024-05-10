using System.Collections.Generic;
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

		public static void InstantiatePolygon_DelaunayTriangulation(
			Polygon polygon, Transform parent, out MeshRenderer mr, out MeshFilter mf, float centeredScale = .9f,
			Color color = default
		) => InstantiateMeshRenderer(
			CreateMesh(polygon.VerticesScaledByCenter(centeredScale)),
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
			var mObj = new GameObject($"Mesh{(name == "" ? "" : " - ")} - {name}");
			mObj.transform.parent = parent;
			mObj.transform.position = Vector3.zero;
			mObj.transform.rotation = Quaternion.identity;
			mObj.transform.localScale = Vector3.one;

			mr = mObj.AddComponent<MeshRenderer>();
			mf = mObj.AddComponent<MeshFilter>();

			// Find Default Material
			mr.sharedMaterial = Resources.Load<Material>("Materials/Geometry Lit");

			mf.sharedMesh = mesh;
		}

		public static Mesh CreateMesh(Vector2[] points, bool XZplane = true)
		{
			var mesh = new Mesh();

			// DELAUNAY TRIANGULATION
			var delaunay = new Delaunay(points);
			delaunay.Run();
			List<Delaunay.Triangle> tris = delaunay.triangles;

			mesh.vertices = new Vector3[tris.Count * 3];
			var indices = new int[tris.Count * 3];
			mesh.triangles = indices.Select((_, index) => index).ToArray();
			for (var i = 0; i < tris.Count; i++)
			{
				Delaunay.Triangle t = tris[i];
				mesh.vertices[i * 3 + 0] = t.v3;
				mesh.vertices[i * 3 + 1] = t.v2;
				mesh.vertices[i * 3 + 2] = t.v1;
			}

			mesh.normals = mesh.vertices.Select(v => XZplane ? Vector3.up : Vector3.back).ToArray();
			mesh.bounds = points.Select(p => XZplane ? p.ToV3xz() : p.ToV3xy()).ToArray().GetBoundingBox();

			return mesh;
		}

		#endregion
	}
}
