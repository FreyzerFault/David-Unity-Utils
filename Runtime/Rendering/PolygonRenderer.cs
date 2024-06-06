using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using DavidUtils.TerrainExtensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PolygonRenderer : DynamicRenderer<Polygon[]>
	{
		protected override string DefaultChildName => "Polygon";

		// LINE
		protected Transform lineParent;
		protected readonly List<LineRenderer> lineRenderers = new();

		// MESH
		protected Transform meshParent;
		protected readonly List<MeshRenderer> meshRenderers = new();
		protected readonly List<MeshFilter> meshFilters = new();

		[SerializeField] private bool wire;
		public bool Wire
		{
			get => wire;
			set
			{
				wire = value;
				lineParent.gameObject.SetActive(wire);
				meshParent.gameObject.SetActive(!wire);
			}
		}

		// SCALE SLIDER
		[SerializeField] [Range(.2f, 1)]
		public float centeredScale = .9f;

		private void Awake() => Initialize();

		public void Initialize()
		{
			if (lineParent == null)
				lineParent = UnityUtils.InstantiateEmptyObject(transform, "Lines").transform;
			if (meshParent == null)
				meshParent = UnityUtils.InstantiateEmptyObject(transform, "Meshes").transform;

			lineParent.gameObject.SetActive(wire);
			meshParent.gameObject.SetActive(!wire);

			InitializeSpetialRenderers();
		}

		public override void Instantiate(Polygon[] points, string childName = null)
		{
			if (lineRenderers.Count != 0 || meshRenderers.Count != 0) Clear();

			if (colors.Length != points.Length) SetRainbowColors(points.Length);

			for (var i = 0; i < points.Length; i++)
			{
				Polygon polygon = points[i];
				InstatiatePolygon(polygon, colors[i], childName);
			}
		}

		/// <summary>
		///     Update ALL Renderers
		///     Si hay mas Regions que Renderers, instancia nuevos
		///     Elimina los Renderers sobrantes
		/// </summary>
		public override void UpdateGeometry(Polygon[] points)
		{
			if (points.Length != colors.Length) SetRainbowColors(points.Length);

			for (var i = 0; i < points.Length; i++)
			{
				Polygon region = points[i];
				UpdatePolygon(region, i);
			}

			int removeCount = meshFilters.Count - points.Length;
			if (removeCount <= 0) return;

			// Elimina los Renderers sobrantes
			for (int i = points.Length; i < meshFilters.Count; i++)
			{
				Destroy(meshFilters[i].gameObject);
				Destroy(lineRenderers[i].gameObject);
			}

			meshFilters.RemoveRange(points.Length, removeCount);
			meshRenderers.RemoveRange(points.Length, removeCount);
			lineRenderers.RemoveRange(points.Length, removeCount);
		}

		public override void Clear()
		{
			base.Clear();

			if (meshFilters == null) return;
			for (var i = 0; i < meshFilters.Count; i++)
			{
				UnityUtils.DestroySafe(meshFilters[i]);
				UnityUtils.DestroySafe(lineRenderers[i]);
			}

			if (lineRenderers == null || meshFilters == null || meshRenderers == null) return;
			lineRenderers.Clear();
			meshFilters.Clear();
			meshRenderers.Clear();
		}

		private void InstatiatePolygon(Polygon polygon, Color color, string polygonName = null)
		{
			// LINE
			lineRenderers.Add(polygon.ToLineRenderer(lineParent, $"{polygonName}", color));

			// MESH
			polygon.InstantiateMesh(
				out MeshRenderer mr,
				out MeshFilter mf,
				meshParent,
				$"{polygonName ?? DefaultChildName}",
				color
			);
			meshRenderers.Add(mr);
			meshFilters.Add(mf);
		}

		public void UpdatePolygon(Polygon polygon, int i)
		{
			Polygon scaledPolygon = polygon.ScaleByCenter(centeredScale);
			if (i >= meshRenderers.Count)
			{
				SetRainbowColors(i + 1);
				InstatiatePolygon(scaledPolygon, colors[i], DefaultChildName);
			}
			else
			{
				meshFilters[i].sharedMesh.SetPolygon(scaledPolygon);
				lineRenderers[i].SetPolygon(scaledPolygon);
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
			hightlightedRegionLineRenderer.SetPolygon(region.ScaleByCenter(centeredScale));

		public void SetSelectedRegion(Polygon region) =>
			selectedRegionLineRenderer.SetPolygon(region.ScaleByCenter(centeredScale));

		public void ToggleHightlighted(bool toggle) => hightlightedRegionLineRenderer.gameObject.SetActive(toggle);
		public void ToggleSelected(bool toggle) => selectedRegionLineRenderer.gameObject.SetActive(toggle);

		private void InitializeSpetialRenderers()
		{
			// SELECTED & HIGHTLIGHTED (hovered)
			if (hightlightedRegionLineRenderer == null)
				hightlightedRegionLineRenderer = LineRendererExtensions.ToLineRenderer(
					transform,
					"Hightlighted Region",
					colors: new[] { Color.yellow },
					loop: true
				);
			if (selectedRegionLineRenderer == null)
				selectedRegionLineRenderer = LineRendererExtensions.ToLineRenderer(
					transform,
					"Selected Region",
					colors: new[] { Color.yellow },
					thickness: .2f,
					loop: true
				);
		}

		#endregion
	}
}
