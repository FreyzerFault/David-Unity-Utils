using System.Collections;
using System.Globalization;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Algorithms;
using DavidUtils.MouseInputs;
using DavidUtils.Rendering;
using Geometry.Algorithms;
using UnityEngine;
using UnityEngine.Serialization;
using RenderMode = DavidUtils.Rendering.PolygonRenderer.PolygonRenderMode;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : DelaunayGenerator
	{
		#region VORONOI

		[Space]
		[Header("VORONOI")]
		public Voronoi voronoi;

		public Polygon[] Polygons => voronoi?.polygons?.ToArray();
		public int PolygonCount => voronoi.polygons.Count;

		#endregion


		#region UNITY

		protected override void Update()
		{
			UpdateMousePolygon();
			UpdateSelectedPolygon();
		}

		#endregion


		#region MAIN METHODS

		public override void Init()
		{
			base.Init();
			
			selectedPolygonIndex = -1;
			voronoi.Seeds = seeds;
			voronoi.delaunay = delaunay;
			
			Renderer.Clear();
		}

		public override void Run()
		{
			if (seeds.IsNullOrEmpty()) GenerateSeeds();
			
			if (AnimatedVoronoi)
			{
				animationCoroutine = StartCoroutine(RunCoroutine());
			}
			else
			{
				delaunay.RunTriangulation();
				OnTrianglesUpdated();
				voronoi.GenerateVoronoi();
				OnAllPolygonsCreated();
			}
		}
		
		protected void OnAllPolygonsCreated() => voronoi.SimplifyPolygons();

		#endregion


		#region ANIMATION

		public bool animatedVoronoi = true;

		public bool AnimatedVoronoi
		{
			get => animatedVoronoi;
			set => animatedVoronoi = value;
		}

		public override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();
			
			if (DrawVoronoi && AnimatedVoronoi)
			{
				while (!voronoi.Ended)
				{
					voronoi.Run_OneIteration();
					yield return new WaitForSecondsRealtime(DelaySeconds);
				}
			}
			else
				voronoi.GenerateVoronoi();
			
			OnAllPolygonsCreated();
		}

		protected override void Run_OneIteration()
		{
			if (!delaunay.ended)
				delaunay.Run_OnePoint();
			else
				voronoi.Run_OneIteration();
		}
		

		#endregion



		#region RENDERING

		[Space] [SerializeField]
		private VoronoiRenderer voronoiRenderer;
		private VoronoiRenderer Renderer => voronoiRenderer ??= GetComponentInChildren<VoronoiRenderer>(true);

		[SerializeField] private RenderMode voronoiRenderMode = PolygonRenderer.PolygonRenderMode.OutlinedMesh;

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			voronoiRenderer ??= Renderer
			                     ?? VoronoiRenderer.Instantiate(
				                     voronoi,
				                     transform,
				                     "VORONOI Renderer",
				                     projectedOnTerrain: true,
				                     centeredScale: PolygonScale,
				                     renderMode: voronoiRenderMode
			                     );

			Renderer.ToggleShadows(false);
			
			// Selected
			InitializeSelectedRenderer();
		}

		#endregion


		#region SELECT POLYGON with MOUSE

		#region MOUSE

		// POLYGON under Mouse (guarda el index del poligono)
		protected int mousePolygonIndex = -1;

		protected Polygon? MousePolygon =>
			mousePolygonIndex != -1 && mousePolygonIndex < PolygonCount
				? voronoi.polygons[mousePolygonIndex]
				: null;

		// MOUSE in AABB [0,0 - 1,1]
		private Vector2 MousePosNorm =>
			AABB.ApplyTransform_XZ(transform.localToWorldMatrix).NormalizeMousePosition_XZ();

		private void UpdateMousePolygon()
		{
			Vector2 mouseNormPos = AABB.ApplyTransform_XZ(transform.localToWorldMatrix).NormalizeMousePosition_XZ();
			mousePolygonIndex = mouseNormPos.IsIn01() ? voronoi.GetPolygonIndex(MousePosNorm) : -1;
		}

		#endregion


		#region SELECTION

		[FormerlySerializedAs("_hoverPolygon")]
		[Space]
		[Header("Polygon Selection")]
		[SerializeField] private bool canHoverPolygon = true;
		[FormerlySerializedAs("_selectPolygon")] [SerializeField] private bool canSelectPolygon = true;

		// Polygon Hovered
		public bool IsPolygonHovering => canHoverPolygon && mousePolygonIndex != -1;

		// Polygon Selected when clicking
		[HideInInspector] public int selectedPolygonIndex = -1;

		public Polygon? SelectedPolygon =>
			IsPolygonSelected ? voronoi.polygons[selectedPolygonIndex] : null;

		public bool IsPolygonSelected =>
			canSelectPolygon && selectedPolygonIndex != -1 && selectedPolygonIndex < PolygonCount;

		private Polygon LastPolygonGenerated => voronoi.polygons[^1];


		#region RENDERING

		[SerializeField] private PolygonRenderer selectedRenderer;
		[SerializeField] private PolygonRenderer hoverRenderer;

		private void InitializeSelectedRenderer()
		{
			selectedRenderer ??= PolygonRenderer.Instantiate(
				new Polygon(),
				transform,
				"Selected Polygon",
				RenderMode.Wire,
				Color.yellow.Desaturate(0.1f),
				voronoiRenderer.Thickness + 0.01f,
				PolygonScale + 0.01f,
				true
			);
			hoverRenderer ??= PolygonRenderer.Instantiate(
				new Polygon(),
				transform,
				"Hover Polygon",
				RenderMode.Wire,
				Color.yellow,
				voronoiRenderer.Thickness,
				PolygonScale + 0.01f,
				true
			);
			
			selectedRenderer.UpdateAllProperties();
			hoverRenderer.UpdateAllProperties();

			selectedRenderer.transform.localPosition += Vector3.up * 2f;
			hoverRenderer.transform.localPosition += Vector3.up * 2f;
		}

		private void UpdateHoverRenderer() => hoverRenderer.Polygon = MousePolygon ?? new Polygon();
		private void UpdateSelectedRenderer() => selectedRenderer.Polygon = SelectedPolygon ?? new Polygon();

		#endregion


		/// <summary>
		///     Update Selected and Hover Polygon by Mouse Input
		/// </summary>
		private void UpdateSelectedPolygon()
		{
			ToggleHover(canHoverPolygon);
			ToggleSelected(canSelectPolygon);
			UpdateHoverRenderer();

			// CLICK DOWN => Select, and start dragging
			bool mouseDown = Input.GetMouseButtonDown(0);
			bool mouseUp = Input.GetMouseButtonUp(0);
			bool mouse = Input.GetMouseButton(0);

			// SELECTING
			if (mouseDown) SelectPolygon(mousePolygonIndex);
			
			selectedRenderer.UpdateAllProperties();
			hoverRenderer.UpdateAllProperties();
			
			// DRAGGING
			if (!mouseDown && !mouseUp && mouse && IsPolygonSelected && canDragPolygons)
			{
				// DRAGGING
				// Hide Hover Polygon, Selected only
				ToggleHover(false);
				MoveSeed(selectedPolygonIndex, MousePosNorm - _draggingOffset);
			}
		}

		private void ToggleHover(bool toggle) => hoverRenderer.gameObject.SetActive(toggle);
		private void ToggleSelected(bool toggle) => selectedRenderer.gameObject.SetActive(toggle);

		/// <summary>
		///     Select Polygon by Index
		///     Index == -1 => Deselect
		/// </summary>
		private void SelectPolygon(int index)
		{
			selectedPolygonIndex = index;
			UpdateSelectedRenderer();

			if (index == -1) return;

			// Update dragging offset to move the polygon from any point of the polygon
			_draggingOffset = MousePosNorm - seeds[index];
		}

		#endregion


		#region DRAGGING

		// For dragging a polygon
		public bool canDragPolygons = false;
		private Vector2 _draggingOffset = Vector2.zero;

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			voronoi.Seeds = seeds;
			voronoi.GenerateVoronoi();

			UpdateSelectedRenderer();

			return true;
		}

		#endregion

		#endregion


		#region UI CONTROL


		public bool DrawVoronoi
		{
			get => Renderer.Active;
			set => Renderer.Active = value;
		}

		public RenderMode VoronoiRenderMode
		{
			get => voronoiRenderMode;
			set
			{
				voronoiRenderMode = value;
				Renderer.RenderMode = value;
			}
		}

		public bool WireVoronoi
		{
			get => VoronoiRenderMode == RenderMode.Wire && Renderer.Active;
			set => VoronoiRenderMode = value ? RenderMode.Wire : RenderMode.OutlinedMesh;
		}

		public float PolygonScale
		{
			get => Renderer.PolygonScale;
			set => Renderer.PolygonScale = value;
		}

		public override bool AnimatedDelaunay
		{
			get => animatedVoronoi || animatedDelaunay;
			set => animatedVoronoi = animatedDelaunay = value;
		}
		public string DelayMillisecondsStr
		{
			get => (delayMilliseconds * 1000).ToString("N0");
			set => delayMilliseconds = int.Parse(value, NumberStyles.Integer);
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		public bool drawSelectedTriangles;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (!drawGizmos) return;

			// DrawVoronoiGizmos();

			// Mientras se Genera, dibujamos detallada la ultima polygon generada
			if (voronoi.polygons.NotNullOrEmpty() && !voronoi.Ended)
				DrawPolygonGizmos_Detailed(PolygonCount - 1);

			// Mouse Position
			MouseInputUtils.DrawGizmos_XZ();

			// DrawPolygonGizmos_Highlighted(mousePolygonIndex);
			//
			// DrawPolygonGizmos_Highlighted(selectedPolygonIndex);

			if (drawSelectedTriangles)
				DrawPolygonGizmos_Detailed(selectedPolygonIndex);
		}

		public void DrawVoronoiGizmos() =>
			voronoi.OnDrawGizmos(BoundsComp.LocalToWorldMatrix_WithXZrotation, Renderer.PolygonScale, Renderer.colors.ToArray());

		public void DrawPolygonGizmos_Detailed(int index) =>
			voronoi.DrawPolygonGizmos_Detailed(index, BoundsComp.LocalToWorldMatrix_WithXZrotation, true);

		public void DrawPolygonGizmos_Highlighted(int index) =>
			voronoi.DrawPolygonGizmos_Highlighted(
				index,
				BoundsComp.LocalToWorldMatrix_WithXZrotation,
				Renderer.PolygonScale,
				true
			);

#endif

		#endregion
	}
}
