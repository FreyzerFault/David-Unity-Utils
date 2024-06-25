using System;
using System.Collections.Generic;
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

		private RenderMode _renderMode = RenderMode.Mesh;
		public RenderMode RenderMode
		{
			get => _renderMode;
			set
			{
				_renderMode = value;
				_renderers.ForEach(r => r.RenderMode = value);
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

			UpdatePolygons();
		}

		public override void Instantiate(Voronoi inGeometry, string childName = null)
		{
			voronoi = inGeometry;
			_renderers = new List<PolygonRenderer>(voronoi.SeedCount);
			SetRainbowColors(voronoi.SeedCount);
			UpdatePolygons();
		}

		public override void UpdateGeometry(Voronoi inGeometry) => throw new NotImplementedException();

		public override void Clear()
		{
			_renderers.ForEach(UnityUtils.DestroySafe);
			_renderers.Clear();
		}

		public void UpdatePolygons()
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
			PolygonRenderer.Instantiate(region, transform, polygonName, _renderMode, color, centeredScale: regionScale);


		#region TERRAIN

		public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f) =>
			_renderers.ForEach(r => r.ProjectOnTerrain(terrain, offset));

		#endregion
	}
}
