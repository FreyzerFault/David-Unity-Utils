using System.Collections;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : DelaunayGenerator
	{
		public Voronoi voronoi;
		private Polygon[] Regions => voronoi.regions.ToArray();


		public override void Initialize()
		{
			base.Initialize();
			voronoi.Seeds = seeds;
		}

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();
			if (animated)
			{
				yield return voronoi.AnimationCoroutine(delay);
				drawGrid = false;
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


#if UNITY_EDITOR

		#region DEBUG

		public bool canSelectRegion = true;

		protected override void OnDrawGizmos()
		{
			Matrix4x4 matrix = transform.localToWorldMatrix;
			base.OnDrawGizmos();
			voronoi.OnDrawGizmos(matrix, colors);

			// Mientras se Genera, dibujamos detallada la ultima region generada
			if (!voronoi.Ended) voronoi.DrawRegionGizmos_Detailed(voronoi.regions[^1], matrix, projectOnTerrain);

			if (!canSelectRegion) return;

			// MOUSE to COORDS in VORONOI Bounding Box
			Vector2 mousePosNorm = (Bounds * matrix).NormalizeMousePosition_XZ();

			// Mouse Pos
			MouseInputUtils.DrawGizmos_XZ();

			// Dibujar solo si el raton esta encima o esta animandose y es la ultima region añadida
			if (!mousePosNorm.IsNormalized()) return;

			Polygon? selectedRegion = voronoi.GetRegion(mousePosNorm);
			if (selectedRegion.HasValue)
				voronoi.DrawRegionGizmos_Detailed(selectedRegion.Value, matrix);
		}

		#endregion

#endif
	}
}
