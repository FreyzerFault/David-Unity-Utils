using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.DevTools.Reflection;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Testing
{
	public class PolygonTest : TestRunner
	{
		private Polygon polygon;
		private Polygon interiorPolygon;
		private Polygon[] splitPolygons;
		private Polygon mergedPolygon;

		private List<Vector2> intersectionPoints = new();

		[ExposedField] public int seed = 9999;
		[ExposedField] public int numVertices = 5;
		public Vector2 interiorPolygonMargin = Vector2.one * 0.1f;
		public bool stepByStep;


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
			InitializeRenderer();
			Random.InitState(seed);
			GenerateVertices();
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
			Renderer.Polygon = polygon;
		}

		public void Reset()
		{
			Debug.Log("Resetting Polygon Test");
			polygon = new Polygon();
			splitPolygons = Array.Empty<Polygon>();
			intersectionPoints.Clear();
			interiorPolygon.Vertices = Array.Empty<Vector2>();
			mergedPolygon.Vertices = Array.Empty<Vector2>();
		}


		#region TESTS

		protected override void InitializeTests()
		{
			AddTest(GenerateVertices, new TestInfo("Generate Random Vertices"));
			
			// ACTION, CONDITION
			AddTest(
				BuildInteriorPolygon,
				new TestInfo(
					"Interior Polygon",
					() => polygon.Vertices.NotNullOrEmpty()
				)
			);
			if (stepByStep)
			{
				
				AddTest(
					AddAutoIntersectionsTest,
					new TestInfo(
						"Add Auto Intersections",
						() => polygon.Vertices.NotNullOrEmpty() && intersectionPoints.Count < 40
					)
				);
				AddTest(
					SplitPolygonsTest,
					new TestInfo(
						"Split Polygon",
						() => splitPolygons.NotNullOrEmpty()
					)
				);
				AddTest(
					MergePolygonInOne,
					new TestInfo(
						"Merge Polygons",
						() => mergedPolygon.Vertices.NotNullOrEmpty()
					)
				);

				OnStartTest += () =>
				{
					UpdatePolygonRenderer();
					Debug.Log(
						$"<color=#00ffff><b>Starting Test #{iterations}</b> - Seed: {seed} - NumVertices: {numVertices} - Time: {Time.time}</color>",
						this
					);
				};

				OnEndTest += () =>
				{
					Reset();
					RandomizeSeed();
					Renderer.Clear();
					_exteriorRenderer.Clear();
				};
			}
			else
			{
				AddTest(LegalizeTest, new TestInfo("Legalización del Polígono Interior"));
			}
		}

		// Construye el poligono interior con un margen
		public void BuildInteriorPolygon()
		{
			interiorPolygon = polygon.InteriorPolygon(Vector2.one * interiorPolygonMargin);
			_exteriorRenderer.Polygon = polygon;
			Renderer.Polygon = interiorPolygon;
			
			UpdatePolygonRenderer();
		}

		// Esto ya hace to el proceso de legalización del poligono interior
		public void LegalizeTest()
		{
			mergedPolygon = interiorPolygon.Legalize();
			Renderer.Polygon = mergedPolygon;
			UpdatePolygonRenderer();
		}

		// Las siguientes funciones son para mostrar el proceso de legalización paso a paso
		public void AddAutoIntersectionsTest()
		{
			interiorPolygon = interiorPolygon.AddAutoIntersections(out intersectionPoints);
			Renderer.Polygon = interiorPolygon;
			UpdatePolygonRenderer();
		}

		public void SplitPolygonsTest()
		{
			AddAutoIntersectionsTest();
			
			splitPolygons = interiorPolygon.SplitAutoIntersectedPolygons(intersectionPoints);
			
			UpdatePolygonRenderer();
		}

		public void SelectCCWpolygons()
		{
			splitPolygons = splitPolygons.Where(p => p.IsCounterClockwise()).ToArray();
			UpdatePolygonRenderer();
		}

		public void MergePolygonInOne()
		{
			SelectCCWpolygons();
			mergedPolygon = new Polygon();
			if (splitPolygons.IsNullOrEmpty()) return;
			if (splitPolygons.Length > 1)
			{
				Vector2 firstCentroid = splitPolygons.First().centroid;
				splitPolygons = splitPolygons.OrderByDescending(p => Vector2.Distance(firstCentroid, p.centroid))
					.ToArray();
			}

			splitPolygons.ForEach(p => mergedPolygon = mergedPolygon.Merge(p));
			Renderer.Polygon = mergedPolygon;
			
			splitPolygons = Array.Empty<Polygon>();
			
			UpdatePolygonRenderer();
		}

		#endregion


		#region RENDERING

		private PolygonRenderer _polygonRenderer;
		private PolygonRenderer _exteriorRenderer;
		private PolygonRenderer[] _splitPolygonRenderers;
		
		private PolygonRenderer Renderer => _polygonRenderer;
		private PolygonRenderer[] SplitRenderers => _splitPolygonRenderers;

		private void InitializeRenderer()
		{
			_polygonRenderer = GetComponent<PolygonRenderer>() ?? gameObject.AddComponent<PolygonRenderer>();
			_polygonRenderer.Color = Color.green;
			_polygonRenderer.Thickness = 0.15f;
			_polygonRenderer.RenderMode = PolygonRenderer.PolygonRenderMode.OutlinedMesh;
			_polygonRenderer.Polygon = polygon;


			_exteriorRenderer = UnityUtils.InstantiateObject<PolygonRenderer>(transform.parent, "Exterior Polygon");
			_exteriorRenderer.Color = Color.grey.WithAlpha(0.2f);
			_exteriorRenderer.Thickness = _polygonRenderer.Thickness / 2;
			_exteriorRenderer.RenderMode = PolygonRenderer.PolygonRenderMode.OutlinedMesh;
			_exteriorRenderer.Polygon = polygon;
			
			Debug.Log("CReated Exterior Renderer", _exteriorRenderer);
		}

		private void UpdatePolygonRenderer()
		{
			if (_splitPolygonRenderers.NotNullOrEmpty())
			{
				_splitPolygonRenderers?.ForEach(UnityUtils.DestroySafe);
				_splitPolygonRenderers = Array.Empty<PolygonRenderer>();
			}

			if (splitPolygons.IsNullOrEmpty())
			{
				_polygonRenderer.Thickness = 0.15f;
				Renderer.Color = Color.green;
				return;
			}
			
			Color[] colors = Color.green.GetRainBowColors(splitPolygons.Length, 0.2f);
			_splitPolygonRenderers = splitPolygons.Select((p, i) =>
			{
				PolygonRenderer polyRender = UnityUtils.InstantiateObject<PolygonRenderer>(transform, "Split Polygon");
				polyRender.Polygon = p;
				polyRender.Color = colors[i];
				polyRender.Thickness = _polygonRenderer.Thickness;
				return polyRender;
			}).ToArray();

			_polygonRenderer.Thickness = 0f;
			Renderer.Color = Color.gray.WithAlpha(0.2f);
		}
		

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

		private enum DrawMode { None, StartingPolygon, InteriorPolygon, SplitPolygons, MergedPolygon }

		private void OnDrawGizmos()
		{
			if (polygon == null) return;
			
			var mode = DrawMode.None;
			if (polygon.Vertices.NotNullOrEmpty()) mode = DrawMode.StartingPolygon;
			if (interiorPolygon.Vertices.NotNullOrEmpty()) mode = DrawMode.InteriorPolygon;
			if (splitPolygons.NotNullOrEmpty()) mode = DrawMode.SplitPolygons;
			if (mergedPolygon.Vertices.NotNullOrEmpty()) mode = DrawMode.MergedPolygon;

			switch (mode)
			{
				case DrawMode.None:
					break;
				case DrawMode.StartingPolygon:
					DrawPolygon();
					break;
				case DrawMode.InteriorPolygon:
					DrawInteriorPolygon();
					break;
				case DrawMode.SplitPolygons:
					DrawSplitPolygons();
					break;
				case DrawMode.MergedPolygon:
					DrawMerged();
					break;
			}
		}

		private void DrawPolygon()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.6f));
			polygon.DrawGizmosVertices(transform.localToWorldMatrix, Color.red, .02f);
			DrawEdges(polygon);
		}

		private void DrawInteriorPolygon()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.8f), Color.green.Darken(0.4f));
			interiorPolygon.DrawGizmos(transform.localToWorldMatrix, Color.yellow.Darken(0.4f), Color.yellow);
			transform.localToWorldMatrix.MultiplyPoint3x4(intersectionPoints).ForEach(p => Gizmos.DrawSphere(p, .05f));
			DrawEdges(interiorPolygon);
		}

		private void DrawSplitPolygons()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.gray.Lighten(0.2f), Color.gray);

			int ccwCount = 0, cwCount = 0;
			splitPolygons.ForEach(
				p =>
				{
					Color color = p.IsCounterClockwise()
						? Color.green.RotateHue(0.1f * ccwCount++).Darken(0.6f)
						: Color.red.RotateHue(0.1f * cwCount++).Darken(0.6f);
					p.DrawGizmos(
						transform.localToWorldMatrix,
						color,
						p.IsCounterClockwise() ? Color.green : Color.red
					);
					DrawEdges(p);
				}
			);
		}

		private void DrawEdges(Polygon polygon)
		{
			Gizmos.color = Color.red;
			Edge[] edges = polygon.Edges.ToArray();
			for (var i = 0; i < polygon.VertexCount; i++)
			{
				Edge edge = edges[i].Shorten(.05f);
				Color color = Color.red.RotateHue(0.1f * i);
				edge.DrawGizmos_Arrow(GizmosExtensions.ArrowCap.Triangle, transform.localToWorldMatrix, color);
			}
		}

		private void DrawMerged()
		{
			polygon.DrawGizmos(transform.localToWorldMatrix, Color.gray.Lighten(0.2f), Color.gray);
			mergedPolygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.4f), Color.green);
			DrawEdges(mergedPolygon);
		}

#endif

		#endregion
	}
}
