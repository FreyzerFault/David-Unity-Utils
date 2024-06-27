using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using Geometry.Algorithms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace DavidUtils.Rendering
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class DelaunayRenderer : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public Color color = Color.white;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            
            _meshRenderer.material = SolidMaterial;
            
            // Line Material for Wire Mode
            lineMaterial = new Material(Shader.Find("Unlit/Color"));
            lineMaterial.color = color;

            if (delaunay != null && delaunay.triangles.NotNullOrEmpty()) UpdateDelaunay();
        }

        
        private void OnRenderObject()
        {
            if (renderMode == DelaunayRenderMode.Wire)
            {
                _meshRenderer.enabled = false;
                DrawWire();
            }
            else
            {
                _meshRenderer.enabled = true;
            }
        }
        

        #region DELAUNAY

        private Delaunay delaunay;

        public Delaunay Delaunay
        {
            get => delaunay;
            set
            {
                delaunay = value;
                UpdateDelaunay();
            }
        }

        public void UpdateDelaunay()
        {
            if (_meshFilter != null)
                _meshFilter.mesh = CreateMesh();
        }

        #endregion


        private Mesh CreateMesh()
        {
            Mesh mesh = delaunay.triangles.CreateMesh(Color.cyan.GetRainBowColors(delaunay.TriangleCount, 0.05f, 20));
            mesh.MarkDynamic();
            return mesh;
        }

        private void UpdateTri(int triIndex, Triangle tri) => 
            _meshFilter.mesh.SetVertices(
                tri.Vertices.ToV3().ToArray(),
                triIndex,
                triIndex + 2,
                MeshUpdateFlags.DontRecalculateBounds
            );

        public void Clear()
        {
            if (_meshFilter != null)
                _meshFilter.mesh?.Clear();
        }


        #region RENDER MODE
        
        private Material SolidMaterial => Resources.Load<Material>("Materials/Geometry Unlit");
        private Material lineMaterial; 
        
        private bool IsWire => renderMode == DelaunayRenderMode.Wire;
        [ConditionalField("IsWire")]
        public float thickness = .1f;
        private static readonly int ThicknessUniform = Shader.PropertyToID("Thickness");
		
        public enum DelaunayRenderMode { Mesh, Wire }
        [SerializeField] private DelaunayRenderMode renderMode;
        public DelaunayRenderMode RenderMode
        {
            get => renderMode;
            set
            {
                renderMode = value;
                _meshRenderer.material.SetFloat(ThicknessUniform, thickness);
            }
        }

        private void DrawWire()
        {
            Mesh mesh = _meshFilter?.mesh;
            if (mesh == null) return;

            lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);

            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                GL.Vertex(mesh.vertices[mesh.triangles[i]]);
                GL.Vertex(mesh.vertices[mesh.triangles[i + 1]]);
                GL.Vertex(mesh.vertices[mesh.triangles[i + 1]]);
                GL.Vertex(mesh.vertices[mesh.triangles[i + 2]]);
                GL.Vertex(mesh.vertices[mesh.triangles[i + 2]]);
                GL.Vertex(mesh.vertices[mesh.triangles[i]]);
            }

            GL.End();
            GL.PopMatrix();
        }

        #endregion
    }
}
