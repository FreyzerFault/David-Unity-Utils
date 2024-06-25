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
		private static readonly Color DefaultColor = Color.white;
		private const PolygonRenderMode DEFAULT_RENDER_MODE = PolygonRenderMode.Mesh;
		private const float DEFAULT_THICKNESS = 1f;
		private const float DEFAULT_CENTERED_SCALE = 1f;

		public enum PolygonRenderMode
		{
			Wire,
			Mesh,
			OutlinedMesh
		}

		[SerializeField] private PolygonRenderMode renderMode = DEFAULT_RENDER_MODE;

		[SerializeField] private Polygon polygon = Polygon.Empty;
		[SerializeField] private Color color = DefaultColor;
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

		private void UpdateAllProperties()
		{
			UpdateRenderMode();
			UpdatePolygon();
			UpdateColor();
			UpdateThickness();
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
			_lineRenderer.enabled = renderMode is PolygonRenderMode.Wire or PolygonRenderMode.OutlinedMesh;
			_meshRenderer.enabled = renderMode is PolygonRenderMode.Mesh or PolygonRenderMode.OutlinedMesh;
		}

		private void UpdatePolygon()
		{
			_lineRenderer.SetPolygon(ScaledPolygon);
			_meshFilter.mesh.SetPolygon(ScaledPolygon);
		}

		private void UpdateColor()
		{
			_lineRenderer.startColor = _lineRenderer.endColor = color;
			_meshFilter.mesh.SetColors(color.ToFilledArray(_meshFilter.mesh.vertexCount).ToArray());
		}

		private void UpdateThickness() => _lineRenderer.startWidth =
			_lineRenderer.endWidth = thickness * 0.01f * transform.lossyScale.x;

		#endregion


		#region TERRAIN

		public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f)
		{
			terrain.ProjectMeshInTerrain(_meshFilter.sharedMesh, _meshFilter.transform, offset);
			_lineRenderer.SetPoints(_lineRenderer.GetPoints().Select(p => terrain.Project(p, offset)).ToArray());
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
			float centeredScale = DEFAULT_CENTERED_SCALE
		)
		{
			var polygonRenderer = UnityUtils.InstantiateEmptyObject(parent, name).AddComponent<PolygonRenderer>();
			polygonRenderer.Polygon = polygon;
			polygonRenderer.Color = color ?? DefaultColor;
			polygonRenderer.Thickness = thickness;
			polygonRenderer.RenderMode = renderMode;
			polygonRenderer.CenteredScale = centeredScale;
			return polygonRenderer;
		}

		#endregion
	}
}
