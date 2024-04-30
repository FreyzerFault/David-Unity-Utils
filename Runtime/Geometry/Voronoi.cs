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

			foreach (Vector2 regionSeed in seeds)
				regions.Add(new Polygon(GenerateRegion(regionSeed), regionSeed));

			return regions.ToArray();
		}

		#region PROGRESSIVE RUN

		public int iteration;

		// Habra terminado cuando para todas las semillas haya una region
		public bool Ended => regions.Count == seeds.Length;

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

		public void Run_OneIteration()
		{
			if (Ended) return;

			if (Triangles.Length == 0) GenerateDelaunay();

			regions.Add(new Polygon(GenerateRegion(seeds[iteration]), seeds[iteration]));

			iteration++;
		}

		// Genera los vertices de una region a partir de la semilla y los triangulos generados con Delaunay
		private Vector2[] GenerateRegion(Vector2 seed)
		{
			var polygon = new List<Vector2>();
			Delaunay.Triangle[] regionTris = delaunay.FindTrianglesAroundVertex(seed);
			var bounds = new Bounds2D(Vector2.zero, Vector2.one);

			// Obtenemos cada circuncentro CCW
			// Los ordenamos en sentido antihorario (a partir de su Coord. Polar respecto a la semilla)
			bool seedInBorder = SeedInBorder(seed, out List<Delaunay.Edge> borderEdges, regionTris);

			polygon.AddRange(regionTris.Select(t => t.GetCircumcenter()));

			// Para que la Region este dentro de unas fronteras
			// Aplicamos algunas modificaciones para RECORTAR o EXPANDIR la región al borde

			// EXTENDER MEDIATRIZ
			// Si la semilla forma parte del borde
			if (seedInBorder)
				// Para cada eje del borde, si el circuncentro de su triangulo no esta fuera de la Bounding Box
				// Extendemos la mediatriz hasta la Bounding Box hasta que intersecte
				foreach (Delaunay.Edge borderEdge in borderEdges)
				{
					Vector2 m = (borderEdge.begin + borderEdge.end) / 2;
					Vector2 edgeDir = (borderEdge.end - borderEdge.begin).normalized;

					// Buscamos la Arista de la Bounding Box que intersecta la Mediatriz
					// Usamos un Rayo que salga del Triangulo para encontrar la interseccion
					// La direccion del rayo debe ser PERPENDICULAR a la arista hacia la derecha (90º CCW) => [-y,x]
					Vector2[] intersections =
						bounds.Intersections_Ray(m, new Vector2(edgeDir.y, -edgeDir.x)).ToArray();

					polygon.AddRange(intersections);
				}


			// Ordenamos los vertices CCW antes de hacer mas modificaciones
			polygon = polygon.SortByAngle(seed).ToList();

			// RECORTE
			// Clampeamos cada Region a la Bounding Box
			polygon = bounds.CropPolygon(polygon.ToArray()).ToList();

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
			return polygon.SortByAngle(seed).ToArray();
		}

		#endregion

		/// <summary>
		///     Comprueba, a partir de sus triangulos que la rodean, si esta semilla forma parte del borde del Voronoi
		///     (No de la Bounding Box)
		///     Buscamos sus triangulos del borde (les falta un vecino) y cogemos el EJE que forma parte del borde (mismo index que
		///     el vecino)
		///     Si la semilla es uno de los vertices de ese eje, significa que está en el borde
		/// </summary>
		private bool SeedInBorder(Vector2 seed, out List<Delaunay.Edge> borderEdges, Delaunay.Triangle[] tris = null)
		{
			borderEdges = new List<Delaunay.Edge>();

			tris ??= delaunay.FindTrianglesAroundVertex(seed);

			// Si no hay ningun triangulo del borde, la seed no puede serlo
			if (tris.All(t => !t.IsBorder)) return false;

			// Buscamos los 2 Triangulos del Borde, y cogemos los ejes del borde (triangulo vecino == null)
			// Uno de sus vertices debe ser la semilla para constar como borde
			foreach (Delaunay.Triangle borderTri in tris.Where(t => t.IsBorder))
				for (var i = 0; i < 3; i++)
				{
					if (borderTri.neighbours[i] != null) continue;

					// Si la semilla no esta en el eje del borde, no se considera borde
					if (borderTri.Edges[i].Vertices.All(v => v != seed)) return false;

					borderEdges.Add(borderTri.Edges[i]);
					break;
				}

			if (borderEdges.Count == 2) return true;
			throw new Exception(
				$"Se han encontrado {borderEdges.Count} ejes del borde. Algo ha ido mal porque debería ser 2"
			);
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

			Polygon regionSelected = Ended ? regions.FirstOrDefault(r => r.Contains_RayCast(normPos)) : regions.Last();

			if (regionSelected.vertices == null || regionSelected.vertices.Length == 0) return;

			DrawRegionSelected(regionSelected, matrix);
		}

		public void DrawRegionSelected(Polygon region, Matrix4x4 matrix)
		{
			var bounds = new Bounds2D(Vector2.zero, Vector2.one);

			// Triangulos usados para generar la region
			foreach (Delaunay.Triangle t in delaunay.FindTrianglesAroundVertex(region.centroid))
			{
				t.OnGizmosDrawWire(matrix, 8, Color.blue);

				// Circuncentros de cada triangulo
				Vector2? circumcenter = t.GetCircumcenter();
				if (!circumcenter.HasValue) continue;
				Vector2 c = circumcenter.Value;

				Gizmos.color = bounds.OutOfBounds(circumcenter.Value) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(c.ToVector3xz()), .05f);

				if (bounds.OutOfBounds(c)) continue;

				// BORDER EDGE
				for (var i = 0; i < 3; i++)
				{
					if (t.neighbours[i] != null) continue;

					Delaunay.Edge borderEdge = t.Edges[i];
					if (borderEdge.begin != region.centroid && borderEdge.end != region.centroid) continue;

					Vector2 m = (borderEdge.begin + borderEdge.end) / 2;
					Vector2 edgeDir = (borderEdge.end - borderEdge.begin).normalized;

					// Buscamos la Arista de la Bounding Box que intersecta la Mediatriz
					// Usamos un Rayo que salga del Triangulo para encontrar la interseccion
					// La direccion del rayo debe ser PERPENDICULAR a la arista hacia la derecha (90º CCW) => [-y,x]
					Vector2[] intersections = bounds.Intersections_Ray(m, new Vector2(edgeDir.y, -edgeDir.x)).ToArray();

					GizmosExtensions.DrawLineThick(
						matrix.MultiplyPoint3x4(m.ToVector3xz()),
						matrix.MultiplyPoint3x4(intersections.First().ToVector3xz()),
						6,
						Color.red
					);
				}
			}

			// Vertices de la Region
			foreach (Vector2 vertex in region.vertices)
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
