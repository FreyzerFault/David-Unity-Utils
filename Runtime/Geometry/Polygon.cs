using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using NUnit.Framework;
using UnityEngine;

namespace DavidUtils.Geometry
{
	[Serializable]
	public struct Polygon : IEquatable<Polygon>
	{
		// 1 Vertex => (0,0)
		public static Polygon Empty => new(new[] {Vector2.zero});

		// Vertices in Counter-Clockwise order
		[SerializeField]
		private Vector2[] _vertices;
		public Vector2 centroid;

		// Empty or 1 single vertex
		public bool IsEmpty => _vertices.IsNullOrEmpty() || _vertices.Length == 1;

		public Vector2[] Vertices
		{
			get => _vertices ?? Array.Empty<Vector2>();
			set
			{
				if (_vertices == value) return;
				_vertices = value;
				_edges = VerticesToEdges(value);
				centroid = value.Center();
				RemoveInvalidEdges();
			}
		}
		public int VertexCount => _vertices?.Length ?? 0;
		
		public Vector2[] VerticesFromCentroid
		{
			get
			{
				Vector2 c = centroid;
				return _vertices?.Select(v => v - c).ToArray();
			}
		}


		#region EDGES

		[SerializeField]
		private Edge[] _edges;
		public Edge[] Edges
		{
			get => _edges;
			set
			{
				if (_edges == value) return;
				_edges = value;
				_vertices = EdgesToVertices(value);
				centroid = _vertices.Center();
				RemoveInvalidEdges();
			}
		}
		public Edge[] EdgesFromCentroid
		{
			get
			{
				Vector2 c = centroid;
				return _edges?.Select(e => new Edge(e.begin - c, e.end - c)).ToArray();
			}
		}

		private static Edge[] VerticesToEdges(Vector2[] verts) =>
			verts.IsNullOrEmpty()
				? Array.Empty<Edge>()
				: verts.IterateByPairs_InLoop((a, b) => new Edge(a, b), false).ToArray();

		private static Vector2[] EdgesToVertices(Edge[] edges) =>
			edges.IsNullOrEmpty()
				? Array.Empty<Vector2>()
				: edges.Select(e => e.begin).ToArray();

		#endregion

		public Polygon(Vector2[] vertices, Edge[] edges, Vector2 centroid)
		{
			_vertices = vertices;
			_edges = edges;
			this.centroid = centroid;
			RemoveInvalidEdges();
		}

		public Polygon(Vector2[] vertices) : this(vertices, VerticesToEdges(vertices), vertices.Center())
		{
		}

		public Polygon(Vector2[] vertices, Edge[] edges) : this(vertices, edges, vertices.Center())
		{
		}

		public Polygon(Edge[] edges) : this(EdgesToVertices(edges), edges)
		{
		}


		#region SECONDARY PROPERTIES

		/// <summary>
		///     Calcula el area como la suma de areas de los triangulos formados por el centroide y cada arista
		///     Pero la formula simplificada es
		///     1 / 2 * SUM (Xi * Yi+1 - Xi+1 * Yi)
		/// </summary>
		public float Area => .5f * DoubleArea;

		// No 1/2 para cuando necesito solo el signo
		public float DoubleArea => _vertices
			.IterateByPairs_InLoop((v1, v2) => v1.x * v2.y - v2.x * v1.y)
			.Sum();

		#endregion


		#region POST-PROCESS

		#region CLEANING

		public void CleanDegeneratePolygon()
		{
			RemoveDuplicates();
			RemoveInvalidEdges();
		}
		
		/// <summary>
		///		Elimina Vertices duplicados
		/// </summary>
		public void RemoveDuplicates() => 
			Vertices = _vertices.Distinct().ToArray();

		/// <summary>
		///     Quita las aristas invalidas (colineales que van y vuelven, generando area nula)
		/// </summary>
		public void RemoveInvalidEdges()
		{
			if (_edges is not { Length: > 2 }) return;
			_edges = _edges.IterateByPairs_InLoop(
					(e1, e2) => Vector2.Distance(e1.Dir, -e2.Dir) < Mathf.Epsilon ? new Edge(e1.begin, e2.end) : e2
				)
				.ToArray();
		}

