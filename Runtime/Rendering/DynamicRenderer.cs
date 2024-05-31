using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public abstract class DynamicRenderer<T> : MonoBehaviour
	{
		protected virtual Material Material => Resources.Load<Material>("Materials/Geometry Unlit");
		protected virtual string DefaultChildName => "Render Child";

		public bool Active
		{
			get => isActiveAndEnabled;
			set => gameObject.SetActive(value);
		}

		public abstract void Instantiate(T polygons, string childName = null);
		public abstract void UpdateGeometry(T regions);

		public virtual void Clear()
		{
		}

		public virtual void ToggleVisibility(bool visible) => gameObject.SetActive(visible);

		#region COLOR

		[HideInInspector] public Color[] colors = Array.Empty<Color>();

		public Color initColorPalette = Color.cyan;
		[Min(-1)] public float colorPaletteStep = .1f;
		[Min(0)] public int colorPaletteRange = 20;

		public Color[] SetRainbowColors(int numColors, Color? initColor = null) =>
			colors = (initColor ?? initColorPalette)
				.GetRainBowColors(numColors, colorPaletteStep, colorPaletteRange)
				.ToArray();

		#endregion


		#region SHADOWS

		public void ToggleShadows(bool castShadows = true) =>
			gameObject.ToggleShadows(castShadows);

		#endregion
	}
}
