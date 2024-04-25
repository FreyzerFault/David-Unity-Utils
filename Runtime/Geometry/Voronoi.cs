using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugExtensions;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

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
			bool lastIsOnBorder = bounds.PointOnBorder(polygon[0], out lastBorderSide);
			for (var i = 1; i < polygon.Count; i++)
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
			while (!Ended)
			{
				Run_OneIteration();
				yield return new WaitForSecondsRealtime(delay);
			}

			yield return null;
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

		public void OnDrawGizmos(Vector3 pos, Vector2 size)
		{
			GizmosSeeds(pos, size);
			GizmosGrid(pos, size);
			GizmosRegions(pos, size, wireRegions);

			// DELAUNAY
			if (drawDelaunayTriangulation)
				delaunay?.OnDrawGizmos(pos, size, wireTriangulation, projectOnTerrain);
		}

		// Draw Seeds as Spheres
		private void GizmosSeeds(Vector3 pos, Vector2 size)
		{
			if (!drawSeeds) return;

			Color[] colors = Color.red.GetRainBowColors(seeds.Length);
			Gizmos.color = Color.grey;

			var drawnedTri = new HashSet<Delaunay.Triangle>();

			for (var i = 0; i < seeds.Length; i++)
			{
				Vector2 seed = seeds[i];
				Color color = colors[i];
				// var seedTris = delaunay.FindTrianglesAroundVertex(seed);
				// if (seedTris is { Length: > 0 } && seedTris.All(t => !drawnedTri.Contains(t)))
				// 	foreach (Delaunay.Triangle triangle in seedTris)
				// 	{
				// 		if (!drawnedTri.Add(triangle)) continue;
				// 		triangle.OnGizmosDraw(pos, size, color);
				// 	}

				Vector2 seedScaled = seed * size;
				Vector3 seedPos = new Vector3(seedScaled.x, 0, seedScaled.y) + pos;
				Gizmos.DrawSphere(seedPos, .1f);
			}
		}

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		private void GizmosGrid(Vector3 pos, Vector2 size)
		{
			if (!drawGrid) return;

			Gizmos.color = Color.blue;
			if (seedDistribution == SeedDistribution.Random)
			{
				// Surrounding Bound only
				GizmosExtensions.DrawQuadWire(
					pos + size.ToVector3xz() / 2,
					size,
					Quaternion.FromToRotation(Vector3.up, Vector3.forward),
					5,
					Color.blue
				);
			}
			else
			{
				// GRID
				int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
				GizmosExtensions.DrawGrid(
					cellRows,
					cellRows,
					pos + size.ToVector3xz() / 2,
					size,
					Quaternion.FromToRotation(Vector3.up, Vector3.forward),
					5,
					Color.blue
				);
			}
		}

		private void GizmosRegions(Vector3 pos, Vector2 size, bool wire = false)
		{
			if (!drawRegions || regions is not { Count: > 0 }) return;

			// MOUSE to COORDS in VORONOI Bounding Box
			Vector3 mousePos = Input.mousePosition;
			Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos).WithY(0);
			Vector2 normalizePos = (worldMousePos - pos).ToVector2xz() / size; // Mouse pos in Voronoi local coords


			Debug.Log($"Mouse Pos: {mousePos}; Screen Width: {Screen.width}; Screen Height: {Screen.height}");

			Gizmos.color = Color.magenta;
			Gizmos.DrawSphere((normalizePos * size).ToVector3xz() + pos, .01f);

			bool isInRegion = MouseInRegion(out int mouseRegionIndex, pos, size);

			// Dibujar solo si el raton esta encima
			if (MouseInScreen() && isInRegion)
			{
				var bounds = new Bounds2D(Vector2.zero, Vector2.one);
				Polygon mouseRegion = regions[mouseRegionIndex];
				foreach (Delaunay.Triangle t in delaunay.FindTrianglesAroundVertex(mouseRegion.centroid))
				{
					t.OnGizmosDraw(pos, size, Color.yellow);
					Vector2? circumcenter = t.GetCircumcenter();
					if (!circumcenter.HasValue) continue;

					Gizmos.color = bounds.OutOfBounds(circumcenter.Value) ? Color.red : Color.green;
					Gizmos.DrawSphere(pos + (t.GetCircumcenter().Value * size).ToVector3xz(), .05f);
				}

				foreach (Vector2 vertex in mouseRegion.vertices)
				{
					Gizmos.color = bounds.PointOnBorder(vertex, out Bounds2D.Side? side) ? Color.red : Color.green;
					Gizmos.DrawSphere(pos + (vertex * size).ToVector3xz(), .1f);
				}
			}

			Color[] colors = Color.red.GetRainBowColors(regions.Count);
			for (var i = 0; i < regions.Count; i++)
			{
				Vector3 regionCentroid = pos + (regions[i].centroid * size).ToVector3xz();
				if (wire)
					regions[i].OnDrawGizmosWire(regionCentroid, size, regionMargin, 5, colors[i]);
				else
					regions[i].OnDrawGizmos(regionCentroid, size, regionMargin, colors[i]);
			}
		}

		public static bool MouseInScreen()
		{
			Vector3 mousePos = Input.mousePosition;
			return mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
		}

		public bool MouseInRegion(out int regionIndex, Vector3 originPos, Vector2 size)
		{
			regionIndex = -1;
			Vector3 mousePos = Input.mousePosition;
			Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos).WithY(0);
			Vector2 normalizePos =
				(worldMousePos - originPos).ToVector2xz() / size; // Mouse pos in Voronoi local coords
			for (var i = 0; i < regions.Count; i++)
			{
				if (!regions[i].Contains_RayCast(normalizePos)) continue;
				regionIndex = i;
				return true;
			}

			return false;
		}

		#endregion
	}
}
