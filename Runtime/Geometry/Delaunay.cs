using System.Collections.Generic;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public static class Delaunay
	{
		// points deben estar Normalizados entre [0,1]
		public static Triangle[] Triangulate(Vector2[] points)
		{
			// Inicializamos el Supertriangulo que contiene a todos
			var triangles = new List<Triangle> { SuperTriangle };

			// Triangulacion incremental
			for (var i = 0; i < points.Length; i++)
			{
				Vector2 p = points[i];
				// Triangulos que se deben eliminar
				var badTriangles = new List<Triangle>();
				// Aristas que se deben eliminar
				var badEdges = new List<Edge>();

				// Buscar Triangulos que contengan a p
				for (var j = 0; j < triangles.Count; j++)
				{
					Triangle t = triangles[j];
					if (!GeometryUtils.IsInsideCircle(p, t.v1xz, t.v2xz, t.v3xz)) continue;

					// TODO
					// int neigh1 = t.neigh1;
					// int neigh2 = t.neigh2;
					// int neigh3 = t.neigh3;
					//
					// if (neigh1 != -1)
					//
					// 	triangles.Add(t);
				}

				// Crear el poligono convexo
				// 	for (var j = 0; j < badEdges.Length; j++)
				// 	{
				// 		Edge e = badEdges[j];
				// 		if (e.tIzq != null && e.tDer != null)
				// 		{
				// 			triangles = RemoveTriangle(triangles, e.tIzq);
				// 			triangles = RemoveTriangle(triangles, e.tDer);
				// 		}
				// 	}
				//
				// 	// Crear nuevos triangulos
				// 	for (var j = 0; j < badEdges.Length; j++)
				// 	{
				// 		Edge e = badEdges[j];
				// 		triangles = AddTriangle(triangles, new Triangle(e.begin, e.end, p));
				// 	}
				// }
				//
				// // Eliminar triangulos que contengan vertices del supertriangulo
				// for (var i = 0; i < triangles.Length; i++)
				// {
				// 	Triangle t = triangles[i];
				// 	if (t.p1 == superTriangle.p1 || t.p1 == superTriangle.p2 || t.p1 == superTriangle.p3 ||
				// 	    t.p2 == superTriangle.p1 || t.p2 == superTriangle.p2 || t.p
				// }
			}

			return triangles.ToArray();
		}


		public static Triangle SuperTriangle => new(
			new Vector2(-100, -100),
			new Vector2(100, -100),
			new Vector2(0, 100)
		);
	}
}
