﻿using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry.MeshExtensions
{
	public static class MeshGeneration
	{
		#region TRIANGLE MESH

		// TRIANGLEs => MESH (1 Color / Triangle)
		public static Mesh CreateMesh(this IEnumerable<Triangle> tris, Color[] colors = null)
		{
			IEnumerable<Triangle> trisEnum = tris as Triangle[] ?? tris.ToArray();
			var indices = new int[trisEnum.Count() * 3].FillBy(i => i);
			
			var vertices = 
				trisEnum.SelectMany((t, _) =>
					t == null
						? Array.Empty<Vector3>()
						: new Vector3[] { t.v3, t.v2, t.v1 });

			Mesh mesh = new Mesh
			{
				vertices = vertices.ToArray(),
				triangles = indices.ToArray()
			};

			if (colors.NotNullOrEmpty())
				mesh.SetColors(colors?.SelectMany(c => new[] { c, c, c }).ToArray());

			mesh.normals = mesh.vertices.Select(_ => Vector3.back).ToArray();

			mesh.bounds = trisEnum.SelectMany(t => t == null ? Array.Empty<Vector2>() : t.Vertices).GetBoundingBox();

			return mesh;
		}

		// TRIANGLEs => MESH (single COLOR)
		public static Mesh CreateMesh(this IEnumerable<Triangle> tris, Color color = default)
		{
			IEnumerable<Triangle> trisEnum = tris as Triangle[] ?? tris.ToArray();
			return trisEnum.CreateMesh(trisEnum.Select(_ => color).ToArray());
		}

		// SINGLE TRIANGLE
		public static Mesh CreateMesh(this Triangle triangle, Color color = default) =>
			CreateMesh(new[] { triangle }, new[] { color });

		// POLYGON => TRIANGLEs => MESH
		public static Mesh CreateMesh(this Polygon polygon, Color color = default)
		{
			(Triangle[] tris, _) = polygon.TriangulateConcave();
			return CreateMesh(tris, color);
		}

		#endregion

		#region PLANE

		/// <summary>
		///     Genera un Plano cuya maya está dividida en celdas con una resolución (celdas de ancho)
		/// </summary>
		/// <param name="resolution"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static void GenerateMeshPlane(this Mesh mesh, float resolution = 10, Vector2 size = default)
		{
			float meshCellSize = 1 / resolution;
			Vector2Int sideCellCount = new Vector2Int(
				Mathf.CeilToInt(size.x / meshCellSize),
				Mathf.CeilToInt(size.y / meshCellSize)
			);
			Vector2Int sideVerticesCount = sideCellCount + Vector2Int.one;

			var vertices = new Vector3[sideVerticesCount.x * sideVerticesCount.y];
			var triangles = new int[sideCellCount.x * sideCellCount.y * 6];

			for (var y = 0; y < sideVerticesCount.y; y++)
			for (var x = 0; x < sideVerticesCount.x; x++)
			{
				Vector3 vertex = new Vector3(x * meshCellSize, 0, y * meshCellSize);

				// Center Mesh
				vertex -= new Vector3(size.x, 0, size.y) / 2;

				int vertexIndex = x + y * sideVerticesCount.x;

				vertices[vertexIndex] = vertex;

				if (x >= sideCellCount.x || y >= sideCellCount.y) continue;

				// Triangles
				int triangleIndex = (x + y * sideCellCount.x) * 6;
				triangles[triangleIndex + 0] = vertexIndex + 0;
				triangles[triangleIndex + 1] = vertexIndex + sideVerticesCount.x + 0;
				triangles[triangleIndex + 2] = vertexIndex + sideVerticesCount.x + 1;

				triangles[triangleIndex + 3] = vertexIndex + 0;
				triangles[triangleIndex + 4] = vertexIndex + sideVerticesCount.x + 1;
				triangles[triangleIndex + 5] = vertexIndex + 1;
			}

			mesh.vertices = vertices;
			mesh.triangles = triangles;

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.Optimize();
		}

		public static Mesh GenerateMeshPlane(float resolution = 10, Vector2 size = default)
		{
			Mesh mesh = new Mesh();
			mesh.GenerateMeshPlane(resolution, size);
			return mesh;
		}

		public static Mesh GenerateTerrainPatch(
			this Mesh mesh,
			Terrain terrain,
			Transform meshTransform,
			Vector3 worldCenter,
			float size,
			float heightOffset
		)
		{
			float cellSize = terrain.terrainData.heightmapScale.x / 2;
			mesh.GenerateMeshPlane(cellSize, Vector2.one * size);
			return terrain.ProjectMeshInTerrain(mesh, meshTransform, heightOffset);
		}


		// Extract Patch from Terrain (Mesh proyectada sobre el terreno)
		public static Mesh GenerateTerrainPatch(
			this Terrain terrain,
			Transform meshTransform,
			Vector3 worldCenter,
			float size,
			float heightOffset
		) => new Mesh().GenerateTerrainPatch(terrain, meshTransform, worldCenter, size, heightOffset);

		#endregion
	}
}
