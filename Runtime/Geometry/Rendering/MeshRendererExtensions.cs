using DavidUtils.Geometry.MeshExtensions;
using UnityEngine;

namespace DavidUtils.Geometry.Rendering
{
	public static class MeshRendererExtensions
	{
		#region MESH => MESH RENDERER

		public static void InstantiateMeshRenderer(
			this Mesh mesh,
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "Mesh"
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

		public static void InstantiateMeshRenderer(
			out MeshRenderer mr,
			out MeshFilter mf,
			Mesh mesh = default,
			Transform parent = null,
			string name = "Mesh"
		) => mesh.InstantiateMeshRenderer(out mr, out mf, parent, name);

		#endregion

		#region TRIANGLE & Tri-SHAPES => MESH RENDERER

		// SINGLE TRIANGLE
		public static void InstantiateMesh(
			this Triangle triangle,
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "Triangle Mesh",
			Color color = default,
			bool XZplane = true
		) => InstantiateMeshRenderer(out mr, out mf, triangle.CreateMesh(color, XZplane), parent, name);

		// TRIANGLES
		public static void InstantiateMesh(
			this Triangle[] triangles,
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "Triangle Mesh",
			Color[] colors = default,
			bool XZplane = true
		) => InstantiateMeshRenderer(out mr, out mf, triangles.CreateMesh(colors, XZplane), parent, name);

		// POLYGON
		public static void InstantiateMesh(
			this Polygon polygon,
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "Polygon Mesh",
			Color color = default,
			bool XZplane = true
		) => InstantiateMeshRenderer(out mr, out mf, polygon.CreateMesh(color, XZplane), parent, name);

		#endregion

		#region PLANE MESH => MESH RENDERER

		// PLANE
		public static void InstantiateMeshPlane(
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "Plane",
			float resolution = 10,
			Vector2 size = default
		) => InstantiateMeshRenderer(out mr, out mf, MeshGeneration.GenerateMeshPlane(resolution, size), parent, name);

		#endregion
	}
}
