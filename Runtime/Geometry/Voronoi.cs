using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using DavidUtils.DebugUtils;
#endif

namespace DavidUtils.Geometry
{
	[Serializable]
	public class Voronoi
	{
		public enum SeedDistribution { Random, Regular, SinWave }

		public int seed = 10;
		public SeedDistribution seedDistribution = SeedDistribution.Random;

		[HideInInspector] public Delaunay delaunay = new();

		[HideInInspector] public Vector2[] seeds;
		[HideInInspector] public List<Polygon> regions;

		private Delaunay.Triangle[] Triangles => delaunay.triangles.ToArray();

		private Vector3[] SeedsInWorld => seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public Voronoi(Vector2[] seeds)
		{
			this.seeds = seeds;
			regions = new List<Polygon>();
		}

		public Voronoi(int numSeeds)
			: this(Array.Empty<Vector2>()) => GenerateSeeds(numSeeds);

		public void GenerateSeeds(int numSeeds = -1)
		{
			Reset();
			Random.InitState(seed);
			numSeeds = numSeeds == -1 ? seeds.Length : numSeeds;
			seeds = seedDistribution switch
			{
				SeedDistribution.Random => GeometryUtils.GenerateSeeds_RandomDistribution(numSeeds),
				SeedDistribution.Regular => GeometryUtils.GenerateSeeds_RegularDistribution(numSeeds),
				SeedDistribution.SinWave => GeometryUtils.GenerateSeeds_WaveDistribution(numSeeds),
				_ => GeometryUtils.GenerateSeeds_RegularDistribution(numSeeds)
			};

			// Las convertimos en vertices para triangularlos con Delaunay primero
			delaunay.vertices = seeds;
		}

		public void Reset()
		{
			iteration = 0;
			regions = new List<Polygon>();
			delaunay.Reset();
		}

		public void GenerateDelaunay()
		{
			delaunay.vertices = seeds;
			delaunay.Triangulate(seeds);
		}

		public Polygon[] GenerateVoronoi()
		{
			// Se necesita triangular las seeds primero.
			if (Triangles.Length == 0) GenerateDelaunay();

			for (var i = 0; i < seeds.Length; i++)
				GenerateRegion(i);

			return regions.ToArray();
		}

		#region PROGRESSIVE RUN

		public int iteration;

		// Habra terminado cuando para todas las semillas haya una region
		public bool Ended => regions.Count == seeds.Length;

		public void Run_OneIteration()
		{
			if (Ended) return;

			if (Triangles.Length == 0) GenerateDelaunay();

			GenerateRegion(iteration);

			iteration++;
		}