		#endregion


		#region VERTEX TRANSFORMATION

		public Polygon NormalizeMinMax(Vector2 min, Vector2 max)
		{
			return new Polygon(_vertices.Select(v =>
				new Vector2(
					Mathf.InverseLerp(min.x, max.x, v.x),
					Mathf.InverseLerp(min.y, max.y, v.y)
				)).ToArray());
		}
		
		public Polygon Revert() => new(_vertices.Reverse().ToArray());

		public Polygon SortCCW() => new(_vertices.SortByAngle(centroid).ToArray());

		/// <summary>
		///     Escala el polígono con centro en su centroide
		/// </summary>
		public Polygon ScaleByCenter(float centeredScale) =>
			Mathf.Approximately(centeredScale, 1) ? this : new Polygon(VerticesScaledByCenter(centeredScale));

		private Vector2[] VerticesScaledByCenter(float centeredScale)
		{
			Vector2 c = centroid;
			return VerticesFromCentroid?.Select(v => c + v * centeredScale).ToArray();
		}
		
		/// <summary>
		///     Reduce el poligono a uno con aristas paralelas, a una distancia dada
		///     Movemos cada arista hacia dentro (vector perpendicular) la distancia del margen (magnitud)
		///     y creamos un nuevo poligono con las intersecciones de las aristas creadas adyacentes
		///     Puede usarse un margen distinto para X e Y
		/// </summary>
		public Polygon InteriorPolygon(Vector2 margin2D)
		{
			if (IsEmpty) return Empty;
			
			// Genero las Aristas paralelas a la distancia dada
			List<Edge> parallels = _edges.Select(e => e.ParallelLeft(new Vector2(margin2D.x, margin2D.y))).ToList();

			// Genero las intersecciones de las aristas paralelas
			Vector2[] intersections = parallels.IterateByPairs_InLoop(
					(e1, e2) =>
					{
						Vector2? intersection = e1.Intersection_LineLine(e2);
						if (!intersection.HasValue) Debug.Log("No intersection found");
						return intersection ?? e2.begin;
					}
				)
				.ToArray();

			// Comparamos la dirección de las aristas nuevas con las paralelas.
			// Si es la contraria, es un eje inválido. Lo eliminamos
			// Y susituimos la arista por un vertices interseccion entre las arista adyacentes
			List<Edge> interiorEdges = VerticesToEdges(intersections).ToList();
			for (var i = 0; i < interiorEdges.Count; i++)
			{
				Vector2 edgeDir = interiorEdges[i].Dir;
				Vector2 parallelDir = parallels[i].Dir;

				if (edgeDir == parallelDir) continue; // Misma direccion, valida
				if (edgeDir != -parallelDir) continue; // No son colineales (no deberia pasar)

				// Calculamos la intersección de los ejes adyacentes
				Edge prev = interiorEdges[(i - 1 + interiorEdges.Count) % interiorEdges.Count];
				Edge next = interiorEdges[(i + 1) % interiorEdges.Count];
				Vector2? intersection = prev.Intersection_LineLine(next);

				// Puede que fueran colineales, lo cual no deberia pasar, pero por si acaso, ignoramos este caso
				if (!intersection.HasValue) continue;

				// NO ES VALIDA => La eliminamos
				interiorEdges.RemoveAt(i);
				parallels.RemoveAt(i);

				// Sustituimos sus adyacentes conectandolos con la interseccion
				// Con cuidado si estan en los extremos
				interiorEdges[i == interiorEdges.Count ? 0 : i].begin = intersection.Value;
				interiorEdges[i == 0 ? ^1 : i - 1].end = intersection.Value;

				// Reiniciamos la busqueda desde 0 porque puede generar aristas invalidas de nuevo
				i = -1;
			}

			// Legalize() hara un postprocessing de limpieza de aristas invalidas y se asegura que sea CCW
			return new Polygon(interiorEdges.ToArray()).Legalize();
		}

