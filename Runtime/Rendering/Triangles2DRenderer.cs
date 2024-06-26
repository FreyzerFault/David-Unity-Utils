using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
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

		public override void SetGeometry(Triangle[] inGeometry, string childName = null)
		{
			if (lineRenderers.Count != 0) Clear();

			if (inGeometry.Length != colors.Length) SetRainbowColors(inGeometry.Length);

			SetMesh(inGeometry);
		}

		public override void UpdateGeometry(Triangle[] inGeometry)
		{
			if (inGeometry.IsNullOrEmpty()) return;

			if (inGeometry.Length != colors.Length) SetRainbowColors(inGeometry.Length);

			SetMesh(inGeometry);
			UpdateBorderLine(inGeometry);

			// LINE
			for (var i = 0; i < inGeometry.Length; i++) UpdateTri(inGeometry[i], i);

			// Elimina los Renderers sobrantes
			int removeCount = lineRenderers.Count - inGeometry.Length;
			if (removeCount <= 0) return;

			for (int i = inGeometry.Length; i < lineRenderers.Count; i++)
				Destroy(lineRenderers[i].gameObject);

			lineRenderers.RemoveRange(inGeometry.Length, removeCount);
		}

		public override void Clear()
		{
			base.Clear();

			foreach (LineRenderer t in lineRenderers)
				UnityUtils.DestroySafe(t);

			if (lineRenderers == null || meshFilter == null || borderLine == null) return;

			lineRenderers.Clear();
			meshFilter.sharedMesh.Clear();
			borderLine.positionCount = 0;
		}


		#region MESH

		private void SetMesh(Triangle[] tris) => meshFilter.sharedMesh = tris.CreateMesh(colors?.ToArray());

		// Si cambian los triangulos minimamente, se instancia de cero el Mesh
		public void UpdateMesh(Triangle[] tris) => SetMesh(tris);

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
				lineRenderers[i].SetPoints(triangle.Vertices);
			}
		}

		#endregion


		#region BORDER

		private void InitializeBorderLine() => borderLine = LineRendererExtensions.ToLineRenderer(
			transform,
			"Border Line",
			null,
			new[] { Color.red },
			thickness * 2,
			loop: true
		);

		public void UpdateBorderLine(Triangle[] tris)
		{
			Vector2[] points = tris.SelectMany(t => t.BorderEdges.Select(e => e.begin)).ToArray();
			points = points.SortByAngle(points.Center());
			borderLine.SetPoints(points);
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
