using System;
using System.Linq;
using DavidUtils.DebugExtensions;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public class Voronoi
	{
		private readonly Vector2[] seeds;
		private readonly Polygon[] _regions;

		private Delaunay _delaunay = new();
		private Delaunay.Triangle[] _triangles = Array.Empty<Delaunay.Triangle>();

		private Vector3[] SeedsInWorld => seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public Voronoi(Vector2[] seeds)
		{
			this.seeds = seeds;
			_regions = new Polygon[seeds.Length];
		}

		public Voronoi(int numSeeds)
			: this(GeometryUtils.GenerateRandomSeeds_RegularDistribution(numSeeds))
		{
		}
		
		public void GenerateVoronoi()
		{
			_delaunay.vertices = seeds;
			_triangles = _delaunay.Triangulate(seeds);
		}

		#region DEBUG

		private int _iteration;
		
		public void GenerateVoronoi_OneIteration()
		{
		}

		public void OnDrawGizmos(Vector3 pos, Vector2 size, Color seedColor = default)
		{
			// SEEDS
			Gizmos.color = seedColor;
			foreach (Vector2 seed in seeds)
			{
				Vector2 seedScaled = seed * size;
				Vector3 seedPos = new Vector3(seedScaled.x, 0, seedScaled.y) + pos;
				Gizmos.DrawSphere(seedPos, 1f);
			}

			// GRID
			Gizmos.color = Color.blue;
			GizmosExtensions.DrawQuadWire(pos, size.ToVector3xz());
			
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
			GizmosExtensions.DrawGrid(cellRows, cellRows, pos, size);
			
			// REGIONES
			Color[] colors = Color.red.GetRainBowColors(_regions.Length);
			for (var i = 0; i < _regions.Length; i++)
				_regions[i].OnDrawGizmos(colors[i]);
		}

		#endregion
	}
}
