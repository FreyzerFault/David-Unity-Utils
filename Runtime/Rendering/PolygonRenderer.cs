using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.TerrainExtensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PolygonRenderer : DynamicRenderer<Polygon[]>
	{
		// LINE
		public Transform lineParent;
		public readonly List<LineRenderer> lineRenderers = new();

		// MESH
		public Transform meshParent;
		public readonly List<MeshRenderer> meshRenderers = new();
		public readonly List<MeshFilter> meshFilters = new();

		[SerializeField] [Range(.2f, 1)]
		public float regionScale = .9f;

		public bool wire;

		public void Initialize(Transform parent)
		{
			base.Initialize(parent);

			lineParent = ObjectGenerator.InstantiateEmptyObject(RenderParent, "Line Renderers").transform;
			meshParent = ObjectGenerator.InstantiateEmptyObject(RenderParent, "Mesh Renderers").transform;

			InitializeSpetialRenderers(parent);
			UpdateVisibility();
		}

		public override void Instantiate(Polygon[] regions)
		{
			if (lineRenderers.Count != 0 || meshRenderers.Count != 0) Clear();

			if (colors.Length != regions.Length) SetRainbowColors(regions.Length);

			for (var i = 0; i < regions.Length; i++)
			{
				Polygon region = regions[i];
				InstatiateRegion(region, colors[i]);
			}
		}

		/// <summary>
		///     Update ALL Renderers
		///     Si hay mas Regions que Renderers, instancia nuevos
		///     Elimina los Renderers sobrantes
		/// </summary>
		public override void Update(Polygon[] regions)
		{
			if (!active) return;

			if (regions.Length != colors.Length) SetRainbowColors(regions.Length);

			for (var i = 0; i < regions.Length; i++)
			{
				Polygon region = regions[i];
				UpdateRegion(region, i);
			}


			int removeCount = meshFilters.Count - regions.Length;
			if (removeCount <= 0) return;

			// Elimina los Renderers sobrantes
			for (int i = regions.Length; i < meshFilters.Count; i++)
			{
				Object.Destroy(meshFilters[i].gameObject);
				Object.Destroy(lineRenderers[i].gameObject);
			}

			meshFilters.RemoveRange(regions.Length, removeCount);
			meshRenderers.RemoveRange(regions.Length, removeCount);
			lineRenderers.RemoveRange(regions.Length, removeCount);
		}

		public override void Clear()
		{
			for (var i = 0; i < meshFilters.Count; i++)
			{
				Object.Destroy(meshFilters[i].gameObject);
				Object.Destroy(lineRenderers[i].gameObject);
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

		private void InstatiateRegion(Polygon region, Color color)
		{
			// LINE
			lineRenderers.Add(region.LineRenderer(lineParent, color));

			// MESH
			region.InstantiateMesh(out MeshRenderer mr, out MeshFilter mf, meshParent, "Region", color);
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


		#region SPETIAL REGIONS

		public LineRenderer hightlightedRegionLineRenderer;
		public LineRenderer selectedRegionLineRenderer;

		public void SetHightlightedRegion(Polygon region) =>
			hightlightedRegionLineRenderer.SetPolygon(region.ScaleByCenter(regionScale));

		public void SetSelectedRegion(Polygon region) =>
			selectedRegionLineRenderer.SetPolygon(region.ScaleByCenter(regionScale));

		public void ToggleHightlighted(bool toggle) => hightlightedRegionLineRenderer.gameObject.SetActive(toggle);
		public void ToggleSelected(bool toggle) => selectedRegionLineRenderer.gameObject.SetActive(toggle);

		private void InitializeSpetialRenderers(Transform parent)
		{
			// SELECTED & HIGHTLIGHTED (hovered)
			hightlightedRegionLineRenderer = LineRendererExtensions.LineRenderer(
				RenderParent,
				"Hightlighted Region",
				colors: new[] { Color.yellow },
				loop: true
			);
			selectedRegionLineRenderer = LineRendererExtensions.LineRenderer(
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
