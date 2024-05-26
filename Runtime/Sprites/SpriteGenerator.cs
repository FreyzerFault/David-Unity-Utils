using UnityEngine;

namespace DavidUtils.Sprites
{
    public static class SpriteGenerator
    {
        private static Rect DefaultRect => new Rect(0, 0, 1, 1);
        private static Rect CenteredRect => new Rect(-.5f, -.5f, 1, 1);
        
        private static Sprite WhiteSprite => Sprite.Create(Texture2D.whiteTexture, DefaultRect, Vector2.zero);
        private static Sprite CenteredWhiteSprite => Sprite.Create(Texture2D.whiteTexture, DefaultRect, new Vector2(.5f, .5f));
        
        /// <summary>
        /// Instancia un Sprite Renderer con el Sprite dado
        /// </summary>
        public static SpriteRenderer InstantiateSpriteRenderer(Sprite sprite, Transform parent, string name = "Sprite Renderer")
        {
            SpriteRenderer sr = UnityUtils.InstantiateEmptyObject(parent, name).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            return sr;
        }
        
        /// <summary>
        /// Instancia un Sprite Renderer con una Textura dada
        /// </summary>
        public static SpriteRenderer InstantiateSpriteRenderer(Texture2D texture, Transform parent, string name = "Sprite Renderer", bool centered = true) => 
            InstantiateSpriteRenderer(Sprite.Create(texture, centered ? CenteredRect : DefaultRect, centered ? new Vector2(.5f, .5f) : Vector2.zero), parent, name);
        
        /// <summary>
        /// Instancia un Sprite Renderer por defecto (1x1 BLANCO)
        /// </summary>
        public static SpriteRenderer InstantiateSpriteRenderer(Transform parent, string name = "Sprite Renderer", Color? tintColor = null, bool centered = true) => 
            InstantiateSpriteRenderer(centered ? CenteredWhiteSprite : WhiteSprite, parent, name);


        public static Sprite GenerateSprite(Color color, bool centered = true)
        {
            Texture2D tex = Texture2D.whiteTexture;
            tex.SetPixel(0, 0, color);
            return Sprite.Create(tex, DefaultRect, centered ? new Vector2(.5f, .5f) : Vector2.zero);
        }
        
        
        /// <summary>
        /// Genera un Sprite por Defecto, en el Rect dado
        /// </summary>
        public static Sprite GenerateSprite(Rect rect, Color color, bool centered = true)
        {
            Texture2D tex = Texture2D.whiteTexture;
            tex.SetPixel(0, 0, color);
            tex.Reinitialize((int)rect.width, (int)rect.height);
            return Sprite.Create(tex, rect, centered ? new Vector2(.5f, .5f) : Vector2.zero);
        }
        
    }
}
