using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Algorithms;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace DavidUtils.Rendering
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(LineRenderer))]
    public class DelaunayRenderer : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private readonly List<LineRenderer> _lineRenderers = new();

        public Color color = Color.white;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
        }

        private void OnEnable()
        {
            SetCommonProperties();
            if (delaunay is { TriangleCount: > 0 })
                UpdateDelaunay();
        }


        public void Clear()
        {
            if (_meshFilter != null)
                _meshFilter.mesh?.Clear();
            if (_lineRenderers.NotNullOrEmpty())
            {
                _lineRenderers.ForEach(UnityUtils.DestroySafe);
                _lineRenderers.Clear();
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
            if (delaunay == null) return;
            
            if (_meshFilter != null)
                GenerateMesh(Color.cyan.GetRainBowColors(delaunay.TriangleCount, 0.05f, 20));
            if (_lineRenderers != null)
                GenerateLinePoints();
        }
        
        private void UpdateTri(int triIndex, Triangle tri)
        {
            _meshFilter.mesh.SetVertices(
                tri.Vertices.ToV3().ToArray(),
                triIndex,
                triIndex + 2,
                MeshUpdateFlags.DontRecalculateBounds
            );
            ApplyTriToLine(_lineRenderers[triIndex], tri);
        }

        #endregion
        
        
        #region COMMON PROPERTIES
        
        private Material SolidMaterial => Resources.Load<Material>("Materials/Geometry Unlit");
        
        private bool IsWire => renderMode == DelaunayRenderMode.Wire;
        
        [ConditionalField("IsWire")] [SerializeField] [Range(0.1f, 1)]
        private float _thickness = .1f;
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                UpdateThickness();
            }
        }
        private void UpdateThickness() => _lineRenderers.ForEach(lr => 
            lr.endWidth = _thickness * 0.01f * transform.lossyScale.x);
		
        public enum DelaunayRenderMode { Mesh, Wire, OutlineMesh }
        [SerializeField] private DelaunayRenderMode renderMode;
        public DelaunayRenderMode RenderMode
        {
            get => renderMode;
            set
            {
                renderMode = value;
                UpdateRenderMode();
            }
        }
        private void UpdateRenderMode()
        {
            _meshRenderer.enabled = renderMode is DelaunayRenderMode.Mesh or DelaunayRenderMode.OutlineMesh;
            _lineRenderers.ForEach(lr => lr.enabled = renderMode is DelaunayRenderMode.Wire or DelaunayRenderMode.OutlineMesh);
        }

        private void SetMaterials()
        {
            _meshRenderer.material = SolidMaterial;
            _lineRenderers.ForEach(lr => lr.material = Resources.Load<Material>("UI/Materials/Line Material"));
        }

        private void SetCommonProperties()
        {
            SetMaterials();
            UpdateRenderMode();
            UpdateThickness();
        }

        #endregion

        
        #region MESH

        private void GenerateMesh(Color[] colors)
        {
            _meshFilter.mesh = delaunay.triangles.CreateMesh(colors);
            _meshFilter.mesh.MarkDynamic();
        }

        #endregion


        #region LINE

        private void GenerateLinePoints()
        {
            delaunay.triangles.ForEach((tri, i) =>
            {
                if (i == _lineRenderers.Count) 
                    _lineRenderers.Add(GenLine(color));
                    
                ApplyTriToLine(_lineRenderers[i], tri);
            });
            
            if (_lineRenderers.Count <= delaunay.TriangleCount) return;
            
            // Elimina los sobrantes
            int triCount = delaunay.TriangleCount, lineCount = _lineRenderers.Count;
            _lineRenderers.Skip(triCount).ForEach(UnityUtils.DestroySafe);
            _lineRenderers.RemoveRange(triCount, lineCount - triCount);
        }

        private LineRenderer GenLine(Color lineColor)
        {
            LineRenderer lr = UnityUtils.InstantiateObject<LineRenderer>(transform, $"Line {_lineRenderers.Count}");
            lr.useWorldSpace = false;
            lr.widthMultiplier = _thickness * 0.01f * transform.lossyScale.x;
            lr.startColor = lr.endColor = lineColor;
            lr.material = Resources.Load<Material>("UI/Materials/Line Material");
            // lr.numCapVertices = lr.numCornerVertices = 3;
            return lr;
        }

        private void ApplyTriToLine(LineRenderer lr, Triangle tri) => lr.SetPoints(tri.Vertices.Append(tri.v1).Select(v => v.ToV3().WithZ(-0.1f)));
        
        #endregion
    }
}
