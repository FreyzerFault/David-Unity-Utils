using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using DavidUtils.TerrainExtensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class Triangles2DRenderer : DynamicRenderer<Triangle[]>
	{
		protected override string DefaultChildName => "Triangle 2D";
		protected override Material Material => Resources.Load<Material>("Materials/Geometry Lit");

		// MESH
		protected MeshRenderer meshRenderer;
		protected MeshFilter meshFilter;

		// LINE
		protected Transform lineParent;
		protected List<LineRenderer> lineRenderers = new();

		protected LineRenderer borderLine;

		public float thickness = .1f;

		[SerializeField] private bool wire;
		public bool Wire
		{
			get => wire;
			set
			{
				wire = value;
				lineParent.gameObject.SetActive(wire);
				meshRenderer.gameObject.SetActive(!wire);
			}
		}

		private void Awake() => Initialize();

		public void Initialize()
		{
			// Line PARENT for Line Renderers
			if (lineParent == null)
				lineParent = UnityUtils.InstantiateEmptyObject(transform, "Lines").transform;

			// MESH RENDERER
			if (meshRenderer == null)
				MeshRendererExtensions.InstantiateMeshRenderer(
					out meshRenderer,
					out meshFilter,
					new Mesh(),
					transform,
					"Mesh"
				);

			lineParent.gameObject.SetActive(wire);
			meshRenderer.gameObject.SetActive(!wire);

			// BORDER LINE RENDERER
			if (borderLine == null)
				InitializeBorderLine();
		}

		private void InitializeBorderLine()
		{
			borderLine = LineRendererExtensions.ToLineRenderer(
				transform,
				"Border Line",
				null,
				new[] { Color.red },
				thickness * 2,
				loop: true
			);
			borderLine.transform.Translate(Vector3.up * .1f);
		}

		public override void Instantiate(Triangle[] points, string childName = null)
		{
			if (lineRenderers.Count != 0) Clear();

			if (points.Length != colors.Length) SetRainbowColors(points.Length);

			InstantiateMesh(points);
		}

		public override void UpdateGeometry(Triangle[] points)
		{
			if (points.Length == 0) return;

			if (points.Length != colors.Length) SetRainbowColors(points.Length);

			InstantiateMesh(points);
			UpdateBorderLine(points);

			// LINE
			for (var i = 0; i < points.Length; i++) UpdateTri(points[i], i);

			// Elimina los Renderers sobrantes
			int removeCount = lineRenderers.Count - points.Length;
			if (removeCount <= 0) return;

			for (int i = points.Length; i < lineRenderers.Count; i++)
				Destroy(lineRenderers[i].gameObject);

			lineRenderers.RemoveRange(points.Length, removeCount);
		}

		public override void Clear()
		{
			base.Clear();

			if (lineRenderers == null) ;

			foreach (LineRenderer t in lineRenderers)
				UnityUtils.DestroySafe(t);

			if (lineRenderers == null || meshFilter == null || borderLine == null) return;

			lineRenderers.Clear();
			meshFilter.sharedMesh.Clear();
			borderLine.positionCount = 0;
		}


		#region MESH

		private void InstantiateMesh(Triangle[] tris) =>
			meshFilter.sharedMesh = tris.CreateMesh(colors.ToArray());

		// Si cambian los triangulos minimamente, se instancia de cero el Mesh
		public void UpdateMesh(Triangle[] tris) => InstantiateMesh(tris);

		#endregion


		#region RENDERING 1 TRIANGLE

		public void InstatiateLineRenderer(Triangle triangle, Color color, string lineName = null) =>
			lineRenderers.Add(triangle.ToLineRenderer(lineParent, lineName ?? DefaultChildName, color, thickness));

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
