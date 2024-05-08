using System.Collections;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : DelaunayGenerator
	{
		public Voronoi voronoi;
		public Polygon[] Regions => voronoi.regions.ToArray();

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
			if (animatedVoronoi)
			{
				yield return voronoi.AnimationCoroutine(delay);
				voronoi.delaunay.drawGizmos = false;
			}
			else
			{
				voronoi.GenerateVoronoi();
			}
		}

		protected override void Run_OneIteration()
		{
			if (!delaunay.ended) delaunay.Run_OnePoint();
			else voronoi.Run_OneIteration();
		}


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

		protected override void Update()
		{
			// Update Hover Region
			if (MouseRegionIndex != -1) hoveredRegionIndex = MouseRegionIndex;

			// Update Selected Region if CLICK
			if (canSelectRegion && Input.GetMouseButtonUp(0))
				selectedRegionIndex = MouseRegionIndex;
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
			base.OnDrawGizmos();
			voronoi.OnDrawGizmos(LocalToWorldMatrix, colors);

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
		}

		public void DrawRegionGizmos(Polygon region) =>
			voronoi.OnDrawGizmos(LocalToWorldMatrix, colors);

		public void DrawRegionGizmos_Detailed(Polygon region) =>
			voronoi.DrawRegionGizmos_Detailed(region, LocalToWorldMatrix, projectOnTerrain);

		public void DrawRegionGizmos_Highlighted(Polygon region) =>
			voronoi.DrawRegionGizmos_Highlighted(region, LocalToWorldMatrix, projectOnTerrain);

#endif

		#endregion
	}
}
