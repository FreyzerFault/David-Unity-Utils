using System;
using System.Linq;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public class VoronoiTest : MonoBehaviour
	{
		public int numSeeds = 10;
		private readonly Vector2[] _seeds = Array.Empty<Vector2>();

		[SerializeField] private Voronoi voronoi;
		private Delaunay.Triangle[] _triangles => voronoi.delaunay.triangles.ToArray();
		private Polygon[] Regions => voronoi.regions.ToArray();

		private Vector3[] SeedsInWorld => _seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public bool animated = true;
		public float delay = 0.1f;
		private Coroutine animationCoroutine;

		private void Start()
		{
			Initialize();

			voronoi.GenerateSeeds(numSeeds);

			if (animated) animationCoroutine = StartCoroutine(voronoi.AnimationCoroutine(delay));
			else voronoi.GenerateVoronoi();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) voronoi.Run_OneIteration();

			if (Input.GetKeyDown(KeyCode.Escape)) StopCoroutine(animationCoroutine);
		}

		private void Initialize() => voronoi.Reset();

		#region DEBUG

		private void OnDrawGizmos() => voronoi.OnDrawGizmos(transform.position, transform.localScale);

		#endregion
	}
}
