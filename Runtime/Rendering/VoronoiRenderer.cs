using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Algorithms;
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
			
			_voronoi.onPolygonAdded += p => SetPolygon(p);
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

		private bool _isMesh;

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

		protected override void UpdateCommonProperties(PolygonRenderer polyRenderer)
		{
			polyRenderer.RenderMode = renderMode;
			polyRenderer.Thickness = thickness;
			polyRenderer.CenteredScale = polygonScale;
			if (singleColor) polyRenderer.Color = GetColor();
			polyRenderer.OutlineColor = outlineColor;
			polyRenderer.ProjectedOnTerrain = ProjectedOnTerrain;
		}

		#endregion


		#region INDIVIDUAL PROPS
		
		public override void UpdateColor()
		{
			renderObjs.ForEach((r, i) => r.Color = GetColor(i));
		}

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

			for (var i = 0; i < _voronoi.PolygonCount; i++) SetPolygon(_voronoi.polygons[i], i);
		}

		public void AddPolygon(Polygon polygon) => SetPolygon(polygon);

		// Asigna un poligono o lo añade si i < 0 o i >= count
		public void SetPolygon(Polygon polygon, int i = -1)
		{
			if (i < 0 || i > renderObjs.Count)
				i = renderObjs.Count;
			
			// Instantiate if not exists
			if (i == renderObjs.Count)
			{
				PolygonRenderer polyRenderer = InstantiateObj(Vector3.zero, $"Polygon {i}");
				renderObjs.Add(polyRenderer);
				SetRainbowColors(_voronoi.PolygonCount);
				polyRenderer.Color = GetColor(i);
			}
			
			// Set the region Polygon
			renderObjs[i].Polygon = polygon;
			
			// Project on TERRAIN
			if (ProjectedOnTerrain) renderObjs[i].ProjectedOnTerrain = true;
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
			VoronoiRenderer renderer = UnityUtils.InstantiateObject<VoronoiRenderer>(parent, name);
			renderer._voronoi = voronoi;
			renderer.BaseColor = color ?? Color.white;
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
