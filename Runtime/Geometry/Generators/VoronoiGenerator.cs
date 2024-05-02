using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators
{
	public class VoronoiGenerator : MonoBehaviour
	{
		public int numSeeds = 10;
		private readonly Vector2[] _seeds = Array.Empty<Vector2>();

		public Voronoi voronoi;
		private Delaunay.Triangle[] _triangles => voronoi.delaunay.triangles.ToArray();
		private Polygon[] Regions => voronoi.regions.ToArray();

		private Vector3[] SeedsInWorld => _seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public bool animated = true;
		public float delay = 0.1f;
		private Coroutine animationCoroutine;

		public bool SeedsGenerated => voronoi.seeds?.Length > 0;

		private void Start()
		{
			Initialize();
			GenerateSeeds();
			RunVoronoi();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) voronoi.Run_OneIteration();

			if (Input.GetKeyDown(KeyCode.Escape)) StopCoroutine(animationCoroutine);
		}

		public void Initialize() => voronoi.Reset();
		public void GenerateSeeds() => voronoi.GenerateSeeds(numSeeds);

		public void GenerateNewSeeds()
		{
			voronoi.seed = Random.Range(1, int.MaxValue);
			GenerateSeeds();
		}

		public void RunVoronoi()
		{
			if (animated) animationCoroutine = StartCoroutine(voronoi.AnimationCoroutine(delay));
			else voronoi.GenerateVoronoi();
		}

		public void StopGeneration() => StopCoroutine(animationCoroutine);


#if UNITY_EDITOR

		#region DEBUG

		private void OnDrawGizmos() => voronoi.OnDrawGizmos(transform.localToWorldMatrix);

		#endregion

#endif
	}
}
