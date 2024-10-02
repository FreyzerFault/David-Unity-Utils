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
		public enum PolygonRenderMode
		{
			Wire,
			Mesh,
			OutlinedMesh
		}
		
		public const PolygonRenderMode DEFAULT_RENDER_MODE = PolygonRenderMode.OutlinedMesh;
		public const float DEFAULT_THICKNESS = 1f;
		public const float DEFAULT_CENTERED_SCALE = 1f;
		public const int DEFAULT_MAX_SUBPOLYGONS_PER_FRAME = 10;
		public const int DEFAULT_MAX_SUBPOLYGONS_COUNT = 500;

		[SerializeField] private PolygonRenderMode renderMode = DEFAULT_RENDER_MODE;

		[SerializeField] private Polygon polygon;
		[SerializeField] private Color color = Color.cyan;
		[SerializeField] public Color outlineColor = Color.white;
		[SerializeField] private float thickness = DEFAULT_THICKNESS;
		[SerializeField] [Range(.2f, 1)] private float centeredScale = DEFAULT_CENTERED_SCALE;

		// SubRenderers
		private LineRenderer _lineRenderer;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;

		public Mesh Mesh => _meshFilter.sharedMesh;
		
		private Polygon ScaledPolygon => polygon.ScaleByCenter(centeredScale);

		protected void Awake()
		{
			_lineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
			_meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
			_meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

			_lineRenderer.useWorldSpace = false;
			_lineRenderer.loop = true;
			_lineRenderer.material = Resources.Load<Material>("UI/Materials/Line Material");
			_meshRenderer.material = Resources.Load<Material>("Materials/Geometry Unlit");
			
			origHeight = transform.localPosition.y;
			generateSubPolygons = generateSubPolygons && maxSubPolygonsPerFrame > 0;
			
			if (_meshFilter.sharedMesh == null) _meshFilter.mesh = new Mesh();
		}
		
		private void Update()
		{
			if (!Application.isPlaying) DestroyQueue();
			if (showSubPolygons && subPolyRenderers.Length != SubPolygonCount) UpdateSubPolygonRenderers();
		}

		// Check si esta en OnValidate() para encolar mas tarde destrucciones en Update() si se está en Editor
		private bool isOnValidate = false;
		
		protected void OnEnable() => UpdateAllProperties();


		public void UpdateAllProperties()
		{
			UpdateRenderMode();
			UpdatePolygon();
			UpdateColor();
			UpdateThickness();
			UpdateTerrainProjection();
		}

		public void Clear()
		{
			polygon = new Polygon();
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
		public int VertexCount => polygon.VertexCount;

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

		public void UpdateRenderMode()
		{
			if (_meshRenderer == null || _lineRenderer == null) return;
			_meshRenderer.enabled = renderMode is PolygonRenderMode.Mesh or PolygonRenderMode.OutlinedMesh && !CanShowSubPolygons;
			_lineRenderer.enabled = renderMode is PolygonRenderMode.Wire or PolygonRenderMode.OutlinedMesh;

			// Line Color
			_lineRenderer.startColor = _lineRenderer.endColor =
				renderMode is PolygonRenderMode.OutlinedMesh ? outlineColor : color;

			if (renderMode == PolygonRenderMode.OutlinedMesh)
				SetLinePoints();
			
			if (renderMode == PolygonRenderMode.Wire)
				CleanSubPolyRenderers();
			
			UpdateColor();
			UpdateThickness();
		}

		public void UpdatePolygon()
		{
			if (polygon == null || polygon.IsEmpty)
			{
				Clear();
				return;
			}
			
			if (_meshFilter == null && _lineRenderer == null) return;
			
			if (_meshFilter.sharedMesh == null) _meshFilter.mesh = new Mesh();
			
			SetLinePoints();

			if (generateSubPolygons)
			{
				if (Application.isPlaying)
				{
					IEnumerator subPolygonCoroutine = _meshFilter.sharedMesh.SetPolygonConcaveCoroutine(
						(sp) => { subPolygons = sp; },
						UpdateSubPolygonRenderers,
						ScaledPolygon, color, maxSubPolygonsPerFrame, maxSubPolygonCount / maxSubPolygonsPerFrame);
					StartCoroutine(subPolygonCoroutine);
				}
				else
				{
					subPolygons = _meshFilter.sharedMesh.SetPolygonConcave(ScaledPolygon, color, maxSubPolygonCount).ToList();
				}
			}
			else
			{
				subPolygons = new List<Polygon>();
				CleanSubPolyRenderers();
				_meshFilter.sharedMesh.SetPolygonConvex(ScaledPolygon, color);
			}
				
			UpdateSubPolygonRenderers();
		}

		public void UpdateColor()
		{
			if (_meshFilter == null || _lineRenderer == null) return;
			_lineRenderer.startColor = _lineRenderer.endColor = outlineColor;
			_meshFilter.sharedMesh?.SetColors(color.ToFilledArray(_meshFilter.sharedMesh.vertexCount).ToArray());
		}

		public void UpdateThickness()
		{
			if (_lineRenderer == null) return;
			_lineRenderer.startWidth =
				_lineRenderer.widthMultiplier = thickness * transform.lossyScale.x;
		}

		#endregion

		
		#region LINE RENDERER

		private void SetLinePoints()
		{
			if (polygon == null)
			{
				_lineRenderer.SetPoints(Array.Empty<Vector3>());
				return;
			}
			_lineRenderer.SetPoints(ScaledPolygon.Vertices.Select(v => v.ToV3xy().WithZ(-0.2f)));
		}

		#endregion


		#region TERRAIN

		private Terrain Terrain => Terrain.activeTerrain;
		[SerializeField] private bool projectedOnTerrain;
		
		public float origHeight = 0;
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
		
		public void UpdateTerrainProjection()
		{
			if (_lineRenderer == null) return;

			if (ProjectedOnTerrain)
				ProjectOnTerrain(false);
			else
				SetLinePoints();
			
			UpdateHeightTerrainOffset();
			
			_lineRenderer.useWorldSpace = ProjectedOnTerrain; 
		}
		public void UpdateHeightTerrainOffset() {
			if (ProjectedOnTerrain)
				ProjectOnTerrain(false);
		}

		/// <summary>
		/// Proyecta el LineRenderer sobre el terreno segmentado a la resolucion del terreno
		/// </summary>
		public void ProjectOnTerrain(bool scaleToTerrainBounds = true)
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
			int maxSubPolygonsPerFrame = 0,
			int maxSubPolygonsCount = 0
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
			polygonRenderer.maxSubPolygonsPerFrame = maxSubPolygonsPerFrame;
			polygonRenderer.maxSubPolygonCount = maxSubPolygonsCount;

			polygonRenderer.UpdateAllProperties();

			return polygonRenderer;
		}

		#endregion


		#region SUBPOLYGON SEGMENTATION
		
		[Space]
		
		public bool generateSubPolygons;
		
		[Range(1,100)] public int maxSubPolygonsPerFrame = DEFAULT_MAX_SUBPOLYGONS_PER_FRAME;
		[FormerlySerializedAs("maxSubPolygonsCount")] [Range(1,1000)] public int maxSubPolygonCount = DEFAULT_MAX_SUBPOLYGONS_PER_FRAME;
		
		[HideInInspector] public List<Polygon> subPolygons = new();
		public int SubPolygonCount => subPolygons.Count;
		
		PolygonRenderer[] subPolyRenderers = Array.Empty<PolygonRenderer>();
		[SerializeField] private bool showSubPolygons = false;

		public bool ShowSubPolygons
		{
			get => showSubPolygons;
			set
			{
				if (showSubPolygons == value) return;
				showSubPolygons = value;
				UpdateSubPolygonRenderers();
			}
		}
		
		private bool CanShowSubPolygons => 
			generateSubPolygons && showSubPolygons && renderMode != PolygonRenderMode.Wire && subPolygons.NotNullOrEmpty();
		
		public void UpdateSubPolygonRenderers()
		{
			subPolyRenderers = GetComponentsInChildren<PolygonRenderer>().Where(pr => pr != this).ToArray();
			
			if (CanShowSubPolygons)
			{
				if (SubPolygonCount > 0)
				{
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
					Instantiate(
						p,
						transform,
                    $"SubPolygon_{i}",
						PolygonRenderMode.OutlinedMesh,
						colors[i].Desaturate(0.1f),
						0.1f,
						1,
						ProjectedOnTerrain,
						terrainHeightOffset,
						Color.black
					))
				.ToArray();
		}
		
		private void CleanSubPolyRenderers()
		{
			if (subPolyRenderers.IsNullOrEmpty()) return;
			if (isOnValidate && !Application.isPlaying)
				_destructionQueue = subPolyRenderers.Select(spr => spr.gameObject).ToArray();
			else
				UnityUtils.DestroySafe(subPolyRenderers);
			subPolyRenderers = Array.Empty<PolygonRenderer>();
		}
		
		#endregion


		#region DESTRUCTION QUEUE

		// Si Destroy() se llama en OnValidate mientras está en modo Editor, da un error
		// Para solucionarlo encolamos el DestroyInmediate en Update cuando quiera destruir algo en OnValidate
		private GameObject[] _destructionQueue;

		private void DestroyQueue()
		{
			_destructionQueue?.ForEach(DestroyImmediate);
			_destructionQueue = Array.Empty<GameObject>();
		}

		#endregion
	}
}
