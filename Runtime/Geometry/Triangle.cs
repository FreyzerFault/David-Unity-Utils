using System;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
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

        public Delaunay.Border[] Borders => BorderEdges.Select(e => new Delaunay.Border(this, e)).ToArray();

        public Triangle(
            Vector2 v1, Vector2 v2, Vector2 v3, Triangle t1 = null, Triangle t2 = null, Triangle t3 = null
        )
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;

            SetAllNeightbours(new[] { t1, t2, t3 });

            e1 = new Edge(v1, v2);
            e2 = new Edge(v2, v3);
            e3 = new Edge(v3, v1);
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
        
        public static void Instantiate(
            Triangle[] triangles, Transform parent, out MeshRenderer mr, out MeshFilter mf, Color color = default, string name = "Mesh"
        ) => ObjectGenerator.InstantiateMeshRenderer(CreateMesh(triangles, color), parent, out mr, out mf, name);

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
        
        public LineRenderer InstantiateLineRenderer(Transform parent, Color color = default, float thickness = Polyline.DEFAULT_THICKNESS, int smoothness = Polyline.DEFAULT_SMOOTHNESS, string name = "Triangle Line")
        {
            Polyline line = new Polyline(Vertices, new[] { color }, thickness, smoothness, true, true);
            return line.Instantiate(parent, name);
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
    
    
}
