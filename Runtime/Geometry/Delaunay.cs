using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;
#if UNITY_EDITOR
using DavidUtils.DebugUtils;
#endif

namespace DavidUtils.Geometry
{
	[Serializable]
	public class Delaunay
	{
		public struct Edge
		{
			public Vector2 begin;
			public Vector2 end;

			public Vector2[] Vertices => new[] { begin, end };

			public Edge(Vector2 a, Vector2 b)
			{
				begin = a;
				end = b;
			}

			// Comprueba si dos aristas son CONCAVAS (el vertice intermedio esta a la Izquierda)
			// Presupone que se suceden (e1.end == e2.begin)
			public static bool IsConcave(Edge e1, Edge e2) =>
				GeometryUtils.IsLeft(e1.begin, e2.end, e1.end);

			private static bool IsConvex(Edge e1, Edge e2) =>
				GeometryUtils.IsRight(e1.begin, e2.end, e1.end);

			public Vector2 Median => (begin + end) / 2;
			public Vector2 Dir => (end - begin).normalized;

			// Dir Derecha => (90º CCW) => [y,-x]
			public Vector2 MediatrizRight => new(Dir.y, -Dir.x);

			// Dir IZQUIERDA => (90º CW) => [-y,x]
			public Vector2 MediatrizLeft => new(-Dir.y, Dir.x);

			/// <summary>
			///     Interseccion de la Mediatriz con la BoundingBox
			///     La direccion del rayo debe ser PERPENDICULAR a la arista
			/// </summary>
			public Vector2[] MediatrizIntersetions(Bounds2D bounds) =>
				bounds.Intersections_Line(Median, MediatrizRight).ToArray();


			/// <summary>
			///     Interseccion de la Mediatriz con la BoundingBox a la derecha de la arista
			/// </summary>
			public Vector2[] MediatrizIntersetions_RIGHT(Bounds2D bounds) =>
				bounds.Intersections_Ray(Median, MediatrizRight).ToArray();

			/// <summary>
			///     Interseccion de la Mediatriz con la BoundingBox a la izquierda de la arista
			/// </summary>
			public Vector2[] MediatrizIntersetions_LEFT(Bounds2D bounds) =>
				bounds.Intersections_Ray(Median, MediatrizRight).ToArray();


			// Ignora el la direccion
			public override bool Equals(object obj)
			{
				if (obj is Edge edge)
					return (edge.begin == begin && edge.end == end) || (edge.begin == end && edge.end == begin);

				return false;
			}

			public override int GetHashCode() => HashCode.Combine(begin, end);

#if UNITY_EDITOR
			public void OnGizmosDraw(
				Matrix4x4 matrix, float thickness = 1, Color color = default, bool projectedOnTerrain = false
			)
			{
				Vector3[] verticesInWorld =
					Vertices.Select(vertex => matrix.MultiplyPoint3x4(vertex.ToV3xz())).ToArray();
				GizmosExtensions.DrawLineThick(
					projectedOnTerrain ? Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld) : verticesInWorld,
					thickness,
					color
				);
			}
#endif
		}

		public class Triangle
		{
			// CCW
			public Vector2 v1, v2, v3;
			public Vector2[] Vertices => new[] { v1, v2, v3 };

			public Edge e1;
			public Edge e2;
			public Edge e3;
			public Edge[] Edges => new[] { e1, e2, e3 };

			// NEIGHBOURS (t1 share e1, etc)
			public Triangle[] neighbours;
			public Triangle T1 => neighbours[0];
			public Triangle T2 => neighbours[1];
			public Triangle T3 => neighbours[2];

			public bool IsBorder => neighbours.Length != 3 || neighbours.Any(n => n == null);

			public Edge[] BorderEdges => Edges.Where((e, i) => neighbours[i] == null).ToArray();

			public Triangle(
				Vector2 v1, Vector2 v2, Vector2 v3, Triangle t1 = null, Triangle t2 = null, Triangle t3 = null
			)
			{
				this.v1 = v1;
				this.v2 = v2;
				this.v3 = v3;

				SetAllNeightbours(new[] { t1, t2, t3 });

				e1 = new Edge { begin = v1, end = v2 };
				e2 = new Edge { begin = v2, end = v3 };
				e3 = new Edge { begin = v3, end = v1 };
			}