		// Overload para usar el mismo margen en ambos ejes
		public Polygon InteriorPolygon(float margin) => InteriorPolygon(new Vector2(margin, margin));

		#endregion



		#region AUTO-INTERSECTIONS

		/// <summary>
		///     Busca y elimina secciones del poligonos invalidas (intersecciona con si mismo)
		/// </summary>
		public Polygon Legalize()
		{
			Polygon autoIntersectedPolygon = AddAutoIntersections(out List<Vector2> intersections);

			// Si no hay intersecciones lo devolvemos tal cual
			if (autoIntersectedPolygon._vertices.Length == _vertices.Length) return this;

			Polygon[] polygons = autoIntersectedPolygon.SplitAutoIntersectedPolygons(intersections);

			// - Filtramos por los poligonos VALIDOS (CCW)
			polygons = polygons.Where(p => p.IsCounterClockwise()).ToArray();
			if (polygons.IsNullOrEmpty()) return new Polygon();


			// - Los unimos en un solo poligono
			if (polygons.Length == 1) return polygons.First();
			var mergedPolygon = new Polygon();

			// Los ordeno segun la distancia con el centroide del 1º poligono
			Vector2 firstCentroid = polygons.First().centroid;
			polygons = polygons.OrderByDescending(p => Vector2.Distance(firstCentroid, p.centroid)).ToArray();

			// UNION
			polygons.ForEach(p => mergedPolygon = mergedPolygon.Merge(p));

			return mergedPolygon;
		}

		/// <summary>
		///     Añade a un polígono que se autointersecta, los puntos de interseccion como vértices
		///     Lo cual dividiria el poligono en subpoligonos que seran algunos CCW y otros CW
		/// </summary>
		public Polygon AddAutoIntersections(out List<Vector2> intersections)
		{
			intersections = new List<Vector2>();
			List<Edge> edgeList = Edges.ToList();

			// - Busca las intersecciones n con n
			// Se busca n a n y se dividen las aristas en caso de interseccion
			// Se repite hasta que no haya intersecciones (por seguridad hasta un maximo de repeticiones)
			var restartCount = 0;
			var maxRestarts = 20;
			for (var i = 0; i < edgeList.Count && restartCount < maxRestarts; i++)
			for (int j = i + 2; j < edgeList.Count && restartCount < maxRestarts; j++)
			{
				if (i == j || (i == 0 && j == edgeList.Count - 1)) continue;
				Edge e1 = edgeList[i];
				Edge e2 = edgeList[j];
				Vector2? intersection = e1.Intersection(e2);

				if (!intersection.HasValue
				    || intersection.Value == e1.begin
				    || intersection.Value == e1.end
				    || intersection.Value == e2.begin
				    || intersection.Value == e2.end)
					continue;

				edgeList.RemoveAt(i);
				edgeList.InsertRange(i, e1.Split(intersection.Value));

				int newJ = j > i ? j + 1 : j;
				edgeList.RemoveAt(newJ);
				edgeList.InsertRange(newJ, e2.Split(intersection.Value));

				intersections.Add(intersection.Value);

				// Repite
				i = -1;
				restartCount++;
				break;
			}

			var autointersectedPoly = new Polygon(edgeList.ToArray());
			autointersectedPoly.RemoveInvalidEdges();
			return autointersectedPoly;
		}

		public Polygon[] SplitAutoIntersectedPolygons(List<Vector2> intersectionVertices)
		{
			// - Separamos el poligono en secciones validas y no validas
			// Iteramos las aristas y en cada interseccion cambiamos de poligono.
			// Para tener dos listas distintas que forman poligonos CCW y CW
			List<Polygon> polygons = new();
			List<Edge> edges1 = new(), edges2 = new();
			List<Edge> current = edges1;
			foreach (Edge edge in Edges)
			{
				// Si el begin es una interseccion, cambiamos de poligono
				if (intersectionVertices.Contains(edge.begin))
					current = current == edges1 ? edges2 : edges1;

				current.Add(edge);
			}

			// Buscamos los bucles en cada lista de aristas. Cada uno es un poligono
			Edge.SearchLoop(edges1.ToArray())?.ForEach(edges => polygons.Add(new Polygon(edges)));
			Edge.SearchLoop(edges2.ToArray())?.ForEach(edges => polygons.Add(new Polygon(edges)));

			return polygons.ToArray();
		}

