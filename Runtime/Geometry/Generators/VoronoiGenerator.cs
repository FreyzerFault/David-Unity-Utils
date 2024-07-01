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

		public Polygon[] Regions => voronoi?.regions?.ToArray();
		public int RegionsCount => voronoi.regions.Count;

		#endregion


		#region UNITY

		protected override void Update()
		{
			UpdateMouseRegion();
			UpdateSelectedRegion();
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
			selectedRegionIndex = -1;
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
				OnAllRegionsCreated();
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
					OnRegionCreated(voronoi.regions[^1], RegionsCount - 1);
					yield return new WaitForSecondsRealtime(DelaySeconds);
				}

				OnAllRegionsCreated();
			}
			else
			{
				voronoi.GenerateVoronoi();
				OnAllRegionsCreated();
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
				OnRegionCreated(voronoi.regions[^1], RegionsCount - 1);
			}
		}

		#endregion


		#region REGION EVENTS

		protected void OnRegionCreated(Polygon region, int i) =>
			UpdateRenderer(i, region);

		protected void OnAllRegionsCreated()
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
				                     centeredScale: RegionScale,
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
			if (Renderer.Active) Renderer.UpdateRegionPolygons();
		}

		private void UpdateRenderer(int i, Polygon region)
		{
			if (Renderer.Active) Renderer.SetRegionPolygon(i, region);
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


		#region SELECT REGION with MOUSE

		#region MOUSE

		// Region under Mouse (guarda el index de la region)
		protected int mouseRegionIndex = -1;

		protected Polygon? MouseRegion =>
			mouseRegionIndex != -1 && mouseRegionIndex < RegionsCount
				? voronoi.regions[mouseRegionIndex]
				: null;

		// MOUSE in AABB [0,0 - 1,1]
		private Vector2 MousePosNorm =>
			AABB.ApplyTransform_XZ(transform.localToWorldMatrix).NormalizeMousePosition_XZ();

		private void UpdateMouseRegion()
		{
			Vector2 mouseNormPos = AABB.ApplyTransform_XZ(transform.localToWorldMatrix).NormalizeMousePosition_XZ();
			mouseRegionIndex = mouseNormPos.IsIn01() ? voronoi.GetRegionIndex(MousePosNorm) : -1;
		}

		#endregion


		#region SELECTION

		[Space]
		[Header("Region Selection")]
		[SerializeField] private bool _hoverRegion = true;
		[SerializeField] private bool _selectRegion = true;

		// Region Hovered
		public bool IsRegionHovering => _hoverRegion && mouseRegionIndex != -1;

		// Region Selected when clicking
		[HideInInspector] public int selectedRegionIndex = -1;

		public Polygon? SelectedRegion =>
			IsRegionSelected ? voronoi.regions[selectedRegionIndex] : null;

		public bool IsRegionSelected =>
			_selectRegion && selectedRegionIndex != -1 && selectedRegionIndex < RegionsCount;

		private Polygon LastRegionGenerated => voronoi.regions[^1];


		#region RENDERING

		[SerializeField] private PolygonRenderer selectedRenderer;
		[SerializeField] private PolygonRenderer hoverRenderer;

		private void InitializeSelectedRenderer()
		{
			selectedRenderer ??= PolygonRenderer.Instantiate(
				Polygon.Empty,
				transform,
				"Selected Region",
				PolygonRenderer.PolygonRenderMode.Wire,
				Color.yellow,
				1.1f,
				RegionScale + 0.01f,
				true
			);
			hoverRenderer ??= PolygonRenderer.Instantiate(
				Polygon.Empty,
				transform,
				"Hover Region",
				PolygonRenderer.PolygonRenderMode.Wire,
				Color.yellow,
				1f,
				RegionScale + 0.01f,
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

		private void UpdateHoverRenderer() => hoverRenderer.Polygon = MouseRegion ?? Polygon.Empty;
		private void UpdateSelectedRenderer() => selectedRenderer.Polygon = SelectedRegion ?? Polygon.Empty;

		#endregion


		/// <summary>
		///     Update Selected and Hover Region by Mouse Input
		/// </summary>
		private void UpdateSelectedRegion()
		{
			ToggleHover(_hoverRegion);
			ToggleSelected(_selectRegion);
			UpdateHoverRenderer();

			// CLICK DOWN => Select, and start dragging
			bool mouseDown = Input.GetMouseButtonDown(0);
			bool mouseUp = Input.GetMouseButtonUp(0);
			bool mouse = Input.GetMouseButton(0);

			if (mouseDown) SelectRegion(mouseRegionIndex);

			if (!mouseDown && !mouseUp && mouse && IsRegionSelected)
			{
				// DRAGGING
				// Hide Hover Region, Selected only
				ToggleHover(false);
				MoveSeed(selectedRegionIndex, MousePosNorm - _draggingOffset);
			}
		}

		private void ToggleHover(bool toggle) => hoverRenderer.gameObject.SetActive(toggle);
		private void ToggleSelected(bool toggle) => selectedRenderer.gameObject.SetActive(toggle);

		/// <summary>
		///     Select Region by Index
		///     Index == -1 => Deselect
		/// </summary>
		private void SelectRegion(int index)
		{
			selectedRegionIndex = index;
			UpdateSelectedRenderer();

			if (index == -1) return;

			// Update dragging offset to move the region from any point of the polygon
			_draggingOffset = MousePosNorm - seeds[index];
		}

		#endregion


		#region DRAGGING

		// For dragging a region
		private Vector2 _draggingOffset = Vector2.zero;

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			voronoi.Seeds = seeds;
			voronoi.GenerateVoronoi();

			Renderer.UpdateRegionPolygons();

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

		public float RegionScale
		{
			get => Renderer.RegionScale;
			set => Renderer.RegionScale = value;
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

			// Mientras se Genera, dibujamos detallada la ultima region generada
			if (voronoi.regions.NotNullOrEmpty() && !voronoi.Ended)
				DrawRegionGizmos_Detailed(RegionsCount - 1);

			// Mouse Position
			MouseInputUtils.DrawGizmos_XZ();

			// DrawRegionGizmos_Highlighted(mouseRegionIndex);
			//
			// DrawRegionGizmos_Highlighted(selectedRegionIndex);

			if (drawSelectedTriangles)
				DrawRegionGizmos_Detailed(selectedRegionIndex);
		}

		public void DrawVoronoiGizmos() =>
			voronoi.OnDrawGizmos(BoundsComp.LocalToWorldMatrix_WithXZrotation, Renderer.RegionScale, Renderer.colors.ToArray());

		public void DrawRegionGizmos_Detailed(int index) =>
			voronoi.DrawRegionGizmos_Detailed(index, BoundsComp.LocalToWorldMatrix_WithXZrotation, true);

		public void DrawRegionGizmos_Highlighted(int index) =>
			voronoi.DrawRegionGizmos_Highlighted(
				index,
				BoundsComp.LocalToWorldMatrix_WithXZrotation,
				Renderer.RegionScale,
				true
			);

#endif

		#endregion
	}
}