			public Triangle(
				Vector2[] vertices, Triangle t1 = null, Triangle t2 = null, Triangle t3 = null
			) : this(vertices[0], vertices[1], vertices[2], t1, t2, t3)
			{
			}

			public Triangle(Vector2[] vertices, Triangle[] neighbours = null)
				: this(vertices[0], vertices[1], vertices[2])
			{
				if (neighbours != null && neighbours.Length > 0)
					SetAllNeightbours(neighbours);
			}

			public void MoveVertex(Vector2 vertex, Vector2 newVertex)
			{
				if (v1 == vertex)
				{
					v1 = newVertex;
					e3.end = newVertex;
					e1.begin = newVertex;
				}
				else if (v2 == vertex)
				{
					v2 = newVertex;
					e1.end = newVertex;
					e2.begin = newVertex;
				}
				else if (v3 == vertex)
				{
					v3 = newVertex;
					e2.end = newVertex;
					e3.begin = newVertex;
				}
			}

			/// <summary>
			///     Asigna vecinos de forma recíproca (Al vecino se le asigna este Triangulo como vecino también)
			/// </summary>
			public void SetAllNeightbours(Triangle[] newNeighbours)
			{
				neighbours = newNeighbours;
				for (var i = 0; i < newNeighbours.Length; i++)
					SetNeighbour(newNeighbours[i], i);
			}

			/// <summary>
			///     Asigna un vecino de forma recíproca cuya arista que los une es Edges[index]
			///     (Al vecino se le asigna este Triangulo como vecino también)
			/// </summary>
			public void SetNeighbour(Triangle t, int index)
			{
				neighbours[index] = t;
				if (t == null) return;

				// Set the neighbour in the other triangle
				Edge sharedEdge = Edges[index];
				for (var i = 0; i < 3; i++)
				{
					Edge edge = t.Edges[i];
					if (edge.Equals(sharedEdge))
						t.neighbours[i] = this;
				}
			}

			public Triangle GetNeighbour(Edge edge) => neighbours[Array.IndexOf(Edges, edge)];

			/// <summary>
			///     MEDIATRIZ para encontrar el circuncentro, vértice buscado en Voronoi
			/// </summary>
			public Vector2 GetCircumcenter()
			{
				Vector2? c = GeometryUtils.CircleCenter(v1, v2, v3);
				if (c.HasValue) return c.Value;

				// Son colineares
				return (v1 + v2 + v3) / 3;
			}

			public Vector2 GetOppositeVertex(Edge edge, out int side)
			{
				for (var i = 0; i < 3; i++)
				{
					Vector2 vertex = Vertices[i];
					if (vertex == edge.begin || vertex == edge.end) continue;

					side = i;
					return vertex;
				}

				side = 0;
				return v1;
			}

			public static Triangle SuperTriangle =>
				new(new Vector2(-2, -1), new Vector2(2, -1), new Vector2(0, 3));

			#region MESH GENERATION

			public static Mesh CreateMesh(Triangle[] tris, Color color = default, bool XZplane = true)
			{
				int[] indices = new int[tris.Length * 3].Select((_, index) => index).ToArray();
				var vertices = new Vector3[tris.Length * 3];
				for (var i = 0; i < tris.Length; i++)
				{
					Triangle t = tris[i];
					vertices[i * 3 + 0] = t.v3.ToV3xz();
					vertices[i * 3 + 1] = t.v2.ToV3xz();
					vertices[i * 3 + 2] = t.v1.ToV3xz();
				}

				var mesh = new Mesh
				{
					vertices = vertices,
					triangles = indices
				};

				var colors = new Color[vertices.Length];
				Array.Fill(colors, color);
				mesh.colors = colors;

				mesh.normals = mesh.vertices.Select(v => XZplane ? Vector3.up : Vector3.back).ToArray();
				mesh.bounds = tris
					.SelectMany(t => t.Vertices)
					.Select(p => XZplane ? p.ToV3xz() : p.ToV3xy())
					.ToArray()
					.GetBoundingBox();

				return mesh;
			}

			#endregion

#if UNITY_EDITOR

			#region DEBUG

