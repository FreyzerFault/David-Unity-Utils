using System.Collections;
using DavidUtils.DevTools.Testing;
using DavidUtils.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace DavidUtils.Geometry.Testing
{
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
            
            AddTest(GenerateTexture, "GenerateTexture");
            
            // TODO: using Breakpoints es MUY MUY MUY LENTO
            AddTest(GenerateTextureUsingScanlineBreakpoints, "GenerateTexture With RAYCAST");
            
            onEndAllTests += RandomizeSeed;
        }
        
        private void RandomizeSeed()
        {
            seed = Random.Range(0, int.MaxValue);
            Random.InitState(seed);
        }

        private void RandomizePolygon() => _polygon.SetRandomVertices(numVertices);

        
        private IEnumerator GenerateTexture()
        {
            texture = _polygon.ToTexture(
                resolution, Color.blue, 
                transparent ? Color.clear : Color.grey,
                transparent: transparent, 
                scalinebrakpointsTest: false);
            RenderPolygon();
            ShowOnImg();
            yield return null;
        }
        
        private IEnumerator GenerateTextureUsingScanlineBreakpoints()
        {
            texture = _polygon.ToTexture(
                resolution, Color.blue, 
                transparent ? Color.clear : Color.grey,
                transparent: transparent);
            RenderPolygon();
            ShowOnImg();
            yield return null;
        } 

        private void RenderPolygon() => polyRenderer.Polygon = _polygon;
        private void ShowOnImg() => img.sprite = CreateSprite();
        
        private Sprite CreateSprite()
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
            return sprite;
        }
    }
}
