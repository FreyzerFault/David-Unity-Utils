using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry
{
	public class Voronoi
	{
		private Vector2 min, max = Vector2.one;
		private readonly Vector2[] seeds;
		private readonly Polygon[] _regions;

		public Voronoi(Vector2[] seeds, Vector2 min, Vector2 max)
		{
			this.seeds = seeds;
			this.min = min;
			this.max = max;
			_regions = new Polygon[seeds.Length];
			GenerateVoronoi();
		}

		public Voronoi(int numSeeds, Vector2 min, Vector2 max)
			: this(GenerateRandomSeeds(numSeeds, min, max), min, max)
		{
		}

		private static Vector2[] GenerateRandomSeeds(int numSeeds, Vector2 min, Vector2 max)
		{
			var seeds = new Vector2[numSeeds];
			for (var i = 0; i < numSeeds; i++)
				seeds[i] = new Vector2(
					Random.Range(min.x, max.x),
					Random.Range(min.y, max.y)
				);
			return seeds;
		}

		private static Vector2[] GenerateRandomSeeds_RegularDistribution(int numSeeds, Vector2 min, Vector2 max)
		{
			var seeds = new Vector2[numSeeds];

			float width = max.x - min.x;
			float height = max.y - min.y;
			float size = Mathf.Min(width, height);
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(numSeeds));
			float cellSize = size / cellRows;

			for (var i = 0; i < cellRows; i++)
			for (var j = 0; j < cellRows; j++)
				seeds[i * cellRows + j] = new Vector2(
					Random.Range(min.x + i * cellSize, min.x + (i + 1) * cellSize),
					Random.Range(min.y + j * cellSize, min.y + (j + 1) * cellSize)
				);
			return seeds;
		}

		private void GenerateVoronoi()
		{
		}

		public void OnDrawGizmos(Color seedColor = default)
		{
			Gizmos.color = seedColor;
			foreach (Vector2 seed in seeds)
				Gizmos.DrawSphere(new Vector3(seed.x, 100, seed.y), 1f);

			Color[] colors = Color.red.GetRainBowColors(_regions.Length);
			for (var i = 0; i < _regions.Length; i++)
				_regions[i].OnDrawGizmos(colors[i]);
		}
	}
}
