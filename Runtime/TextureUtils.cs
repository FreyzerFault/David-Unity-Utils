using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace DavidUtils
{
	public static class TextureUtils
	{
		public static Texture2D ColorDataToTexture2D(IEnumerable<Color> colorData, int width, int height)
		{
			Texture2D texture = new(width, height);
			texture.SetPixels(colorData.ToArray());
			texture.Apply();
			return texture;
		}

		public static Texture2D ColorDataToTexture2D(IEnumerable<Color32> colorData, int width, int height)
		{
			Texture2D texture = new(width, height);
			texture.SetPixels32(colorData.ToArray());
			texture.Apply();
			return texture;
		}

		public static Texture2D ColorDataToTexture2D(NativeArray<Color32> colorData, int width, int height)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
			texture.GetRawTextureData<Color32>().CopyFrom(colorData);
			texture.Apply();
			return texture;
		}

		public static Color32 ToColor32(this Color color) => color;
		public static Color ToColor(this Color32 color) => color;


		#region TEXTURE GENERATION

		public static Texture2D GeneratePointTexture(int size, Color color) => GenerateCircleTexture(size, color);

		public static Texture2D GenerateCircleTexture(int size, Color color, float radius = -1, float thickness = -1)
		{
			radius = radius < 0 ? size / 4f : radius;
			radius = thickness < Mathf.Epsilon ? thickness / 4f : radius;

			var texture = new Texture2D(size, size);
			var pixels = new Color[size * size];

			float centerX = size / 2f;
			float centerY = size / 2f;

			for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				float dx = centerX - x;
				float dy = centerY - y;
				float distance = Mathf.Sqrt(dx * dx + dy * dy);

				if (distance > radius - thickness && distance < radius + thickness)
					pixels[y * size + x] = color;
				else
					pixels[y * size + x] = Color.clear;
			}

			texture.SetPixels(pixels);
			texture.Apply();

			return texture;
		}

		#endregion


		#region TEXTURE STATIC RESOURCES

		// TODO Me da que no va a coger el Circulo como una Textura porque es un Prefab
		public static Texture2D GetCircle() => Resources.Load<Texture2D>("Prefabs/Geometry/Circle");
		public static Texture2D GetCircumference() => Resources.Load<Texture2D>("Prefabs/Geometry/Circumference");
		public static Texture2D GetTriangle() => Resources.Load<Texture2D>("Prefabs/Geometry/Triangle");

		#endregion
	}
}
