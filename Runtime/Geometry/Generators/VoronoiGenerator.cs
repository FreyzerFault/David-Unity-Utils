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

		private void OnRegionCreated(Polygon region, int i) =>
			_renderer.UpdateRegion(region, i);


		#region RENDERING

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

			public Renderer(Transform parent)
			{
				lineParent = ObjectGenerator.InstantiateEmptyObject(parent, "VORONOI Line Renderers").transform;
				meshParent = ObjectGenerator.InstantiateEmptyObject(parent, "VORONOI Mesh Renderers").transform;

				UpdateVisibility();

				InitializeSpetialRenderers(parent);
			}

			private void InstatiateRegion(Polygon region, Color color)
			{
				// LINE
				Polygon scaledRegion = region.ScaleByCenter(regionScale);
				lineRenderers.Add(scaledRegion.LineRenderer(lineParent, color));

				// MESH
				scaledRegion.InstantiateMesh(out MeshRenderer mr, out MeshFilter mf, meshParent, "Region", color);
				meshRenderers.Add(mr);
				meshFilters.Add(mf);
			}

			public void UpdateRegion(Polygon region, int i)
			{
				if (i >= meshRenderers.Count)
				{
					SetRainbowColors(i + 1);
					InstatiateRegion(region, colors[i]);
				}
				else
				{
					meshFilters[i].sharedMesh.SetPolygon(region.ScaleByCenter(regionScale));
					lineRenderers[i].SetPoints(region.ScaleByCenter(regionScale).Vertices3D_XY.ToArray());
				}
			}

			/// <summary>
			///     Update ALL Renderers
			///     Si hay mas Regions que Renderers, instancia nuevos
			///     Elimina los Renderers sobrantes
			/// </summary>
			public void Update(Polygon[] regions)
			{
				if (regions.Length != colors.Count)
					SetRainbowColors(regions.Length);

				for (var i = 0; i < regions.Length; i++)
				{
					Polygon region = regions[i].ScaleByCenter(regionScale);
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

			#region COLOR

			public List<Color> colors;

			protected void SetRainbowColors(int numColors)
			{
				if (numColors == colors.Count) return;
				if (numColors > colors.Count)
					colors.AddRange(colors[^1].GetRainBowColors(numColors - colors.Count));
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

		private Renderer _renderer;

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();

			_renderer = new Renderer(transform);
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
			_renderer.ToggleHightlighted(MouseRegion.HasValue);
			_renderer.ToggleSelected(SelectedRegion.HasValue);

			// Update Hover Region
			if (MouseRegionIndex != -1) hoveredRegionIndex = MouseRegionIndex;

			if (HoveredRegion.HasValue)
				_renderer.SetHightlightedRegion(HoveredRegion.Value);

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
				_renderer.SetSelectedRegion(SelectedRegion.Value);
				_renderer.ToggleHightlighted(false);

				MoveSeed(selectedRegionIndex, MousePosNorm - draggingOffset);
			}
		}

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			voronoi.Seeds = seeds;
			voronoi.GenerateVoronoi();

			_renderer.Update(Regions);

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
			get => _renderer.active;
			set
			{
				_renderer.active = value;
				_renderer.UpdateVisibility();
			}
		}

		public bool WireVoronoi
		{
			get => _renderer.wire && _renderer.active;
			set
			{
				_renderer.wire = value;
				_renderer.UpdateVisibility();
			}
		}

		public float RegionScale
		{
			get => _renderer.regionScale;
			set
			{
				_renderer.Update(Regions);
				_renderer.regionScale = value;
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
			voronoi.OnDrawGizmos(LocalToWorldMatrix, _renderer.regionScale, seedColors);

		public void DrawRegionGizmos_Detailed(Polygon region) =>
			voronoi.DrawRegionGizmos_Detailed(region, LocalToWorldMatrix, projectOnTerrain);

		public void DrawRegionGizmos_Highlighted(Polygon region) =>
			voronoi.DrawRegionGizmos_Highlighted(region, LocalToWorldMatrix, _renderer.regionScale, projectOnTerrain);

#endif

		#endregion
	}
}
