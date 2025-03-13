using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Testing
{
    public class PolygonGeneratorTest: TestRunner
    {
        [Space]
        
        public int seed = 1234;
        public int numVertices = 5;
        public float maxSize = 1;
        
        private bool autointersected;
        
        private Polygon _polygon = new Polygon();

        protected override void Awake()
        {
            base.Awake();
            Random.InitState(seed);

            renderer = GetComponentInChildren<PolygonRenderer>();
            if (renderer == null)
                renderer = UnityUtils.InstantiateObject<PolygonRenderer>(transform, "Renderer");

            renderer.Thickness = 0.1f;
        }

        protected override void InitializeTests()
        {
            AddTest(GeneratePolygon,
                new TestInfo("SetRandomVertices", () => !autointersected));
        }

        private void GeneratePolygon()
        {
            _polygon.SetRandomVertices(numVertices, maxSize / 2);
            UpdateRenderer();
            FocusCameraInCentroid();
            autointersected = _polygon.HasAutoIntersections();
        }

        public new PolygonRenderer renderer;
        
        private void UpdateRenderer() 
            => renderer.Polygon = _polygon;
        
        
        private void FocusCameraInCentroid()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            AABB_2D aabb = new(_polygon);
            transform.localToWorldMatrix.MultiplyPoint3x4(aabb.Corners);

            cam.orthographicSize = aabb.Size.magnitude * .9f;

            Vector3 position = transform.localToWorldMatrix.MultiplyPoint3x4(_polygon.centroid) + Vector3.back * 10;
            cam.transform.position = position;
            cam.transform.LookAt(transform.position);
        }
    }
}
