using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : DelaunayGenerator
	{
		public Voronoi voronoi;
		public Polygon[] Regions => voronoi.regions.ToArray();
		public int RegionsCount => voronoi.regions.Count;

		public bool animatedVoronoi = true;

		public override void Initialize()
		{
			base.Initialize();

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

			// REGION COLORS
			seedColors = Color.red.GetRainBowColors(seeds.Count);

			// Reset RENDERERS
			ClearRenderers();
		}

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();

			bool delaunayDrawGizmos = delaunay.drawGizmos;

			if (animatedVoronoi)
			{
				voronoi.delaunay.drawGizmos = true;
				while (!voronoi.Ended)
				{
					voronoi.Run_OneIteration();
					OnRegionCreated(voronoi.regions[^1]);
					yield return new WaitForSecondsRealtime(delayMilliseconds);
				}

				voronoi.delaunay.drawGizmos = delaunayDrawGizmos;
			}
			else
			{
				voronoi.GenerateVoronoi();

				foreach (Polygon region in Regions)
					OnRegionCreated(region);
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
				OnRegionCreated(voronoi.regions[^1]);
			}
		}

		private void OnRegionCreated(Polygon region)
		{
			// Instantiate
			if (DrawVoronoi && RegionsCount > 0)
				InstatiateRegion(region);
		}

		#region RENDERING

		[SerializeField] [Range(.2f, 1)]
		private float regionScale = .9f;
		public float RegionScale
		{
			get => regionScale;
			set
			{
				UpdateLineRenderersPoints();
				regionScale = value;
			}
		}

		private readonly List<LineRenderer> regionLineRenderers = new();
		private readonly List<MeshRenderer> regionMeshRenderers = new();
		private readonly List<MeshFilter> regionMeshFilters = new();

		private void InstatiateRegion(Polygon region)
		{
			if (WireVoronoi)
			{
				regionLineRenderers.Add(
					ShapeDrawing.InstantiatePolygonWire(transform, region, regionScale, seedColors[RegionsCount - 1])
				);
			}
			else
			{
				MeshRenderer mr;
				MeshFilter mf;
				ShapeDrawing.InstantiatePolygon(
					region,
					transform,
					out mr,
					out mf,
					regionScale,
					seedColors[RegionsCount - 1]
				);
				regionMeshRenderers.Add(mr);
				regionMeshFilters.Add(mf);
			}
		}

		private void UpdateMeshRenderersPoints()
		{
			for (var i = 0; i < RegionsCount; i++)
				regionMeshFilters[i].sharedMesh = ShapeDrawing.CreateMesh(
					voronoi.regions[i].VerticesScaledByCenter(regionScale)
				);
		}

		private void UpdateLineRenderersPoints()
		{
			for (var i = 0; i < RegionsCount; i++)
			{
				Vector3[] newPoints = voronoi.regions[i]
					.VerticesScaledByCenter(regionScale)
					.Select(v => v.ToV3xz())
					.ToArray();
				transform.TransformPoints(newPoints);
				regionLineRenderers[i].positionCount = newPoints.Length;
				regionLineRenderers[i].SetPositions(newPoints);
			}
		}

		private void ClearRenderers()
		{
			regionLineRenderers.ForEach(Destroy);
			regionLineRenderers.Clear();
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
		private Vector2 draggingOffset = Vector2.zero;

		protected override void Update()
		{
			// Update Hover Region
			if (MouseRegionIndex != -1) hoveredRegionIndex = MouseRegionIndex;

			if (!canSelectRegion || !voronoi.Ended) return;

			// Update Selected Region if CLICK and start dragging
			if (Input.GetMouseButtonDown(0))
			{
				selectedRegionIndex = MouseRegionIndex;
				if (SelectedRegion.HasValue)
					draggingOffset = MousePosNorm - SelectedRegion.Value.centroid;
			}

			if (Input.GetMouseButton(0) && SelectedRegion.HasValue)
			{
				voronoi.MoveSeed(selectedRegionIndex, MousePosNorm - draggingOffset);
				UpdateLineRenderersPoints();
			}
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
			get => voronoi.drawGizmos;
			set => voronoi.drawGizmos = value;
		}

		public bool DrawDelaunay
		{
			get => delaunay.drawGizmos;
			set => delaunay.drawGizmos = value;
		}

		public bool WireVoronoi
		{
			get => voronoi.drawWire;
			set => voronoi.drawWire = value;
		}

		public bool WireDelaunay
		{
			get => delaunay.drawWire;
			set => delaunay.drawWire = value;
		}

		public bool Animated
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
			if (drawSeeds) DrawSeeds();
			if (drawGrid) DrawBoundingBox();

			voronoi.OnDrawGizmos(LocalToWorldMatrix, regionScale, seedColors);

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

		public void DrawRegionGizmos(Polygon region) =>
			voronoi.OnDrawGizmos(LocalToWorldMatrix, regionScale, seedColors);

		public void DrawRegionGizmos_Detailed(Polygon region) =>
			voronoi.DrawRegionGizmos_Detailed(region, LocalToWorldMatrix, projectOnTerrain);

		public void DrawRegionGizmos_Highlighted(Polygon region) =>
			voronoi.DrawRegionGizmos_Highlighted(region, LocalToWorldMatrix, regionScale, projectOnTerrain);

#endif

		#endregion
	}
}
