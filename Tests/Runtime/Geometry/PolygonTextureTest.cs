using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Rendering;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace DavidUtils.Tests.Runtime.Geometry
{
    /// <summary>
    ///     Texture Generation Test
    ///     Using 2 Algorithms:
    ///     - Raycast (FASTER for Bigger Textures)
    ///     - Scanline (FASTER for Smaller Textures)
    /// </summary>
    public class PolygonTextureTest : TestRunner
    {
        public int seed = 1234;
        public int numVertices;
        public Vector2Int resolution;
        public bool transparent = true;
        
        public Image img;
        public Texture2D texture;

        public PolygonRenderer polyRenderer;
        private Polygon _polygon = new();

        protected override void Awake()
        {
            base.Awake();
            
            Random.InitState(seed);
        }

        protected override void InitializeTests()
        {
            onStartAllTests += RandomizePolygon;
            
            // AddTest(GenerateTextureTest, "GenerateTexture With SCANLINES");
            // AddTest(GenerateTextureUsingRaycastTest, "GenerateTexture With RAYCAST");
            AddTest(GenerateTextureWithPolygonWithVerticesRepeatedTest, 
                new TestInfo("Now with Repeated Vertices", 
                    () => _polygon.intersectionsByScanline.Count > 0,
                    () =>
                    {
                        KeyValuePair<float, Vector2[]>[] oddIntersections = _polygon.intersectionsByScanline
                            .Where(pair => pair.Value.Length % 2 == 1).ToArray();
                        
                        KeyValuePair<float, Vector2[]>[] oddIntersectionsMoreThanOne = oddIntersections
                            .Where(pair => pair.Value.Length > 1).ToArray();
                        
                        string oddIntersectionsStr =
                            oddIntersectionsMoreThanOne.IsNullOrEmpty()
                                ? $"No Odd Intersections (I >= 3) ✔\n" 
                                : $"ODD INTERSECTIONS (I >= 3)\n" +
                                string.Join("\n",
                                    oddIntersections
                                    .Select(pair => $"<color=red>H {pair.Key:f2} ({pair.Value.Length} inters.):</color> " +
                                                    $"{string.Join(", ", pair.Value)}"));
                        string intersectionsByScanline =
                            $"INTERSECTIONS\n" +
                            string.Join('\n',
                            _polygon.intersectionsByScanline.Select(pair =>
                                $"{(pair.Value.Length % 2 == 0 ? "<color=green>" : "<color=red>")}" +
                                $"H {pair.Key:f2} ({pair.Value.Length} inters.):</color> " +
                                $"{string.Join(", ", pair.Value)}"));
                        
                        if (oddIntersectionsMoreThanOne.IsNullOrEmpty())
                            Debug.Log(oddIntersectionsStr + "\n\n" + intersectionsByScanline);
                        else
                            Debug.LogWarning("ODD Intersections ( >= 3 ) FOUND:\n" +
                                oddIntersectionsStr + "\n\n" + intersectionsByScanline);
                    }));
            
            onEndAllTests += RandomizeSeed;
        }
        
        private void RandomizeSeed()
        {
            seed = Random.Range(0, int.MaxValue);
            Random.InitState(seed);
        }

        private void RandomizePolygon() => _polygon.SetRandomVertices(numVertices);

        
        private IEnumerator GenerateTextureTest()
        {
            texture = GenerateTexture();
            RenderPolygon();
            ShowOnImg();
            yield return null;
        }
        
        private IEnumerator GenerateTextureUsingRaycastTest()
        {
            texture = GenerateTextureUsingRaycast();
            RenderPolygon();
            ShowOnImg();
            yield return null;
        }
        
        // Genera una Textura rasterizando un poligono que tiene vertices repetidos
        private IEnumerator GenerateTextureWithPolygonWithVerticesRepeatedTest()
        {
            RandomizePolygon();
            
            // 2 Duplicated
            _polygon.Vertices[1] = _polygon.Vertices[0];
            
            // 3 Duplicated
            if (_polygon.VertexCount > 4)
                _polygon.Vertices[4] = _polygon.Vertices[3] = _polygon.Vertices[2];
            
            // No adyacent duplicated
            if (_polygon.VertexCount > 8)
                _polygon.Vertices[5] = _polygon.Vertices[8];
            
            // Return Vertex
            if (_polygon.VertexCount > 10)
                _polygon.Vertices[8] = _polygon.Vertices[10];
            
            // CLOSE as fuck Vertices
            if (_polygon.VertexCount > 12)
                _polygon.Vertices[11] = _polygon.Vertices[12] + Vector2.up * 0.00001f;
            
            // Colinear vertices
            if (_polygon.VertexCount > 14)
                _polygon.Vertices[13] = _polygon.Vertices[14] + Vector2.right * 0.00001f;

            
            texture = GenerateTexture();
            RenderPolygon();
            ShowOnImg();
            yield return null;
        }

        private Texture2D GenerateTexture() =>
            _polygon.ToTexture_ScanlineRaster(resolution, Color.blue, Color.grey, transparent: transparent, debugInfo: false);
        
        private Texture2D GenerateTextureUsingRaycast() =>
            _polygon.ToTexture_ContainsRaycastPerPixel(resolution, Color.blue, Color.grey, transparent: transparent);
        
        
        

        private void RenderPolygon() => polyRenderer.Polygon = _polygon;
        private void ShowOnImg() => img.sprite = CreateSprite();
        
        private Sprite CreateSprite()
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
            return sprite;
        }
    }
}
