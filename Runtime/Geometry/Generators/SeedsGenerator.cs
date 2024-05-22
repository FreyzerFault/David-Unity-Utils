using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using DavidUtils.TerrainExtensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators
{
	public class SeedsGenerator : MonoBehaviour
	{
		public enum SeedsDistribution { Random, Regular, SinWave }

		public int randSeed = 10;

		public int numSeeds = 10;
		public SeedsDistribution seedsDistribution = SeedsDistribution.Random;


		[HideInInspector] public List<Vector2> seeds = new();
		public List<Vector2> Seeds
		{
			get => seeds;
			set
			{
				seeds = value;
				numSeeds = seeds.Count;
				OnSeedsUpdated();
			}
		}

		public bool SeedsAreGenerated => seeds?.Count == numSeeds;

		#region TO WORLD COORDS

		protected Vector3[] Seeds3D_XY => Seeds.Select(seed => seed.ToV3xy()).ToArray();
		protected Vector3[] Seeds3D_XZ => Seeds.Select(seed => seed.ToV3xz()).ToArray();

		protected Vector3[] SeedsInWorld_XZ =>
			Seeds.Select(s => transform.localToWorldMatrix.MultiplyPoint3x4(s.ToV3xz())).ToArray();
		protected Vector3[] SeedsInWorld_XY =>
			Seeds.Select(s => transform.localToWorldMatrix.MultiplyPoint3x4(s.ToV3xy())).ToArray();

		#endregion

		public Bounds2D Bounds => Bounds2D.NormalizedBounds;

		protected virtual void Awake()
		{
			InitializeRenderer();
			Reset();
			GenerateSeeds();
			OnSeedsUpdated();
		}

		protected virtual void Start() => InstantiateRenderer();

		public virtual void Reset()
		{
		}

		public virtual void OnSeedsUpdated() => UpdateRenderer();

		public void RandomizeSeeds()
		{
			randSeed = Random.Range(1, int.MaxValue);
			GenerateSeeds();
			OnSeedsUpdated();
		}

		public void GenerateSeeds() => seeds = GenerateSeeds(numSeeds, randSeed, seedsDistribution).ToList();


		#region MODIFICATION

		/// <summary>
		///     Mueve una semilla a una nueva posición siempre y cuando:
		///     No colisione con otra, no sea la misma posición y esté dentro del rango [0,1]
		/// </summary>
		/// <returns>True si Cumple con los requisitos y se ha movido</returns>
		public virtual bool MoveSeed(int index, Vector2 newPos)
		{
			newPos = newPos.Clamp01();
			Vector2 oldPos = seeds[index];
			if (newPos == oldPos)
				return false;

			// Si colisiona con otra semilla no la movemos
			bool collision = seeds
				.Where((p, i) => i != index)
				.Any(p => Vector2.Distance(p, newPos) < 0.02f);

			if (collision) return false;

			seeds[index] = newPos;

			seeds2DRenderer.MoveSeed(index, newPos);

			return true;
		}

		#endregion


		#region RENDERER

		[SerializeField] private Points2DRenderer seeds2DRenderer = new();

		public bool projectOnTerrain = true;

		public bool DrawSeeds
		{
			get => seeds2DRenderer.active;
			set
			{
				seeds2DRenderer.active = value;
				seeds2DRenderer.UpdateVisibility();
			}
		}

		protected virtual void InitializeRenderer() =>
			seeds2DRenderer.Initialize(transform);

		protected virtual void InstantiateRenderer()
		{
			seeds2DRenderer.Instantiate(seeds.ToArray());
			if (projectOnTerrain && Terrain.activeTerrain != null)
				seeds2DRenderer.ProjectOnTerrain(Terrain.activeTerrain);
		}

		protected virtual void UpdateRenderer() => seeds2DRenderer.Update(seeds.ToArray());

		#endregion


		#region GENERATION ALGORITHMS

		/// <summary>
		///     Genera un set de puntos 2D random dentro del rango [0,0] -> [1,1]
		///     Con distintos tipos de distribución
		/// </summary>
		public static Vector2[] GenerateSeeds(
			int numSeeds, int randSeed = 1, SeedsDistribution seedsDistribution = SeedsDistribution.Regular
		)
		{
			Random.InitState(randSeed);

			return seedsDistribution switch
			{
				SeedsDistribution.Random => GenerateSeeds_RandomDistribution(numSeeds),
				SeedsDistribution.Regular => GenerateSeeds_RegularDistribution(numSeeds),
				SeedsDistribution.SinWave => GenerateSeeds_WaveDistribution(numSeeds),
				_ => GenerateSeeds_RegularDistribution(numSeeds)
			};
		}


		/// <summary>
		///     Genera un set de puntos 2D random dentro del rango [0,0] -> [1,1]
		/// </summary>
		public static Vector2[] GenerateSeeds_RandomDistribution(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			for (var i = 0; i < numSeeds; i++) seeds[i] = new Vector2(Random.value, Random.value);
			return seeds;
		}

		/// <summary>
		///     Genera un set de puntos 2D random dentro del rango [0,0] -> [1,1]
		///     Repartidos equitativamente en una grid NxN
		///     N = floor(sqrt(numSeeds))
		///     9 seeds => 3x3; 10 seeds => 3x3, el restante se genera empezando desde 0,0 en adelante
		/// </summary>
		public static Vector2[] GenerateSeeds_RegularDistribution(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(numSeeds));
			float cellSize = 1f / cellRows;

			int cellRow = 0, cellCol = 0;

			for (var i = 0; i < numSeeds; i++)
			{
				var cellOrigin = new Vector2(cellRow * cellSize, cellCol * cellSize);
				seeds[i] = new Vector2(Random.value * cellSize, Random.value * cellSize) + cellOrigin;

				// Next Row
				cellRow = (cellRow + 1) % cellRows;

				// Jump Column
				if (cellRow == cellRows - 1)
					cellCol = (cellCol + 1) % cellRows;
			}

			return seeds;
		}

		/// <summary>
		///     Genera un set de puntos 2D en el rango [0,0] -> [1,1]
		///     Forman una onda senoidal en una grid NxN
		/// </summary>
		public static Vector2[] GenerateSeeds_WaveDistribution(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(numSeeds));
			float cellSize = 1f / cellRows;

			int cellRow = 0, cellCol = 0;

			for (var i = 0; i < numSeeds; i++)
			{
				var cellOrigin = new Vector2(cellRow * cellSize, cellCol * cellSize);
				seeds[i] = new Vector2(.5f * cellSize, (Mathf.Sin(i) + 1) / 2 * cellSize) + cellOrigin;

				// Next Row
				cellRow = (cellRow + 1) % cellRows;

				// Jump Column
				if (cellRow == cellRows - 1)
					cellCol = (cellCol + 1) % cellRows;
			}

			return seeds;
		}

		#endregion

		#region DEBUG

		public bool drawGrid = true;

		private static readonly int BaseColorId = Shader.PropertyToID("_Base_Color");

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		protected virtual void OnDrawGizmos()
		{
			if (DrawSeeds) GizmosSeeds();
			if (drawGrid) GizmosBoundingBox();
		}

		protected void GizmosBoundingBox()
		{
			Matrix4x4 matrix = transform.localToWorldMatrix;
			Color gridColor = Color.blue;
			if (seedsDistribution == SeedsDistribution.Random)
				GizmosBoundingBox(matrix, gridColor);
			else
				GizmosGrid(matrix, gridColor);
		}

		private void GizmosBoundingBox(Matrix4x4 matrix, Color color = default)
		{
			if (projectOnTerrain)
				GizmosExtensions.DrawQuadWire_OnTerrain(matrix, 5, color);
			else
				GizmosExtensions.DrawQuadWire(matrix, 5, color);
		}

		private void GizmosGrid(Matrix4x4 matrix, Color color = default)
		{
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Count));
			if (projectOnTerrain)
				GizmosExtensions.DrawGrid_OnTerrain(cellRows, cellRows, matrix, 5, color);
			else
				GizmosExtensions.DrawGrid(cellRows, cellRows, matrix, 5, color);
		}

		protected void GizmosSeeds()
		{
			Gizmos.color = seeds2DRenderer.colors?.Length > 0 ? seeds2DRenderer.colors[0] : Color.grey;
			var terrain = Terrain.activeTerrain;
			Vector3[] seedsInWorld = projectOnTerrain
				? SeedsInWorld_XZ.Select(s => terrain.Project(s)).ToArray()
				: SeedsInWorld_XZ;
			for (var i = 0; i < seedsInWorld.Length; i++)
			{
				if (seeds2DRenderer.colors?.Length > 0)
					Gizmos.color = seeds2DRenderer.colors[i];
				Gizmos.DrawSphere(seedsInWorld[i], 0.1f);
			}
		}

		#endregion
	}
}
