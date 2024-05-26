using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using DavidUtils.TerrainExtensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class Triangles2DRenderer : DynamicRenderer<Triangle[]>
	{
		// MESH
		protected MeshRenderer meshRenderer;
		protected MeshFilter meshFilter;

		// LINE
		protected Transform lineParent;
		protected List<LineRenderer> lineRenderers = new();

		protected LineRenderer borderLine;

		public bool wire;

		protected override Material Material => Resources.Load<Material>("Materials/Geometry Lit");
		protected override string DefaultName => "Triangles 2D Renderer";
		protected override string DefaultChildName => "Triangle 2D";

		public override void Initialize(Transform parent, string name = null)
		{
			base.Initialize(parent, name);

			// Line PARENT for Line Renderers
			lineParent = UnityUtils.InstantiateEmptyObject(RenderParent, "Lines").transform;

			// MESH RENDERER
			MeshRendererExtensions.InstantiateMeshRenderer(
				out meshRenderer,
				out meshFilter,
				new Mesh(),
				RenderParent,
				"Meshes"
			);

			// BORDER LINE RENDERER
			Color borderColor = Color.red;
			var borderThickness = .2f;
			borderLine = LineRendererExtensions.ToLineRenderer(
				RenderParent,
				"Border Line",
				null,
				new[] { borderColor },
				borderThickness,
				loop: true
			);
			borderLine.transform.Translate(Vector3.up * .1f);

			UpdateVisibility();
		}

		public override void Instantiate(Triangle[] points, string childName = null)
		{
			if (lineRenderers.Count != 0) Clear();

			if (points.Length != colors.Length) SetRainbowColors(points.Length);

			InstantiateMesh(points);
		}

		public override void Update(Triangle[] points)
		{
			if (!active) return;

			if (points.Length != colors.Length) SetRainbowColors(points.Length);

			InstantiateMesh(points);
			UpdateBorderLine(points);

			// LINE
			for (var i = 0; i < points.Length; i++) UpdateTri(points[i], i);

			// Elimina los Renderers sobrantes
			int removeCount = lineRenderers.Count - points.Length;
			if (removeCount <= 0) return;

			for (int i = points.Length; i < lineRenderers.Count; i++)
				Object.Destroy(lineRenderers[i].gameObject);

			lineRenderers.RemoveRange(points.Length, removeCount);
		}

		public override void Clear()
		{
			foreach (LineRenderer t in lineRenderers)
				UnityUtils.DestroySafe(t);

			lineRenderers.Clear();
			meshFilter.sharedMesh.Clear();
			borderLine.positionCount = 0;
		}

		public override void UpdateVisibility()
		{
			base.UpdateVisibility();
			lineParent.gameObject.SetActive(wire);
			meshRenderer.gameObject.SetActive(!wire);
		}


		#region MESH

		private void InstantiateMesh(Triangle[] tris) =>
			meshFilter.sharedMesh = tris.CreateMesh(colors.ToArray());

		// Si cambian los triangulos minimamente, se instancia de cero el Mesh
		public void UpdateMesh(Triangle[] tris) => InstantiateMesh(tris);

		#endregion


		#region RENDERING 1 TRIANGLE

		public void InstatiateLineRenderer(Triangle triangle, Color color, string lineName = null) =>
			lineRenderers.Add(triangle.ToLineRenderer(lineParent, lineName ?? DefaultChildName, color: color));

		// Update Triangle Line Renderer (if no Renderer, instantiate it)
		public void UpdateTri(Triangle triangle, int i)
		{
			if (i >= lineRenderers.Count)
			{
				SetRainbowColors(i + 1);
				InstatiateLineRenderer(triangle, Color.white);
			}
			else
			{
				lineRenderers[i].SetPoints(triangle.Vertices3D_XZ.ToArray());
			}
		}

		#endregion


		#region BORDER

		public void UpdateBorderLine(Triangle[] tris)
		{
			Vector2[] points = tris.SelectMany(t => t.BorderEdges.Select(e => e.begin)).ToArray();
			points = points.SortByAngle(points.Center());
			borderLine.SetPoints(points.ToV3xz().ToArray());
		}

		#endregion


		#region PROJECTION ON TERRAIN

		public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f)
		{
			terrain.ProjectMeshInTerrain(meshFilter.sharedMesh, meshFilter.transform, offset);

			foreach (LineRenderer lr in lineRenderers)
				lr.SetPoints(lr.GetPoints().Select(p => terrain.Project(p, offset)).ToArray());
		}

		#endregion
	}
}
