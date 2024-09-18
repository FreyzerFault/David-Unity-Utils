using System;
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
			int oldVertexCount = mesh.vertexCount;
			mesh.SetVertices(
				newTriangles.SelectMany(t => new[] { t.v3, t.v2, t.v1 }).ToV3().ToArray()
			);

			mesh.RecalculateBounds();

			// Indices, Normales y Colores no deberian cambiar si el numero de vertices no cambia
			if (oldVertexCount == mesh.vertexCount) return;

			mesh.SetTriangles(mesh.vertices.Select((_, i) => i).ToArray(), 0);

			if (mesh.normals.Length > 0) mesh.SetNormal(mesh.normals[0]);
			if (mesh.colors.Length > 0) mesh.SetColor(mesh.colors[0]);
		}

		/// <summary>
		///     Actualiza la Mesh con un Polygon sin tener que recrearlo
		/// </summary>
		public static void SetPolygon(this Mesh mesh, Polygon polygon, Color? color = null)
		{
			int oldVertexCount = mesh.vertexCount;
			Debug.Log($"Polygon to Triangulate: {polygon}\n" +
			          $"Edges: {string.Join(", ", polygon.Edges.Select(e => $"{e.begin} - {e.end}"))}\n" +
			          $"Triangulated: {string.Join(", ", polygon.Triangulate().Select(tri => tri.ToString()))}");
			
			// TODO Extraer todos los poligonos segmentados
			Polygon[] convexPolys = polygon.OptimalConvexDecomposition();
			
			Triangle[] tris = polygon.IsConvex() ? polygon.Triangulate() : convexPolys.SelectMany(p => p.Triangulate()).ToArray();

			Vector3[] newVertices = tris.SelectMany(t => new[] { t.v3, t.v2, t.v1 }).ToV3().ToArray();

			// Indices, Normales y Colores no deberian cambiar si el numero de vertices no cambia
			if (oldVertexCount == newVertices.Length)
			{
				mesh.vertices = newVertices;
			}
			else
			{
				int[] indices = tris.SelectMany((_, i) => new[] { i * 3, i * 3 + 1, i * 3 + 2 }).ToArray();
				mesh.triangles = Array.Empty<int>();
				mesh.vertices = newVertices;
				mesh.triangles = indices;

				mesh.SetNormal(Vector3.back);

				mesh.SetColor(color ?? (mesh.colors.Length > 0 ? mesh.colors[0] : Color.white));
			}

			mesh.RecalculateBounds();
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
