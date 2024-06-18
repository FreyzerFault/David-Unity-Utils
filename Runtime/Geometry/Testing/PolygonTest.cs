using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.DevTools.Reflection;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Testing
{
	public class PolygonTest : TestRunner
	{
		public Polygon polygon;
		public Polygon interiorPolygon;
		public Polygon[] splitPolygons;

		private List<Vector2> intersectionPoints = new();

		public Vector2 interiorPolygonMargin = Vector2.one * 0.1f;
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

		protected override void Awake()
		{
			Random.InitState(seed);
			GenerateVertices();
			InitializeTests();
			base.Awake();
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
			for (var i = 0; i < numVertices; i++) vertices.Add(Random.insideUnitCircle * 10);

			polygon = new Polygon(vertices.SortByAngle(vertices.Center()));
		}

		public void Reset()
		{
			splitPolygons = Array.Empty<Polygon>();
			intersectionPoints.Clear();
			interiorPolygon.vertices = Array.Empty<Vector2>();
		}


		#region TESTS

		private void InitializeTests()
		{
			// ACTION, CONDITION
			AddTest(BuildInteriorPolygon, () => polygon.vertices.NotNullOrEmpty());
			AddTest(
				AddAutoIntersectionsTest,
				() => polygon.vertices.NotNullOrEmpty() && intersectionPoints.Count < 40
			);
			AddTest(SplitPolygonsTest, () => splitPolygons.NotNullOrEmpty());
			// AddTest(LegalizeTest, () => polygon.vertices.NotNullOrEmpty());

			OnEndTest += () =>
			{
				Reset();
				RandomizeSeed();
			};
		}

		public void BuildInteriorPolygon() =>
			interiorPolygon = polygon.InteriorPolygon(Vector2.one * interiorPolygonMargin);

		public void AddAutoIntersectionsTest() =>
			polygon = interiorPolygon.AddAutoIntersections(out intersectionPoints);

		public void SplitPolygonsTest() => splitPolygons = polygon.SplitAutoIntersectedPolygons(intersectionPoints);

		public void LegalizeTest() => polygon = polygon.Legalize();

		#endregion


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
			bool drawInterior = interiorPolygon.vertices.NotNullOrEmpty();
			bool drawSplitPolygons = splitPolygons.NotNullOrEmpty();

			if (drawSplitPolygons) DrawSplitPolygons();
			else if (drawInterior) DrawInteriorPolygon();
			else DrawPolygon();

			Gizmos.color = Color.red;
			Edge[] edges = drawInterior ? interiorPolygon.Edges.ToArray() : polygon.Edges.ToArray();
			for (var i = 0; i < (drawInterior ? interiorPolygon.VextexCount : polygon.VextexCount); i++)
			{
				Edge edge = edges[i].Shorten(.05f);
				Color color = Color.red.RotateHue(0.1f * i);
				edge.DrawGizmos_Arrow(GizmosExtensions.ArrowCap.Triangle, transform.localToWorldMatrix, color);
			}
		}

		private void DrawPolygon()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.6f));
			polygon.DrawGizmosVertices(transform.localToWorldMatrix, Color.red, .02f);
		}

		private void DrawInteriorPolygon()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.8f), Color.green.Darken(0.4f));
			interiorPolygon.DrawGizmos(transform.localToWorldMatrix, Color.yellow.Darken(0.4f), Color.yellow);
			transform.localToWorldMatrix.MultiplyPoint3x4(intersectionPoints).ForEach(p => Gizmos.DrawSphere(p, .05f));
		}

		private void DrawSplitPolygons()
		{
			int ccwCount = 0, cwCount = 0;
			splitPolygons.ForEach(
				p =>
				{
					Matrix4x4 separationMatrix =
						Matrix4x4.Translate((p.centroid - polygon.centroid).normalized * 0.2f);
					p.vertices = separationMatrix.MultiplyPoint3x4(p.vertices).ToV2().ToArray();
					Color color = p.IsCounterClockwise()
						? Color.green.RotateHue(0.1f * ccwCount++).Darken(0.6f)
						: Color.red.RotateHue(0.1f * cwCount++).Darken(0.6f);
					p.DrawGizmos(
						transform.localToWorldMatrix,
						color,
						p.IsCounterClockwise() ? Color.green : Color.red
					);
				}
			);
		}

#endif

		#endregion
	}
}
