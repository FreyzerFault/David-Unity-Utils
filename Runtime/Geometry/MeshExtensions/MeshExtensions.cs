using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry.MeshExtensions
{
	public static class MeshExtensions
	{
		/// <summary>
		///     Actualiza una malla de triangulos sin tener que recrearla
		/// </summary>
		public static void SetTriangles(this Mesh mesh, Triangle[] newTriangles)
		{
			if (newTriangles.IsNullOrEmpty())
			{
				mesh.Clear();
				return;
			}
			
			Vector3[] vertices = newTriangles.SelectMany(t =>
				t == null
				? Array.Empty<Vector2>()
				: new[] { t.v3, t.v2, t.v1 }).ToV3().ToArray();
			
			// Indices, Normales y Colores no deberian cambiar si el numero de vertices no cambia
			if (mesh.vertexCount == vertices.Length)
			{
				mesh.SetVertices(vertices);
			}
			else
			{
				Vector3 normal = mesh.normals.Length > 0 ? mesh.normals[0] : Vector3.back;
				Color color = mesh.colors.Length > 0 ? mesh.colors[0] : Color.white;
				
				mesh.Clear();
				mesh.SetVertices(vertices);
				mesh.SetTriangles(vertices.Select((_, i) => i).ToArray(), 0);
				
				mesh.RecalculateBounds();
				mesh.SetNormal(normal);
				mesh.SetColor(color);
			}

		}

		/// <summary>
		///     Actualiza la Mesh con un Polygon sin tener que recrearlo
		///		Si el polígono es Cóncavo, se descompone en polígonos convexos y se triangula cada uno.
		/// </summary>
		/// <returns>Poligonos Convexos segmentados</returns>
		public static Polygon[] SetPolygonConcave(this Mesh mesh, Polygon polygon, Color? color = null, int maxSubPolygons = 10)
		{
			// Se descompone en poligonos convexos y se triangula cada uno.
			(Triangle[] tris, Polygon[] subpolygons) = polygon.TriangulateConcave(maxSubPolygons);
			subpolygons = subpolygons.Where(p => p != null).ToArray();
			
			mesh.SetTriangles(tris);
			mesh.SetNormal(Vector3.back);
			mesh.SetColor(color ?? (mesh.colors.Length > 0 ? mesh.colors[0] : Color.white));
			mesh.RecalculateBounds();

			return subpolygons;
		}

		/// <summary>
		///		Actualiza la Mesh con un Polygon sin tener que recrearlo
		///		Asume que el poligono es convexo
		/// </summary>
		public static void SetPolygonConvex(this Mesh mesh, Polygon polygon, Color? color = null)
		{
			// Se descompone en poligonos convexos y se triangula cada uno.
			Triangle[] tris = polygon.TriangulateConvex();
			
			mesh.SetTriangles(tris);
			mesh.SetNormal(Vector3.back);
			mesh.SetColor(color ?? (mesh.colors.Length > 0 ? mesh.colors[0] : Color.white));
			mesh.RecalculateBounds();
		}

		/// <summary>
		///		Actualiza la Mesh con un Polygon sin tener que recrearlo
		///		Si es CONCAVO, se descompone en poligonos convexos y se triangula cada uno.
		///		Genera una subrutina para generar los subpoligonos de forma asincrona
		/// </summary>
		public static IEnumerator SetPolygonConcaveCoroutine(this Mesh mesh, Action<List<Polygon>> onSubPolygonsGenerated, Action onEnd, Polygon polygon, Color? color = null,
			int maxSubPolygonsPerFrame = 10, int maxIterations = 100)
		{
			if (polygon.IsEmpty)
			{
				mesh.Clear();
				yield break;
			}
			
			mesh.SetPolygonConvex(polygon, color);

			List<Polygon> subPolygons = new List<Polygon> {polygon};
			List<Triangle> tris = new List<Triangle>();
			Triangle[] lastTris = Array.Empty<Triangle>();
			yield return subPolygons.ToArray();
			
			int iterations = 0;
			
			// Se descompone en poligonos convexos y se triangula cada uno.
			do
			{
				// Elimino los triangulos del ultimo poligono para splitearlo bien
				if (tris.Count != 0)
					tris.RemoveRange(tris.Count - lastTris.Length, lastTris.Length);
				
				Polygon[] newSubPolygons = subPolygons.Last().OptimalConvexDecomposition(maxSubPolygonsPerFrame);
				
				if (newSubPolygons.IsNullOrEmpty() || newSubPolygons.Length == 1)
				{
					Debug.LogWarning("Last subpolygon can't be segmented");
					break;
				}
				
				subPolygons.RemoveAt(subPolygons.Count - 1);
				subPolygons.AddRange(newSubPolygons);

				Triangle[] newTris = newSubPolygons.SkipLast(1).SelectMany(p => p.TriangulateConvex()).ToArray();
				tris.AddRange(newTris);
				
				lastTris = newSubPolygons.Last().TriangulateConvex().ToArray();
				tris.AddRange(lastTris);

				mesh.SetTriangles(tris.ToArray());
				mesh.SetNormal(Vector3.back);
				mesh.SetColor(color ?? (mesh.colors.Length > 0 ? mesh.colors[0] : Color.white));
				mesh.RecalculateBounds();
				
				onSubPolygonsGenerated?.Invoke(subPolygons);
				
				yield return subPolygons.ToArray();
			} while (subPolygons.NotNullOrEmpty() && subPolygons.Last().IsConcave() && ++iterations < maxIterations);
			
			onEnd?.Invoke();
		}

		public static void SetColor(this Mesh mesh, Color color) =>
			mesh.SetColors(color.ToFilledArray(mesh.vertexCount).ToArray());

		public static void SetNormal(this Mesh mesh, Vector3 normal) =>
			mesh.SetNormals(normal.ToFilledArray(mesh.vertexCount).ToArray());

		#region MESH TRANSFORMATIONS

		// TRANSLATION
		
		/// <summary>
		/// Mueve todos los vertices
		/// </summary>
		public static void Translate(this Mesh mesh, Vector3 translation)
		{
			mesh.SetVertices(mesh.vertices.Select(v => v + translation).ToArray());
			mesh.RecalculateBounds();
		}
		
		public static void TranslateToOrigin(this Mesh mesh) => 
			mesh.Translate(mesh.bounds.min);

		public static void TranslateToOriginCentered(this Mesh mesh) => 
			mesh.Translate(mesh.bounds.center);
		
		/// <summary>
		/// Mueve todos los vertices de las mallas por igual
		/// </summary>
		public static void TranslateAsGroup(this IEnumerable<Mesh> mesh, Vector3 translation) => 
			mesh.ForEach(m => m.Translate(translation));
		
		public static void TranslateToOrigin(this IEnumerable<Mesh> mesh)
		{
			// Busca la posicion minima de todos los bounds
			var minPositions = mesh.Select(m => m.bounds.min).ToArray();
			var min = new Vector3(minPositions.Min(v => v.x), minPositions.Min(v => v.y), minPositions.Min(v => v.z));
			
			mesh.TranslateAsGroup(-min);
		}
		
		public static void TranslateToOriginCentered(this IEnumerable<Mesh> mesh)
		{
			// Busca la posicion minima de todos los bounds
			var minPositions = mesh.Select(m => m.bounds.min).ToArray();
			var maxPositions = mesh.Select(m => m.bounds.max).ToArray();
			var min = new Vector3(minPositions.Min(v => v.x), minPositions.Min(v => v.y), minPositions.Min(v => v.z));
			var max = new Vector3(maxPositions.Max(v => v.x), maxPositions.Max(v => v.y), maxPositions.Max(v => v.z));
			
			mesh.TranslateAsGroup(-(max + min) / 2);
		}
		
		// ROTATION
		
		
		public static void Rotate(this Mesh mesh, Quaternion rotation)
		{
			mesh.SetVertices(mesh.vertices.Select(v => rotation * v).ToArray());
			mesh.SetNormals(mesh.normals.Select(n => rotation * n).ToArray());
			mesh.RecalculateBounds();
		}

		#endregion
	}
}
