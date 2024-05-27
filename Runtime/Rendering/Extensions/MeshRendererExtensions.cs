using System;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using UnityEngine;

namespace DavidUtils.Rendering.Extensions
{
	public static class MeshRendererExtensions
	{
		private static Material DefaultMaterial => Resources.Load<Material>("Materials/Geometry Unlit");


		#region MESH => MESH RENDERER

		public static void InstantiateMeshRenderer(
			this Mesh mesh,
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = ""
		)
		{
			// LINE RENDERER
			var mObj = new GameObject($"{name} [Mesh]");
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
			string name = ""
		) => mesh.InstantiateMeshRenderer(out mr, out mf, parent, name);

		#endregion


		#region TRIANGLE & Tri-SHAPES => MESH RENDERER

		// SINGLE TRIANGLE
		public static void InstantiateMesh(
			this Triangle triangle,
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "Triangle",
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
			string name = "Polygon",
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


		#region SPHERE

		public static void InstantiateSphere(
			out MeshRenderer mr,
			out MeshFilter mf,
			Transform parent = null,
			string name = "",
			Color color = default,
			Material material = null
		)
		{
			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = $"{name} [Sphere]";
			sphere.transform.parent = parent;

			mr = sphere.GetComponent<MeshRenderer>();
			mf = sphere.GetComponent<MeshFilter>();

			// COLOR
			var colors = new Color[mf.sharedMesh.vertexCount];
			Array.Fill(colors, color);
			mf.sharedMesh.SetColors(colors);

			// MATERIAL
			mr.sharedMaterial = material ?? DefaultMaterial;
		}

		#endregion
	}
}
