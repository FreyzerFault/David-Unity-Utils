using System;
using System.Linq;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class DelaunayGenerator : MonoBehaviour
	{
		public int numSeeds = 10;
		private Vector2[] _seeds = Array.Empty<Vector2>();

		private readonly Delaunay _delaunay = new();
		private Delaunay.Triangle[] _triangles = Array.Empty<Delaunay.Triangle>();

		private Vector3[] SeedsInWorld => _seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public bool animated = true;
		public float delay = 0.1f;
		private Coroutine animationCoroutine;

		private void Awake()
		{
			_seeds = GeometryUtils.GenerateSeeds_WaveDistribution(numSeeds);
			_delaunay.seeds = _seeds;

			_delaunay.GetBoundingBoxTriangles();
		}

		private void Start()
		{
			if (animated)
				animationCoroutine = StartCoroutine(_delaunay.AnimationCoroutine(delay));
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) _delaunay.Run_OnePoint();

			if (Input.GetKeyDown(KeyCode.Escape)) StopCoroutine(animationCoroutine);
		}

#if UNITY_EDITOR
		private void OnDrawGizmos() => _delaunay.OnDrawGizmos(transform.localToWorldMatrix);
#endif
	}
}
