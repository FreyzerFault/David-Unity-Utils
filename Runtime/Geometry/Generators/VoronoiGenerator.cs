using System.Collections;
using System.Globalization;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
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

		public override void Reset()
		{
			base.Reset();
			ResetVoronoi();
		}

		public void ResetVoronoi()
		{
			selectedPolygonIndex = -1;
			voronoi.Seeds = seeds;
			voronoi.delaunay = delaunay;
			Renderer.Clear();
		}

		public override void Run()
		{
			if (seeds.IsNullOrEmpty()) GenerateSeeds();
			
			ResetDelaunay();
			ResetVoronoi();
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

			ResetVoronoi();
			if (DrawVoronoi && AnimatedVoronoi)
			{
				while (!voronoi.Ended)
				{
					voronoi.Run_OneIteration();
					OnPolygonCreated(voronoi.polygons[^1], PolygonCount - 1);
					yield return new WaitForSecondsRealtime(DelaySeconds);
				}

				OnAllPolygonsCreated();
			}
			else
			{
				voronoi.GenerateVoronoi();
				OnAllPolygonsCreated();
			}
		}

		protected override void Run_OneIteration()
		{
			if (!delaunay.ended)
			{
				delaunay.Run_OnePoint();
			}
			else
			{
				voronoi.Run_OneIteration();
				OnPolygonCreated(voronoi.polygons[^1], PolygonCount - 1);
			}
		}

		#endregion


		#region POLYGON EVENTS

		protected void OnPolygonCreated(Polygon poly, int i) =>
			UpdateRenderer(i, poly);

		protected void OnAllPolygonsCreated()
		{
			voronoi.SimplifyPolygons();
			UpdateRenderer();
		}

		#endregion


		#region RENDERING

		[Space] [SerializeField]
		private VoronoiRenderer _voronoiRenderer;
		private VoronoiRenderer Renderer => _voronoiRenderer ??= GetComponentInChildren<VoronoiRenderer>(true);

		[FormerlySerializedAs("voronoiRenderMode")] [SerializeField] private RenderMode voronoiVoronoiRenderMode = PolygonRenderer.PolygonRenderMode.OutlinedMesh;

		protected override void InitializeRenderer()
		{
			_voronoiRenderer ??= Renderer
			                     ?? VoronoiRenderer.Instantiate(
				                     voronoi,
				                     transform,
				                     "VORONOI Renderer",
				                     projectOnTerrain: true,
				                     centeredScale: PolygonScale,
				                     renderMode: voronoiVoronoiRenderMode
			                     );
			_voronoiRenderer.Voronoi = voronoi;

			Renderer.ToggleShadows(false);
			
			// Selected
			InitializeSelectedRenderer();

			base.InitializeRenderer();
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();
			if (Renderer != null && Renderer.Active) 
				Renderer.UpdateVoronoi();
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			if (Renderer.Active) Renderer.UpdatePolygons();
		}

		private void UpdateRenderer(int i, Polygon poly)
		{
			if (Renderer.Active) Renderer.SetPolygon(i, poly);
		}
		
		/// <summary>
		/// Posiciona el Renderer del Voronoi ajustado al AABB
		/// </summary>
		protected override void PositionRenderer()
		{
			base.PositionRenderer();

			if (Renderer == null) return;
			
			BoundsComp.TransformToBounds_Local(Renderer);

			PositionSelectedRenderer();
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

		[Space]
		[Header("Polygon Selection")]
		[SerializeField] private bool _hoverPolygon = true;
		[SerializeField] private bool _selectPolygon = true;

		// Polygon Hovered
		public bool IsPolygonHovering => _hoverPolygon && mousePolygonIndex != -1;

		// Polygon Selected when clicking
		[HideInInspector] public int selectedPolygonIndex = -1;

		public Polygon? SelectedPolygon =>
			IsPolygonSelected ? voronoi.polygons[selectedPolygonIndex] : null;

		public bool IsPolygonSelected =>
			_selectPolygon && selectedPolygonIndex != -1 && selectedPolygonIndex < PolygonCount;

		private Polygon LastPolygonGenerated => voronoi.polygons[^1];


		#region RENDERING

		[SerializeField] private PolygonRenderer selectedRenderer;
		[SerializeField] private PolygonRenderer hoverRenderer;

		private void InitializeSelectedRenderer()
		{
			selectedRenderer ??= PolygonRenderer.Instantiate(
				Polygon.Empty,
				transform,
				"Selected Polygon",
				PolygonRenderer.PolygonRenderMode.Wire,
				Color.yellow,
				1.1f,
				PolygonScale + 0.01f,
				true
			);
			hoverRenderer ??= PolygonRenderer.Instantiate(
				Polygon.Empty,
				transform,
				"Hover Polygon",
				PolygonRenderer.PolygonRenderMode.Wire,
				Color.yellow,
				1f,
				PolygonScale + 0.01f,
				true
			);

			PositionSelectedRenderer();
		}

		/// <summary>
		/// Posiciona los Renderers de Selected y Hover ajustados al AABB
		/// </summary>
		private void PositionSelectedRenderer()
		{
			if (selectedRenderer == null) return;

			BoundsComp.TransformToBounds_Local(selectedRenderer);
			BoundsComp.TransformToBounds_Local(hoverRenderer);
			
			selectedRenderer.transform.localPosition += Vector3.up * 0.01f;
			hoverRenderer.transform.localPosition += Vector3.up * 0.01f;
		}

		private void UpdateHoverRenderer() => hoverRenderer.Polygon = MousePolygon ?? Polygon.Empty;
		private void UpdateSelectedRenderer() => selectedRenderer.Polygon = SelectedPolygon ?? Polygon.Empty;

		#endregion


		/// <summary>
		///     Update Selected and Hover Polygon by Mouse Input
		/// </summary>
		private void UpdateSelectedPolygon()
		{
			ToggleHover(_hoverPolygon);
			ToggleSelected(_selectPolygon);
			UpdateHoverRenderer();

			// CLICK DOWN => Select, and start dragging
			bool mouseDown = Input.GetMouseButtonDown(0);
			bool mouseUp = Input.GetMouseButtonUp(0);
			bool mouse = Input.GetMouseButton(0);

			if (mouseDown) SelectPolygon(mousePolygonIndex);

			if (!mouseDown && !mouseUp && mouse && IsPolygonSelected)
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
		private Vector2 _draggingOffset = Vector2.zero;

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			voronoi.Seeds = seeds;
			voronoi.GenerateVoronoi();

			Renderer.UpdatePolygons();

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
			get => voronoiVoronoiRenderMode;
			set
			{
				voronoiVoronoiRenderMode = value;
				Renderer.RenderMode = value;
			}
		}

		public bool WireVoronoi
		{
			get => VoronoiRenderMode == PolygonRenderer.PolygonRenderMode.Wire && Renderer.Active;
			set => VoronoiRenderMode = value ? PolygonRenderer.PolygonRenderMode.Wire : PolygonRenderer.PolygonRenderMode.OutlinedMesh;
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
