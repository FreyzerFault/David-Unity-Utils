using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;
#if UNITY_EDITOR
using DavidUtils.DebugUtils;
#endif

namespace DavidUtils.Geometry
{
	[Serializable]
	public class Voronoi
	{
		[HideInInspector] public Delaunay delaunay;

		[HideInInspector] public List<Polygon> regions;

		private List<Vector2> seeds;
		public List<Vector2> Seeds
		{
			get => seeds;
			set
			{
				seeds = value;
				Reset();
			}
		}

		private Delaunay.Triangle[] Triangles => delaunay.triangles.ToArray();

		public Voronoi(IEnumerable<Vector2> seeds, Delaunay delaunay = null)
		{
			this.seeds = seeds.ToList();
			regions = new List<Polygon>();
			this.delaunay = delaunay ?? new Delaunay(this.seeds);
		}


		public void MoveSeed(int index, Vector2 newPos)
		{
			// Si colisiona con otra semilla no la movemos
			if (seeds.Where((p, i) => i != index).Any(p => Vector2.Distance(p, newPos) < GeometryUtils.Epsilon))
				return;

			seeds[index] = newPos;

			delaunay.MoveSeed(index, newPos);

			if (Ended)
			{
				Reset();
				GenerateVoronoi();
			}
			else
			{
				Reset();
			}
		}

		public void MoveSeed(Vector2 oldPos, Vector2 newPos) =>
			MoveSeed(seeds.IndexOf(oldPos), newPos);

		public void Reset()
		{
			iteration = 0;
			regions = new List<Polygon>();
			delaunay.Reset();
		}

		public void GenerateDelaunay() => delaunay.Triangulate(seeds);

		public IEnumerable<Polygon> GenerateVoronoi()
		{
			// Se necesita triangular las seeds primero.
			if (Triangles.Length == 0) GenerateDelaunay();

			foreach (Vector2 regionSeed in seeds)
			{
				Vector2[] region = GenerateRegionPolygon(regionSeed).ToArray();
				if (region.Length <= 2) continue;
				regions.Add(new Polygon(region, regionSeed));
			}

			return regions;
		}

		#region PROGRESSIVE RUN

		public int iteration;

		// Habra terminado cuando para todas las semillas haya una region
		public bool Ended => regions.Count == seeds.Count;

		public IEnumerator AnimationCoroutine(float delay = 0.1f)
		{
			if (!delaunay.ended)
				yield return delaunay.AnimationCoroutine(delay);

			while (!Ended)
			{
				Run_OneIteration();
				yield return new WaitForSecondsRealtime(delay);
			}
		}

		public void Run_OneIteration()
		{
			if (Ended) return;

			if (!delaunay.ended)
			{
				delaunay.Run_OnePoint();
			}
			else
			{
				Vector2[] regionPolygon = GenerateRegionPolygon(seeds[iteration]).ToArray();
				if (regionPolygon.Length > 2)
					regions.Add(new Polygon(regionPolygon, seeds[iteration]));
			}

			iteration++;
		}

		// Genera los vertices de una region a partir de la semilla y los triangulos generados con Delaunay
		private IEnumerable<Vector2> GenerateRegionPolygon(Vector2 seed)
		{
			var polygon = new List<Vector2>();
			Delaunay.Triangle[] regionTris = delaunay.FindTrianglesAroundVertex(seed).ToArray();
			Bounds2D bounds = Bounds2D.NormalizedBounds;

			// Obtenemos cada circuncentro CCW
			polygon.AddRange(regionTris.Select(t => t.GetCircumcenter()));

			if (polygon.Count == 0) return polygon.ToArray();

			// Para que la Region este dentro de unas fronteras
			// Aplicamos algunas modificaciones para RECORTAR o EXPANDIR la región al borde

			// EXTENDER MEDIATRIZ
			// Si la semilla forma parte del borde
			if (regionTris.Any(t => t.IsBorder))
				foreach (Delaunay.Triangle t in regionTris)
				{
					// Para cada triangulo del borde, si el circuncentro no esta fuera de la Bounding Box
					// Extendemos la mediatriz de la arista del borde hasta la Bounding Box hasta que intersecte
					if (bounds.OutOfBounds(t.GetCircumcenter())) continue;

					for (var i = 0; i < 3; i++)
					{
						Delaunay.Triangle neigh = t.neighbours[i];
						if (neigh != null) continue;

						Delaunay.Edge edge = t.Edges[i];
						if (edge.Vertices.All(v => v != seed)) continue;

						// Buscamos la Arista de la Bounding Box que intersecta la Mediatriz
						// Usamos un Rayo que salga del Triangulo para encontrar la interseccion
						// La direccion del rayo debe ser PERPENDICULAR a la arista hacia la derecha (90º CCW) => [-y,x]
						Vector2[] intersections = edge.MediatrizIntersetions_RIGHT(bounds);

						polygon.AddRange(intersections);
					}
				}

			if (polygon.Count <= 2) return polygon.ToArray();


			// Ordenamos los vertices CCW antes de hacer mas modificaciones
			polygon = polygon.SortByAngle(seed).ToList();

			// RECORTE
			// Clampeamos cada Region a la Bounding Box
			polygon = bounds.CropPolygon(polygon.ToArray()).ToList();

			if (polygon.Count <= 2) return polygon.ToArray();

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
			return polygon.SortByAngle(seed);
		}