			public void OnGizmosDrawWire(
				Matrix4x4 matrix, float thickness = 1, Color color = default, bool projectedOnTerrain = false
			)
			{
				Vector3[] verticesInWorld = Vertices
					.Select(vertex => matrix.MultiplyPoint3x4(vertex.ToV3xz()))
					.ToArray();

				if (projectedOnTerrain)
					GizmosExtensions.DrawPolygonWire(
						Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld),
						thickness,
						color
					);
				else
					GizmosExtensions.DrawTriWire(verticesInWorld, thickness, color);
			}

			public void OnGizmosDraw(Matrix4x4 matrix, Color color = default, bool projectedOnTerrain = false)
			{
				Vector3[] verticesInWorld = Vertices
					.Select(vertex => matrix.MultiplyPoint3x4(vertex.ToV3xz()))
					.ToArray();

				if (projectedOnTerrain)
					GizmosExtensions.DrawPolygon(Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld), color);
				else
					GizmosExtensions.DrawTri(verticesInWorld, color);
			}

			#endregion

#endif
		}

		private List<Vector2> _seeds;
		public List<Vector2> Seeds
		{
			get => _seeds;
			set
			{
				_seeds = value;
				Reset();
			}
		}

		[HideInInspector] public List<Vector2> vertices = new();
		[HideInInspector] public List<Triangle> triangles = new();

		public Delaunay(IEnumerable<Vector2> seeds = null) =>
			_seeds = seeds == null ? new List<Vector2>() : seeds.ToList();

		public void Run() => triangles = Triangulate(_seeds).ToList();

		// Algoritmo de Bowyer-Watson
		// - Por cada punto busca los triangulos cuyo círculo circunscrito contenga al punto
		// - Elimina los triangulos invalidos y crea nuevos triangulos con el punto
		// points deben estar Normalizados entre [0,1]
		public IEnumerable<Triangle> Triangulate(List<Vector2> points)
		{
			Reset();

			foreach (Vector2 p in points)
				Triangulate(p);

			RemoveBoundingBox();

			ended = true;

			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();

			return triangles;
		}


		public IEnumerable<Triangle> FindTrianglesAroundVertex(Vector2 vertex) =>
			triangles.Where(t => t.Vertices.Any(v => v.Equals(vertex)));

		/// <summary>
		///     Crea un borde a partir de un vertice.
		///     Recoge todas las aristas opuestas al vertice de los triangulos a los que pertenece
		///     Y los ordena CCW
		/// </summary>
		private IEnumerable<Border> GetBordersAround(Vector2 centerVertex, IEnumerable<Triangle> trisAround = null)
		{
			trisAround ??= FindTrianglesAroundVertex(centerVertex);
			IEnumerable<Border> borders = trisAround
				.SelectMany(
					t =>
					{
						List<Border> borders = new();
						for (var i = 0; i < 3; i++)
						{
							Edge edge = t.Edges[i];
							Triangle neighbour = t.neighbours[i];
							if (edge.begin != centerVertex && edge.end != centerVertex)
								borders.Add(new Border(neighbour, edge));
						}

						return borders;
					}
				);

			return Border.SortByAngle(borders, centerVertex);
		}

		public void MoveSeed(int index, Vector2 newPos)
		{
			Vector2 vertex = _seeds[index];

			Triangle[] tris = FindTrianglesAroundVertex(vertex).ToArray();
			foreach (Triangle tri in tris)
				tri.MoveVertex(vertex, newPos);

			_seeds[index] = newPos;
			vertices[index] = newPos;

			// LEGALIZACION
			// Busca el primer Triangulo ilegal, lo flipea y vuelve a reiniciar la busqueda con los triangulos nuevos
			// Cuando no haya ningun triangulo ilegal, sale del bucle
			bool illegal;
			do
			{
				illegal = false;
				foreach (Triangle tri in tris)
				{
					if (IsLegal(tri, out List<int> illegalSides)) continue;

					Flip(tri, illegalSides[0]);
					tris = FindTrianglesAroundVertex(newPos).ToArray();
					illegal = true;
					break;
				}
			} while (illegal);
		}

		/// <summary>
		///     Comprueba si el triangulo es legal
		///     (Alguno de los triangulos vecinos tiene un vértice opuesto
		///     dentro del circulo circunscrito formado por el triangulo)
		///     Guarda los indices de los lados ilegales en illegalSides
		/// </summary>
		private bool IsLegal(Triangle tri, out List<int> illegalSides)
		{
			illegalSides = new List<int>();
			for (var i = 0; i < 3; i++)
			{
				Edge edge = tri.Edges[i];
				Triangle neighbour = tri.neighbours[i];
				if (neighbour == null) continue;
				if (GeometryUtils.PointInCirle(neighbour.GetOppositeVertex(edge, out int side), tri.v1, tri.v2, tri.v3))
					illegalSides.Add(i);
			}

			return illegalSides.Count == 0;
		}

		/// <summary>
		///     Flipea la arista del triangulo tri.Edges[side]
		///     Eliminando el triangulo y su vecino, y creando nuevos
		///     Actualiza los vecinos también
		/// </summary>
		private void Flip(Triangle tri, int side)
		{
			Edge edge = tri.Edges[side];
			Triangle neighbour = tri.neighbours[side];

			Vector2 opposite1 = tri.GetOppositeVertex(edge, out int opSide1);
			Vector2 opposite2 = neighbour.GetOppositeVertex(edge, out int opSide2);

			var newTri1 = new Triangle(opposite1, opposite2, edge.end);
			var newTri2 = new Triangle(opposite2, opposite1, edge.begin);

			// NEIGHBOURS
			newTri1.SetAllNeightbours(new[] { newTri2, neighbour.neighbours[opSide2], tri.neighbours[(side + 1) % 3] });
			newTri2.SetAllNeightbours(
				new[] { newTri1, tri.neighbours[(side + 2) % 3], neighbour.neighbours[(opSide2 + 2) % 3] }
			);

			addedTris = new List<Triangle>(new[] { newTri1, newTri2 });
			removedTris = new List<Triangle>(new[] { tri, neighbour });

			triangles.Remove(tri);
			triangles.Remove(neighbour);

			triangles.AddRange(addedTris);
		}


		#region TRIANGULATION

		/// <summary>
		///     Delaunay Incremental Bowyer–Watson Algorithm
		///     Elimina los Triangulos en los que el Vertice este dentro de su Circulo Circunscrito
		///     Crea un Polígono con el hueco que se forma al eliminar los triángulos
		///     Y añade un triangulo por cada arista del polígono
		///     Por definición, los triángulos generados deben ser LEGALES
		///     Por lo que no hace falta flipear ninguna arista
		/// </summary>
		public void Triangulate(Vector2 point)
		{
			if (vertices.Any(v => Vector2.Distance(v, point) < GeometryUtils.Epsilon)) return;
			vertices.Add(point);

			// Si no hay triangulos, creamos el SuperTriangulo o una Bounding Box
			if (triangles.Count == 0)
			{
				Triangle[] bb = GetBoundingBoxTriangles();

				triangles.Add(bb[0]);
				triangles.Add(bb[1]);
			}

			polygon = new List<Edge>();
			var neighbours = new List<Triangle>();

			// Triangulos que se deben eliminar
			List<Triangle> badTris = triangles.Where(t => GeometryUtils.PointInCirle(point, t.v1, t.v2, t.v3)).ToList();

			// Ignoramos los ejes compartidos por los triangulos invalidos
			// Guardamos el Triangulo Vecino y la Arista
			foreach (Triangle t in badTris)
				for (var i = 0; i < 3; i++)
				{
					Edge e = t.Edges[i];
					Triangle neighbour = t.neighbours[i];
					if (neighbour != null && badTris.Contains(neighbour)) continue;

					neighbours.Add(neighbour);
					polygon.Add(e);
				}


			// var badTrisIndices = badTris.Select(t => triangles.IndexOf(t));
			// Debug.Log($"Bad Tris: {string.Join(", ", badTrisIndices)}");
			// Debug.Log($"Sorted: {String.Join(", ", polygon.Select(e => $"[{Vector2.SignedAngle(Vector2.right, e.begin - point)}, {Vector2.SignedAngle(Vector2.right, e.end - point)} ({triangles.IndexOf(e.tOpposite)})]")) }");

			// Rellenamos el poligono con nuevos triangulos validos por cada arista del poligono
			// Le asignamos t0 como vecino a la arista
			var newTris = new Triangle[polygon.Count];
			for (var i = 0; i < polygon.Count; i++)
			{
				Edge e = polygon[i];
				Triangle neighbour = neighbours[i];
				newTris[i] = new Triangle(e.begin, e.end, point);
				if (neighbour != null)
					newTris[i].SetNeighbour(neighbour, 0); // SetNeighbour() es Reciproco
			}

			// Ordenamos los nuevo triangulos del poligono CCW
			newTris = newTris.OrderBy(
					t =>
					{
						Vector2 polarPos = t.v1 - point;
						return Mathf.Atan2(polarPos.y, polarPos.x);
					}
				)
				.ToArray();


			// Asignamos vecinos entre ellos. Como esta ordenado CCW t1 es el siguiente, y t2 el anterior
			for (var j = 0; j < newTris.Length; j++)
			{
				Triangle t = newTris[j];
				t.neighbours[1] = newTris[(j + 1) % newTris.Length];
				t.neighbours[2] = newTris[(j - 1 + newTris.Length) % newTris.Length];
			}

			// Eliminamos los triangulos invalidos dentro del poligono
			foreach (Triangle badTri in badTris)
				triangles.Remove(badTri);

			// Añadimos los nuevos triangulos
			triangles.AddRange(newTris);
			removedTris = badTris;
			addedTris = newTris.ToList();
		}


		// NO FUNCA, ALTERNATIVA => Al mover un vertice modificar los triangulos y legalizarlos en MoveSeed()

		// /// <summary>
		// ///     Deshace la Triangulacion del vertice con el indice dado
		// ///     Eliminando los Triangulos a los que pertenece el Vertice
		// ///     Y creando nuevos triangulos con los vertices que forman el poligono que queda como hueco
		// /// </summary>
		// public void UndoTriangulation(int vertexIndex)
		// {
		// 	Vector2 point = vertices[vertexIndex];
		// 	// Buscamos los triangulos que contengan el vertice
		// 	removedTris = FindTrianglesAroundVertex(point).ToList();
		//
		// 	// Border ordered CCW
		// 	Border[] borders = GetBordersAround(point, removedTris);
		//
		// 	Triangulate_Hole(borders);
		//
		// 	// Eliminamos los triangulos que contengan el vertice
		// 	foreach (Triangle t in removedTris)
		// 		triangles.Remove(t);
		// }
		//
		// private void Triangulate_Hole(Border[] borderPolygon)
		// {
		// 	// Polygon hecho de los ejes del borde
		// 	polygon = borderPolygon.Select(e => e.edge).ToList();
		// 	addedTris.Clear();
		//
		// 	// Situacion TRIVIAL => 3 aristas => Creamos 1 Triangulo
		// 	if (borderPolygon.Length == 3)
		// 		addedTris.Add(
		// 			new Triangle(
		// 				polygon.Select(e => e.begin).ToArray(),
		// 				borderPolygon[0].tri,
		// 				borderPolygon[1].tri,
		// 				borderPolygon[2].tri
		// 			)
		// 		);
		// 	else
		// 		// Buscamos las combinaciones de triangulos que formen el poligono
		// 		// y cuyos vertices formen un ciruclo donde no entre ninguno de los demas vertices
		// 		for (var i = 0; i < borderPolygon.Length; i++)
		// 		{
		// 			Edge edge = borderPolygon[i].edge;
		// 			Edge nextEdge = borderPolygon[(i + 1) % borderPolygon.Length].edge;
		// 			
		// 			if (addedTris.Exists(t => t.Edges.Any(e => e == nextEdge)) nextEdge)
		// 			
		// 			Triangle neighbour = borderPolygon[i].tri;
		// 			Triangle nextNeighbour = borderPolygon[(i + 1) % borderPolygon.Length].tri;
		// 			Vector2 v1 = edge.begin, v2 = edge.end, v3 = nextEdge.end;
		//
		// 			// Deben ser convexos
		// 			// Si no, ya tendran un triangulo asociado fuera del poligono que contiene el vertice eliminado
		// 			if (GeometryUtils.IsLeft(v1, v3, v2)) continue;
		//
		// 			Vector2[] otherVertices = polygon.Select(e => e.begin)
		// 				.Where(vertex => v1 != vertex && v2 != vertex && v3 != vertex)
		// 				.ToArray();
		//
		// 			// Test Point in Circle / vertice
		//
		// 			var ilegalTriangle = false;
		// 			foreach (Vector2 vertex in otherVertices)
		// 			{
		// 				// Si un vertice esta dentro del Circulo formado por v1,v2,v3, no es legal
		// 				ilegalTriangle = GeometryUtils.PointInCirle(vertex, v1, v2, v3);
		// 				if (ilegalTriangle) break;
		// 			}
		//
		// 			if (ilegalTriangle) continue;
		//
		// 			// Creo el Triangulo y asigno vecinos
		// 			addedTris.Add(
		// 				new Triangle(
		// 					v1,
		// 					v2,
		// 					v3,
		// 					neighbour,
		// 					nextNeighbour
		// 				)
		// 			);
		// 			i++;
		// 		}
		//
		// 	// Añadimos los nuevos triangulos
		// 	triangles.AddRange(addedTris);
		//
		// 	// Asignamos vecinos entre ellos
		// 	// Buscamos que triangulo comparte la arista v3->v1 (Edges[2])
		// 	// Ambos ejes compartidos tendran direccion opuesta, por lo que end == begin
		// 	foreach (Triangle tri in addedTris)
		// 	{
		// 		Triangle neighbour = addedTris.First(t => t != tri && tri.Edges[2].begin == t.Edges[2].end);
		// 		tri.SetNeighbour(neighbour, 2);
		// 	}
		// }

		#endregion


		#region BORDER

		// Lista de Aristas que forman el Borde
		// (Busca las que no tengan triangulo vecino)
		// Ordenadas CCW por sus coords polares respecto a 0,0
		private IEnumerable<Border> Borders
		{
			get
			{
				List<Border> borders = new();
				foreach (Triangle tri in triangles)
				{
					if (!tri.IsBorder) continue;

					Triangle tri1 = tri;
					borders.AddRange(tri.BorderEdges.Select(e => new Border(tri1, e)));
				}

				return borders.OrderBy(border => border.PolarAngle).ToArray();
			}
		}

		#endregion


		#region PROGRESIVE RUN

		[HideInInspector] public int iterations;
		[HideInInspector] public bool ended;
		private List<Triangle> removedTris = new();
		private List<Triangle> addedTris = new();
		private List<Edge> polygon = new();

		public void Run_OnePoint()
		{
			if (iterations > _seeds.Count)
				return;

			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();

			if (iterations == _seeds.Count)
				RemoveBoundingBox();
			else
				Triangulate(_seeds[iterations]);

			iterations++;

			ended = iterations > _seeds.Count;
		}

		public void Reset()
		{
			iterations = 0;
			ended = false;
			triangles = new List<Triangle>();
			vertices = new List<Vector2>();
			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();
		}

		#endregion


		#region BOUNDING BOX or SUPERTRIANGLE

		// Vertices almancenados para buscar al final todos los triangulos que lo tengan
		private Vector2[] _boundingVertices = Array.Empty<Vector2>();

		public Triangle[] GetBoundingBoxTriangles()
		{
			var bounds = new Bounds2D(Vector2.one * -.1f, Vector2.one * 1.1f);
			var t1 = new Triangle(bounds.BR, bounds.TL, bounds.BL);
			var t2 = new Triangle(bounds.TL, bounds.BR, bounds.TR);

			t1.neighbours[0] = t2;
			t2.neighbours[0] = t1;

			_boundingVertices = bounds.Corners;

			return new[] { t1, t2 };
		}

		public void InitializeSuperTriangle()
		{
			var superTri = Triangle.SuperTriangle;
			triangles.Add(superTri);
			_boundingVertices = superTri.Vertices;
		}

		// Elimina todos los Triangulos que contengan un vertice del SuperTriangulo/s
		// Reasigna el vecino a de cualquier triangulo vecino que tenga como vecino al triangulo eliminado a null
		// Y repara el borde
		public void RemoveBoundingBox() => RemoveBorder(_boundingVertices);

		private void RemoveBorder(IEnumerable<Vector2> points)
		{
			// Cogemos todos los Triangulos que contengan un vertice del SuperTriangulo
			HashSet<Triangle> trisToRemove = points.SelectMany(FindTrianglesAroundVertex).ToHashSet();

			foreach (Triangle t in trisToRemove)
			{
				// Eliminamos las referencias de los vecinos
				foreach (Triangle neighbour in t.neighbours)
				{
					if (neighbour == null) continue;
					for (var i = 0; i < neighbour.neighbours.Length; i++)
						if (neighbour.neighbours[i] == t)
							neighbour.neighbours[i] = null;
				}

				triangles.Remove(t);
			}

			// Reparamos el borde para que sea CONVEXO

			// Cogemos de cada triangulo que forme parte del borde su arista de borde
			// y guardamos el indice del triangulo al que pertenece para asignar vecinos despues
			Border[] borderEdges = Borders.ToArray();

			// Iteramos los ejes por PARES, para ver si son convexos o no
			// Si NO es convexo, añadimos un triangulo al borde
			for (var i = 0; i < borderEdges.Length; i++)
			{
				Border border = borderEdges[i];
				Border nextBorder = borderEdges[(i + 1) % borderEdges.Length];

				// Siendo los ejes v1 - v2, v2 - v3
				// Es convexo si el vertice intermedio v2 esta a la derecha de la linea v1 - v3
				Vector2 v1 = border.edge.begin, v2 = border.edge.end, v3 = nextBorder.edge.end;
				if (!Edge.IsConcave(border.edge, nextBorder.edge)) continue;

				var tri = new Triangle(v3, v2, v1);
				tri.SetNeighbour(nextBorder.tri, 0);
				tri.SetNeighbour(border.tri, 1);
				triangles.Add(tri);
			}
		}

		private void RemovePointFromBorder(Vector2 point)
		{
			// TODO Desencapsular el RemoveBorder() de punto en punto
		}

		private struct Border
		{
			public readonly Triangle tri;
			public readonly Edge edge;

			public Vector2 PolarPos => edge.begin - Vector2.one * .5f;
			public float PolarAngle => Mathf.Atan2(PolarPos.y, PolarPos.x);

			public Border(Triangle tri, Edge edge)
			{
				this.tri = tri;
				this.edge = edge;
			}

			// Ordena el borde respecto a un centro dado
			public static IEnumerable<Border> SortByAngle(IEnumerable<Border> borders, Vector2 center) =>
				borders.OrderBy(
					e =>
					{
						Vector2 polarCoord = e.edge.begin - center;
						return Mathf.Atan2(
							polarCoord.y,
							polarCoord.x
						);
					}
				);
		}

		#endregion


