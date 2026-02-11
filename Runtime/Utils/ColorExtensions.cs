using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DavidUtils.Utils
{
	/// <summary>
	///     Represents list of supported by Unity Console color names
	/// </summary>
	public enum DebugColor
	{
		Aqua, Black, Blue, Brown, Cyan, Darkblue, Fuchsia, Green, Grey, Lightblue,
		Lime, Magenta, Maroon, Navy, Olive, Purple, Red, Silver, Teal, White, Yellow
	}

	public static class ColorExtensions
	{
		#region GENERATE COLORS

		/// <summary>
		///     Color Random con la mayor saturación
		/// </summary>
		public static Color RandomColorSaturated() =>
			Random.ColorHSV(0, 1, 1, 1, 1, 1);

		/// <summary>
		///     Array de Colores rotados en la rueda HSV como en un Arcoiris
		/// </summary>
		/// <param name="initColor">Empieza aqui</param>
		/// <param name="count">Numero de Colores</param>
		/// <param name="step">Salto de Tono entre Color y Color</param>
		/// <param name="range">Rango de colores generados (util para generar una paleta de colores cercanos)</param>
		/// <returns></returns>
		public static Color[] GetRainBowColors(this Color initColor, int count, float step = 0.1f, int range = 0) =>
			new Color[count].Select((_, index) => initColor.RotateHue(step * (range == 0 ? index : index % range)))
				.ToArray();

		#endregion


		#region MODIFICATIONS

		#region HUE (HSV)

		// Rota en la rueda HSV el Hue (Tonalidad) del color
		public static Color RotateHue(this Color color, float hueRotation)
		{
			// RGB -> HSV -> RGB
			Color.RGBToHSV(color, out float h, out float s, out float v);
			h = (h + hueRotation) % 1;
			return Color.HSVToRGB(h, s, v);
		}

		public static Color Invert(this Color color) =>
			color == Color.white ? Color.black :
			color == Color.black ? Color.white :
			color.RotateHue(.5f);

		#endregion

		#region SATURATION (HSV)

		private const float DefaultSaturationOffset = 0.0625f;

		public static Color Desaturate(this Color color, float? saturationAmount = null) =>
			color.OffsetSaturation(-saturationAmount ?? DefaultSaturationOffset);

		public static Color Saturate(this Color color, float? saturationAmount = null) =>
			color.OffsetSaturation(saturationAmount ?? DefaultSaturationOffset);

		private static Color OffsetSaturation(this Color color, float offset)
		{
			// RGB -> HSV -> RGB
			Color.RGBToHSV(color, out float h, out float s, out float v);
			return Color.HSVToRGB(h, Mathf.Clamp01(s + offset), v);
		}

		#endregion
		
		#region VALUE (HSV)

		private const float DefaultValueOffset = 0.0625f;

		public static Color Darken(this Color color, float? saturationAmount = null) =>
			color.OffsetValue(-saturationAmount ?? DefaultValueOffset);

		public static Color Lighten(this Color color, float? lightenAmount = null) =>
			color.OffsetValue(lightenAmount ?? DefaultValueOffset);

		private static Color OffsetValue(this Color color, float offset)
		{
			// RGB -> HSV -> RGB
			Color.RGBToHSV(color, out float h, out float s, out float v);
			return Color.HSVToRGB(h, s, Mathf.Clamp01(v + offset));
		}

		#endregion
		

		#region ALPHA

		/// <summary>
		///     Returns new Color with Alpha set to a
		/// </summary>
		public static Color WithAlpha(this Color color, float a) => new(color.r, color.g, color.b, a);

		/// <summary>
		///     Set Alpha of Image.Color
		/// </summary>
		public static void SetAlpha(this Graphic graphic, float a) => graphic.color = graphic.color.WithAlpha(a);

		/// <summary>
		///     Set Alpha of Renderer.Color
		/// </summary>
		public static void SetAlpha(this SpriteRenderer renderer, float a) =>
			renderer.color = renderer.color.WithAlpha(a);

		#endregion

		#endregion


		#region CONVERSIONS

		public static Gradient ToGradient(this IEnumerable<Color> colors)
		{
			Gradient gradient = new Gradient();
			IEnumerable<Color> colorsEnumerable = colors as Color[] ?? colors.ToArray();
			int numColors = colorsEnumerable.Count();
			GradientColorKey[] colorKeys =
				colorsEnumerable.Select((c, i) => new GradientColorKey(c, (float)i / numColors)).ToArray();
			GradientAlphaKey[] alphaKeys =
				colorsEnumerable.Select((_, i) => new GradientAlphaKey(1f, (float)i / numColors)).ToArray();
			gradient.SetKeys(colorKeys, alphaKeys);
			return gradient;
		}

		#endregion


		#region COLOR FORMATS

		/// <summary>
		///     To string of "#b5ff4f" format
		/// </summary>
		public static string ToHex(this Color color) =>
			$"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}";

		/// <summary>
		///     Converts a HTML color string into UnityEngine.Color.
		///     See UnityEngine.ColorUtility.TryParseHtmlString for conversion conditions.
		/// </summary>
		public static Color ToUnityColor(this string source)
		{
			ColorUtility.TryParseHtmlString(source, out Color res);
			return res;
		}

		#endregion
	}
}
