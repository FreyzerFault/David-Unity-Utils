using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[RequireComponent(typeof(LineRenderer), typeof(MeshRenderer), typeof(MeshFilter))]
	public class PolygonRenderer : MonoBehaviour
	{
		public const PolygonRenderMode DEFAULT_RENDER_MODE = PolygonRenderMode.Mesh;
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

		// SCALE SLIDER
		[SerializeField] [Range(.2f, 1)]
		public float centeredScale = DEFAULT_CENTERED_SCALE;

		private LineRenderer _lineRenderer;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;

		private Polygon ScaledPolygon => polygon.ScaleByCenter(centeredScale);

		private void Awake()
		{
			_lineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
			_meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
			_meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

			_lineRenderer.useWorldSpace = false;
			_lineRenderer.loop = true;
			_lineRenderer.material = Resources.Load<Material>("UI/Materials/Line Material");
			_meshRenderer.material = Resources.Load<Material>("Materials/Geometry Unlit");
		}

		private void OnValidate()
		{
			if (isActiveAndEnabled) UpdateAllProperties();
		}

		private void OnEnable() => UpdateAllProperties();

		public void UpdateAllProperties()
		{
			UpdateRenderMode();
			UpdatePolygon();
			UpdateColor();
			UpdateThickness();
			UpdateTerrainProjection();
		}


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
			_meshRenderer.enabled = renderMode is PolygonRenderMode.Mesh or PolygonRenderMode.OutlinedMesh;
			_lineRenderer.enabled = renderMode is PolygonRenderMode.Wire or PolygonRenderMode.OutlinedMesh;
			
			// Line Color
			_lineRenderer.startColor = _lineRenderer.endColor =
				renderMode is PolygonRenderMode.OutlinedMesh ? outlineColor : color;
		}

		private void UpdatePolygon()
		{
			if (projectOnTerrain && Terrain != null)
			{
				ProjectOnTerrain(Terrain, terrainHeightOffset);
			}
			else
			{
				_lineRenderer.SetPolygon(ScaledPolygon);
				_meshFilter.mesh.SetPolygon(ScaledPolygon, color);
			}
		}

		private void UpdateColor()
		{
			_lineRenderer.startColor = _lineRenderer.endColor = renderMode == PolygonRenderMode.Wire ? color : outlineColor;
			_meshFilter.mesh.SetColors(color.ToFilledArray(_meshFilter.mesh.vertexCount).ToArray());
		}

		private void UpdateThickness() => _lineRenderer.startWidth =
			_lineRenderer.endWidth = thickness * 0.01f * transform.lossyScale.x;

		#endregion


		#region TERRAIN

		private Terrain Terrain => Terrain.activeTerrain;
		public bool projectOnTerrain;
		public float terrainHeightOffset = 0.1f;

		public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f, bool scaleToTerrainBounds = true)
		{
			_lineRenderer.SetPoints(
				Terrain.ProjectPathToTerrain(
					scaleToTerrainBounds
						? ScaledPolygon.Vertices.Select(terrain.GetWorldPosition).ToArray()
						: ScaledPolygon.Vertices.ToV3xz().ToArray(),
					true,
					terrainHeightOffset
				)
			);
			UpdateTerrainProjection();
		}


		// If project on terrain => render as wire on world space
		private void UpdateTerrainProjection()
		{
			if (projectOnTerrain && Terrain != null)
			{
				renderMode = PolygonRenderMode.Wire;
				_lineRenderer.useWorldSpace = true;
			}
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
			polygonRenderer.projectOnTerrain = projectOnTerrain;
			polygonRenderer.terrainHeightOffset = terrainHeightOffset;

			polygonRenderer.UpdateAllProperties();

			return polygonRenderer;
		}

		#endregion
	}
}
