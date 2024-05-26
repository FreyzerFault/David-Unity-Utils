using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using DavidUtils.TerrainExtensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PolygonRenderer : DynamicRenderer<Polygon[]>
	{
		// LINE
		protected Transform lineParent;
		protected readonly List<LineRenderer> lineRenderers = new();

		// MESH
		protected Transform meshParent;
		protected readonly List<MeshRenderer> meshRenderers = new();
		protected readonly List<MeshFilter> meshFilters = new();
		
		protected override string DefaultName => "Polygons Renderer";
		protected override string DefaultChildName => "Line";
		
		public bool wire;
		
		// SCALE SLIDER
		[SerializeField] [Range(.2f, 1)]
		public float regionScale = .9f;


		public override void Initialize(Transform parent, string name = null)
		{
			base.Initialize(parent, name);

			lineParent = UnityUtils.InstantiateEmptyObject(RenderParent, "Line Renderers").transform;
			meshParent = UnityUtils.InstantiateEmptyObject(RenderParent, "Mesh Renderers").transform;

			InitializeSpetialRenderers(parent);
			UpdateVisibility();
		}

		public override void Instantiate(Polygon[] points, string childName = null)
		{
			if (lineRenderers.Count != 0 || meshRenderers.Count != 0) Clear();

			if (colors.Length != points.Length) SetRainbowColors(points.Length);

			for (var i = 0; i < points.Length; i++)
			{
				Polygon region = points[i];
				InstatiateRegion(region, colors[i], childName);
			}
		}

		/// <summary>
		///     Update ALL Renderers
		///     Si hay mas Regions que Renderers, instancia nuevos
		///     Elimina los Renderers sobrantes
		/// </summary>
		public override void Update(Polygon[] points)
		{
			if (!active) return;

			if (points.Length != colors.Length) SetRainbowColors(points.Length);

			for (var i = 0; i < points.Length; i++)
			{
				Polygon region = points[i];
				UpdateRegion(region, i);
			}


			int removeCount = meshFilters.Count - points.Length;
			if (removeCount <= 0) return;

			// Elimina los Renderers sobrantes
			for (int i = points.Length; i < meshFilters.Count; i++)
			{
				Object.Destroy(meshFilters[i].gameObject);
				Object.Destroy(lineRenderers[i].gameObject);
			}

			meshFilters.RemoveRange(points.Length, removeCount);
			meshRenderers.RemoveRange(points.Length, removeCount);
			lineRenderers.RemoveRange(points.Length, removeCount);
		}

		public override void Clear()
		{
			for (var i = 0; i < meshFilters.Count; i++)
			{
				UnityUtils.DestroySafe(meshFilters[i]);
				UnityUtils.DestroySafe(lineRenderers[i]);
			}

			lineRenderers.Clear();
			meshFilters.Clear();
			meshRenderers.Clear();
		}

		public override void UpdateVisibility()
		{
			base.UpdateVisibility();
			lineParent.gameObject.SetActive(wire);
			meshParent.gameObject.SetActive(!wire);
		}

		private void InstatiateRegion(Polygon region, Color color, string regionName = null)
		{
			// LINE
			lineRenderers.Add(region.ToLineRenderer(lineParent, color));

			// MESH
			region.InstantiateMesh(out MeshRenderer mr, out MeshFilter mf, meshParent, $"{regionName ?? DefaultChildName}", color);
			meshRenderers.Add(mr);
			meshFilters.Add(mf);
		}

		public void UpdateRegion(Polygon region, int i)
		{
			Polygon scaledRegion = region.ScaleByCenter(regionScale);
			if (i >= meshRenderers.Count)
			{
				SetRainbowColors(i + 1);
				InstatiateRegion(scaledRegion, colors[i]);
			}
			else
			{
				meshFilters[i].sharedMesh.SetPolygon(scaledRegion);
				lineRenderers[i].SetPolygon(scaledRegion);
			}
		}


		#region PROJECTION ON TERRAIN

		public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f)
		{
			foreach (MeshFilter mf in meshFilters)
				terrain.ProjectMeshInTerrain(mf.sharedMesh, mf.transform, offset);

			foreach (LineRenderer lr in lineRenderers)
				lr.SetPoints(lr.GetPoints().Select(p => terrain.Project(p, offset)).ToArray());
		}

		#endregion


		#region REGION SELECTION

		protected LineRenderer hightlightedRegionLineRenderer;
		protected LineRenderer selectedRegionLineRenderer;

		public void SetHightlightedRegion(Polygon region) =>
			hightlightedRegionLineRenderer.SetPolygon(region.ScaleByCenter(regionScale));

		public void SetSelectedRegion(Polygon region) =>
			selectedRegionLineRenderer.SetPolygon(region.ScaleByCenter(regionScale));

		public void ToggleHightlighted(bool toggle) => hightlightedRegionLineRenderer.gameObject.SetActive(toggle);
		public void ToggleSelected(bool toggle) => selectedRegionLineRenderer.gameObject.SetActive(toggle);

		private void InitializeSpetialRenderers(Transform parent)
		{
			// SELECTED & HIGHTLIGHTED (hovered)
			hightlightedRegionLineRenderer = LineRendererExtensions.ToLineRenderer(
				RenderParent,
				"Hightlighted Region",
				colors: new[] { Color.yellow },
				loop: true
			);
			selectedRegionLineRenderer = LineRendererExtensions.ToLineRenderer(
				RenderParent,
				"Selected Region",
				colors: new[] { Color.yellow },
				thickness: .2f,
				loop: true
			);
		}

		#endregion
	}
}
