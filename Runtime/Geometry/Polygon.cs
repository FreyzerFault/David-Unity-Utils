using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry
{
	[Serializable]
	public class Polygon : IEquatable<Polygon>
	{
		// Vertices in Counter-Clockwise order
		[SerializeField]
		private Vector2[] vertices;
		public Vector2 centroid;

		[SerializeField] private AABB_2D aabb;
		public Vector2 Min => aabb.max;
		public Vector2 Max => aabb.min;
		public Vector2 Size => aabb.Size;
		
		public AABB_2D AABB
		{
			get => aabb;
			set
			{
				vertices = NormalizeVertices();
				// TODO To new AABB
				aabb = value;
			}
		}

		// Empty or 1 single vertex
		public bool IsEmpty => vertices.IsNullOrEmpty();

		public Vector2[] Vertices
		{
			get => vertices ?? Array.Empty<Vector2>();
			set
			{
				if (vertices == value) return;
				vertices = value;
				OnUpdateVertices();
			}
		}
		public int VertexCount => vertices?.Length ?? 0;
		
		public Vector2[] VerticesFromCentroid
		{
			get
			{
				Vector2 c = centroid;
				return vertices?.Select(v => v - c).ToArray();
			}
		}

		private void OnUpdateVertices()
		{
			_edges = VerticesToEdges(vertices);
			centroid = vertices.Center();
			aabb = new AABB_2D(vertices);
		}
		

		#region EDGES

		[HideInInspector]
		[SerializeField]
		private Edge[] _edges;
		public Edge[] Edges
		{
			get => _edges;
			set
			{
				if (_edges == value) return;
				_edges = value;
				vertices = EdgesToVertices(value);
				centroid = vertices.Center();
				aabb = new AABB_2D(vertices);
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

		private static Edge[] VerticesToEdges(IEnumerable<Vector2> verts) =>
			verts.IsNullOrEmpty()
				? Array.Empty<Edge>()
				: verts.IterateByPairs_InLoop((a, b) => new Edge(a, b), false).ToArray();

		private static Vector2[] EdgesToVertices(Edge[] edges) =>
			edges.IsNullOrEmpty()
				? Array.Empty<Vector2>()
				: edges.Select(e => e.begin).ToArray();

		#endregion

		public Polygon() : this(Array.Empty<Vector2>()) { }

		public Polygon(Vector2[] vertices, Edge[] edges, Vector2 centroid, bool cleanInvalidEdges = true)
		{
			this.vertices = vertices;
			_edges = edges;
			this.centroid = centroid;
			aabb = new AABB_2D(vertices);
			
			if (cleanInvalidEdges)
				RemoveInvalidEdges();
		}

		public Polygon(Vector2[] vertices, bool cleanInvalidEdges = true) 
			: this(vertices, VerticesToEdges(vertices), vertices.Center(), cleanInvalidEdges)
		{ }

		public Polygon(Vector2[] vertices, Edge[] edges, bool cleanInvalidEdges = true) 
			: this(vertices, edges, vertices.Center(), cleanInvalidEdges)
		{
		}

		public Polygon(Edge[] edges, bool cleanInvalidEdges = true) 
			: this(EdgesToVertices(edges), edges, cleanInvalidEdges)
		{
		}

		public void Clear() => Vertices = Array.Empty<Vector2>();

		#region SECONDARY PROPERTIES

		/// <summary>
		///     Calcula el area como la suma de areas de los triangulos formados por el centroide y cada arista
		///     Pero la formula simplificada es
		///     1 / 2 * SUM (Xi * Yi+1 - Xi+1 * Yi)
		/// </summary>
		public float Area => .5f * DoubleArea;

		// No 1/2 para cuando necesito solo el signo
		public float DoubleArea => vertices
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
			Vertices = vertices.Distinct().ToArray();

		/// <summary>
		///     Remove all Invalid Edges till ALL are VALID
		///		(Collinear, that generates Area 0)
		///		and lenght 0 edges (begin == end)
		/// </summary>
		/// <returns>
		///		TRUE	=> edges removed
		///		FALSE	=> no invalid edges found
		/// </returns>
		public bool RemoveInvalidEdges()
		{
			var validEdges = _edges.RemoveInvalidEdges();
			bool edgesRemoved = validEdges.Length != _edges.Length;
			Edges = validEdges;
			return edgesRemoved;
		}

		#endregion


		#region VERTEX TRANSFORMATION

		public Polygon Normalize() => NormalizeMinMax(Min, Max);

		public Polygon NormalizeMinMax(Vector2 min, Vector2 max) => 
			new(NormalizeVerticesMinMax(min, max));
		
		public Vector2[] NormalizeVertices() => NormalizeVerticesMinMax(Min, Max);
		
		public Vector2[] NormalizeVerticesMinMax(Vector2 min, Vector2 max) =>
			vertices.Select(v => v - min / (max - min)).ToArray();

		public Polygon Revert() => new(vertices.Reverse().ToArray());

		public Polygon SortCCW() => new(vertices.SortByAngle(centroid).ToArray());

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
			if (IsEmpty) return this;
			
			// Genero las Aristas paralelas a la distancia dada
			List<Edge> parallels = _edges.Select(e => e.ParallelLeft(new Vector2(margin2D.x, margin2D.y))).ToList();

			// Genero las intersecciones de las aristas paralelas
			Vector2[] intersections = parallels.IterateByPairs_InLoop(
					(e1, e2) =>
					{
						bool intersected = e1.Intersection_LineLine(e2, out Vector2 intersection);
						if (!intersected) Debug.Log("No intersection found");
						return intersected ? intersection : e2.begin;
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

				// Puede que fueran colineales, lo cual no deberia pasar, pero por si acaso, ignoramos este caso
				if (!prev.Intersection_LineLine(next, out Vector2 intersection))
					continue;

				// NO ES VALIDA => La eliminamos
				interiorEdges.RemoveAt(i);
				parallels.RemoveAt(i);

				// Sustituimos sus adyacentes conectandolos con la interseccion
				// Con cuidado si estan en los extremos
				interiorEdges[i == interiorEdges.Count ? 0 : i].begin = intersection;
				interiorEdges[i == 0 ? ^1 : i - 1].end = intersection;

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
			if (autoIntersectedPolygon.vertices.Length == vertices.Length) return this;

			Polygon[] polygons = autoIntersectedPolygon.SplitAutoIntersectedPolygons(intersections);

			// - Filtramos por los poligonos VALIDOS (CCW)
			polygons = polygons.Where(p => p.IsCounterClockwise()).ToArray();
			if (polygons.IsNullOrEmpty()) return new Polygon();


			// - Los unimos en un solo poligono
			if (polygons.Length == 1) return polygons.First();
			Polygon mergedPolygon = new();

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
		/// <param name="simpleCheck">
		///		To check if there's a single intersection, for optimal testing purposes
		/// </param>
		public Polygon AddAutoIntersections(out List<Vector2> intersections, bool simpleCheck = false)
		{
			intersections = new List<Vector2>();
			List<Edge> edgeList = Edges.ToList();

			//	Busca las intersecciones n con n (edges[i] con edges[j])
			//	Se evita comparar aristas adyacentes ya que no deberían intersectarse
			//	Las aristas que intersecan se dividen en 4
			//	Se repite con la arista i hasta que no haya intersecciones
			//	(Si encuentra una intersección i con j, i se repite con las siguientes aristas)
			//	(hasta un máximo de repeticiones con la misma arista i, por seguridad)
			
			const int maxRestarts = 20;
			var restartCount = 0;
			for (var i = 0; i < edgeList.Count && restartCount < maxRestarts; i++)
			for (int j = i + 2; j < edgeList.Count && restartCount < maxRestarts; j++)
			{
				// No need to compare with the previous edge in the case i = 0 and j = n-1
				if (i == 0 && j == edgeList.Count - 1) continue;
				
				Edge e1 = edgeList[i];
				Edge e2 = edgeList[j];
				
				bool intersect = e1.Intersection(e2, out Vector2 intersection);
				
				// Invalid intersection => Intersection is an existing vertex
				bool intersectInVertex =
					intersect
					&& (intersection == e1.begin
					    || intersection == e1.end
					    || intersection == e2.begin
					    || intersection == e2.end);

				// No Intersection or Vertex Intersection => Check Next Edge Pair 
				if (!intersect || intersectInVertex)
					continue;

				if (simpleCheck)
				{
					intersections.Add(intersection);
					return this;
				}
					
				// Elimina ambos segmentos y los sustituye por los segmentos divididos en el punto de intersección
				edgeList.RemoveAt(i);
				edgeList.InsertRange(i, e1.Split(intersection));

				// Adjust index j after insertion
				int newJ = j > i ? j + 1 : j;
				edgeList.RemoveAt(newJ);
				edgeList.InsertRange(newJ, e2.Split(intersection));

				// Save intersection for later use
				intersections.Add(intersection);

				// Repeat edge i with next edge j till max restarts reached or all edges j checked
				i = -1;
				restartCount++;
				break;
			}

			// Build the new polygon with the new edges
			Polygon autointersectedPoly = new(edgeList.ToArray());
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

			if (polygons.Count == 0) polygons.Add(this);
			
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
		    if (IsEmpty || maxSubPolygons < 2 || IsConvex()) return Array.Empty<Polygon>();

		    List<Polygon> convexPolygons = new List<Polygon>();
		    Stack<Polygon> stack = new Stack<Polygon>();
		    stack.Push(this);

		    var iterations = 0;
		    var maxIterations = 1000;

		    while (stack.Count > 0 && convexPolygons.Count < maxSubPolygons - 1 && iterations++ < maxIterations)
		    {
		        Polygon poly = stack.Pop();
		        if (poly == null) continue;
		        if (poly.IsConvex())
		        {
		            convexPolygons.Add(poly);
		        }
		        else
		        {
		            (Polygon convexPolygon, Polygon croppedPolygon) = poly.SplitConvex();
		            
		            // CONVEX POLYGON = null si no se puede subdividir
		            if (convexPolygon == null) 
			            convexPolygons.Add(croppedPolygon);
		            else 
		            {
			            // Si se subdivide guardamos el Convexo y realimentamos el bucle con el restante
			            convexPolygons.Add(convexPolygon);
			            stack.Push(croppedPolygon);
		            }
		        }
		    }

		    return stack.Count > 0 ? convexPolygons.Concat(stack).ToArray() : convexPolygons.ToArray();
		}

		struct PointTest
		{
			public bool onEdge;
			public bool insideTri;
			public bool edge1, edge2, edge3;
			public int a, b, c;
			public int pointIn;

			
			
			public override string ToString()
			{
				string edgesTest = $"{(edge1 ? "<color=red>1</color>" : "\u2714\ufe0f")} | " +
				                   $"{(edge2 ? "<color=red>2</color>" : "\u2714\ufe0f")} | " +
				                   $"{(edge3 ? "<color=red>3</color>" : "\u2714\ufe0f")}";
				return $"Vertex {pointIn} on Tri [{a}, {b}, {c}] => " +
				       $"{(insideTri ? "<color=red>INSIDE Triangle</color>" : "\u2714\ufe0f")} | " +
				       $"{(onEdge ? "<color=red>ON Edge</color>" : "\u2714\ufe0f")}\n" +
				       $"{(onEdge ? edgesTest : "")}";
			}
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
			for (var i = 0; i < vertices.Length; i++)
			{
				int a = i, b = (i + 1) % VertexCount, c = (i + 2) % VertexCount;
				
				// El Triangulo [a,b,c] debe ser CCW para ser CONVEXO
				var tri = new Triangle(vertices[a], vertices[b], vertices[c]);
				
				List<Vector2> outOfConvexPolyVertices =
					c > i
						? vertices.Skip(c + 1).Concat(vertices.Take(a)).ToList()
						: vertices.Take(a).Skip(c + 1).ToList();

				bool hasPointsOnTri = outOfConvexPolyVertices.Any(v => 
					tri.Contains_CrossProd(v) || tri.IsPointOnEdge(v)
				);

				
				if (tri.IsCW() || hasPointsOnTri) continue;
		        
				List<Vector2> convexPolyVertices = new List<Vector2>(tri.Vertices);
				
				// Comprobamos si el siguiente vertice c hace que el poligono deje de ser CONVEXO:
				// Deben ser CCW el Tri [a,b,c], el Tri [b,c,i] y el Tri [c,i,i+1]
				// O contenga un punto del restante poligono
				bool isConcave, hasAnyPointInside = false;
				do
				{
					a++;
					b++;
					c++;
					a %= VertexCount;
					b %= VertexCount;
					c %= VertexCount;
					
					isConcave =
						GeometryUtils.IsRight(vertices[a], vertices[b], vertices[c])
						|| GeometryUtils.IsRight(vertices[b], vertices[c], vertices[i])
						|| GeometryUtils.IsRight(vertices[c], vertices[i], vertices[(i + 1) % VertexCount]);

					// Checkeamos si hay puntos dentro del nuevo Polygon SOLO si es Convexo
					if (!isConcave)
					{
						// Cogemos el siguiente punto de fuera del Poligono Convexo
						convexPolyVertices.Add(outOfConvexPolyVertices.First());
						outOfConvexPolyVertices.RemoveAt(0);

						// Basta con comprobar si hay punto solo en la region nueva
						// que se añade al poligono al añadir el vertice c
						// Esta region es el Triangulo [b,c,i]
						var newTri = new Triangle(vertices[b], vertices[c], vertices[i]);
						
						// Si hay algun punto dentro o colinear con un Eje, no es Válido
						hasAnyPointInside = outOfConvexPolyVertices.Any(v =>
							newTri.Contains_CrossProd(v) || newTri.IsPointOnEdge(v));
						
						if (hasAnyPointInside)
						{
							// Devuelve el punto al poligono original
							outOfConvexPolyVertices.Insert(0, convexPolyVertices.Last());
							convexPolyVertices.RemoveAt(convexPolyVertices.Count - 1);
						}
					}

					if (isConcave || hasAnyPointInside)
					{
						// Si deja de ser Convexo o tiene vertices dentro, deshacemos el desplazamiento
						a--;
						b--;
						c--;
						a = a < 0 ? VertexCount - 1 : a;
						b = b < 0 ? VertexCount - 1 : b;
						c = c < 0 ? VertexCount - 1 : c;
					}
				} while ((c + 1) % VertexCount != i && !isConcave && !hasAnyPointInside);
		        
				outOfConvexPolyVertices.InsertRange(0, new []{vertices[i], vertices[c]});
				var croppedPolygon = new Polygon(outOfConvexPolyVertices.ToArray(), false);
				
				var convexPolygon = new Polygon(convexPolyVertices.ToArray(), false);
				
				return (convexPolygon, croppedPolygon);
			}
			
			Debug.LogWarning("No convex subpolygon found, polygon might be degenerate.\n" +
			               $"{VertexCount} vertices\n" +
			               $"{this}");
			return (null, this);
		}

		/// <summary>
		/// DEBUG Point On Triangle Tests to search for BUGS
		/// </summary>
		private (Polygon, Polygon) SplitConvexDebug()
		{
			
			// Si el Triangulo es CCW, es CONVEXO 
			// Podemos añadir los siguientes vertices hasta que deje de ser CONVEXO
			// O contenga un punto del poligono restante
			// Para buscar el maximo poligono CONVEXO a partir de el vertice i
			for (var i = 0; i < vertices.Length; i++)
			{
				int a = i, b = (i + 1) % VertexCount, c = (i + 2) % VertexCount;
				
				// El Triangulo [a,b,c] debe ser CCW para ser CONVEXO
				var tri = new Triangle(vertices[a], vertices[b], vertices[c]);
				List<Vector2> outOfConvexPolyVertices =
					c > i
						? vertices.Skip(c + 1).Concat(vertices.Take(a)).ToList()
						: vertices.Take(a).Skip(c + 1).ToList();

				bool hasPointsOnTri = outOfConvexPolyVertices.Any(v => tri.Contains_CrossProd(v) || tri.IsPointOnEdge(v));
				bool hasPointsOnEdge = outOfConvexPolyVertices.Any(v => tri.IsPointOnEdge(v));
				bool hasPointsInsideTri = outOfConvexPolyVertices.Any(v => tri.Contains_CrossProd(v));
				
				PointTest[] hasPointsOnTriTests = outOfConvexPolyVertices.Select((v, index) => 
					new PointTest
						{
							insideTri = tri.Contains_CrossProd(v),
							onEdge = tri.IsPointOnEdge(v),
							edge1 = GeometryUtils.PointOnSegment(v, tri.e1.begin, tri.e1.end),
							edge2 = GeometryUtils.PointOnSegment(v, tri.e2.begin, tri.e2.end),
							edge3 = GeometryUtils.PointOnSegment(v, tri.e3.begin, tri.e3.end),
							a = a,
							b = b,
							c = c,
							pointIn = index
						}).Where(t => t.insideTri || t.onEdge).ToArray();
				
				if (tri.IsCCW())
					hasPointsOnTriTests.ForEach(t => Debug.Log("CONVEX TRI: " + t));
				
				
				if (tri.IsCW() || hasPointsOnTri) continue;
		        
				List<Vector2> convexPolyVertices = new List<Vector2>(tri.Vertices);
				
				// Comprobamos si el siguiente vertice c hace que el poligono deje de ser CONVEXO:
				// Deben ser CCW el Tri [a,b,c], el Tri [b,c,i] y el Tri [c,i,i+1]
				// O contenga un punto del restante poligono
				bool isConcave, hasAnyPointInside = false;
				do
				{
					a++;
					b++;
					c++;
					a %= VertexCount;
					b %= VertexCount;
					c %= VertexCount;
					
					isConcave =
						GeometryUtils.IsRight(vertices[a], vertices[b], vertices[c])
						|| GeometryUtils.IsRight(vertices[b], vertices[c], vertices[i])
						|| GeometryUtils.IsRight(vertices[c], vertices[i], vertices[(i + 1) % VertexCount]);

					// Checkeamos si hay puntos dentro del nuevo Polygon SOLO si es Convexo
					if (!isConcave)
					{
						// Cogemos el siguiente punto de fuera del Poligono Convexo
						convexPolyVertices.Add(outOfConvexPolyVertices[0]);
						outOfConvexPolyVertices.RemoveAt(0);

						// Basta con comprobar si hay punto solo en la region nueva
						// que se añade al poligono al añadir el vertice c
						// Esta region es el Triangulo [b,c,i]
						var newTri = new Triangle(vertices[b], vertices[c], vertices[i]);
						
						// Si hay algun punto dentro o colinear con un Eje, no es Válido
						hasAnyPointInside = outOfConvexPolyVertices.Any(v =>
							newTri.Contains_CrossProd(v) || newTri.IsPointOnEdge(v));
						
						if (hasAnyPointInside)
						{
							// Devuelve el punto al poligono original
							outOfConvexPolyVertices.Insert(0, convexPolyVertices.Last());
							convexPolyVertices.RemoveAt(convexPolyVertices.Count - 1);
						}
					}

					if (isConcave || hasAnyPointInside)
					{
						// Si deja de ser Convexo o tiene vertices dentro, deshacemos el desplazamiento
						a--;
						b--;
						c--;
						a = a < 0 ? VertexCount - 1 : a;
						b = b < 0 ? VertexCount - 1 : b;
						c = c < 0 ? VertexCount - 1 : c;
					}
				} while ((c + 1) % VertexCount != i && !isConcave && !hasAnyPointInside);
		        
				outOfConvexPolyVertices.InsertRange(0, new []{vertices[i], vertices[c]});
				var croppedPolygon = new Polygon(outOfConvexPolyVertices.ToArray(), false);
				
				var convexPolygon = new Polygon(convexPolyVertices.ToArray(), false);
				
				Debug.Log($"Polygon splited from {i} to {c} => {vertices[i]} to {vertices[c]}\n" +
				          $"Convex Poly: {convexPolygon}\n" +
				          $"Polygon Restante: {croppedPolygon}");

				return (convexPolygon, croppedPolygon);
			}
			
			Debug.LogWarning("No convex subpolygon found, polygon might be degenerate.\n" +
			               $"{VertexCount} vertices\n" +
			               $"{this}");
			return (null, this);
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
				vertices.Skip(i1)
					.Concat(vertices.Take(i1 + 1)) // a -> a
					.Concat(other.vertices.Skip(i2).Concat(other.vertices.Take(i2 + 1))) // b -> b
					.ToArray()
			);
		}

		#endregion


		#region INTERSECTIONS

		/// <summary>
		///		Intersections with a Line or Segment. 
		///		If Line use edgeIsAInfiniteLine = true
		/// </summary>
		public bool Intersects(Edge edge, out Vector2[] intersections, bool edgeIsAInfiniteLine = false)
		{
			bool isConvex = IsConvex();
			
			List<Vector2> intersList = new List<Vector2>();
			foreach (Edge polyEdge in _edges)
			{
				if (edgeIsAInfiniteLine 
					    ? !edge.Intersection_LineSegment(polyEdge, out Vector2 intersection)
					    : !edge.Intersection(polyEdge, out intersection)) continue;
				
				// Intersection Found
				intersList.Add(intersection);
				
				// CONVEX Polygon always has 2 intersections on lines
				if (isConvex && intersList.Count == 2)
					break;
			}

			intersections = intersList.ToArray();
			return intersections.NotNullOrEmpty();
		}

		#endregion
		

		#region TESTS
		
		public bool IsValid() => vertices.NotNullOrEmpty() || vertices.Length > 2;
		
		public bool HasAutoIntersections()
		{
			AddAutoIntersections(out List<Vector2> intersections, true);
			return intersections.NotNullOrEmpty();
		}

		public bool IsDegenerate() => 
			vertices.Distinct() != vertices 
			|| _edges.IterateByPairs_InLoop(
				(e1, e2) => Vector2.Distance(e1.begin, e2.end) < Edge.VertexEpsilon
			).Any(b => b);

		public bool IsConvex() => vertices.IsConvex();
		public bool IsConcave() => vertices.IsConcave();

		/// <summary>
		///     Puntos mas cercanos de dos poligonos que no se intersectan
		/// </summary>
		public (Vector2, Vector2)? GetNearestPoints(Polygon other)
		{
			(int i1, int i2) = GetNearestPoints_Indices(other);
			if (i1 == -1 || i2 == -1) return null;
			return (vertices[i1], other.vertices[i2]);
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
		
		public bool IsPointOnEdge(Vector2 point) =>
			_edges.Any(e => GeometryUtils.CollinearPointInLine(e.begin, e.end, point));
		
		
		/// <summary>
		///     Indices de los vertices mas cercanos de dos poligonos que no se intersectan
		/// </summary>
		public (int, int) GetNearestPoints_Indices(Polygon other)
		{
			int i1 = -1, i2 = -1;
			var minDist = float.MaxValue;
			for (var i = 0; i < vertices.Length; i++)
			for (var j = 0; j < other.vertices.Length; j++)
			{
				float dist = Vector2.Distance(vertices[i], other.vertices[j]);
				if (minDist < dist) continue;
				minDist = dist;
				i1 = i;
				i2 = j;
			}

			return (i1, i2);
		}

		#endregion

		
		#region MESH

		public (Triangle[], Polygon[]) TriangulateConcave(int maxSubPolygons = 10)
		{
			if (IsEmpty) return (Array.Empty<Triangle>(), Array.Empty<Polygon>());

			Polygon[] subpolygons = OptimalConvexDecomposition(maxSubPolygons);
			Triangle[] tris = subpolygons.IsNullOrEmpty()
				? TriangulateConvex()
				: subpolygons.SelectMany(p => p == null ? Array.Empty<Triangle>() : p.TriangulateConvex()).ToArray();
			
			return (tris, subpolygons);
		}

		/// <summary>
		///     Triangula creando un triangulo por arista, siendo el centroide el tercer vertice
		///		Solo funciona bien con Convex Polygons
		/// </summary>
		public Triangle[] TriangulateConvex()
		{
			Vector2 c = centroid;
			return IsEmpty
				? Array.Empty<Triangle>()
				: VertexCount == 3 
					? new [] { new Triangle(vertices) } 
					: Edges.Select(e => new Triangle(e.begin, e.end, c)).ToArray();
		}

		#endregion

		
		#region DEBUG

#if UNITY_EDITOR

		public void DrawGizmosWire(
			Matrix4x4 mTRS, float thickness = 1, Color color = default, bool projectOnTerrain = false
		)
		{
			if (vertices.IsNullOrEmpty()) return;

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
			if (vertices.IsNullOrEmpty()) return;

			Vector3[] verticesInWorld = mTRS.MultiplyPoint3x4(vertices).ToArray();

			if (projectOnTerrain)
				GizmosExtensions.DrawPolygon_OnTerrain(verticesInWorld, color, outlineColor ?? color);
			else
				GizmosExtensions.DrawPolygon(verticesInWorld, color, outlineColor ?? color);
		}

		public void DrawGizmosVertices(Matrix4x4 localToWorldMatrix, Color color = default, float radius = .1f)
		{
			Gizmos.color = color;
			vertices.ForEach(
				v =>
					Gizmos.DrawSphere(
						localToWorldMatrix.MultiplyPoint3x4(v),
						radius * localToWorldMatrix.lossyScale.magnitude
					)
			);
		}

		public void DrawGizmosVertices_CheckAABBborder(Matrix4x4 localToWorldMatrix, float radius = .1f) =>
			vertices.ForEach(
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

		
		#region PROCEDURAL GENERATION
		
		/// <summary>
		///		Generate Random Vertices.
		/// </summary>
		/// <param name="autointersected">
		/// FALSE => Polygon will be sorted by CCW.
		/// </param>
		public void SetRandomVertices(int numVertices, float radius = 10, bool convex = false, bool autointersected = false)
		{
			if (numVertices < 3) 
				Debug.LogError("Random Polygon must have at least 3 vertices");

			List<Vector2> verticesList = new List<Vector2>();
			
			for (var i = 0; i < numVertices; i++)
			{
				if (convex)
					verticesList.Add(Random.insideUnitCircle.normalized * radius);
				else
					verticesList.Add(Random.insideUnitCircle * radius);
			}

			vertices = verticesList.ToArray();
			
			OnUpdateVertices();
			
			// Sort by Angle to avoid autointersections
			if (!autointersected)
				Vertices = vertices.SortByAngle(vertices.Center());
		}

		#endregion

		
		#region TEXTURE
		
		/// <summary>
		/// 	Render on Texture with same dimensions as Polygon AABB
		/// 	You can reproject the Polygon to the Texture dimension you want before this
		/// 	Or use ToTexture(Vector2 texSize) to render on a texture with a different size
		/// </summary>
		public Texture2D ToTexture(Color fillColor = default, Color backgroundColor = default
			, bool transparent = false, bool optimizationTest = true)
			=> ToTexture_ScanlineRaster(Vector2Int.CeilToInt(AABB.Size), fillColor, backgroundColor, transparent, optimizationTest);

		/// <summary>
		/// 	Render on Texture with dimension = texSize.
		/// 	To test if a point is INSIDE => Raycast Test.
		/// 	If pixel is contained, it is painted with fillColor, otherwise with backgroundColor
		/// </summary>
		public Texture2D ToTexture_ContainsRaycastPerPixel(Vector2Int texSize, Color fillColor = default, Color backgroundColor = default
			, bool transparent = false, bool optimizationTest = true, bool debug = false)
		{
			if (transparent) backgroundColor = Color.clear; // Transparent background
			if (fillColor == backgroundColor) fillColor = backgroundColor.Invert(); // Invert fill if same color
			
			// AABB resized to match the texture aspect ratio
			AABB_2D aabbUpscaled = new(aabb.min, aabb.max); 
			aabbUpscaled.UpscaleToMatchAspectRatio(texSize);

			bool matchAABBWithTextureSize = Vector2Int.CeilToInt(aabbUpscaled.Size) == texSize;
			
			Color[] pixels = new Color[texSize.x * texSize.y];
			for (var y = 0; y < texSize.y; y++)
			{
				for (var x = 0; x < texSize.x; x++)
				{
					// X to Polygon World Space
					float worldX = matchAABBWithTextureSize ? x : x / (float)texSize.x * aabbUpscaled.Width + aabbUpscaled.min.x;
					float worldY = matchAABBWithTextureSize ? y : y / (float)texSize.y * aabbUpscaled.Height + aabbUpscaled.min.y;
			
					// Set Fill Color if X between ANY of the inters. pairs
					pixels[y * texSize.x + x] = Contains_RayCast(new Vector2(worldX, worldY))
							? fillColor
							: backgroundColor;
				}
				
			}
			
			Texture2D texture =
				transparent 
					? new Texture2D(texSize.x,texSize.y, TextureFormat.RGBA32, false) 
					: new Texture2D(texSize.x,texSize.y);
			
			texture.SetPixels(pixels);
			texture.Apply();
			
			return texture;
		}

		// When using the scanline algorithm, if UseScalineBreakpointsGeneration is TRUE:
		// generate line pixels by the breakpoints, not iterating pixel by pixel 
		private const bool UseScalineBreakpointsGeneration = false;

		public Dictionary<float, Vector2[]> intersectionsByScanline = new(); 
		
		/// <summary>
		/// 	Render on Texture with dimension = texSize.
		/// 	To test if a point is INSIDE => Scanline Algorithm.
		///		In each height, it creates a SCANLINE from left to right,
		///		check intersections with all edges (stop when 2 inters. found if CONVEX),
		///		sort them by X and group by pairs to check if a pixel fall between a pair.
		/// 	If it is, it is painted with fillColor, otherwise with backgroundColor
		/// </summary>
		public Texture2D ToTexture_ScanlineRaster(Vector2Int texSize, Color fillColor = default, Color backgroundColor = default
			, bool transparent = false, bool debugInfo = false)
		{
			if (transparent) backgroundColor = Color.clear; // Transparent background
			if (fillColor == backgroundColor) fillColor = backgroundColor.Invert(); // Invert fill if same color
			
			// AABB resized to match the texture aspect ratio
			AABB_2D aabbUpscaled = new(aabb.min, aabb.max); 
			aabbUpscaled.UpscaleToMatchAspectRatio(texSize);

			bool matchAABBWithTextureSize = Vector2Int.CeilToInt(aabbUpscaled.Size) == texSize;
			
			Color[] pixels = UseScalineBreakpointsGeneration 
				? Array.Empty<Color>()
				: backgroundColor.ToFilledArray(texSize.x * texSize.y).ToArray();
			
			for (var y = 0; y < texSize.y; y++)
			{
				// Use directly Y if AABB match the Texture Size
				float scanHeight = matchAABBWithTextureSize ? y : y / (float)texSize.y * aabbUpscaled.Height + aabbUpscaled.min.y;
				
				// Scanline from left to right
				Edge scanLine = new(new Vector2(0, scanHeight), new Vector2(1, scanHeight)); // Don't need X
				bool intersects = Intersects(scanLine, out Vector2[] intersections, true);
				
				// Delete repeated intersections
				intersections = intersections.Distinct().ToArray();
				
				if (!intersects) // No Intersections
				{
					if (UseScalineBreakpointsGeneration) // Fill the scanline
						pixels = pixels.Concat(backgroundColor.ToFilledArray(texSize.x)).ToArray();
					
					continue;
				}
				
				// 1 Intersection (Pointy Vertex)
				if (intersections.Length == 1) // 1 Intersection => Add 1 pixel
				{
					if (debugInfo)
						Debug.LogWarning("Polygon-Line Intersection: single intersection, unexpected behavior");
					
					if (UseScalineBreakpointsGeneration) // Fill the scanline first
						pixels = pixels.Concat(backgroundColor.ToFilledArray(texSize.x)).ToArray();
					
					Vector2Int texPoint = Vector2Int.RoundToInt(aabbUpscaled.Normalize(intersections[0]) * texSize);
					pixels[texPoint.y * texSize.x + texPoint.x] = fillColor;
					
					continue;
				}
				
				// ODD Intersections (Collinear edges) => Ignore intersections
				// that are the begin or end of Colinear Edges if before begin or after end, respectively 
				// enters inside the Polygon (the middle point with the previous or next Intersection is INSIDE the Polygon)
				if (intersections.Length % 2 == 1)
				{
					Edge[] collinearEdges = Edges.Where(scanLine.Collinear).ToArray();
					if (collinearEdges.NotNullOrEmpty())
					{
						// Sort by X
						intersections = intersections.OrderBy(v => v.x).ToArray();

						foreach (Edge collinearEdge in collinearEdges)
						{
							Vector2 begin = collinearEdge.begin;
							Vector2 end = collinearEdge.end;
							
							int beginIndex = intersections.FirstIndex(v => Vector2.Distance(v, begin) < 0.001f);
							int endIndex = intersections.FirstIndex(v => Vector2.Distance(v, end) < 0.001f);
							
							int prevIndex = beginIndex - 1;
							int nextIndex = endIndex + 1;

							Vector2 middlePrevPoint = (intersections[prevIndex] + begin) / 2;
							Vector2 middleNextPoint = (intersections[nextIndex] + end) / 2;
							
							// If the Middle Point (from Begin to Previous Inters. and from End to Next Inters.)
							// is inside the Polygon => Ignore Colinear Vertex
							bool canIgnoreBegin = prevIndex >= 0 && Contains_RayCast(middlePrevPoint);
							bool canIgnoreEnd = nextIndex < intersections.Length && Contains_RayCast(middleNextPoint);
							
							if (!canIgnoreBegin && !canIgnoreEnd) continue;
							
							// Remove Begin and/or End
							List<Vector2> intersList = intersections.ToList();
							intersList.RemoveRange(
								canIgnoreBegin ? beginIndex : endIndex,
								canIgnoreBegin && canIgnoreEnd ? 2 : 1);
							intersections = intersList.ToArray();
						}
					}
					else
					{
						Debug.LogError($"Polygon-Line Intersection: Odd number ({intersections.Length})" +
						               $" of intersections while NO Collinear edge (horizontal) at Height {scanHeight}");
					}
				}
				
				// It can't have ODD Intersections.
				// Intersections on a Vertex must count as 2 because both adyacent edges may count as intersected 
				if (intersections.Length % 2 == 1) 
					Debug.LogError("Polygon-Line Intersection: Odd number of intersections, unexpected behavior");
				
				// Sort by X
				intersections = intersections.OrderBy(v => v.x).ToArray();
				
				// Save Intersections for Debug Info
				intersectionsByScanline.TryAdd(scanHeight, intersections);

				if (UseScalineBreakpointsGeneration)
				{
					// Pixels that acts as breakpoints for the color + first and last pixel
					int[] xIndexBreakpoints = GetScanlineBreakpoints(texSize, aabbUpscaled, intersections, matchAABBWithTextureSize);
					
					// Iterate from breakpoint to breakpoint alternating colors
					List<Color> pixelLine = GeneratePixelLine(xIndexBreakpoints, fillColor, backgroundColor);
					pixels = pixels.Concat(pixelLine).ToArray();

					if (!debugInfo) continue;
					
					// DEBUG INFO
					string breakpoints = string.Join(',', xIndexBreakpoints.Select(i => i.ToString()));
					Debug.Log($"H: {scanHeight:f2} => " +
					          $"[{breakpoints}] " +
					          $"{pixelLine.Count}: {string.Join(", ", pixelLine.Select(p => p == fillColor ? "0" : "·"))}");
				}
				else
				{
					// Group in Pairs: [a,b], [c,d], ...
					Tuple<Vector2, Vector2>[] intersectionPairs = intersections.GroupInPairs().ToArray();
					
					// Fill the Line of Pixels between the intersection pairs with fillColor, otherwise with backgroundColor
					FillScanlinePixels(
						ref pixels, y, texSize, aabbUpscaled, matchAABBWithTextureSize, 
						intersectionPairs, fillColor, backgroundColor);

					if (!debugInfo) continue;
					
					// DEBUG INFO
					string[] pairsStr = intersectionPairs.Select(pair => $"{pair.Item1} - {pair.Item2}").ToArray();
					Debug.Log($"H: {scanHeight:f2} =>" + $" {string.Join(',', pairsStr)}");
				}
				
			}
			
			Texture2D texture =
				transparent 
					? new Texture2D(texSize.x,texSize.y, TextureFormat.RGBA32, false) 
					: new Texture2D(texSize.x,texSize.y);
			
			texture.SetPixels(pixels);
			texture.Apply();
			
			return texture;
		}

		
		#region TEXTURE UTILS
		
		/// <summary>
		/// Fill a Line of Pixels between the intersections (precalculated) of the polygon with a scanline
		/// </summary>
		private static void FillScanlinePixels(ref Color[] pixels, int y, Vector2Int texSize, AABB_2D aabb, 
			bool matchAABBWithTextureSize, Tuple<Vector2, Vector2>[] intersectionPairs,
			Color fillColor, Color backgroundColor)
		{
			for (var x = 0; x < texSize.x; x++)
			{
				// X to Polygon World Space
				float worldX = matchAABBWithTextureSize ? x : x / (float)texSize.x * aabb.Width + aabb.min.x;

				// Set Fill Color if X between ANY of the inters. pairs
				pixels[y * texSize.x + x] =
					intersectionPairs.Any(pair => worldX >= pair.Item1.x && worldX <= pair.Item2.x)
						? fillColor
						: backgroundColor;
			}
		}
		
		/// <summary>
		///		Generate the pixel Indices of the Scanline Breakpoints to use to fill the texture
		/// </summary>
		private static int[] GetScanlineBreakpoints(Vector2Int texSize, AABB_2D aabbUpscaled, 
			Vector2[] breakpointPositions, bool matchAABBWithTextureSize)
		{
			return new[] { 0 } // First Pixel Index
				.Concat(
					// X to Texture Space Indices
					breakpointPositions.Select(i =>
						Mathf.RoundToInt(matchAABBWithTextureSize
							? i.x
							: (i.x - aabbUpscaled.min.x) / aabbUpscaled.Width * texSize.x)
					)
				)
				.Append(texSize.x) // Last Pixel Index
				.ToArray();
		}

		/// <summary>
		/// Fill the scanline Pixels with colors alternating in each X Index Breakpoint
		/// </summary>
		private static List<Color> GeneratePixelLine(int[] xIndexBreakpoints, Color fillColor, Color backgroundColor)
		{
			Color currentColor = fillColor;
			List<Color> pixelLine = new List<Color>();
			for (var k = 0; k < xIndexBreakpoints.Length - 1; k++)
			{
				currentColor = currentColor == fillColor ? backgroundColor : fillColor; // Switch color
				pixelLine.AddRange(currentColor.ToFilledArray(xIndexBreakpoints[k + 1] - xIndexBreakpoints[k])); // Fill with color
			}
			return pixelLine;
		}

		#endregion
		
		#endregion
		
		
		public bool Equals(Polygon other) => other != null && Equals(vertices, other.vertices) && centroid.Equals(other.centroid);

		public override bool Equals(object obj) => obj is Polygon other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(vertices, centroid);

		public override string ToString() =>
			$"{VertexCount} vertices: {string.Join(", ", vertices.Take(10))} {(VertexCount > 10 ? "..." : "")}\n" +
			$"Centroid: {centroid}";

	}
}
