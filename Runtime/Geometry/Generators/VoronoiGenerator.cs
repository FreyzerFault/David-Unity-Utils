using System.Collections;
using System.Globalization;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using DavidUtils.Rendering;
using Geometry.Algorithms;
using UnityEngine;

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

			voronoi = new Voronoi(seeds, delaunay);

			Renderer.Clear();
			InstantiateRenderer();
		}

		public override void Run()
		{
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
				bool delaunayWire = DelaunayWire;
				DelaunayWire = true;

				while (!voronoi.Ended)
				{
					voronoi.Run_OneIteration();
					OnRegionCreated(voronoi.regions[^1], RegionsCount - 1);
					yield return new WaitForSecondsRealtime(DelaySeconds);
				}

				OnAllRegionsCreated();
				DelaunayWire = delaunayWire;
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

		[Space]
		private VoronoiRenderer _voronoiRenderer;
		private VoronoiRenderer Renderer => _voronoiRenderer ??= GetComponentInChildren<VoronoiRenderer>(true);

		protected override void InitializeRenderer()
		{
			_voronoiRenderer ??= Renderer ?? UnityUtils.InstantiateEmptyObject(transform, "VORONOI Renderer")
				.AddComponent<VoronoiRenderer>();

			// Selected
			InitializeSelectedRenderer();

			base.InitializeRenderer();
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();

			if (!Renderer.Active) return;
			Renderer.Clear();
			Renderer.Instantiate(voronoi);

			if (CanProjectOnTerrain && Terrain.activeTerrain != null)
				Renderer.ProjectOnTerrain(Terrain.activeTerrain);

			Renderer.ToggleShadows(false);
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			if (!Renderer.Active) return;
			Renderer.UpdatePolygons();
			Renderer.ToggleShadows(false);
		}

		private void UpdateRenderer(int i, Polygon region)
		{
			if (!Renderer.Active) return;
			Renderer.SetRegion(i, region);
			Renderer.ToggleShadows(false);
		}


		protected override void PositionRenderer()
		{
			base.PositionRenderer();

			if (Renderer == null) return;
			Renderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			Renderer.transform.localScale = Vector3.one;
			BoundsComp.AdjustTransformToBounds(Renderer);

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
				.1f,
				RegionScale + 0.01f
			);
			hoverRenderer ??= PolygonRenderer.Instantiate(
				Polygon.Empty,
				transform,
				"Hover Region",
				PolygonRenderer.PolygonRenderMode.Wire,
				Color.yellow,
				.05f,
				RegionScale + 0.01f
			);

			PositionSelectedRenderer();
		}

		private void PositionSelectedRenderer()
		{
			if (selectedRenderer == null || hoverRenderer == null) return;
			selectedRenderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			selectedRenderer.transform.localScale = Vector3.one;
			hoverRenderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			hoverRenderer.transform.localScale = Vector3.one;

			BoundsComp.AdjustTransformToBounds(selectedRenderer);
			BoundsComp.AdjustTransformToBounds(hoverRenderer);
			selectedRenderer.transform.localPosition += Vector3.up * 1;
			hoverRenderer.transform.localPosition += Vector3.up * 1;
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

			Renderer.UpdatePolygons();

			UpdateSelectedRenderer();

			return true;
		}

		#endregion

		#endregion


		#region UI CONTROL

		public string NumSeedsStr
		{
			get => Seeds.Count.ToString();
			set
			{
				bool isDigit = int.TryParse(value, out int num);
				if (!isDigit) num = 16;
				numSeeds = num;
			}
		}

		public bool DrawVoronoi
		{
			get => Renderer.Active;
			set => Renderer.Active = value;
		}

		public bool WireVoronoi
		{
			get => Renderer.RenderMode == PolygonRenderer.PolygonRenderMode.Wire && Renderer.Active;
			set => Renderer.RenderMode =
				value ? PolygonRenderer.PolygonRenderMode.Wire : PolygonRenderer.PolygonRenderMode.Mesh;
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
			voronoi.OnDrawGizmos(LocalToWorldMatrix, Renderer.RegionScale, Renderer.colors.ToArray());

		public void DrawRegionGizmos_Detailed(int index) =>
			voronoi.DrawRegionGizmos_Detailed(index, LocalToWorldMatrix, CanProjectOnTerrain);

		public void DrawRegionGizmos_Highlighted(int index) =>
			voronoi.DrawRegionGizmos_Highlighted(
				index,
				LocalToWorldMatrix,
				Renderer.RegionScale,
				CanProjectOnTerrain
			);

#endif

		#endregion
	}
}
