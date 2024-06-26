using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Geometry;
using Geometry.Algorithms;
using UnityEngine;
using RenderMode = DavidUtils.Rendering.PolygonRenderer.PolygonRenderMode;

namespace DavidUtils.Rendering
{
	public class VoronoiRenderer : DynamicRenderer<Voronoi>
	{
		public Voronoi voronoi;
		private List<PolygonRenderer> _renderers = new();

		[SerializeField] private RenderMode renderMode = RenderMode.Mesh;
		public RenderMode RenderMode
		{
			get => renderMode;
			set
			{
				renderMode = value;
				_renderers.ForEach(r => r.RenderMode = value);
			}
		}

		[SerializeField] private float thickness = PolygonRenderer.DEFAULT_THICKNESS;
		public float Thickness
		{
			get => thickness;
			set
			{
				thickness = value;
				_renderers.ForEach(r => r.Thickness = value);
			}
		}

		[Range(.2f, 1)] [SerializeField]
		private float regionScale = 1;
		public float RegionScale
		{
			get => regionScale;
			set
			{
				regionScale = value;
				_renderers.ForEach(r => r.CenteredScale = value);
			}
		}

		protected override string DefaultChildName => "Region";

		private void OnEnable()
		{
			if (voronoi == null || voronoi.RegionCount == 0) return;

			_renderers.ForEach(r => r.UpdateAllProperties());
		}

		public override void SetGeometry(Voronoi inGeometry, string childName = null)
		{
			voronoi = inGeometry;
			Clear();

			if (voronoi.RegionCount <= 0) return;

			SetRainbowColors(voronoi.RegionCount);
			_renderers = voronoi.regions.Select((r, i) => InstantiateRegion(r, $"Region {i}", colors[i])).ToList();
		}

		public override void UpdateGeometry(Voronoi inGeometry) => throw new NotImplementedException();

		public override void Clear()
		{
			_renderers.ForEach(UnityUtils.DestroySafe);
			_renderers = new List<PolygonRenderer>(voronoi?.SeedCount ?? 0);
		}

		public void UpdateRegions()
		{
			if (_renderers.Count != voronoi.RegionCount)
				SetRainbowColors(voronoi.RegionCount);

			for (var i = 0; i < voronoi.RegionCount; i++)
				SetRegion(i, voronoi.regions[i]);
		}

		public void SetRegion(int i, Polygon region)
		{
			if (i >= 0 && i < _renderers.Count)
			{
				_renderers[i].Polygon = voronoi.regions[i];
			}
			else
			{
				SetRainbowColors(voronoi.RegionCount + 1);
				_renderers.Add(InstantiateRegion(region, $"Region {i}", colors[i]));
			}
		}

		private PolygonRenderer InstantiateRegion(Polygon region, string polygonName = null, Color? color = null) =>
			PolygonRenderer.Instantiate(
				region,
				transform,
				polygonName,
				renderMode,
				color,
				thickness,
				regionScale,
				projectOnTerrain,
				terrainHeightOffset
			);


		#region TERRAIN

		private Terrain Terrain => Terrain.activeTerrain;
		public bool projectOnTerrain;
		public float terrainHeightOffset = 0.1f;

		public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f) =>
			_renderers.ForEach(r => r.ProjectOnTerrain(terrain, offset));

		#endregion


		#region INSTANTIATION

		public static VoronoiRenderer Instantiate(
			Voronoi voronoi,
			Transform parent,
			string name = "Voronoi Renderer",
			RenderMode renderMode = PolygonRenderer.DEFAULT_RENDER_MODE,
			Color? color = null,
			float thickness = PolygonRenderer.DEFAULT_THICKNESS,
			float centeredScale = PolygonRenderer.DEFAULT_CENTERED_SCALE,
			bool projectOnTerrain = false,
			float terrainHeightOffset = 0.1f
		)
		{
			var renderer = UnityUtils.InstantiateEmptyObject(parent, name).AddComponent<VoronoiRenderer>();
			renderer.voronoi = voronoi;
			renderer.initColorPalette = color ?? PolygonRenderer.DefaultColor;
			renderer.renderMode = renderMode;
			renderer.thickness = thickness;
			renderer.regionScale = centeredScale;
			renderer.projectOnTerrain = projectOnTerrain;
			renderer.terrainHeightOffset = terrainHeightOffset;

			renderer.UpdateRegions();

			return renderer;
		}

		#endregion
	}
}
