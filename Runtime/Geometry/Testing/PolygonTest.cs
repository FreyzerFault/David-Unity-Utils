using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.Reflection;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Testing
{
	public class PolygonTest : MonoBehaviour
	{
		public Polygon polygon;

		[ExposedField] public int numVertices = 5;
		[ExposedField] public int seed = 9999;

		public int Seed
		{
			get => seed;
			set
			{
				seed = value;
				GenerateVertices();
			}
		}
		public int NumVertices
		{
			get => numVertices;
			set
			{
				numVertices = value;
				GenerateVertices();
			}
		}

		public string NumVerticesStr
		{
			get => numVertices.ToString();
			set => NumVertices = int.Parse(value);
		}

		#region UNITY

		private void Awake()
		{
			Random.InitState(seed);
			GenerateVertices();
		}

		private void Update()
		{
			// SPACE => Randomize Seed
			if (Input.GetKeyDown(KeyCode.Space)) RandomizeSeed();
		}

		#endregion


		public void RandomizeSeed() => Seed = Random.Range(1, int.MaxValue);

		private void GenerateVertices()
		{
			SetRandomVertices(numVertices);
			FocusCameraCentroid();
		}

		private void SetRandomVertices(int numVertices)
		{
			Random.InitState(seed);

			if (numVertices < 3) return;

			List<Vector2> vertices = new(numVertices);
			// float angle = 0;
			// float stepAngle = 20;
			for (var i = 0; i < numVertices; i++) vertices.Add(Random.insideUnitCircle * 5);

			polygon = new Polygon(vertices);
		}


		#region CAMERA

		private void FocusCameraCentroid()
		{
			Camera camera = Camera.main;
			if (camera == null) return;

			var aabb = new AABB_2D(polygon);
			transform.localToWorldMatrix.MultiplyPoint3x4(aabb.Corners);

			camera.orthographicSize = aabb.Size.magnitude * .5f;

			Vector3 position = transform.localToWorldMatrix.MultiplyPoint3x4(polygon.centroid) + Vector3.back * 10;
			camera.transform.position = position;
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.6f), Color.cyan);
			polygon.DrawGizmosVertices(transform.localToWorldMatrix, Color.grey, .05f);
			Edge[] edges = polygon.Edges.ToArray();
			for (var i = 0; i < polygon.VextexCount; i++)
			{
				Edge edge = edges[i];
				Color color = Color.red.RotateHue(0.1f * i);
				edge.DrawGizmos_ArrowWire(transform.localToWorldMatrix, 5f, color);
			}
		}

#endif

		#endregion
	}
}