		// Genera una region a partir de una semilla
		private void GenerateRegion(int seedIndex)
		{
			Vector2 seed = seeds[seedIndex];

			// Filtramos todos los triangulos alrededor de la semilla
			Delaunay.Triangle[] regionTris = delaunay.FindTrianglesAroundVertex(seed);

			var bounds = new Bounds2D(Vector2.zero, Vector2.one);

			// Obtenemos cada circuncentro CCW
			// Los ordenamos en sentido antihorario (a partir de su Coord. Polar respecto a la semilla)
			var polygon = new List<Vector2>();
			foreach (Delaunay.Triangle tri in regionTris)
			{
				Vector2? circumcenter = tri.GetCircumcenter();

				// Podria ser un triangulo con vertices colineales. En ese caso lo ignoramos
				if (!circumcenter.HasValue) continue;

				Vector2 c = circumcenter.Value;

				polygon.Add(c);

				if (!tri.IsBorder || !SeedInBorder(seed, regionTris) || bounds.OutOfBounds(c)) continue;

				// Si la semilla y este triangulo forman parte del borde y su circuncentro no esta fuera de la BB
				// Extendemos la mediatriz hasta la Bounding Box hasta que intersecte
				for (var i = 0; i < 3; i++)
				{
					if (tri.neighbours[i] != null) continue;

					Delaunay.Edge borderEdge = tri.Edges[i];
					if (borderEdge.begin != seed && borderEdge.end != seed) continue;

					Vector2 m = (borderEdge.begin + borderEdge.end) / 2;

					// Buscamos la Arista de la Bounding Box que intersecta la Mediatriz con el Rayo [c -> m]
					// Si el circuncentro está fuera del triangulo debemos invertir el rayo
					// Si esta a la derecha => esta fuera => Invertimos el Rayo [m -> c]
					Vector2[] intersections = Array.Empty<Vector2>();
					if (GeometryUtils.Equals(c, m))
					{
						Vector2 edgeV = borderEdge.end - borderEdge.begin;
						// LA PERPENDICULAR
						Vector2 mediatriz = new Vector2(edgeV.y, -edgeV.x).normalized;
						intersections = bounds.Intersections_Ray(m, mediatriz).ToArray();
					}
					else
					{
						intersections =
							GeometryUtils.IsRight(borderEdge.begin, borderEdge.end, c)
								? bounds.Intersections_Ray(m, c - m).ToArray()
								: bounds.Intersections_Ray(c, m - c).ToArray();
					}

					// if (intersections.Length == 1)
					// 	polygon.Add(intersections[0]);
					polygon.AddRange(intersections);
				}
			}

			// Ordenamos los vertices CCW
			polygon = polygon.OrderBy(p => Vector2.SignedAngle(Vector2.right, p - seed)).ToList();

			// RECORTE
			// Clampeamos cada Region a la Bounding Box
			List<Vector2> clampedPolygon = new();
			for (var i = 0; i < polygon.Count; i++)
			{
				Vector2 vertex = polygon[i];

				// Si está dentro, conservamos el vertice
				if (bounds.Contains(vertex))
				{
					clampedPolygon.Add(vertex);
					continue;
				}

				// Si esta fuera de la Bounding Box, buscamos la interseccion de sus aristas con la BB
				Vector2 prev = polygon[(i - 1 + polygon.Count) % polygon.Count];
				Vector2 next = polygon[(i + 1) % polygon.Count];
				Vector2[] i1 = bounds.Intersections_Segment(prev, vertex).ToArray();
				Vector2[] i2 = bounds.Intersections_Segment(vertex, next).ToArray();

				// Añadimos las intersecciones en vez del vertice si las hay
				clampedPolygon.AddRange(i1);
				clampedPolygon.AddRange(i2);
			}

			polygon = clampedPolygon;

			// ESQUINAS
			// Añadimos las esquinas de la Bounding Box, buscando las regiones que tengan vertices pertenecientes a dos bordes distintos
			Bounds2D.Side? lastBorderSide;
			bool lastIsOnBorder = bounds.PointOnBorder(polygon[^1], out lastBorderSide);
			for (var i = 0; i < polygon.Count; i++)
			{
				Vector2 vertex = polygon[i];
				bool isOnBorder = bounds.PointOnBorder(vertex, out Bounds2D.Side? borderSide);
				if (!isOnBorder || !lastIsOnBorder || lastBorderSide.Value == borderSide.Value)
				{
					lastIsOnBorder = isOnBorder;
					lastBorderSide = borderSide;
					continue;
				}

				// Solo añadimos la esquina si el vertice y su predecesor pertenecen a dos bordes distintos
				Vector2 corner = bounds.GetCorner(lastBorderSide.Value, borderSide.Value);
				polygon.Insert(i, corner);
				break;
			}

			// Ordenamos los vertices CCW
			polygon = polygon.OrderBy(p => Vector2.SignedAngle(Vector2.right, p - seed)).ToList();

			// Creamos la región
			regions.Add(new Polygon(polygon.ToArray(), seed));
		}


		public IEnumerator AnimationCoroutine(float delay = 0.1f)
		{
			yield return delaunay.AnimationCoroutine(delay);
			while (!Ended)
			{
				Run_OneIteration();
				yield return new WaitForSecondsRealtime(delay);
			}

			drawDelaunayTriangulation = false;
		}

		#endregion

		/// <summary>
		///     Comprueba, a partir de sus triangulos que la rodean, si esta semilla forma parte del borde del Voronoi
		///     (No de la Bounding Box)
		///     Buscamos sus triangulos del borde (les falta un vecino) y cogemos el EJE que forma parte del borde (mismo index que
		///     el vecino)
		///     Si la semilla es uno de los vertices de ese eje, significa que está en el borde
		/// </summary>
		private bool SeedInBorder(Vector2 seed, Delaunay.Triangle[] tris = null)
		{
			tris ??= delaunay.FindTrianglesAroundVertex(seed);

			// Si no hay ningun borde, la seed no puede serlo
			if (tris.All(t => !t.IsBorder)) return false;

			// Buscamos un Triangulo del Borde, y cogemos el eje cuyo vecino es null
			// Si cualquiera de sus vertices es la semilla, entonces esta en el borde
			Delaunay.Triangle borderTri = tris.First(t => t.IsBorder);
			for (var i = 0; i < 3; i++)
			{
				if (borderTri.neighbours[i] != null) continue;
				if (borderTri.Edges[i].Vertices.Any(v => v == seed)) return true;
			}

			return false;
		}


