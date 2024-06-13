using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public struct Polygon : IEquatable<Polygon>
	{
		// Vertices in Counter-Clockwise order
		public Vector2[] vertices;
		public Vector2 centroid;

		public int VextexCount => vertices.Length;

		// EDGES
		public IEnumerable<Edge> Edges => vertices.IterateByPairs_InLoop((a, b) => new Edge(a, b), false);


		public Polygon(IEnumerable<Vector2> vertices, Vector2 centroid)
		{
			this.vertices = vertices?.ToArray() ?? Array.Empty<Vector2>();
			this.centroid = centroid;
		}

		public Polygon(IEnumerable<Vector2> vertices) : this(vertices, default) => UpdateCentroid();

		public Polygon(IEnumerable<Edge> edges) : this(edges.Select(e => e.begin))
		{
		}

		public void UpdateCentroid() => centroid = vertices.Center();

		public Vector3 GetCentroidInWorld(Matrix4x4 matrixTRS) => matrixTRS.MultiplyPoint3x4(centroid.ToV3());

		public Vector2[] VerticesScaledByCenter(float centeredScale)
		{
			Vector2 c = centroid;
			return vertices.Select(v => c + (v - c) * centeredScale).ToArray();
		}

		public Polygon ScaleByCenter(float centeredScale) =>
			Mathf.Approximately(centeredScale, 1) ? this : new Polygon(VerticesScaledByCenter(centeredScale), centroid);


		#region POLYGON CONVERSIONS

		/// <summary>
		///     Reduce el poligono a uno con aristas paralelas, a una distancia dada
		///     Movemos cada arista hacia dentro (vector perpendicular) la distancia del margen (magnitud)
		///     y creamos un nuevo poligono con las intersecciones de las aristas creadas adyacentes
		///     Puede usarse un margen distinto para cada eje
		/// </summary>
		public Polygon InteriorPolygon(Vector2 margin2D)
		{
			Edge[] edges = Edges.ToArray();

			// Genero las Aristas paralelas a la distancia dada
			Edge[] parallels = edges.Select(e => e.ParallelLeft(new Vector2(margin2D.x, margin2D.y))).ToArray();

			// Genero las intersecciones de las aristas paralelas
			Vector2[] newVertices = parallels.IterateByPairs_InLoop(
					(e1, e2) =>
					{
						Vector2? intersection = e1.Intersection_LineLine(e2);
						if (!intersection.HasValue) Debug.Log("No intersection found");
						return intersection ?? e2.begin;
					}
				)
				.ToArray();

			// El poligono puede tener intersecciones consigo mismo, se legaliza
			return new Polygon(newVertices); //.Legalize();
		}

		// Overload para usar el mismo margen en ambos ejes
		public Polygon InteriorPolygon(float margin) => InteriorPolygon(new Vector2(margin, margin));

		/// <summary>
		///     Busca y elimina secciones del poligonos invalidas (intersecciona con si mismo)
		/// </summary>
		public Polygon Legalize()
		{
			Polygon autoIntersectedPolygon = AddAutoIntersections();

			// Si no hay intersecciones lo devolvemos tal cual
			bool hasIntersections = autoIntersectedPolygon.vertices.Length != vertices.Length;
			if (!hasIntersections) return this;

			Polygon[] polygons = autoIntersectedPolygon.SplitAutoIntersectedPolygons(out Vector2[] intersections);

			// - Filtramos por los poligonos VALIDOS (CCW)
			(Polygon p, int i)[] sections =
				polygons
					.Select((p, i) => (p, i))
					.Where(pair => pair.p.IsCounterClockwise())
					.ToArray();

			// - Los unimos en un nuevo poligono (CCW)
			// TODO: No se si funcione bien la union
			// Por ahora los unire por su punto de interseccion con el que los separe
			List<Vector2> finalVertices = new();
			foreach ((Polygon, int) section in sections)
			{
				int index = section.Item2;
				Vector2 connectionVertex = intersections[index];
				Polygon polygon = section.Item1;

				// Los ordeno siendo el inicial el vertice de conexion
				int initIndex = polygon.vertices.IndexOf(connectionVertex);
				finalVertices.AddRange(polygon.vertices.Skip(initIndex));
				finalVertices.AddRange(polygon.vertices.Take(initIndex));

				// Y loopeo el primer vertice para conectar
				finalVertices.Add(polygon.vertices.First());
			}

			// Elimino el ultimo porque se repetir con el primero
			finalVertices.RemoveAt(finalVertices.Count - 1);

			return new Polygon(finalVertices);
		}

		/// <summary>
		///     Añade a un polígono que se autointersecta, los puntos de interseccion como vértices
		///     Lo cual dividiria el poligono en subpoligonos que seran algunos CCW y otros CW
		/// </summary>
		private Polygon AddAutoIntersections()
		{
			List<Edge> edges = Edges.ToList();

			// - Busca las intersecciones n con n
			// Se busca n a n y se dividen las aristas en caso de interseccion
			// Se repite hasta que no haya intersecciones (por seguridad hasta un maximo de repeticiones)
			int initialEdges = edges.Count;
			var restartCount = 0;
			var maxRestarts = 20;
			for (var i = 0; i < edges.Count && restartCount < maxRestarts; i++)
			for (int j = i + 2; j < edges.Count && restartCount < maxRestarts; j++)
			{
				if (i == j || (i == 0 && j == edges.Count - 1)) continue;
				Edge e1 = edges[i];
				Edge e2 = edges[j];
				Vector2? intersection = e1.Intersection(e2);
				if (intersection.HasValue)
				{
					if (Vector2.Distance(intersection.Value, e1.end) < 0.001f
					    || Vector2.Distance(intersection.Value, e2.begin) < 0.001f) continue;

					edges.RemoveAt(i);
					edges.InsertRange(i, e1.Split(intersection.Value));

					int newJ = j > i ? j + 1 : j;
					edges.RemoveAt(newJ);
					edges.InsertRange(newJ, e2.Split(intersection.Value));

					// Repite
					i = -1;
					restartCount++;
					break;
				}
			}

			if (edges.Count > 40)
				Debug.Log("DEMASIADOS");
			return new Polygon(edges);
		}

		private Polygon[] SplitAutoIntersectedPolygons(out Vector2[] intersectionVertices)
		{
			// - Separamos el poligono en secciones validas y no validas
			// Iteramos los vertices y cuando encontremos un vertice que se repita
			// guardamos los vertices desde la coincidencia como un polígono cerrado
			Dictionary<Polygon, Vector2> sections = new();
			List<Vector2> currentVertices = new();
			foreach (Edge edge in Edges)
			{
				Vector2 vertex = edge.begin;
				// Si encuentra una coincidencia, añadimos el poligono desde el vertice repetido hasta el final
				// Este poligono sera una seccion cerrada
				// Eliminamos los vertices de la seccion manteniendo los anteriores y seguimos
				int foundIndex = currentVertices.FindIndex(v => v == edge.begin);
				if (foundIndex != -1)
				{
					sections.Add(new Polygon(currentVertices.Skip(foundIndex)), currentVertices[foundIndex]);
					currentVertices = currentVertices.Take(foundIndex).ToList();
				}

				currentVertices.Add(vertex);
			}

			// Last section
			sections.Add(new Polygon(currentVertices), currentVertices.First());

			intersectionVertices = sections.Values.ToArray();
			return sections.Keys.ToArray();
		}

		#endregion


		#region TESTS

		/// <summary>
		///     Calcula el area como la suma de areas de los triangulos formados por el centroide y cada arista
		///     Pero la formula simplificada es
		///     1 / 2 * SUM (Xi * Yi+1 - Xi+1 * Yi)
		/// </summary>
		public float Area() => .5f * DoubleArea();

		// No 1/2 para cuando necesito solo el signo
		public float DoubleArea() => vertices
			.IterateByPairs_NoLoop((v1, v2) => v1.x * v2.y - v2.x * v1.y)
			.Sum();

		public bool IsCounterClockwise() => DoubleArea() > 0;
		public bool IsClockwise() => DoubleArea() < 0;

		// TEST Point is inside Polygon
		// Using Cross Product
		// Only works on CONVEX polygons
		public bool Contains_CrossProd(Vector2 point)
		{
			// TEST Point LEFT for each Edge
			for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
			{
				Vector2 a = vertices[j], b = vertices[i];
				RelativePos relativePos = PointRelativePos(a, b, point);
				if (relativePos != RelativePos.LEFT) return false;
			}

			return true;
		}

		private enum RelativePos { LEFT, RIGHT, ON }

		private RelativePos PointRelativePos(Vector2 a, Vector2 b, Vector2 point)
		{
			Vector2 ab = b - a, ap = point - a;

			// Cross Product
			float cross = ab.x * ap.y - ab.y * ap.x;

			// AB x AP > 0 => RIGHT => OUTSIDE
			return cross > 0 ? RelativePos.RIGHT :
				cross < 0 ? RelativePos.LEFT :
				RelativePos.ON;
		}

		// TEST Point is inside Polygon
		// Uses RayCasting
		// It is less efficient but works on CONCAVE polygons
		public bool Contains_RayCast(Vector2 point)
		{
			var contains = false;
			for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
			{
				Vector2 a = vertices[j], b = vertices[i];
				if (b.y > point.y != a.y > point.y &&
				    point.x < (a.x - b.x) * (point.y - b.y) / (a.y - b.y) + b.x)
					contains = !contains;
			}

			return contains;
		}

		#endregion

		#region MESH

		/// <summary>
		///     Triangula creando un triangulo por arista, siendo el centroide el tercer vertice
		/// </summary>
		public Triangle[] Triangulate()
		{
			var tris = new Triangle[vertices.Length];
			for (var i = 0; i < vertices.Length; i++)
				tris[i] = new Triangle(vertices[i], vertices[(i + 1) % vertices.Length], centroid);
			return tris;
		}

		#endregion

		#region DEBUG

#if UNITY_EDITOR


		public void DrawGizmosWire(
			Matrix4x4 mTRS, float thickness = 1, Color color = default, bool projectOnTerrain = false
		)
		{
			if (vertices == null || vertices.Length == 0) return;

			Vector3[] verticesInWorld = mTRS.MultiplyPoint3x4(vertices).ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygonWire_OnTerrain(verticesInWorld, thickness, color);
			else
				GizmosExtensions.DrawPolygonWire(verticesInWorld, thickness, color);
		}

		public void DrawGizmos(
			Matrix4x4 mTRS, Color color = default, Color? outlineColor = null, bool projectOnTerrain = false
		)
		{
			if (vertices == null || vertices.Length == 0) return;

			Vector3[] verticesInWorld = mTRS.MultiplyPoint3x4(vertices).ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygon_OnTerrain(verticesInWorld, color, outlineColor ?? color);
			else
				GizmosExtensions.DrawPolygon(verticesInWorld, color, outlineColor ?? color);
		}

		public void DrawGizmosVertices(Matrix4x4 localToWorldMatrix)
		{
			foreach (Vector2 vertex in vertices)
			{
				Vector3 worldPoint = localToWorldMatrix.MultiplyPoint3x4(vertex);
				Gizmos.color = AABB_2D.NormalizedAABB.PointOnBorder(vertex, out _) ? Color.red : Color.green;
				Gizmos.DrawSphere(worldPoint, .1f);
			}
		}

#endif

		#endregion

		public bool Equals(Polygon other) => Equals(vertices, other.vertices) && centroid.Equals(other.centroid);

		public override bool Equals(object obj) => obj is Polygon other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(vertices, centroid);
	}
}