		#endregion


		#region CONCAVE to CONVEX

		/// <summary>
		///		Algoritmo de Hertel-Mehlhorn:
		///   Divide un poligono concavo en varios poligonos convexos
		///   Recorta un polígono Convexo recurrentemente, hasta que el poligono restante sea convexo
		/// </summary>
		public Polygon[] OptimalConvexDecomposition(int maxSubPolygons = 10)
		{
		    if (IsEmpty || maxSubPolygons < 2) return new[] { this };

		    List<Polygon> convexPolygons = new List<Polygon>();
		    Stack<Polygon> stack = new Stack<Polygon>();
		    stack.Push(this);

		    while (stack.Count > 0 && convexPolygons.Count < maxSubPolygons - 1)
		    {
		        Polygon poly = stack.Pop();
		        if (poly.IsConvex())
		        {
		            convexPolygons.Add(poly);
		        }
		        else
		        {
		            (Polygon convexPolygon, Polygon croppedPolygon) = poly.SplitConvex();
		            convexPolygons.Add(convexPolygon);
		            stack.Push(croppedPolygon);
		        }
		    }

		    return stack.Count > 0 ? convexPolygons.Concat(stack).ToArray() : convexPolygons.ToArray();
		}


		/// <summary>
		///		Busca un Subpolígono Convexo y devuelve el subpolígono restante
		/// </summary>
		/// <returns>(Convexo, Restante)</returns>
		/// <exception cref="InvalidOperationException">No hay ningún subpolígono Convexo: POLIGONO DEGENERADO</exception>
		private (Polygon, Polygon) SplitConvex()
		{
			// Si el Triangulo es CCW, es CONVEXO 
			// Podemos añadir los siguientes vertices hasta que deje de ser CONVEXO
			// O contenga un punto del poligono restante
			// Para buscar el maximo poligono CONVEXO a partir de el vertice i
			for (var i = 0; i < _vertices.Length; i++)
			{
				// El Triangulo [a,b,c] debe ser CCW para ser CONVEXO
				var tri = new Triangle(_vertices[i], _vertices[(i + 1) % VertexCount], _vertices[(i + 2) % VertexCount]);
				if (tri.IsCW() || _vertices.CropOutOfRange((i-1 + VertexCount) % VertexCount, (i+3) % VertexCount).Any(v => tri.Contains_CrossProd(v) || tri.IsOnEdge(v))) continue;
		        
				// Comprobamos si el siguiente vertice j2 hace que el poligono deje de ser CONVEXO:
				// Deben ser CCW el Tri [j, j+1, j+2], el Tri [j+1, j+2, i] y el Tri [j+2, i, i+1]
				// O contenga un punto del restante poligono
				int j = i, j1, j2;
				Polygon convexPolygon;
				bool isConvex, hasAnyPointInside = false;
				do
				{
					j++;
					j1 = (j + 1) % VertexCount;
					j2 = (j + 2) % VertexCount;
					int i1 = (i + 1) % VertexCount;
					
					isConvex =
						new Triangle(_vertices[j], _vertices[j1], _vertices[j2]).IsCCW()
					&& new Triangle(_vertices[j1], _vertices[j2], _vertices[i]).IsCCW()
					&& new Triangle(_vertices[j2], _vertices[i], _vertices[i1]).IsCCW();

					if (isConvex)
					{
						// Checkeamos si hay puntos dentro SOLO si es Convexo
						convexPolygon = new Polygon(_vertices.CropRange(i, j2).ToArray());

						var outOfConvexVertices =
							_vertices.CropOutOfRange((j2 + 1) % VertexCount, (i - 1 + VertexCount) % VertexCount);
						hasAnyPointInside = outOfConvexVertices.Any(v => convexPolygon.Contains_CrossProd(v) || tri.IsOnEdge(v));
					}
					if (!isConvex || hasAnyPointInside) j2--;
				} while ((j2 + 1) % VertexCount != i && isConvex && !hasAnyPointInside);
		        
				convexPolygon = new Polygon(_vertices.CropRange(i, j2).ToArray());
				Polygon croppedPolygon = new Polygon(_vertices.CropOutOfRange(i, j2).ToArray());

				return (convexPolygon, croppedPolygon);
			}
			
		    throw new InvalidOperationException("No convex subpolygon found, polygon might be degenerate.");
		}
		
