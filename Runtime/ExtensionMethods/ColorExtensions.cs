using System.Linq;
using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
	public static class ColorExtensions
	{
		// Rota en la rueda HSV el Hue (Tonalidad) del color
		public static Color RotateHue(this Color color, float hueRotation)
		{
			// Convertir el color RGB a HSV
			Color.RGBToHSV(color, out float H, out float S, out float V);

			// Rotar el Hue
			H = (H + hueRotation) % 1;

			// Convertir el color HSV de vuelta a RGB
			return Color.HSVToRGB(H, S, V);
		}


		public static Color Darken(this Color color, float darkenAmount)
		{
			// Convertir el color RGB a HSV
			Color.RGBToHSV(color, out float H, out float S, out float V);

			// Oscurecer el color
			V = Mathf.Max(0, V - darkenAmount);

			// Convertir el color HSV de vuelta a RGB
			return Color.HSVToRGB(H, S, V);
		}

		public static Color Lighten(this Color color, float lightenAmount)
		{
			// Convertir el color RGB a HSV
			Color.RGBToHSV(color, out float H, out float S, out float V);

			// Oscurecer el color
			V = Mathf.Max(0, V + lightenAmount);

			// Convertir el color HSV de vuelta a RGB
			return Color.HSVToRGB(H, S, V);
		}


		// Color Random con la mayor saturacion
		public static Color RandomColorSaturated() => Random.ColorHSV(0, 1, 1, 1, 1, 1);


		// COLORES Dinamicos

		public static Color[] GetRainBowColors(this Color initColor, int count, float step = 0.1f) =>
			new Color[count].Select((value, index) => initColor.RotateHue(step * index)).ToArray();

		public static Color SetAlpha(this Color color, float newAlpha) => new(color.r, color.g, color.b, newAlpha);


		public static Gradient ToGradient(this Color[] colors)
		{
			var gradient = new Gradient();
			GradientColorKey[] colorKeys =
				colors.Select((c, i) => new GradientColorKey(c, (float)i / colors.Length)).ToArray();
			GradientAlphaKey[] alphaKeys =
				colors.Select((c, i) => new GradientAlphaKey(1f, (float)i / colors.Length)).ToArray();
			gradient.SetKeys(colorKeys, alphaKeys);
			return gradient;
		}
	}
}
