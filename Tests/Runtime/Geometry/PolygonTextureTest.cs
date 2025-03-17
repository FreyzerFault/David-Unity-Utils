using System.Collections;
using DavidUtils.DevTools.Testing;
using DavidUtils.Geometry;
using DavidUtils.Rendering;
using UnityEngine;
using UnityEngine.UI;

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
            
            AddTest(GenerateTextureTest, "GenerateTexture With SCANLINES");
            AddTest(GenerateTextureUsingRaycastTest, "GenerateTexture With RAYCAST");
            
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

        private Texture2D GenerateTexture() =>
            _polygon.ToTexture(resolution, Color.blue, Color.grey, transparent: transparent);
        
        private Texture2D GenerateTextureUsingRaycast() =>
            _polygon.ToTextureContainsRaycast(resolution, Color.blue, Color.grey, transparent: transparent);
        
        

        private void RenderPolygon() => polyRenderer.Polygon = _polygon;
        private void ShowOnImg() => img.sprite = CreateSprite();
        
        private Sprite CreateSprite()
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
            return sprite;
        }
    }
}
