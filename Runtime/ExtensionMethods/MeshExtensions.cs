using System;
using System.Linq;
using DavidUtils.Geometry;
using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
    public static class MeshExtensions
    {
        #region MESH MODIFICATION

        /// <summary>
        /// Actualiza una malla de triangulos sin tener que recrearla
        /// </summary>
        public static void SetTriangles(this Mesh mesh, Triangle[] newTriangles)
        {
            int oldVertexCount = mesh.vertexCount;
            mesh.SetVertices(newTriangles.SelectMany(t => new[] { t.v3.ToV3xz(), t.v2.ToV3xz(), t.v1.ToV3xz() }).ToArray());
            
            mesh.RecalculateBounds();

            // Indices, Normales y Colores no deberian cambiar si el numero de vertices no cambia
            if (oldVertexCount == mesh.vertexCount) return;
            
            mesh.SetTriangles(mesh.vertices.Select((_, i) => i).ToArray(), 0);
                
            if (mesh.normals.Length > 0) mesh.SetNormal(mesh.normals[0]);
            if (mesh.colors.Length > 0) mesh.SetColor(mesh.colors[0]);
        }
        
        /// <summary>
        /// Actualiza la Mesh con un Polygon sin tener que recrearlo
        /// </summary>
        public static void SetPolygon(this Mesh mesh, Polygon polygon)
        {
            int oldVertexCount = mesh.vertexCount;
            Triangle[] tris = polygon.Triangulate();
            mesh.SetVertices(tris.SelectMany(t => new[] { t.v3.ToV3xz(), t.v2.ToV3xz(), t.v1.ToV3xz() }).ToArray());
            
            mesh.RecalculateBounds();

            // Indices, Normales y Colores no deberian cambiar si el numero de vertices no cambia
            if (oldVertexCount == mesh.vertexCount) return;
            
            mesh.SetTriangles(mesh.vertices.Select((_, i) => i).ToArray(), 0);
                
            if (mesh.normals.Length > 0) mesh.SetNormal(mesh.normals[0]);
            if (mesh.colors.Length > 0) mesh.SetColor(mesh.colors[0]);
        }

        public static void SetColor(this Mesh mesh, Color color)
        {
            var colors = new Color[mesh.vertexCount];
            Array.Fill(colors, color);
            mesh.SetColors(colors);
        }
        
        public static void SetNormal(this Mesh mesh, Vector3 normal, bool XZplane = true)
        {
            var normals = new Vector3[mesh.vertexCount];
            Array.Fill(normals, normal);
            mesh.SetNormals(normals);
        }

        #endregion
        
        
        #region MESH RENDERER INSTANTIATION

        public static void InstantiateMeshRenderer(
            out MeshRenderer mr,
            out MeshFilter mf,
            Mesh mesh = default,
            Transform parent = null,
            string name = "Mesh"
        )
        {
            // LINE RENDERER
            var mObj = new GameObject($"Mesh{(name == "" ? "" : " - " + name)}");
            mObj.transform.parent = parent;
            mObj.transform.localPosition = Vector3.zero;
            mObj.transform.localRotation = Quaternion.identity;
            mObj.transform.localScale = Vector3.one;

            mr = mObj.AddComponent<MeshRenderer>();
            mf = mObj.AddComponent<MeshFilter>();

            // Find Default Material
            mr.sharedMaterial = Resources.Load<Material>("Materials/Geometry Lit");

            mf.sharedMesh = mesh;
        }

        // PLANE
        public static void InstantiateMeshPlane(
            out MeshRenderer mr,
            out MeshFilter mf,
            Transform parent = null,
            string name = "Plane",
            float resolution = 10,
            Vector2 size = default
        ) => InstantiateMeshRenderer(out mr, out mf, GenerateMeshPlane(resolution, size), parent, name);

        
        // SINGLE TRIANGLE
        public static void InstantiateMesh(
            this Triangle triangle,
            out MeshRenderer mr,
            out MeshFilter mf,
            Transform parent = null,
            string name = "Triangle Mesh",
            Color color = default,
            bool XZplane = true
        ) => InstantiateMeshRenderer(out mr, out mf, triangle.CreateMesh(color, XZplane), parent, name);
        
        // TRIANGLES
        public static void InstantiateMesh(
            this Triangle[] triangles,
            out MeshRenderer mr,
            out MeshFilter mf,
            Transform parent = null,
            string name = "Triangle Mesh",
            Color[] colors = default,
            bool XZplane = true
        ) => InstantiateMeshRenderer(out mr, out mf, triangles.CreateMesh(colors, XZplane), parent, name);
        
        // POLYGON
        public static void InstantiateMesh(
            this Polygon polygon,
            out MeshRenderer mr,
            out MeshFilter mf,
            Transform parent = null,
            string name = "Polygon Mesh",
            Color color = default,
            bool XZplane = true
        ) => InstantiateMeshRenderer(out mr, out mf, polygon.CreateMesh(color, XZplane), parent, name);
        
        #endregion
        

        #region MESH GENERATION

        public static Mesh GenerateMeshPlane(float resolution = 10, Vector2 size = default)
        {
            var meshCellSize = 1 / resolution;
            var sideCellCount = new Vector2Int(
                Mathf.CeilToInt(size.x / meshCellSize),
                Mathf.CeilToInt(size.y / meshCellSize)
            );
            var sideVerticesCount = sideCellCount + Vector2Int.one;

            var vertices = new Vector3[sideVerticesCount.x * sideVerticesCount.y];
            var triangles = new int[sideCellCount.x * sideCellCount.y * 6];

            for (var y = 0; y < sideVerticesCount.y; y++)
            for (var x = 0; x < sideVerticesCount.x; x++)
            {
                var vertex = new Vector3(x * meshCellSize, 0, y * meshCellSize);

                // Center Mesh
                vertex -= new Vector3(size.x, 0, size.y) / 2;

                var vertexIndex = x + y * sideVerticesCount.x;

                vertices[vertexIndex] = vertex;

                if (x >= sideCellCount.x || y >= sideCellCount.y) continue;

                // Triangles
                var triangleIndex = (x + y * sideCellCount.x) * 6;
                triangles[triangleIndex + 0] = vertexIndex + 0;
                triangles[triangleIndex + 1] = vertexIndex + sideVerticesCount.x + 0;
                triangles[triangleIndex + 2] = vertexIndex + sideVerticesCount.x + 1;

                triangles[triangleIndex + 3] = vertexIndex + 0;
                triangles[triangleIndex + 4] = vertexIndex + sideVerticesCount.x + 1;
                triangles[triangleIndex + 5] = vertexIndex + 1;
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles
            };

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();

            return mesh;
        }
        
        // TRIANGLES
        public static Mesh CreateMesh(this Triangle[] tris, Color[] colors = null, bool XZplane = true)
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

            mesh.SetColors(colors ?? Array.Empty<Color>());
            
            mesh.normals = mesh.vertices.Select(v => XZplane ? Vector3.up : Vector3.back).ToArray();
            mesh.bounds = tris
                .SelectMany(t => t.Vertices)
                .Select(p => XZplane ? p.ToV3xz() : p.ToV3xy())
                .ToArray()
                .GetBoundingBox();

            return mesh;
        }

        public static Mesh CreateMesh(this Triangle[] tris, Color color = default, bool XZplane = true) =>
            tris.CreateMesh(tris.SelectMany(_ => new Color[] { color, color, color }).ToArray(), XZplane);

        
        public static Mesh CreateMesh(this Triangle triangle, Color color = default, bool XZplane = true) => 
            CreateMesh(new [] {triangle}, new []{color}, XZplane);
        
        // POLYGON
        public static Mesh CreateMesh(this Polygon polygon, Color color = default, bool XZplane = true) => 
            CreateMesh(polygon.Triangulate(), color, XZplane);
        #endregion
    }
}
