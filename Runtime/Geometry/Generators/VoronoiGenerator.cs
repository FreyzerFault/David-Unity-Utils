using System.Collections;
using System.Globalization;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using DavidUtils.Rendering;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : DelaunayGenerator
	{
		public Voronoi voronoi;
		public Polygon[] Regions => voronoi.regions.ToArray();
		public int RegionsCount => voronoi.regions.Count;

		public bool animatedVoronoi = true;

		public override void Reset()
		{
			base.Reset();

			selectedRegionIndex = -1;
			hoveredRegionIndex = -1;

			if (voronoi == null)
			{
				voronoi ??= new Voronoi(seeds, delaunay);
			}
			else
			{
				voronoi.Seeds = seeds;
				voronoi.delaunay = delaunay;
			}

			voronoiRenderer.Clear();
		}

		public override void Run()
		{
			Reset();
			if (animatedDelaunay)
			{
				animationCoroutine = StartCoroutine(RunCoroutine());
			}
			else
			{
				delaunay.RunTriangulation();
				OnTrianglesUpdated();
				voronoi.GenerateVoronoi();
				for (var i = 0; i < RegionsCount; i++)
					OnRegionCreated(voronoi.regions[i], i);
			}
		}

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();

			if (animatedVoronoi)
			{
				bool delaunayWire = DelaunayWire;
				DelaunayWire = true;

				while (!voronoi.Ended)
				{
					voronoi.Run_OneIteration();
					OnRegionCreated(voronoi.regions[^1], RegionsCount - 1);
					yield return new WaitForSecondsRealtime(delayMilliseconds);
				}

				DelaunayWire = delaunayWire;
			}
			else
			{
				voronoi.GenerateVoronoi();

				for (var i = 0; i < RegionsCount; i++)
					OnRegionCreated(voronoi.regions[i], i);
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

		private void OnRegionCreated(Polygon region, int i) =>
			voronoiRenderer.UpdateRegion(region, i);


		#region RENDERING

		[SerializeField] private PolygonRenderer voronoiRenderer = new();

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			voronoiRenderer.Initialize(transform);
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();
			voronoiRenderer.Update(Regions);
			if (projectOnTerrain && Terrain.activeTerrain != null)
				voronoiRenderer.ProjectOnTerrain(Terrain.activeTerrain);
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			voronoiRenderer.Update(Regions);
		}

		#endregion


		#region MOUSE INPUTS

		private Vector3 MousePos => MouseInputUtils.MouseWorldPosition;

		// MOUSE to COORDS in VORONOI Bounding Box
		private Vector2 MousePosNorm =>
			Bounds.ApplyTransform_XZ(transform.localToWorldMatrix).NormalizeMousePosition_XZ();
		private bool MouseInBounds => MousePosNorm.IsIn01();

		// Region under Mouse
		protected int MouseRegionIndex => MouseInBounds ? voronoi.GetRegionIndex(MousePosNorm) : -1;
		protected Polygon? MouseRegion => MouseRegionIndex == -1 ? null : voronoi.regions[MouseRegionIndex];

		// Region Hovered
		public int hoveredRegionIndex = -1;
		public Polygon? HoveredRegion => hoveredRegionIndex == -1 ? null : voronoi.regions[hoveredRegionIndex];

		// Region Selected when clicking
		public int selectedRegionIndex = -1;
		public Polygon? SelectedRegion => selectedRegionIndex == -1 ? null : voronoi.regions[selectedRegionIndex];

		// For dragging a region
		private Vector2 _draggingOffset = Vector2.zero;

		protected override void Update()
		{
			voronoiRenderer.ToggleHightlighted(MouseRegion.HasValue);
			voronoiRenderer.ToggleSelected(SelectedRegion.HasValue);

			// Update Hover Region
			if (MouseRegionIndex != -1) hoveredRegionIndex = MouseRegionIndex;

			if (HoveredRegion.HasValue)
				voronoiRenderer.SetHightlightedRegion(HoveredRegion.Value);

			if (!canSelectRegion || !voronoi.Ended) return;

			// Update Selected Region if CLICK and start dragging
			if (Input.GetMouseButtonDown(0))
			{
				selectedRegionIndex = MouseRegionIndex;
				if (SelectedRegion.HasValue)
					_draggingOffset = MousePosNorm - SelectedRegion.Value.centroid;
			}

			// DRAGGING
			if (SelectedRegion.HasValue && Input.GetMouseButton(0))
			{
				voronoiRenderer.SetSelectedRegion(SelectedRegion.Value);
				voronoiRenderer.ToggleHightlighted(false);

				MoveSeed(selectedRegionIndex, MousePosNorm - _draggingOffset);
			}
		}

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			voronoi.Seeds = seeds;
			voronoi.GenerateVoronoi();

			voronoiRenderer.Update(Regions);

			return true;
		}

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
			get => voronoiRenderer.active;
			set
			{
				voronoiRenderer.active = value;
				voronoiRenderer.UpdateVisibility();
			}
		}

		public bool WireVoronoi
		{
			get => voronoiRenderer.wire && voronoiRenderer.active;
			set
			{
				voronoiRenderer.wire = value;
				voronoiRenderer.UpdateVisibility();
			}
		}

		public float RegionScale
		{
			get => voronoiRenderer.regionScale;
			set
			{
				voronoiRenderer.Update(Regions);
				voronoiRenderer.regionScale = value;
			}
		}

		public override bool Animated
		{
			get => animatedVoronoi || animatedDelaunay;
			set => animatedVoronoi = animatedDelaunay = value;
		}
		public string DelayMillisecondsStr
		{
			get => (delayMilliseconds * 1000).ToString("N0");
			set => delayMilliseconds = int.Parse(value, NumberStyles.Integer) / 1000f;
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		public bool canSelectRegion = true;
		public bool drawSelectedTriangles = true;

		private Polygon LastRegionGenerated => voronoi.regions[^1];
		private Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix;

		protected override void OnDrawGizmos()
		{
			if (DrawSeeds) GizmosSeeds();
			if (drawGrid) GizmosBoundingBox();

			DrawVoronoiGizmos();

			// Mientras se Genera, dibujamos detallada la ultima region generada
			if (voronoi.regions != null && voronoi.regions.Count != 0 && !voronoi.Ended)
				DrawRegionGizmos_Detailed(LastRegionGenerated);

			// Mouse Position
			MouseInputUtils.DrawGizmos_XZ();

			if (HoveredRegion.HasValue) DrawRegionGizmos_Highlighted(HoveredRegion.Value);
			if (SelectedRegion.HasValue)
			{
				DrawRegionGizmos_Highlighted(SelectedRegion.Value);
				if (drawSelectedTriangles)
					DrawRegionGizmos_Detailed(SelectedRegion.Value);
			}

			delaunay.OnDrawGizmos(LocalToWorldMatrix, projectOnTerrain);
		}

		public void DrawVoronoiGizmos() =>
			voronoi.OnDrawGizmos(LocalToWorldMatrix, voronoiRenderer.regionScale, voronoiRenderer.colors.ToArray());

		public void DrawRegionGizmos_Detailed(Polygon region) =>
			voronoi.DrawRegionGizmos_Detailed(region, LocalToWorldMatrix, projectOnTerrain);

		public void DrawRegionGizmos_Highlighted(Polygon region) =>
			voronoi.DrawRegionGizmos_Highlighted(
				region,
				LocalToWorldMatrix,
				voronoiRenderer.regionScale,
				projectOnTerrain
			);

#endif

		#endregion
	}
}
