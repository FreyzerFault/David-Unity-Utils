using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

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
		[FormerlySerializedAs("maxSubPolygons")] [Range(1,500)] public int maxSubPolygonsPerFrame = DEFAULT_MAX_SUBPOLYGONS;
		[HideInInspector] public List<Polygon> subPolygons = new();
		public int SubPolygonCount => subPolygons.Count;

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
			
		}

		private bool isOnValidate = false;
		private void OnValidate()
		{
			if (!isActiveAndEnabled) return;
			isOnValidate = true;
			UpdateAllProperties();
			isOnValidate = false;
		}

		protected virtual void OnEnable()
		{
			UpdateAllProperties();
		}

		public void UpdateAllProperties()
		{
			UpdateRenderMode();
			UpdatePolygon();
			UpdateColor();
			UpdateThickness();
			UpdateTerrainProjection();
		}

		public virtual void Clear()
		{
			Polygon = Polygon.Empty;
			UnityUtils.DestroySafe(subPolyRenderers);
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
				
				_lineRenderer.SetPolygon(ScaledPolygon);

				if (Application.isPlaying)
				{
					IEnumerator subPolygonCoroutine = _meshFilter.sharedMesh.SetPolygonConcaveCoroutine(
						(sp) =>
						{
							subPolygons = sp;
							UpdateSubPolygons();
						},
						() =>
						{
							
						},
						ScaledPolygon, color, maxSubPolygonsPerFrame);
					StartCoroutine(subPolygonCoroutine);
				}
				else
				{
					subPolygons = _meshFilter.sharedMesh.SetPolygonConcave(ScaledPolygon, color, 200).ToList();
				}
				
				
				UpdateSubPolygons();
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
			float terrainHeightOffset = 0.1f,
			Color? outlineColor = null,
			int maxSubPolygonsForMesh = 0
		)
		{
			var polygonRenderer = UnityUtils.InstantiateObject<PolygonRenderer>(parent, name);
			polygonRenderer.polygon = polygon;
			polygonRenderer.color = color ?? Color.white;
			polygonRenderer.outlineColor = outlineColor ?? Color.black;
			polygonRenderer.thickness = thickness;
			polygonRenderer.renderMode = renderMode;
			polygonRenderer.centeredScale = centeredScale;
			polygonRenderer.projectedOnTerrain = projectOnTerrain;
			polygonRenderer.terrainHeightOffset = terrainHeightOffset;
			polygonRenderer.maxSubPolygonsPerFrame = maxSubPolygonsForMesh;

			polygonRenderer.UpdateAllProperties();

			return polygonRenderer;
		}

		#endregion


		#region DEBUG
		
		PolygonRenderer[] subPolyRenderers = Array.Empty<PolygonRenderer>();
		private bool showSubPolygons = false;

		public bool ShowSubPolygons
		{
			get => showSubPolygons;
			set
			{
				if (showSubPolygons == value) return;
				showSubPolygons = value;
				UpdateSubPolygons();
			}
		}
		
		private void Update()
		{
			if (!Application.isPlaying) DestroyQueue();
			if (showSubPolygons && subPolyRenderers.Length != SubPolygonCount) UpdateSubPolygons();
		}

		private void UpdateSubPolygons()
		{
			subPolyRenderers = GetComponentsInChildren<PolygonRenderer>().Where(pr => pr != this).ToArray();
			
			if (showSubPolygons)
			{
				if (SubPolygonCount > 0)
				{
					_lineRenderer.enabled = false;
					_meshRenderer.enabled = false;
					
					if (subPolyRenderers.IsNullOrEmpty() || SubPolygonCount != subPolyRenderers.Length)
						InstantiateSubPolyRenderers();
					else
						subPolyRenderers.ForEach((spr, i) => spr.Polygon = subPolygons[i]);
				}
				else
				{
					CleanSubPolyRenderers();
				}
			}
			else
			{
				if (subPolyRenderers.NotNullOrEmpty())
					CleanSubPolyRenderers();
				
				_lineRenderer.enabled = true;
				_meshRenderer.enabled = true;
				UpdateRenderMode();
			}
		}

		private void InstantiateSubPolyRenderers()
		{
			if (SubPolygonCount == 0) return;
			
			// Clean ALL
			if (subPolyRenderers.NotNullOrEmpty())
				CleanSubPolyRenderers();

			var colors = Color.red.GetRainBowColors(SubPolygonCount).Reverse().ToArray();

			subPolyRenderers = subPolygons
				.Select((p, i) => 
					Instantiate(p, transform, $"SubPolygon_{i}", PolygonRenderMode.OutlinedMesh, colors[i].Desaturate(0.1f), 0.1f, 1, ProjectedOnTerrain, terrainHeightOffset, Color.black, 0))
				.ToArray();
		}

		private void CleanSubPolyRenderers()
		{
			if (subPolyRenderers.IsNullOrEmpty()) return;
			if (isOnValidate && !Application.isPlaying)
				queuedForDestruction = subPolyRenderers.Select(spr => spr.gameObject).ToArray();
			else
				UnityUtils.DestroySafe(subPolyRenderers);
			subPolyRenderers = Array.Empty<PolygonRenderer>();
		}

		#endregion


		#region DESTRUCTION QUEUE

		// Necesito encolar la destruccion en Update cuando quiera destruir algo en OnValidate y no esté en modo Play
		private GameObject[] queuedForDestruction;


		private void DestroyQueue()
		{
			queuedForDestruction?.ForEach(DestroyImmediate);
			queuedForDestruction = Array.Empty<GameObject>();
		}

		#endregion
	}
}
