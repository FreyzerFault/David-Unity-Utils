using System;
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
		}

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();

			bool delaunayDrawGizmos = delaunay.draw;

			if (animatedVoronoi)
			{
				voronoi.delaunay.draw = true;
				while (!voronoi.Ended)
				{
					voronoi.Run_OneIteration();
					OnRegionCreated(voronoi.regions[^1], RegionsCount - 1);
					yield return new WaitForSecondsRealtime(delayMilliseconds);
				}

				voronoi.delaunay.draw = delaunayDrawGizmos;
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

		private void OnRegionCreated(Polygon region, int i)
		{
			if (i < regionMeshRenderers.Count)
			{
				UpdateMeshRenderer(i);
				UpdateLineRenderer(i);
			}
			else
			{
				InstatiateRegion(region, seedColors[i]);
			}
		}

		#region RENDERING

		[SerializeField] [Range(.2f, 1)]
		private float regionScale = .9f;
		public float RegionScale
		{
			get => regionScale;
			set
			{
				UpdateLineRenderers();
				regionScale = value;
			}
		}

		// Parents
		private GameObject voronoilineParent;
		private GameObject voronoiMeshParent;

		private readonly List<LineRenderer> regionLineRenderers = new();
		private readonly List<MeshRenderer> regionMeshRenderers = new();
		private readonly List<MeshFilter> regionMeshFilters = new();

		private LineRenderer hightlightedRegionLineRenderer;
		private LineRenderer selectedRegionLineRenderer;

		protected override void InitializeRenderObjects()
		{
			base.InitializeRenderObjects();

			// PARENTS
			voronoilineParent = ObjectGenerator.InstantiateEmptyObject(transform, "VORONOI Line Renderers");
			voronoiMeshParent = ObjectGenerator.InstantiateEmptyObject(transform, "VORONOI Mesh Renderers");

			UpdateVisibility();

			// SELECTED & HIGHTLIGHTED (hovered)
			hightlightedRegionLineRenderer = new Polyline(Array.Empty<Vector2>(), new[] { Color.yellow },  loop: true).Instantiate(transform, "Hightlighted Region");
			selectedRegionLineRenderer = new Polyline(Array.Empty<Vector2>(), new[] { Color.yellow }, 0.2f,  loop: true).Instantiate(transform, "Selected Region");
		}

		private void InstatiateRegion(Polygon region, Color color)
		{
			regionLineRenderers.Add(region.InstantiateLineRenderer(voronoilineParent.transform, regionScale, color));

			region.Instantiate(voronoiMeshParent.transform, out MeshRenderer mr, out MeshFilter mf, regionScale, color);
			regionMeshRenderers.Add(mr);
			regionMeshFilters.Add(mf);
		}


		private void UpdateMeshRenderer(int i)
		{
			Polygon region = voronoi.regions[i];
			regionMeshFilters[i].sharedMesh = region.CreateMesh(regionScale, seedColors[i]);
		}

		private void UpdateLineRenderer(int i)
		{
			Polygon region = voronoi.regions[i];
			Vector3[] newPoints = region
				.VerticesScaledByCenter(regionScale)
				.Select(v => v.ToV3xz())
				.ToArray();
			regionLineRenderers[i].SetPoints(newPoints);
		}

		protected override void UpdateRenderers()
		{
			base.UpdateRenderers();
			UpdateMeshRenderers();
			UpdateLineRenderers();
		}

		private void UpdateMeshRenderers()
		{
			for (var i = 0; i < RegionsCount; i++)
				if (i >= regionMeshFilters.Count)
					InstatiateRegion(voronoi.regions[i], seedColors[i]);
				else
					UpdateMeshRenderer(i);
		}

		private void UpdateLineRenderers()
		{
			for (var i = 0; i < RegionsCount; i++)
				if (i >= regionLineRenderers.Count)
					InstatiateRegion(voronoi.regions[i], seedColors[i]);
				else
					UpdateLineRenderer(i);
		}

		protected override void ClearRenderers()
		{
			base.ClearRenderers();

			for (var i = 0; i < regionMeshFilters.Count; i++)
			{
				Destroy(regionMeshFilters[i].gameObject);
				Destroy(regionLineRenderers[i].gameObject);
			}

			regionLineRenderers.Clear();
			regionMeshFilters.Clear();
			regionMeshRenderers.Clear();
		}

		private void UpdateVisibility()
		{
			voronoilineParent.SetActive(WireVoronoi);
			voronoiMeshParent.SetActive(DrawVoronoi && !WireVoronoi);
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
			hightlightedRegionLineRenderer.gameObject.SetActive(MouseRegion.HasValue);
			selectedRegionLineRenderer.gameObject.SetActive(SelectedRegion.HasValue);

			// Update Hover Region
			if (MouseRegionIndex != -1) hoveredRegionIndex = MouseRegionIndex;

			if (HoveredRegion.HasValue)
				hightlightedRegionLineRenderer.CopyLineRendererPoints(regionLineRenderers[hoveredRegionIndex]);

			if (!canSelectRegion || !voronoi.Ended) return;

			// Update Selected Region if CLICK and start dragging
			if (Input.GetMouseButtonDown(0))
			{
				selectedRegionIndex = MouseRegionIndex;
				if (SelectedRegion.HasValue)
					draggingOffset = MousePosNorm - SelectedRegion.Value.centroid;
			}

			// DRAGGING
			if (SelectedRegion.HasValue && Input.GetMouseButton(0))
			{
				selectedRegionLineRenderer.CopyLineRendererPoints(regionLineRenderers[selectedRegionIndex]);
				hightlightedRegionLineRenderer.gameObject.SetActive(false);

				MoveSeed(selectedRegionIndex, MousePosNorm - draggingOffset);
			}
		}

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			voronoi.Seeds = seeds;
			voronoi.GenerateVoronoi();

			UpdateRenderers();

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
			get => voronoi.drawGizmos;
			set
			{
				voronoi.drawGizmos = value;
				UpdateVisibility();
			}
		}

		public bool WireVoronoi
		{
			get => voronoi.drawWire && voronoi.drawGizmos;
			set
			{
				voronoi.drawWire = value;
				UpdateVisibility();
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
			voronoi.OnDrawGizmos(LocalToWorldMatrix, regionScale, seedColors);

		public void DrawRegionGizmos_Detailed(Polygon region) =>
			voronoi.DrawRegionGizmos_Detailed(region, LocalToWorldMatrix, projectOnTerrain);

		public void DrawRegionGizmos_Highlighted(Polygon region) =>
			voronoi.DrawRegionGizmos_Highlighted(region, LocalToWorldMatrix, regionScale, projectOnTerrain);

#endif

		#endregion
	}
}
