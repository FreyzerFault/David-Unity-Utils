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
				UpdatePolygons();
			}
		}
		
		
		private void OnEnable()
		{
			if (_voronoi == null || _voronoi.PolygonCount == 0) return;

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
				_isMesh = value == RenderMode.Mesh;
			}
		}

		private bool _isMesh = false;

		[ConditionalField("_isMesh", true)]
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

		[FormerlySerializedAs("regionScale")] [Range(.2f, 1)] [SerializeField]
		private float polygonScale = 1;
		public float PolygonScale
		{
			get => polygonScale;
			set
			{
				polygonScale = value;
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
			polyRenderer.CenteredScale = polygonScale;
			polyRenderer.Color = GetColor();
			polyRenderer.OutlineColor = outlineColor;
			polyRenderer.ProjectedOnTerrain = ProjectedOnTerrain;
		}

		#endregion


		#region INDIVIDUAL PROPS
		
		protected override void UpdateColor() => renderObjs.ForEach((r,i) => r.Color = GetColor(i));

		#endregion

		
		
		// Esto genera el renderer de 0
		public void UpdateVoronoi()
		{
			Clear();

			if (_voronoi.PolygonCount <= 0) return;

			SetRainbowColors(_voronoi.PolygonCount);
			InstantiateObjs(Vector3.zero.ToFilledArray(_voronoi.PolygonCount).ToArray(), "Region");
			UpdatePolygons();
		}

		// Actualiza unicamente los poligonos
		public void UpdatePolygons()
		{
			if (colors.Length != _voronoi.PolygonCount) SetRainbowColors(_voronoi.PolygonCount);

			for (var i = 0; i < _voronoi.PolygonCount; i++) SetPolygon(i, _voronoi.polygons[i]);
		}

		// Asigna un poligono a una region
		public void SetPolygon(int i, Polygon polygon)
		{
			// Instantiate if not exists
			if (i < 0 || i >= renderObjs.Count)
			{
				i = renderObjs.Count;
				renderObjs.Add(InstantiateObj(Vector3.zero, $"Region {i}"));
				SetRainbowColors(_voronoi.PolygonCount);
				renderObjs[i].Color = GetColor(i);
			}
			
			// Set the region Polygon
			renderObjs[i].Polygon = polygon;
			
			// Project on TERRAIN
			if (ProjectedOnTerrain) renderObjs[i].ProjectOnTerrain(terrainHeightOffset);
		}



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
			bool projectedOnTerrain = false,
			float terrainHeightOffset = 0.1f
		)
		{
			var renderer = UnityUtils.InstantiateObject<VoronoiRenderer>(parent, name);
			renderer._voronoi = voronoi;
			renderer.InitialColor = color ?? Color.white;
			renderer.outlineColor = outlineColor ?? Color.black;
			renderer.renderMode = renderMode;
			renderer.thickness = thickness;
			renderer.polygonScale = centeredScale;
			renderer.projectedOnTerrain = projectedOnTerrain;
			renderer.terrainHeightOffset = terrainHeightOffset;

			renderer.UpdateVoronoi();

			return renderer;
		}

		#endregion
	}
}
