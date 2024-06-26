﻿using System;
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
			Triangle[] tris = polygon.Triangulate();

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
	}
}
