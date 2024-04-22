using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DavidUtils.Geometry
{
    public class DelaunayTest: MonoBehaviour
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
            _seeds = GeometryUtils.GenerateRandomSeeds_WaveDistribution(numSeeds);
            _delaunay.vertices = _seeds;
            
            _delaunay.InitializeSuperQuad();
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

        private void OnDrawGizmos()
        {
            _delaunay.OnDrawGizmos(transform.position, transform.localScale);
        }
    }
}