		#region DEBUG

#if UNITY_EDITOR

		private Vector3 MousePos => Input.mousePosition;

		[Range(0, 1)]
		public float regionMargin = 0.05f;

		public bool drawSeeds = true;
		public bool drawGrid = true;
		public bool drawRegions = true;
		public bool wireRegions;
		public bool drawDelaunayTriangulation;
		public bool wireTriangulation = true;
		public bool projectOnTerrain = true;

		public void OnDrawGizmos(Matrix4x4 matrix)
		{
			GizmosSeeds(matrix);
			GizmosGrid(matrix, Color.grey);
			GizmosRegions(matrix, wireRegions);

			// DELAUNAY
			if (drawDelaunayTriangulation)
				delaunay?.OnDrawGizmos(matrix, wireTriangulation, projectOnTerrain);
		}

		// Draw Seeds as Spheres
		private void GizmosSeeds(Matrix4x4 matrix)
		{
			if (!drawSeeds) return;

			Color[] colors = Color.red.GetRainBowColors(seeds.Length);
			Gizmos.color = Color.grey;

			for (var i = 0; i < seeds.Length; i++)
			{
				Vector2 s = seeds[i];
				Color color = colors[i];

				Gizmos.color = color;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(s.ToVector3xz()), .1f);
			}
		}

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		private void GizmosGrid(Matrix4x4 matrix, Color color = default)
		{
			if (!drawGrid) return;

			if (seedDistribution == SeedDistribution.Random)
			{
				// Surrounding Bound only
				GizmosExtensions.DrawQuadWire(matrix, 5, color);
			}
			else
			{
				// GRID
				int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
				GizmosExtensions.DrawGrid(cellRows, cellRows, matrix, 5, color);
			}
		}

		private void GizmosRegions(Matrix4x4 matrix, bool wire = false)
		{
			if (!drawRegions || regions is not { Count: > 0 }) return;

			Vector3 pos = matrix.GetPosition();
			Vector2 size = matrix.lossyScale.ToVector2xz();

			// Region Polygons
			Color[] colors = Color.red.GetRainBowColors(regions.Count);
			for (var i = 0; i < regions.Count; i++)
				if (wire)
					regions[i].OnDrawGizmosWire(matrix, regionMargin, 5, colors[i]);
				else
					regions[i].OnDrawGizmos(matrix, regionMargin, colors[i]);

			// MOUSE to COORDS in VORONOI Bounding Box
			bool mouseOverVoronoi = MouseInputUtils.MouseInArea_CenitalView(pos, size, out Vector2 normPos);

			// Mouse Pos
			Gizmos.color = Color.magenta;
			Gizmos.DrawSphere((normPos * size).ToVector3xz() + pos, .01f);

			// Dibujar solo si el raton esta encima o esta animandose y es la ultima region añadida
			if (!mouseOverVoronoi && Ended) return;

			Polygon regionSelected = Ended ? regions.First(r => r.Contains_RayCast(normPos)) : regions.Last();

			var bounds = new Bounds2D(Vector2.zero, Vector2.one);

			// Triangulos usados para generar la region
			foreach (Delaunay.Triangle t in delaunay.FindTrianglesAroundVertex(regionSelected.centroid))
			{
				t.OnGizmosDrawWire(matrix, 8, Color.blue);

				// Circuncentros de cada triangulo
				Vector2? circumcenter = t.GetCircumcenter();
				if (!circumcenter.HasValue) continue;
				Gizmos.color = bounds.OutOfBounds(circumcenter.Value) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(circumcenter.Value.ToVector3xz()), .05f);
			}

			// Vertices de la Region
			foreach (Vector2 vertex in regionSelected.vertices)
			{
				Gizmos.color = bounds.PointOnBorder(vertex, out Bounds2D.Side? side) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(vertex.ToVector3xz()), .1f);
			}
		}

		public bool MouseInRegion(out int regionIndex, Vector3 originPos, Vector2 size)
		{
			regionIndex = -1;
			MouseInputUtils.MouseInArea_CenitalView(originPos, size, out Vector2 normalizedPos);
			for (var i = 0; i < regions.Count; i++)
			{
				if (!regions[i].Contains_RayCast(normalizedPos)) continue;
				regionIndex = i;
				return true;
			}

			return false;
		}

#endif

		#endregion
	}
}