#if UNITY_EDITOR

		#region DEBUG

		public bool draw;
		public bool drawWire = true;

		public void OnDrawGizmos(Matrix4x4 matrix, bool projectOnTerrain = false)
		{
			if (!draw) return;

			// TRIANGULATION
			Color[] colors = Color.cyan.GetRainBowColors(triangles.Count, 0.02f);

			for (var i = 0; i < triangles.Count; i++)
			{
				Triangle tri = triangles[i];

				if (ended && !drawWire)
					tri.OnGizmosDraw(matrix, colors[i], projectOnTerrain);
				else
					tri.OnGizmosDrawWire(matrix, 2, Color.white, projectOnTerrain);
			}

			// ADDED TRIANGLES
			colors = Color.cyan.GetRainBowColors(addedTris.Count, 0.02f);
			foreach (Triangle t in addedTris) t.OnGizmosDrawWire(matrix, 3, Color.white);
			for (var i = 0; i < addedTris.Count; i++)
				addedTris[i].OnGizmosDraw(matrix, colors[i], projectOnTerrain);

			// DELETED TRIANGLES
			foreach (Triangle t in removedTris) t.OnGizmosDrawWire(matrix, 3, Color.red, projectOnTerrain);

			// Hole POLYGON
			foreach (Edge e in polygon) e.OnGizmosDraw(matrix, 3, Color.green, projectOnTerrain);

			// Highlight Border
			GizmosHightlightBorder(matrix);
		}

		private void GizmosHightlightBorder(Matrix4x4 matrix) => GizmosExtensions.DrawPolygonWire(
			Borders.Select(t => t.edge.begin).Select(p => matrix.MultiplyPoint3x4(p.ToV3xz())).ToArray(),
			10,
			Color.red
		);

		#endregion

#endif
	}
}
