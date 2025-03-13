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
	public class PolygonInteriorGeneratorTest : TestRunner
	{
		// Polygon Generator
		[ExposedField] public int seed = 9999;
		[ExposedField] public int numVertices = 5;
		
		// Interior Polygon Settings
		public Vector2 interiorPolygonMargin = Vector2.one * 0.1f;

		// Test Settings
		public bool stepByStep;
		
		private Polygon _polygon = new();
		private Polygon _interiorPolygon = new();
		private Polygon[] _splitPolygons = Array.Empty<Polygon>();
		private Polygon _mergedPolygon = new();

		private List<Vector2> _intersectionPoints = new();


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
			InitializeRenderer();
			GenerateVertices();
			base.Awake();
		}

		#endregion


		public void RandomizeSeed() => Seed = Random.Range(1, int.MaxValue);

		private void GenerateVertices()
		{
			_polygon.SetRandomVertices(numVertices, 10, true);
			Renderer.Polygon = _polygon;
			FocusCameraInCentroid();
		}

		public void Reset()
		{
			Debug.Log("Resetting Polygon Test");
			_polygon = new Polygon();
			_splitPolygons = Array.Empty<Polygon>();
			_intersectionPoints.Clear();
			_interiorPolygon.Vertices = Array.Empty<Vector2>();
			_mergedPolygon.Vertices = Array.Empty<Vector2>();
			
			Renderer.Polygon = _polygon;
			_exteriorRenderer.Polygon = _polygon;
			
			UpdatePolygonRenderers();
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
					() => _polygon.Vertices.NotNullOrEmpty()
				)
			);
			
			onStartAllTests += () =>
			{
				UpdatePolygonRenderers();
				Debug.Log(
					$"<color=#00ffff><b>Starting Test #{iterations}</b> - Seed: {seed} - NumVertices: {numVertices} - Time: {Time.time}</color>",
					this
				);
			};

			onEndAllTests += () =>
			{
				Reset();
				RandomizeSeed();
			};
			
			if (stepByStep)
			{
				
				AddTest(
					AddAutoIntersectionsTest,
					new TestInfo(
						"Add Auto Intersections",
						() => _polygon.Vertices.NotNullOrEmpty() && _intersectionPoints.Count < 40
					)
				);
				AddTest(
					SplitPolygonsTest,
					new TestInfo(
						"Split Polygon",
						() => _splitPolygons.NotNullOrEmpty()
					)
				);
				AddTest(
					MergePolygonInOne,
					new TestInfo(
						"Merge Polygons",
						() => _mergedPolygon.Vertices.NotNullOrEmpty()
					)
				);

			}
			else
			{
				AddTest(LegalizeTest, new TestInfo("Legalización del Polígono Interior"));
			}
		}

		// Construye el poligono interior con un margen
		public void BuildInteriorPolygon()
		{
			_interiorPolygon = _polygon.InteriorPolygon(Vector2.one * interiorPolygonMargin);
			_exteriorRenderer.Polygon = _polygon;
			Renderer.Polygon = _interiorPolygon;
			
			UpdatePolygonRenderers();
		}

		// Esto ya hace to el proceso de legalización del poligono interior
		public void LegalizeTest()
		{
			_mergedPolygon = _interiorPolygon.Legalize();
			Renderer.Polygon = _mergedPolygon;
			UpdatePolygonRenderers();
		}

		// Las siguientes funciones son para mostrar el proceso de legalización paso a paso
		public void AddAutoIntersectionsTest()
		{
			_interiorPolygon = _interiorPolygon.AddAutoIntersections(out _intersectionPoints);
			Renderer.Polygon = _interiorPolygon;
			UpdatePolygonRenderers();
		}

		public void SplitPolygonsTest()
		{
			_splitPolygons = _interiorPolygon.SplitAutoIntersectedPolygons(_intersectionPoints);
			
			UpdatePolygonRenderers();
		}

		public void SelectCCWPolygons()
		{
			_splitPolygons = _splitPolygons.Where(p => !p.IsEmpty && p.IsCounterClockwise()).ToArray();
			UpdatePolygonRenderers();
		}

		public void MergePolygonInOne()
		{
			SelectCCWPolygons();
			_mergedPolygon = new Polygon();
			if (_splitPolygons.IsNullOrEmpty()) return;
			if (_splitPolygons.Length > 1)
			{
				Vector2 firstCentroid = _splitPolygons.First().centroid;
				_splitPolygons = _splitPolygons.OrderByDescending(p => Vector2.Distance(firstCentroid, p.centroid))
					.ToArray();
			}

			_splitPolygons.ForEach(p => _mergedPolygon = _mergedPolygon.Merge(p));
			Renderer.Polygon = _mergedPolygon;
			
			_splitPolygons = Array.Empty<Polygon>();
			
			UpdatePolygonRenderers();
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
			_polygonRenderer = UnityUtils.InstantiateObject<PolygonRenderer>(transform, "Interior Polygon");
			_polygonRenderer.Color = Color.green;
			_polygonRenderer.Thickness = 0.15f;
			_polygonRenderer.RenderMode = PolygonRenderer.PolygonRenderMode.OutlinedMesh;
			_polygonRenderer.Polygon = _polygon;

			_exteriorRenderer = UnityUtils.InstantiateObject<PolygonRenderer>(transform, "Exterior Polygon");
			_exteriorRenderer.Color = Color.grey.WithAlpha(0.2f);
			_exteriorRenderer.Thickness = _polygonRenderer.Thickness / 2;
			_exteriorRenderer.RenderMode = PolygonRenderer.PolygonRenderMode.OutlinedMesh;
			_exteriorRenderer.Polygon = _polygon;
			_exteriorRenderer.transform.position = _polygonRenderer.transform.position + Vector3.forward * 0.1f;
		}

		private void UpdatePolygonRenderers()
		{
			// Clear Split Renderers
			if (_splitPolygonRenderers.NotNullOrEmpty())
			{
				_splitPolygonRenderers?.ForEach(UnityUtils.DestroySafe);
				_splitPolygonRenderers = Array.Empty<PolygonRenderer>();
			}

			// Individual Polygon
			if (_splitPolygons.IsNullOrEmpty())
			{
				_polygonRenderer.Thickness = 0.15f;
				Renderer.Color = Color.green;
				return;
			}
			
			// Split Polygons with Independent Renderers
			Color[] colors = Color.green.GetRainBowColors(_splitPolygons.Length, 0.2f);
			_splitPolygonRenderers = _splitPolygons.Select((p, i) =>
			{
				PolygonRenderer polyRender = UnityUtils.InstantiateObject<PolygonRenderer>(transform, "Split Polygon");
				polyRender.Polygon = p;
				polyRender.Color = colors[i];
				polyRender.Thickness = _polygonRenderer.Thickness;
				polyRender.transform.position += Vector3.back * 0.2f;
				return polyRender;
			}).ToArray();

			// Fade Main Polygon Renderer to highlight split polygons
			_polygonRenderer.Thickness = 0f;
			Renderer.Color = Color.gray.WithAlpha(0.2f);
		}
		

		#endregion
		
		

		#region CAMERA

		private void FocusCameraInCentroid()
		{
			Camera cam = Camera.main;
			if (cam == null) return;

			AABB_2D aabb = new(_polygon);
			transform.localToWorldMatrix.MultiplyPoint3x4(aabb.Corners);

			cam.orthographicSize = aabb.Size.magnitude * .5f;

			Vector3 position = transform.localToWorldMatrix.MultiplyPoint3x4(_polygon.centroid) + Vector3.back * 10;
			cam.transform.position = position;
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		private enum DrawMode { None, StartingPolygon, InteriorPolygon, SplitPolygons, MergedPolygon }

		private void OnDrawGizmos()
		{
			DrawMode mode = DrawMode.None;
			if (_polygon.Vertices.NotNullOrEmpty()) mode = DrawMode.StartingPolygon;
			if (_interiorPolygon.Vertices.NotNullOrEmpty()) mode = DrawMode.InteriorPolygon;
			if (_splitPolygons.NotNullOrEmpty()) mode = DrawMode.SplitPolygons;
			if (_mergedPolygon.Vertices.NotNullOrEmpty()) mode = DrawMode.MergedPolygon;

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
			_polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.6f));
			_polygon.DrawGizmosVertices(transform.localToWorldMatrix, Color.red, .02f);
			DrawEdges(_polygon);
		}

		private void DrawInteriorPolygon()
		{
			_polygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.8f), Color.green.Darken(0.4f));
			_interiorPolygon.DrawGizmos(transform.localToWorldMatrix, Color.yellow.Darken(0.4f), Color.yellow);
			transform.localToWorldMatrix.MultiplyPoint3x4(_intersectionPoints).ForEach(p => Gizmos.DrawSphere(p, .05f));
			DrawEdges(_interiorPolygon);
		}

		private void DrawSplitPolygons()
		{
			_polygon.DrawGizmos(transform.localToWorldMatrix, Color.gray.Lighten(0.2f), Color.gray);
			
			int ccwCount = 0, cwCount = 0;
			_splitPolygons.ForEach(
				p =>
				{
					if (p.IsEmpty) return;
					
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
			_polygon.DrawGizmos(transform.localToWorldMatrix, Color.gray.Lighten(0.2f), Color.gray);
			_mergedPolygon.DrawGizmos(transform.localToWorldMatrix, Color.green.Darken(0.4f), Color.green);
			DrawEdges(_mergedPolygon);
		}

#endif

		#endregion
	}
}
