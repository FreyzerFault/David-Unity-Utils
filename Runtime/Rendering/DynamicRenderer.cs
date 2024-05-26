using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public abstract class DynamicRenderer<T>
	{
		protected GameObject renderObj;
		public Transform RenderParent => renderObj.transform;

		public bool active = true;

		public bool IsInitialized => renderObj != null;

		protected virtual string DefaultName => "Render Obj";
		protected virtual string DefaultChildName => "Render Child";
		protected virtual Material Material => Resources.Load<Material>("Materials/Geometry Unlit");

		public virtual void Initialize(Transform parent, string name = null) =>
			renderObj = new GameObject(name ?? DefaultName)
			{
				transform =
				{
					parent = parent,
					localPosition = Vector3.zero,
					localRotation = Quaternion.identity,
					localScale = Vector3.one
				}
			};

		public abstract void Instantiate(T points, string childName = null);
		public abstract void Update(T points);

		public abstract void Clear();

		public virtual void UpdateVisibility() => renderObj.SetActive(active);

		#region COLOR

		[HideInInspector] public Color[] colors = Array.Empty<Color>();
		public Color DefaultColor => Color.cyan;

		public Color initColorPalette = Color.cyan;
		[Min(-1)] public float colorPaletteStep = .1f;
		[Min(0)] public int colorPaletteRange = 20;

		public Color[] SetRainbowColors(int numColors, Color? initColor = null) =>
			colors = (initColor ?? initColorPalette).Darken(.3f).GetRainBowColors(numColors, colorPaletteStep, colorPaletteRange).ToArray();

		#endregion
	}
}