		#endregion

		#endregion


		#region MERGE

		/// <summary>
		///     Merge 2 polygons into one by the nearest point
		///     TODO Si intersectan no es viable
		/// </summary>
		public Polygon Merge(Polygon other)
		{
			if (other.VertexCount < 3) return this;
			if (VertexCount < 3) return other;

			// Merge polygons siendo a y b los puntos mas cercanos de cada poligono
			// Ordeno los poligonos empezando por a y b y añado a y b repetidos al final
			// Al unirlos se conectan a y b y termina en b, loopeando en a
			(int i1, int i2) = GetNearestPoints_Indices(other);
			return new Polygon(
				_vertices.Skip(i1)
					.Concat(_vertices.Take(i1 + 1)) // a -> a
					.Concat(other._vertices.Skip(i2).Concat(other._vertices.Take(i2 + 1))) // b -> b
					.ToArray()
			);
		}

		#endregion


		#region TESTS

		public bool IsConvex() => _vertices.IsConvex();
		public bool IsConcave() => _vertices.IsConcave();

		/// <summary>
		///     Puntos mas cercanos de dos poligonos que no se intersectan
		/// </summary>
		public (Vector2, Vector2)? GetNearestPoints(Polygon other)
		{
			(int i1, int i2) = GetNearestPoints_Indices(other);
			if (i1 == -1 || i2 == -1) return null;
			return (_vertices[i1], other._vertices[i2]);
		}

		/// <summary>
		///     +Area => CCW
		///     -Area => CW
		/// </summary>
		public bool IsCounterClockwise() => DoubleArea > 0;

		public bool IsClockwise() => DoubleArea < 0;

		// TEST Point is inside Polygon
		// Using Cross Product
		// Only works on CONVEX polygons
		public bool Contains_CrossProd(Vector2 point) =>
			_edges.All(e => GeometryUtils.IsLeft(e.begin, e.end, point));

		// TEST Point is inside Polygon
		// Uses RayCasting
		// It is less efficient but works on CONCAVE polygons
		public bool Contains_RayCast(Vector2 point)
		{
			var contains = false;
			_edges.ForEach(
				e =>
				{
					Vector2 a = e.begin, b = e.end;
					if (b.y > point.y != a.y > point.y && point.x < (a.x - b.x) * (point.y - b.y) / (a.y - b.y) + b.x)
						contains = !contains;
				}
			);
			return contains;
		}
		
		
		/// <summary>
		///     Indices de los vertices mas cercanos de dos poligonos que no se intersectan
		/// </summary>
		public (int, int) GetNearestPoints_Indices(Polygon other)
		{
			int i1 = -1, i2 = -1;
			var minDist = float.MaxValue;
			for (var i = 0; i < _vertices.Length; i++)
			for (var j = 0; j < other._vertices.Length; j++)
			{
				float dist = Vector2.Distance(_vertices[i], other._vertices[j]);
				if (minDist < dist) continue;
				minDist = dist;
				i1 = i;
				i2 = j;
			}

			return (i1, i2);
		}

		#endregion

		
		#region MESH

		public (Triangle[], Polygon[]) Triangulate(int maxSubPolygons = 10)
		{
			if (IsEmpty) return (Array.Empty<Triangle>(), Array.Empty<Polygon>());

			Polygon[] subpolygons = OptimalConvexDecomposition(maxSubPolygons);
			Triangle[] tris = IsConvex()
				? TriangulateConvex()
				: subpolygons.SelectMany(p => p.TriangulateConvex()).ToArray();
			
			return (tris, subpolygons);
		}

