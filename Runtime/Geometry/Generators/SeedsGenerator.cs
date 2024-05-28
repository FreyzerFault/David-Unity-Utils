using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Rendering;
using DavidUtils.TerrainExtensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators
{
	[RequireComponent(typeof(BoundsComponent))]
	public class SeedsGenerator : MonoBehaviour
	{
		public enum SeedsDistribution { Random, Regular, SinWave }

		[Header("SEEDS")]
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

		#region BOUNDS

		[Space] protected BoundsComponent boundsComp;
		public Bounds2D Bounds => boundsComp?.bounds2D ?? (boundsComp = GetComponent<BoundsComponent>()).bounds2D;

		public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix * Bounds.LocalToBoundsMatrix();
		public Vector3 ToWorld(Vector2 pos) => LocalToWorldMatrix.MultiplyPoint3x4(pos.ToV3xz());

		#endregion


		#region UNITY

		protected virtual void Awake()
		{
			InitializeRenderer();
			Reset();
			GenerateSeeds();
		}

		protected virtual void Start() => InstantiateRenderer();

		#endregion


		#region MAIN METHODS

		public virtual void Reset()
		{
			seeds = new List<Vector2>();
			Renderer.Clear();
		}

		public virtual void OnSeedsUpdated() => UpdateRenderer();

		public void RandomizeSeeds()
		{
			randSeed = Random.Range(1, int.MaxValue);
			GenerateSeeds();
			OnSeedsUpdated();
		}

		public void GenerateSeeds()
		{
			seeds = GenerateSeeds(numSeeds, randSeed, seedsDistribution).ToList();
			DeleteRedundant();
		}

		private void DeleteRedundant() => seeds = seeds.Where(
				(s1, i) => seeds
					.Where((_, j) => j != i) // Exclude itself
					.All(s2 => Vector2.Distance(s1, s2) > 0.01f) // Todos superan el minimo de distancia
			)
			.ToList();

		#endregion


		#region SEEDS MODIFICATION

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
				.Where((_, i) => i != index)
				.Any(p => Vector2.Distance(p, newPos) < 0.02f);

			if (collision) return false;

			seeds[index] = newPos;

			Renderer.MovePoint(index, newPos);

			return true;
		}

		#endregion


		#region RENDERER

		[Space]
		private Points2DRenderer _seedsRenderer;
		private Points2DRenderer Renderer => _seedsRenderer ??= GetComponentInChildren<Points2DRenderer>(true);

		private readonly bool projectOnTerrain = true;
		protected static Terrain Terrain => Terrain.activeTerrain;
		public bool CanProjectOnTerrain => projectOnTerrain && Terrain != null;

		public bool DrawSeeds
		{
			get => Renderer.Active;
			set => Renderer.Active = value;
		}

		protected virtual void InitializeRenderer()
		{
			_seedsRenderer ??= Renderer
			                   ?? UnityUtils.InstantiateEmptyObject(transform, "Seeds Renderer")
				                   .AddComponent<Points2DRenderer>();

			Renderer.transform.ApplyMatrix(Bounds.LocalToBoundsMatrix());
			Renderer.transform.Translate(Vector3.up * .5f);
		}

		protected virtual void InstantiateRenderer()
		{
			Renderer.Instantiate(seeds.ToArray(), "Seed");
			if (CanProjectOnTerrain && Terrain != null)
				Renderer.ProjectOnTerrain(Terrain);

			Renderer.ToggleShadows(false);
		}

		protected virtual void UpdateRenderer() => Renderer.UpdateGeometry(seeds.ToArray());

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

		protected bool drawGizmos = false;
		public bool drawGrid = true;

		private static readonly int BaseColorId = Shader.PropertyToID("_Base_Color");

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		protected virtual void OnDrawGizmos()
		{
			if (!drawGizmos) return;

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
			if (CanProjectOnTerrain)
				GizmosExtensions.DrawQuadWire_OnTerrain(matrix, 5, color);
			else
				GizmosExtensions.DrawQuadWire(matrix, 5, color);
		}

		private void GizmosGrid(Matrix4x4 matrix, Color color = default)
		{
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Count));
			if (CanProjectOnTerrain)
				GizmosExtensions.DrawGrid_OnTerrain(cellRows, cellRows, matrix, 5, color);
			else
				GizmosExtensions.DrawGrid(cellRows, cellRows, matrix, 5, color);
		}

		protected void GizmosSeeds()
		{
			Gizmos.color = Renderer.colors?.Length > 0 ? Renderer.colors[0] : Color.grey;
			for (var i = 0; i < seeds.Count; i++)
			{
				if (Renderer.colors?.Length > 0)
					Gizmos.color = Renderer.colors[i];
				Gizmos.DrawSphere(CanProjectOnTerrain ? Terrain.Project(ToWorld(seeds[i])) : ToWorld(seeds[i]), 0.1f);
			}
		}

		#endregion
	}
}
