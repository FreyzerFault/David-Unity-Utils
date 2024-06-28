using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using Geometry.Algorithms;
using UnityEngine;
using UnityEngine.Serialization;
using RenderMode = DavidUtils.Rendering.PolygonRenderer.PolygonRenderMode;

namespace DavidUtils.Rendering
{
	public class VoronoiRenderer : DynamicRenderer<PolygonRenderer>
	{
		protected override string DefaultChildName => "Region";
		
		private Voronoi _voronoi;

		public Voronoi Voronoi
		{
			set
			{
				_voronoi = value;
				UpdateRegionPolygons();
			}
		}
		
		
		private void OnEnable()
		{
			if (_voronoi == null || _voronoi.RegionCount == 0) return;

			// Update properties if they changed while disabled
			renderObjs.ForEach(r => r.UpdateAllProperties());
		}
		

		#region COMMON PROPERTIES

		[SerializeField] private RenderMode renderMode = RenderMode.Mesh;
		public RenderMode RenderMode
		{
			get => renderMode;
			set
			{
				renderMode = value;
				renderObjs.ForEach(r => r.RenderMode = value);
			}
		}
		private bool IsMesh => renderMode == RenderMode.Mesh;

		[ConditionalField("IsMesh", true)]
		[SerializeField] private float thickness = PolygonRenderer.DEFAULT_THICKNESS;
		public float Thickness
		{
			get => thickness;
			set
			{
				thickness = value;
				renderObjs.ForEach(r => r.Thickness = value);
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
				renderObjs.ForEach(r => r.CenteredScale = value);
			}
		}
		
		[SerializeField]
		private Color outlineColor = Color.white;
		public Color OutlineColor
		{
			get => outlineColor;
			set
			{
				outlineColor = value;
				renderObjs.ForEach(r => r.OutlineColor = value);
			}
		}
		
		protected override void SetCommonProperties(PolygonRenderer polyRenderer)
		{
			polyRenderer.RenderMode = renderMode;
			polyRenderer.Thickness = thickness;
			polyRenderer.CenteredScale = regionScale;
			polyRenderer.Color = GetColor();
			polyRenderer.OutlineColor = outlineColor;
		}

		#endregion
		

		
		
		// Esto genera el renderer de 0
		public void UpdateVoronoi()
		{
			Clear();

			if (_voronoi.RegionCount <= 0) return;

			SetRainbowColors(_voronoi.RegionCount);
			InstantiateObjs(Vector3.zero.ToFilledArray(_voronoi.RegionCount).ToArray(), "Region");
			UpdateRegionPolygons();
		}

		// Actualiza unicamente los poligonos
		public void UpdateRegionPolygons()
		{
			if (colors.Length != _voronoi.RegionCount) SetRainbowColors(_voronoi.RegionCount);

			for (var i = 0; i < _voronoi.RegionCount; i++) SetRegionPolygon(i, _voronoi.regions[i]);
		}

		// Asigna un poligono a una region
		public void SetRegionPolygon(int i, Polygon regionPolygon)
		{
			// Instantiate if not exists
			if (i < 0 || i >= renderObjs.Count)
			{
				i = renderObjs.Count;
				renderObjs.Add(InstantiateObj(Vector3.zero, $"Region {i}"));
				SetRainbowColors(_voronoi.RegionCount);
				renderObjs[i].Color = GetColor(i);
			}
			
			// Set the region Polygon
			renderObjs[i].Polygon = regionPolygon;
			
			// Project on TERRAIN
			if (projectOnTerrain) renderObjs[i].ProjectOnTerrain(Terrain, terrainHeightOffset);
		}


		#region TERRAIN

		private Terrain Terrain => Terrain.activeTerrain;
		public bool projectOnTerrain;
		public float terrainHeightOffset = 0.1f;

		#endregion


		#region INSTANTIATION

		public static VoronoiRenderer Instantiate(
			Voronoi voronoi,
			Transform parent,
			string name = "Voronoi Renderer",
			RenderMode renderMode = PolygonRenderer.DEFAULT_RENDER_MODE,
			Color? color = null,
			Color? outlineColor = null,
			float thickness = PolygonRenderer.DEFAULT_THICKNESS,
			float centeredScale = PolygonRenderer.DEFAULT_CENTERED_SCALE,
			bool projectOnTerrain = false,
			float terrainHeightOffset = 0.1f
		)
		{
			var renderer = UnityUtils.InstantiateObject<VoronoiRenderer>(parent, name);
			renderer._voronoi = voronoi;
			renderer.InitialColor = color ?? Color.white;
			renderer.outlineColor = outlineColor ?? Color.black;
			renderer.renderMode = renderMode;
			renderer.thickness = thickness;
			renderer.regionScale = centeredScale;
			renderer.projectOnTerrain = projectOnTerrain;
			renderer.terrainHeightOffset = terrainHeightOffset;

			renderer.UpdateVoronoi();

			return renderer;
		}

		#endregion
	}
}