		/// <summary>
		///     Triangula creando un triangulo por arista, siendo el centroide el tercer vertice
		///		Solo funciona bien con Convex Polygons
		/// </summary>
		private Triangle[] TriangulateConvex()
		{
			Vector2 c = centroid;
			return IsEmpty
				? Array.Empty<Triangle>()
				: VertexCount == 3 
					? new [] { new Triangle(_vertices) } 
					: Edges.Select(e => new Triangle(e.begin, e.end, c)).ToArray();
		}

		#endregion

		
		#region DEBUG

#if UNITY_EDITOR


		public void DrawGizmosWire(
			Matrix4x4 mTRS, float thickness = 1, Color color = default, bool projectOnTerrain = false
		)
		{
			if (_vertices.IsNullOrEmpty()) return;

			Vector3[] verticesInWorld = mTRS.MultiplyPoint3x4(_vertices).ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygonWire_OnTerrain(verticesInWorld, thickness, color);
			else
				GizmosExtensions.DrawPolygonWire(verticesInWorld, thickness, color);
		}

		public void DrawGizmos(
			Matrix4x4 mTRS, Color color = default, Color? outlineColor = null, bool projectOnTerrain = false
		)
		{
			if (_vertices.IsNullOrEmpty()) return;

			Vector3[] verticesInWorld = mTRS.MultiplyPoint3x4(_vertices).ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygon_OnTerrain(verticesInWorld, color, outlineColor ?? color);
			else
				GizmosExtensions.DrawPolygon(verticesInWorld, color, outlineColor ?? color);
		}

		public void DrawGizmosVertices(Matrix4x4 localToWorldMatrix, Color color = default, float radius = .1f)
		{
			Gizmos.color = color;
			_vertices.ForEach(
				v =>
					Gizmos.DrawSphere(
						localToWorldMatrix.MultiplyPoint3x4(v),
						radius * localToWorldMatrix.lossyScale.magnitude
					)
			);
		}

		public void DrawGizmosVertices_CheckAABBborder(Matrix4x4 localToWorldMatrix, float radius = .1f) =>
			_vertices.ForEach(
				v =>
				{
					bool inBorder = AABB_2D.NormalizedAABB.PointOnBorder(v, out _);
					Vector3 worldPoint = localToWorldMatrix.MultiplyPoint3x4(v);
					Gizmos.color = inBorder ? Color.red : Color.green;
					Gizmos.DrawSphere(worldPoint, radius);
				}
			);

#endif

		#endregion

		
		public bool Equals(Polygon other) => Equals(_vertices, other._vertices) && centroid.Equals(other.centroid);

		public override bool Equals(object obj) => obj is Polygon other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(_vertices, centroid);

		public override string ToString() =>
			$"{VertexCount} vertices: {string.Join(", ", _vertices.Take(10))} {(VertexCount > 10 ? "..." : "")}\n" +
			$"Centroid: {centroid}";

		public Texture2D ToTexture(Vector2 texSize)
		{
			AABB_2D	aabb = new(_vertices);
			Vector2 aabbSize = new Vector2(aabb.Size.x, aabb.Size.y);
			Color[] pixels = new Color[Mathf.CeilToInt(texSize.x) * Mathf.CeilToInt(texSize.y)];
			for (var y = 0; y < Mathf.CeilToInt(texSize.y); y++)
			for (var x = 0; x < Mathf.CeilToInt(texSize.x); x++)
			{
				float xt = x / texSize.y, yt = y / texSize.y;
				Vector2 point = new Vector2(xt * aabbSize.x, yt * aabbSize.y) + aabb.min;
				pixels[y * Mathf.CeilToInt(texSize.x) + x] = Contains_RayCast(point) ? Color.white : Color.black;
			}
			
			var texture = new Texture2D(Mathf.CeilToInt(texSize.x),Mathf.CeilToInt(texSize.y));
			texture.SetPixels(pixels);
			texture.Apply();
			
			return texture;
		}
	}
}
