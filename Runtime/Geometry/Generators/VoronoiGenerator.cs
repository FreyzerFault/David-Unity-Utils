using System.Collections;
using System.Globalization;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using DavidUtils.Rendering;
using Geometry.Algorithms;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : DelaunayGenerator
	{
		#region VORONOI

		[Space]
		[Header("VORONOI")]
		
		public Voronoi voronoi;
		
		public Polygon[] Regions => voronoi.regions.ToArray();
		public int RegionsCount => voronoi.regions.Count;

		#endregion


		#region UNITY

		protected override void Update() => UpdateMouseRegion();

		#endregion


		#region MAIN METHODS

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

		#endregion


		#region ANIMATION

		public bool animatedVoronoi = true;

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
			voronoiRenderer.UpdateRegion(region, i);

		protected void OnAllRegionsCreated() =>
			voronoiRenderer.Update(Regions);

		#endregion
		

		#region RENDERING

		[Space]
		[SerializeField] private PolygonRenderer voronoiRenderer = new();

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			voronoiRenderer.Initialize(transform, "VORONOI Renderer");
			
			voronoiRenderer.RenderParent.transform.localPosition = Bounds.min.ToV3xz();
			voronoiRenderer.RenderParent.transform.localScale = Bounds.Size.ToV3xz().WithY(1);
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();
			voronoiRenderer.Update(Regions);
			if (CanProjectOnTerrain && Terrain.activeTerrain != null)
				voronoiRenderer.ProjectOnTerrain(Terrain.activeTerrain);
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			voronoiRenderer.Update(Regions);
		}
		
		#endregion


		#region SELECT REGION with MOUSE

		#region MOUSE

		private Vector3 MousePos => MouseInputUtils.MouseWorldPosition;

		// MOUSE to COORDS in VORONOI Bounding Box
		private Vector2 MousePosNorm =>
			Bounds.ApplyTransform_XZ(transform.localToWorldMatrix).NormalizeMousePosition_XZ();
		private bool MouseInBounds => MousePosNorm.IsIn01();
		
		// Region under Mouse
		protected int MouseRegionIndex => MouseInBounds ? voronoi.GetRegionIndex(MousePosNorm) : -1;
		protected Polygon? MouseRegion => MouseRegionIndex == -1 ? null : voronoi.regions[MouseRegionIndex];

		#endregion


		#region SELECTION
		
		[Space]
		[Header("Region Selection")]
		public bool canSelectRegion = true;
		public bool drawSelectedTriangles = true;

		// Region Hovered
		[HideInInspector] public int hoveredRegionIndex = -1;
		public Polygon? HoveredRegion => IsRegionHovering ? voronoi.regions[hoveredRegionIndex] : null;
		public bool IsRegionHovering => hoveredRegionIndex != -1;

		// Region Selected when clicking
		[HideInInspector] public int selectedRegionIndex = -1;
		public Polygon? SelectedRegion => IsRegionSelected ? voronoi.regions[selectedRegionIndex] : null;
		public bool IsRegionSelected => selectedRegionIndex != -1;

		private Polygon LastRegionGenerated => voronoi.regions[^1];
		
		private void UpdateMouseRegion()
		{
			voronoiRenderer.ToggleHightlighted(MouseRegion.HasValue);
			voronoiRenderer.ToggleSelected(SelectedRegion.HasValue);

			int mouseRegionIndex = MouseRegionIndex;
			
			if (mouseRegionIndex == -1) return;
			
			// HOVER
			HoverRegion(mouseRegionIndex);

			// Para seleccionar debe estar activa la selección y debe haber acabado la generación
			if (!canSelectRegion || !voronoi.Ended) return;

			// CLICK DOWN => Select, and start dragging
			if (Input.GetMouseButtonDown(0)) SelectRegion(MouseRegionIndex);

			// DRAGGING
			if (SelectedRegion.HasValue && Input.GetMouseButton(0))
			{
				// NO HOVERING
				HoverRegion(-1);
				MoveSeed(selectedRegionIndex, MousePosNorm - _draggingOffset);
			}
		}

		public void HoverRegion(int index)
		{
			hoveredRegionIndex = MouseRegionIndex;
			
			voronoiRenderer.ToggleHightlighted(index != -1);
			
			if (HoveredRegion.HasValue) 
				voronoiRenderer.SetHightlightedRegion(HoveredRegion.Value);
		}

		public void SelectRegion(int index)
		{
			selectedRegionIndex = index;
			voronoiRenderer.ToggleSelected(index != -1);
			
			if (SelectedRegion.HasValue)
			{
				voronoiRenderer.SetSelectedRegion(SelectedRegion.Value);
				_draggingOffset = MousePosNorm - SelectedRegion.Value.centroid;
			}
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

			voronoiRenderer.Update(Regions);

			return true;
		}

		private void UpdateDragRegion()
		{
			
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
		
		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			
			if (!drawGizmos) return;
			
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

			delaunay.OnDrawGizmos(LocalToWorldMatrix, CanProjectOnTerrain);
		}

		public void DrawVoronoiGizmos() =>
			voronoi.OnDrawGizmos(LocalToWorldMatrix, voronoiRenderer.regionScale, voronoiRenderer.colors.ToArray());

		public void DrawRegionGizmos_Detailed(Polygon region) =>
			voronoi.DrawRegionGizmos_Detailed(region, LocalToWorldMatrix, CanProjectOnTerrain);

		public void DrawRegionGizmos_Highlighted(Polygon region) =>
			voronoi.DrawRegionGizmos_Highlighted(
				region,
				LocalToWorldMatrix,
				voronoiRenderer.regionScale,
				CanProjectOnTerrain
			);

#endif

		#endregion
	}
}
