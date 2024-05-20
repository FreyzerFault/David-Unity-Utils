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

		[SerializeField] private Renderer voronoiRenderer;

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

		[Serializable]
		private class Renderer
		{
			// Parents
			public Transform lineParent;
			public Transform meshParent;

			public readonly List<LineRenderer> lineRenderers = new();
			public readonly List<MeshRenderer> meshRenderers = new();
			public readonly List<MeshFilter> meshFilters = new();

			[SerializeField] [Range(.2f, 1)]
			public float regionScale = .9f;

			public bool active = true;
			public bool wire;

			public void Initialize(Transform parent)
			{
				lineParent = ObjectGenerator.InstantiateEmptyObject(parent, "VORONOI Line Renderers").transform;
				meshParent = ObjectGenerator.InstantiateEmptyObject(parent, "VORONOI Mesh Renderers").transform;

				UpdateVisibility();

				InitializeSpetialRenderers(parent);
			}

			/// <summary>
			///     Update ALL Renderers
			///     Si hay mas Regions que Renderers, instancia nuevos
			///     Elimina los Renderers sobrantes
			/// </summary>
			public void Update(Polygon[] regions)
			{
				if (!active) return;

				if (regions.Length != colors.Count)
					SetRainbowColors(regions.Length);

				for (var i = 0; i < regions.Length; i++)
				{
					Polygon region = regions[i];
					UpdateRegion(region, i);
				}

				if (regions.Length >= meshFilters.Count) return;

				// Elimina los Renderers sobrantes
				int removeCount = meshFilters.Count - regions.Length;

				for (int i = regions.Length; i < meshFilters.Count; i++)
				{
					Destroy(meshFilters[i].gameObject);
					Destroy(lineRenderers[i].gameObject);
				}

				meshFilters.RemoveRange(regions.Length, removeCount);
				meshRenderers.RemoveRange(regions.Length, removeCount);
				lineRenderers.RemoveRange(regions.Length, removeCount);
			}

			public void UpdateRegion(Polygon region, int i)
			{
				Polygon scaledRegion = region.ScaleByCenter(regionScale);
				if (i >= meshRenderers.Count)
				{
					SetRainbowColors(i + 1);
					InstatiateRegion(scaledRegion, colors[i]);
				}
				else
				{
					meshFilters[i].sharedMesh.SetPolygon(scaledRegion);
					lineRenderers[i].SetPolygon(scaledRegion);
				}
			}

			private void InstatiateRegion(Polygon region, Color color)
			{
				// LINE
				lineRenderers.Add(region.LineRenderer(lineParent, color));

				// MESH
				region.InstantiateMesh(out MeshRenderer mr, out MeshFilter mf, meshParent, "Region", color);
				meshRenderers.Add(mr);
				meshFilters.Add(mf);
			}

			public void UpdateVisibility()
			{
				lineParent.gameObject.SetActive(active && wire);
				meshParent.gameObject.SetActive(active && !wire);
			}

			public void Clear()
			{
				for (var i = 0; i < meshFilters.Count; i++)
				{
					Destroy(meshFilters[i].gameObject);
					Destroy(lineRenderers[i].gameObject);
				}

				lineRenderers.Clear();
				meshFilters.Clear();
				meshRenderers.Clear();
			}

			public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f)
			{
				foreach (MeshFilter mf in meshFilters)
					terrain.ProjectMeshInTerrain(mf.sharedMesh, mf.transform, offset);

				foreach (LineRenderer lr in lineRenderers)
					lr.SetPoints(lr.GetPoints().Select(p => terrain.Project(p, offset)).ToArray());
			}


			#region COLOR

			public List<Color> colors = new();

			protected void SetRainbowColors(int numColors)
			{
				if (numColors == colors.Count) return;
				if (numColors > colors.Count)
					colors.AddRange(
						(colors.Count == 0 ? Color.magenta : colors.Last())
						.GetRainBowColors(numColors - colors.Count + 1)
						.Skip(1)
					);
				else
					colors.RemoveRange(numColors, colors.Count - numColors);
			}

			#endregion

			#region SPETIAL REGIONS

			public LineRenderer hightlightedRegionLineRenderer;
			public LineRenderer selectedRegionLineRenderer;

			public void SetHightlightedRegion(Polygon region) =>
				hightlightedRegionLineRenderer.SetPolygon(region.ScaleByCenter(regionScale));

			public void SetSelectedRegion(Polygon region) =>
				selectedRegionLineRenderer.SetPolygon(region.ScaleByCenter(regionScale));

			public void ToggleHightlighted(bool toggle) => hightlightedRegionLineRenderer.gameObject.SetActive(toggle);
			public void ToggleSelected(bool toggle) => selectedRegionLineRenderer.gameObject.SetActive(toggle);

			private void InitializeSpetialRenderers(Transform parent)
			{
				// SELECTED & HIGHTLIGHTED (hovered)
				hightlightedRegionLineRenderer = LineRendererExtensions.LineRenderer(
					parent,
					"Hightlighted Region",
					colors: new[] { Color.yellow },
					loop: true
				);
				selectedRegionLineRenderer = LineRendererExtensions.LineRenderer(
					parent,
					"Selected Region",
					colors: new[] { Color.yellow },
					thickness: .2f,
					loop: true
				);
			}

			#endregion
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
					draggingOffset = MousePosNorm - SelectedRegion.Value.centroid;
			}

			// DRAGGING
			if (SelectedRegion.HasValue && Input.GetMouseButton(0))
			{
				voronoiRenderer.SetSelectedRegion(SelectedRegion.Value);
				voronoiRenderer.ToggleHightlighted(false);

				MoveSeed(selectedRegionIndex, MousePosNorm - draggingOffset);
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