		#endregion

		/// <summary>
		///     Comprueba, a partir de sus triangulos que la rodean, si esta semilla forma parte del borde del Voronoi
		///     (No de la Bounding Box)
		///     Buscamos sus triangulos del borde (les falta un vecino) y cogemos el EJE que forma parte del borde (mismo index que
		///     el vecino)
		///     Si la semilla es uno de los vertices de ese eje, significa que está en el borde
		/// </summary>
		private bool SeedInBorder(
			Vector2 seed, out List<Delaunay.Edge> borderEdges, out List<Vector2> circumcenters,
			Delaunay.Triangle[] tris = null
		)
		{
			borderEdges = new List<Delaunay.Edge>();
			circumcenters = new List<Vector2>();

			tris ??= delaunay.FindTrianglesAroundVertex(seed).ToArray();

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
					circumcenters.Add(borderTri.GetCircumcenter());
				}

			if (borderEdges.Count == 2) return true;
			throw new Exception(
				$"Se han encontrado {borderEdges.Count} ejes del borde. Algo ha ido mal porque debería ser 2"
			);
		}

		public Polygon? GetRegion(Vector2 point) =>
			regions?.Count > 0
				? regions[GetRegionIndex(point)]
				: null;

		public int GetRegionIndex(Vector2 point)
		{
			if (regions.Count == 0 || !point.IsIn01()) return -1;

			Tuple<int, float>[] distances = regions
				.Select((r, i) => new Tuple<int, float>(i, Vector2.Distance(r.centroid, point)))
				.OrderBy(t => t.Item2)
				.ToArray();

			if (distances.Length == 0) return -1;
			return distances[0].Item1;
		}

		#region DEBUG

#if UNITY_EDITOR

		[Range(.2f, 1)]
		public float regionScale = 1;

		public bool drawGizmos = true;
		public bool drawWire;

		public void OnDrawGizmos(Matrix4x4 matrix, Color[] colors = null, bool projectOnTerrain = false)
		{
			if (!drawGizmos || regions is not { Count: > 0 }) return;

			if (colors is null || colors.Length != regions.Count)
				colors = Color.red.GetRainBowColors(regions.Count);

			// Region Polygons
			for (var i = 0; i < regions.Count; i++)
				if (drawWire)
					regions[i].OnDrawGizmosWire(matrix, regionScale, 5, colors[i], projectOnTerrain);
				else
					regions[i].OnDrawGizmos(matrix, regionScale, colors[i], projectOnTerrain);
		}

		public void DrawRegionGizmos_Highlighted(Polygon region, Matrix4x4 matrix, bool projectOnTerrain = false) =>
			region.OnDrawGizmosWire(matrix, regionScale + .01f, 8, Color.yellow, projectOnTerrain);

		public void DrawRegionGizmos_Detailed(Polygon region, Matrix4x4 matrix, bool projectOnTerrain = false)
		{
			Bounds2D bounds = Bounds2D.NormalizedBounds;

			// VERTEX in Bounding Box Edges
			foreach (Vector2 vertex in region.vertices)
			{
				Gizmos.color = bounds.PointOnBorder(vertex, out Bounds2D.Side? _) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(vertex.ToVector3xz()), .1f);
			}

			// Triangulos usados para generar la region
			foreach (Delaunay.Triangle t in delaunay.FindTrianglesAroundVertex(region.centroid))
			{
				t.OnGizmosDrawWire(matrix, 8, Color.blue, projectOnTerrain);

				// CIRCUNCENTROS
				Vector2 c = t.GetCircumcenter();

				Gizmos.color = bounds.OutOfBounds(c) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(c.ToVector3xz()), .05f);

				if (!t.IsBorder || bounds.OutOfBounds(c)) continue;

				// BORDER EDGE
				foreach (Delaunay.Edge borderEdge in t.BorderEdges)
				{
					if (borderEdge.begin != region.centroid && borderEdge.end != region.centroid) continue;

					// Interseccion con la Bounding Box hacia fuera del triangulo
					// Debe tener 1, porque todas las aristas deben estar dentro de la Boundig Box
					Vector2 intersections = borderEdge.MediatrizIntersetions_RIGHT(bounds).First();

					if (projectOnTerrain)
						GizmosExtensions.DrawLineThick_OnTerrain(
							matrix.MultiplyPoint3x4(borderEdge.Median.ToVector3xz()),
							matrix.MultiplyPoint3x4(intersections.ToVector3xz()),
							6,
							Color.red
						);
					else
						GizmosExtensions.DrawLineThick(
							matrix.MultiplyPoint3x4(borderEdge.Median.ToVector3xz()),
							matrix.MultiplyPoint3x4(intersections.ToVector3xz()),
							6,
							Color.red
						);
				}
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
