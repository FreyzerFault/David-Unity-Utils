using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[ExecuteAlways]
	[RequireComponent(typeof(LineRenderer), typeof(MeshRenderer), typeof(MeshFilter))]
	public class PolygonRenderer : MonoBehaviour
	{
		public const PolygonRenderMode DEFAULT_RENDER_MODE = PolygonRenderMode.OutlinedMesh;
		public const int DEFAULT_MAX_SUBPOLYGONS = 10;
		public const float DEFAULT_THICKNESS = 1f;
		public const float DEFAULT_CENTERED_SCALE = 1f;

		public enum PolygonRenderMode
		{
			Wire,
			Mesh,
			OutlinedMesh
		}

		[SerializeField] private PolygonRenderMode renderMode = DEFAULT_RENDER_MODE;

		[SerializeField] private Polygon polygon = Polygon.Empty;
		[SerializeField] private Color color = Color.black;
		[SerializeField] public Color outlineColor = Color.white;
		[SerializeField] private float thickness = DEFAULT_THICKNESS;
		
		// SUBPOLYGONS
		[Range(1,500)] public int maxSubPolygons = DEFAULT_MAX_SUBPOLYGONS;
		public Polygon[] subPolygons = Array.Empty<Polygon>();
		public int SubPolygonCount => subPolygons.Length;

		// SCALE SLIDER
		[SerializeField] [Range(.2f, 1)]
		public float centeredScale = DEFAULT_CENTERED_SCALE;

		private LineRenderer _lineRenderer;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;

		private Polygon ScaledPolygon => polygon.ScaleByCenter(centeredScale);
		
		public Mesh Mesh => _meshFilter.sharedMesh;

		protected virtual void Awake()
		{
			_lineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
			_meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
			_meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

			_lineRenderer.useWorldSpace = false;
			_lineRenderer.loop = true;
			_lineRenderer.material = Resources.Load<Material>("UI/Materials/Line Material");
			_meshRenderer.material = Resources.Load<Material>("Materials/Geometry Unlit");
			
			UpdateAllProperties();
		}

		private void OnValidate()
		{
			if (isActiveAndEnabled) UpdateAllProperties();
		}

		protected virtual void OnEnable() => UpdateAllProperties();

		public void UpdateAllProperties()
		{
			UpdateRenderMode();
			UpdatePolygon();
			UpdateColor();
			UpdateThickness();
			UpdateTerrainProjection();
		}

		public virtual void Clear() => Polygon = Polygon.Empty;


		#region MODIFIABLE PROPERTIES

		public PolygonRenderMode RenderMode
		{
			get => renderMode;
			set
			{
				renderMode = value;
				UpdateRenderMode();
			}
		}


		public Polygon Polygon
		{
			get => polygon;
			set
			{
				polygon = value;
				UpdatePolygon();
			}
		}

		public Color Color
		{
			get => color;
			set
			{
				color = value;
				UpdateColor();
			}
		}

		public Color OutlineColor
		{
			get => outlineColor;
			set
			{
				outlineColor = value;
				UpdateColor();
			}
		}

		public float Thickness
		{
			get => thickness;
			set
			{
				thickness = value;
				UpdateThickness();
			}
		}

		public float CenteredScale
		{
			get => centeredScale;
			set
			{
				centeredScale = value;
				UpdatePolygon();
			}
		}

		private void UpdateRenderMode()
		{
			if (_meshRenderer == null || _lineRenderer == null) return;
			_meshRenderer.enabled = renderMode is PolygonRenderMode.Mesh or PolygonRenderMode.OutlinedMesh;
			_lineRenderer.enabled = renderMode is PolygonRenderMode.Wire or PolygonRenderMode.OutlinedMesh;

			// Line Color
			_lineRenderer.startColor = _lineRenderer.endColor =
				renderMode is PolygonRenderMode.OutlinedMesh ? outlineColor : color;
		}

		public void UpdatePolygon()
		{
			if (_meshFilter == null && _lineRenderer == null) return;
			if (ProjectedOnTerrain)
			{
				ProjectOnTerrain(terrainHeightOffset);
			}
			else
			{
				if (_meshFilter.sharedMesh == null)
					_meshFilter.mesh = new Mesh();
				
				subPolygons = _meshFilter.sharedMesh.SetPolygon(ScaledPolygon, color, maxSubPolygons);
				_lineRenderer.SetPolygon(ScaledPolygon);
			}
		}

		private void UpdateColor()
		{
			if (_meshFilter == null || _lineRenderer == null) return;
			_lineRenderer.startColor =
				_lineRenderer.endColor = renderMode == PolygonRenderMode.Wire ? color : outlineColor;
			_meshFilter.sharedMesh.SetColors(color.ToFilledArray(_meshFilter.sharedMesh.vertexCount).ToArray());
		}

		private void UpdateThickness()
		{
			if (_lineRenderer == null) return;
			_lineRenderer.startWidth =
				_lineRenderer.widthMultiplier = thickness * 0.01f * transform.lossyScale.x;
		}

		#endregion


		#region TERRAIN

		private Terrain Terrain => Terrain.activeTerrain;
		[SerializeField] private bool projectedOnTerrain;
		
		public float terrainHeightOffset = 0.1f;
		public bool ProjectedOnTerrain
		{
			get => projectedOnTerrain && Terrain != null;
			set
			{
				projectedOnTerrain = value;
				UpdateTerrainProjection();
			}
		}
		
		private void UpdateTerrainProjection()
		{
			if (ProjectedOnTerrain) ProjectOnTerrain(terrainHeightOffset);
			
			if (_lineRenderer == null) return;
			_lineRenderer.useWorldSpace = ProjectedOnTerrain;
		}

		/// <summary>
		/// Proyecta el LineRenderer sobre el terreno segmentado a la resolucion del terreno
		/// </summary>
		public virtual void ProjectOnTerrain(float offset = 0.1f, bool scaleToTerrainBounds = true)
		{
			RenderMode = PolygonRenderMode.Wire;
			_lineRenderer.SetPoints(
				Terrain.ProjectPathToTerrain(
						scaleToTerrainBounds
							? ScaledPolygon.Vertices.Select(Terrain.GetWorldPosition).ToArray()
							: ScaledPolygon.Vertices.ToV3xz().ToArray(),
						true,
						terrainHeightOffset
					)
					.Select(p => p.WithY(p.y + offset))
			);
		}

		#endregion


		#region INSTANTIATION

		public static PolygonRenderer Instantiate(
			Polygon polygon,
			Transform parent,
			string name = "Polygon",
			PolygonRenderMode renderMode = DEFAULT_RENDER_MODE,
			Color? color = null,
			float thickness = DEFAULT_THICKNESS,
			float centeredScale = DEFAULT_CENTERED_SCALE,
			bool projectOnTerrain = false,
			float terrainHeightOffset = 0.1f
		)
		{
			var polygonRenderer = UnityUtils.InstantiateObject<PolygonRenderer>(parent, name);
			polygonRenderer.polygon = polygon;
			polygonRenderer.color = color ?? Color.white;
			polygonRenderer.thickness = thickness;
			polygonRenderer.renderMode = renderMode;
			polygonRenderer.centeredScale = centeredScale;
			polygonRenderer.projectedOnTerrain = projectOnTerrain;
			polygonRenderer.terrainHeightOffset = terrainHeightOffset;

			polygonRenderer.UpdateAllProperties();

			return polygonRenderer;
		}

		#endregion
	}
}
